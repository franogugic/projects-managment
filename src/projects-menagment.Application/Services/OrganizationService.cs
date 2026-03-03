using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using projects_menagment.Application.Dtos.Organizations;
using projects_menagment.Application.Exceptions;
using projects_menagment.Application.Interfaces.Communication;
using projects_menagment.Application.Interfaces.Organizations;
using projects_menagment.Application.Interfaces.Repositories;
using projects_menagment.Application.Interfaces.Services;
using projects_menagment.Domain.Entities;
using projects_menagment.Domain.Enums;

namespace projects_menagment.Application.Services;

public sealed class OrganizationService(
    IUserRepository userRepository,
    IPlanRepository planRepository,
    IOrganizationRepository organizationRepository,
    IOrganizationMemberRepository organizationMemberRepository,
    IOrganizationMemberInvitationRepository invitationRepository,
    IOrganizationInviteLinkBuilder inviteLinkBuilder,
    IEmailSender emailSender,
    ILogger<OrganizationService> logger) : IOrganizationService
{
    public async Task<CreateOrganizationResponseDto> CreateAsync(
        CreateOrganizationRequestDto request,
        Guid createdByUserId,
        CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Organization name is required.");
        }

        if (name.Length > 150)
        {
            throw new ValidationException("Organization name must not exceed 150 characters.");
        }

        if (request.PlanId == Guid.Empty)
        {
            throw new ValidationException("Plan id is required.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new ValidationException("Created by user id is required.");
        }

        var user = await userRepository.GetByIdAsync(createdByUserId, cancellationToken);
        if (user is null)
        {
            logger.LogWarning("Organization creation failed because creator user {UserId} was not found", createdByUserId);
            throw new NotFoundException("Creator user was not found.");
        }

        if (!user.IsActive)
        {
            logger.LogWarning("Organization creation denied because user {UserId} is inactive", user.Id);
            throw new ForbiddenException("User account is inactive.");
        }

        var plan = await planRepository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan is null)
        {
            logger.LogWarning("Organization creation failed because plan {PlanId} was not found", request.PlanId);
            throw new NotFoundException("Plan was not found.");
        }

        if (!plan.IsActive)
        {
            logger.LogWarning("Organization creation denied because plan {PlanId} is inactive", plan.Id);
            throw new ForbiddenException("Selected plan is inactive.");
        }

        var organization = Organization.Create(name, request.PlanId, createdByUserId);
        var ownerMember = OrganizationMember.Create(
            organization.Id,
            createdByUserId,
            OrganizationMemberRole.Owner);

        await organizationRepository.AddWithOwnerAsync(organization, ownerMember, cancellationToken);

        logger.LogInformation(
            "Organization {OrganizationId} created by user {UserId} with plan {PlanId}; owner membership created",
            organization.Id,
            organization.CreatedByUserId,
            organization.PlanId);

        return new CreateOrganizationResponseDto(
            organization.Id,
            organization.Name,
            organization.PlanId,
            organization.CreatedByUserId,
            organization.CreatedAt);
    }

    public async Task<IReadOnlyCollection<UserOrganizationDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ValidationException("User id is required.");
        }

        var organizations = await organizationRepository.GetByUserIdAsync(userId, cancellationToken);
        logger.LogInformation(
            "Fetched {Count} organizations for user {UserId}",
            organizations.Count,
            userId);

        return organizations;
    }

    public async Task<InviteOrganizationMemberResponseDto> InviteMemberAsync(
        InviteOrganizationMemberRequestDto request,
        Guid invitedByUserId,
        CancellationToken cancellationToken)
    {
        if (request.OrganizationId == Guid.Empty)
        {
            throw new ValidationException("Organization id is required.");
        }

        if (invitedByUserId == Guid.Empty)
        {
            throw new ValidationException("Invited by user id is required.");
        }

        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email is required.");
        }

        var organization = await organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);
        if (organization is null)
        {
            throw new NotFoundException("Organization was not found.");
        }

        var inviterRole = await organizationMemberRepository.GetUserRoleInOrganizationAsync(
            request.OrganizationId,
            invitedByUserId,
            cancellationToken);

        if (inviterRole is not OrganizationMemberRole.Owner and not OrganizationMemberRole.Menager)
        {
            throw new ForbiddenException("Only OWNER or MENAGER can invite members.");
        }

        var user = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("User with provided email was not found.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("Target user account is inactive.");
        }

        var alreadyMember = await organizationMemberRepository.ExistsAsync(request.OrganizationId, user.Id, cancellationToken);
        if (alreadyMember)
        {
            throw new ConflictException("User is already a member of this organization.");
        }

        var role = ParseRequestedRole(request.Role);
        var token = GenerateInvitationToken();
        var expiresAt = DateTime.UtcNow.AddDays(3);

        var invitation = OrganizationMemberInvitation.Create(
            request.OrganizationId,
            email,
            role,
            invitedByUserId,
            token,
            expiresAt);

        await invitationRepository.AddAsync(invitation, cancellationToken);

        var inviteLink = inviteLinkBuilder.BuildInviteLink(token);
        await emailSender.SendOrganizationInviteAsync(email, organization.Name, inviteLink, cancellationToken);

        logger.LogInformation(
            "Member invitation {InvitationId} created for organization {OrganizationId}, email {Email}, role {Role}",
            invitation.Id,
            request.OrganizationId,
            email,
            role);

        return new InviteOrganizationMemberResponseDto(
            invitation.Id,
            email,
            role.ToString().ToUpperInvariant(),
            expiresAt,
            inviteLink);
    }

    public async Task<AcceptOrganizationInvitationResponseDto> AcceptInvitationAsync(
        AcceptOrganizationInvitationRequestDto request,
        CancellationToken cancellationToken)
    {
        var token = request.Token?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ValidationException("Invitation token is required.");
        }

        var invitation = await invitationRepository.GetByTokenAsync(token, cancellationToken);
        if (invitation is null)
        {
            throw new NotFoundException("Invitation was not found.");
        }

        var organization = await organizationRepository.GetByIdAsync(invitation.OrganizationId, cancellationToken);
        if (organization is null)
        {
            throw new NotFoundException("Organization was not found.");
        }

        if (invitation.IsAccepted)
        {
            throw new ConflictException("Invitation has already been accepted.");
        }

        if (invitation.IsExpired(DateTime.UtcNow))
        {
            throw new ForbiddenException("Invitation has expired.");
        }

        var user = await userRepository.GetByEmailAsync(invitation.Email, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("User with invitation email was not found.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("User account is inactive.");
        }

        var exists = await organizationMemberRepository.ExistsAsync(invitation.OrganizationId, user.Id, cancellationToken);
        if (exists)
        {
            invitation.MarkAccepted(DateTime.UtcNow);
            await invitationRepository.UpdateAsync(invitation, cancellationToken);
            throw new ConflictException("User is already a member of this organization.");
        }

        var member = OrganizationMember.Create(invitation.OrganizationId, user.Id, invitation.Role);
        await organizationMemberRepository.AddAsync(member, cancellationToken);

        invitation.MarkAccepted(DateTime.UtcNow);
        await invitationRepository.UpdateAsync(invitation, cancellationToken);

        return new AcceptOrganizationInvitationResponseDto(
            invitation.OrganizationId,
            organization.Name,
            user.Id,
            invitation.Role.ToString().ToUpperInvariant(),
            "Invitation accepted successfully.");
    }

    public async Task<OrganizationInvitationPreviewDto> GetInvitationPreviewAsync(string token, CancellationToken cancellationToken)
    {
        var invitationToken = token?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(invitationToken))
        {
            throw new ValidationException("Invitation token is required.");
        }

        var invitation = await invitationRepository.GetByTokenAsync(invitationToken, cancellationToken);
        if (invitation is null)
        {
            throw new NotFoundException("Invitation was not found.");
        }

        var organization = await organizationRepository.GetByIdAsync(invitation.OrganizationId, cancellationToken);
        if (organization is null)
        {
            throw new NotFoundException("Organization was not found.");
        }

        return new OrganizationInvitationPreviewDto(
            organization.Id,
            organization.Name,
            invitation.Email,
            invitation.Role.ToString().ToUpperInvariant(),
            invitation.ExpiresAt,
            invitation.IsAccepted,
            invitation.IsExpired(DateTime.UtcNow));
    }

    public async Task<IReadOnlyCollection<OrganizationMemberDto>> GetOrganizationMembersAsync(
        Guid organizationId,
        Guid requestUserId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ValidationException("Organization id is required.");
        }

        if (requestUserId == Guid.Empty)
        {
            throw new ValidationException("Request user id is required.");
        }

        var requestUserRole = await organizationMemberRepository.GetUserRoleInOrganizationAsync(
            organizationId,
            requestUserId,
            cancellationToken);

        if (requestUserRole is null)
        {
            throw new ForbiddenException("User is not a member of this organization.");
        }

        var members = await organizationRepository.GetMembersByOrganizationIdAsync(organizationId, cancellationToken);
        logger.LogInformation(
            "Fetched {Count} members for organization {OrganizationId} requested by user {UserId}",
            members.Count,
            organizationId,
            requestUserId);

        return members;
    }

    private static OrganizationMemberRole ParseRequestedRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return OrganizationMemberRole.Employee;
        }

        return role.Trim().ToUpperInvariant() switch
        {
            "OWNER" => OrganizationMemberRole.Owner,
            "MENAGER" => OrganizationMemberRole.Menager,
            "EMPLOYEE" => OrganizationMemberRole.Employee,
            _ => throw new ValidationException("Invalid role. Allowed values: OWNER, MENAGER, EMPLOYEE.")
        };
    }

    private static string GenerateInvitationToken()
    {
        Span<byte> randomBytes = stackalloc byte[48];
        RandomNumberGenerator.Fill(randomBytes);
        return Convert.ToBase64String(randomBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
