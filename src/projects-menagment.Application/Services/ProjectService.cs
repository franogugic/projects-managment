using Microsoft.Extensions.Logging;
using projects_menagment.Application.Dtos.Projects;
using projects_menagment.Application.Exceptions;
using projects_menagment.Application.Interfaces.Repositories;
using projects_menagment.Application.Interfaces.Services;
using projects_menagment.Domain.Entities;
using projects_menagment.Domain.Enums;

namespace projects_menagment.Application.Services;

public sealed class ProjectService(
    IUserRepository userRepository,
    IOrganizationRepository organizationRepository,
    IOrganizationMemberRepository organizationMemberRepository,
    IPlanRepository planRepository,
    IProjectRepository projectRepository,
    IProjectMemberRepository projectMemberRepository,
    ILogger<ProjectService> logger) : IProjectService
{
    public async Task<CreateProjectResponseDto> CreateAsync(
        Guid organizationId,
        CreateProjectRequestDto request,
        Guid createdByUserId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ValidationException("Organization id is required.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new ValidationException("Created by user id is required.");
        }

        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Project name is required.");
        }

        if (name.Length > 150)
        {
            throw new ValidationException("Project name must not exceed 150 characters.");
        }

        if (request.Budget < 0)
        {
            throw new ValidationException("Project budget must not be negative.");
        }

        var creator = await userRepository.GetByIdAsync(createdByUserId, cancellationToken);
        if (creator is null)
        {
            throw new NotFoundException("Creator user was not found.");
        }

        if (!creator.IsActive)
        {
            throw new ForbiddenException("User account is inactive.");
        }

        var organization = await organizationRepository.GetByIdAsync(organizationId, cancellationToken);
        if (organization is null)
        {
            throw new NotFoundException("Organization was not found.");
        }

        var creatorRole = await organizationMemberRepository.GetUserRoleInOrganizationAsync(
            organizationId,
            createdByUserId,
            cancellationToken);

        if (creatorRole is not OrganizationMemberRole.Owner and not OrganizationMemberRole.Menager)
        {
            throw new ForbiddenException("Only OWNER or MENAGER can create projects.");
        }

        var plan = await planRepository.GetByIdAsync(organization.PlanId, cancellationToken);
        if (plan is null)
        {
            throw new NotFoundException("Organization plan was not found.");
        }

        if (!plan.IsActive)
        {
            throw new ForbiddenException("Organization plan is inactive.");
        }

        var activeProjectCount = await projectRepository.CountActiveByOrganizationIdAsync(organizationId, cancellationToken);
        if (activeProjectCount >= plan.MaxProjects)
        {
            throw new ConflictException("Organization reached max active projects allowed by current plan.");
        }

        var status = ParseProjectStatus(request.Status);
        var project = Project.Create(
            organizationId,
            name,
            createdByUserId,
            request.Budget,
            request.Description,
            request.Deadline,
            status);

        await projectRepository.AddAsync(project, cancellationToken);

        logger.LogInformation(
            "Project {ProjectId} created in organization {OrganizationId} by user {UserId}",
            project.Id,
            organizationId,
            createdByUserId);

        return new CreateProjectResponseDto(
            project.Id,
            project.OrganizationId,
            project.Name,
            project.Description,
            project.Deadline,
            project.Budget,
            project.Status.ToString().ToUpperInvariant(),
            project.TotalTasksCount,
            project.FinishedTasksCount,
            project.CreatedByUserId,
            project.CreatedAt,
            project.IsArchived);
    }

    public async Task<IReadOnlyCollection<ProjectListItemDto>> GetByOrganizationIdAsync(
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

        var organization = await organizationRepository.GetByIdAsync(organizationId, cancellationToken);
        if (organization is null)
        {
            throw new NotFoundException("Organization was not found.");
        }

        var userRole = await organizationMemberRepository.GetUserRoleInOrganizationAsync(
            organizationId,
            requestUserId,
            cancellationToken);

        if (userRole is null)
        {
            throw new ForbiddenException("User is not a member of this organization.");
        }

        var projects = await projectRepository.GetByOrganizationIdAsync(organizationId, cancellationToken);
        logger.LogInformation(
            "Fetched {Count} projects for organization {OrganizationId} requested by user {UserId}",
            projects.Count,
            organizationId,
            requestUserId);

        return projects;
    }

    public async Task<AddProjectMemberResponseDto> AddMemberAsync(
        Guid organizationId,
        AddProjectMemberRequestDto request,
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

        if (request.ProjectId == Guid.Empty)
        {
            throw new ValidationException("Project id is required.");
        }

        if (request.UserId == Guid.Empty)
        {
            throw new ValidationException("Target user id is required.");
        }

        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null || project.OrganizationId != organizationId)
        {
            throw new NotFoundException("Project was not found.");
        }

        var requesterRole = await organizationMemberRepository.GetUserRoleInOrganizationAsync(
            organizationId,
            requestUserId,
            cancellationToken);

        if (requesterRole is not OrganizationMemberRole.Owner and not OrganizationMemberRole.Menager)
        {
            throw new ForbiddenException("Only OWNER or MENAGER can add project members.");
        }

        var targetUser = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (targetUser is null)
        {
            throw new NotFoundException("Target user was not found.");
        }

        if (!targetUser.IsActive)
        {
            throw new ForbiddenException("Target user account is inactive.");
        }

        var isOrganizationMember = await organizationMemberRepository.ExistsAsync(organizationId, request.UserId, cancellationToken);
        if (!isOrganizationMember)
        {
            throw new ForbiddenException("Target user is not a member of this organization.");
        }

        var alreadyProjectMember = await projectMemberRepository.ExistsAsync(request.ProjectId, request.UserId, cancellationToken);
        if (alreadyProjectMember)
        {
            throw new ConflictException("User is already a member of this project.");
        }

        var role = ParseProjectMemberRole(request.Role);
        var member = ProjectMember.Create(request.ProjectId, request.UserId, role);
        await projectMemberRepository.AddAsync(member, cancellationToken);

        logger.LogInformation(
            "User {TargetUserId} added to project {ProjectId} by user {RequestUserId} with role {Role}",
            request.UserId,
            request.ProjectId,
            requestUserId,
            role);

        return new AddProjectMemberResponseDto(
            member.Id,
            member.ProjectId,
            member.UserId,
            member.Role.ToString().ToUpperInvariant(),
            member.CreatedAt);
    }

    private static ProjectStatus ParseProjectStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return ProjectStatus.Planned;
        }

        return status.Trim().ToUpperInvariant() switch
        {
            "PLANNED" => ProjectStatus.Planned,
            "IN_PROGRESS" => ProjectStatus.InProgress,
            "ON_HOLD" => ProjectStatus.OnHold,
            "COMPLETED" => ProjectStatus.Completed,
            "CANCELLED" => ProjectStatus.Cancelled,
            _ => throw new ValidationException(
                "Invalid project status. Allowed values: PLANNED, IN_PROGRESS, ON_HOLD, COMPLETED, CANCELLED.")
        };
    }

    private static ProjectMemberRole ParseProjectMemberRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return ProjectMemberRole.Employee;
        }

        return role.Trim().ToUpperInvariant() switch
        {
            "MENAGER" => ProjectMemberRole.Menager,
            "MANAGER" => ProjectMemberRole.Menager,
            "EMPLOYEE" => ProjectMemberRole.Employee,
            _ => throw new ValidationException("Invalid member role. Allowed values: MENAGER, EMPLOYEE.")
        };
    }
}
