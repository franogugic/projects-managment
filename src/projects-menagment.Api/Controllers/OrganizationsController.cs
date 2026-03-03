using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projects_menagment.Api.Dtos.Common;
using projects_menagment.Api.Dtos.Organizations;
using projects_menagment.Application.Dtos.Organizations;
using projects_menagment.Application.Exceptions;
using projects_menagment.Application.Interfaces.Services;

namespace projects_menagment.Api.Controllers;

[ApiController]
[Route("api/organizations")]
[Authorize]
public sealed class OrganizationsController(
    IOrganizationService organizationService,
    ILogger<OrganizationsController> logger) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateOrganizationResponseBodyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationRequestBodyDto? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ValidationException("Request body is required.");
        }

        logger.LogInformation(
            "Processing organization create request for name {OrganizationName} by user {UserId}",
            request.Name,
            GetAuthenticatedUserId(User));

        var authenticatedUserId = GetAuthenticatedUserId(User);

        var response = await organizationService.CreateAsync(
            new CreateOrganizationRequestDto(
                request.Name ?? string.Empty,
                request.PlanId),
            authenticatedUserId,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateOrganizationResponseBodyDto(
                response.Id,
                response.Name,
                response.PlanId,
                response.CreatedByUserId,
                response.CreatedAt));
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserOrganizationResponseBodyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMyOrganizations(CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId(User);
        logger.LogInformation("Processing organizations fetch for user {UserId}", userId);

        var organizations = await organizationService.GetByUserIdAsync(userId, cancellationToken);
        var response = organizations
            .Select(item => new UserOrganizationResponseBodyDto(
                item.OrganizationId,
                item.Name,
                item.PlanId,
                item.CreatedByUserId,
                item.CreatedAt,
                item.Role))
            .ToList();

        return Ok(response);
    }

    [HttpPost("{organizationId:guid}/members/invite")]
    [ProducesResponseType(typeof(InviteOrganizationMemberResponseBodyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> InviteMember(
        Guid organizationId,
        [FromBody] InviteOrganizationMemberRequestBodyDto? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ValidationException("Request body is required.");
        }

        var result = await organizationService.InviteMemberAsync(
            new InviteOrganizationMemberRequestDto(
                organizationId,
                request.Email ?? string.Empty,
                request.Role),
            GetAuthenticatedUserId(User),
            cancellationToken);

        return Ok(new InviteOrganizationMemberResponseBodyDto(
            result.InvitationId,
            result.Email,
            result.Role,
            result.ExpiresAt,
            result.InvitationLink));
    }

    [HttpPost("member-invitations/accept")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AcceptOrganizationInvitationResponseBodyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AcceptInvitation(
        [FromBody] AcceptOrganizationInvitationRequestBodyDto? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ValidationException("Request body is required.");
        }

        var result = await organizationService.AcceptInvitationAsync(
            new AcceptOrganizationInvitationRequestDto(request.Token ?? string.Empty),
            cancellationToken);

        return Ok(new AcceptOrganizationInvitationResponseBodyDto(
            result.OrganizationId,
            result.OrganizationName,
            result.UserId,
            result.Role,
            result.Message));
    }

    // NASTAVIT OVDEEEEE
    [HttpGet("member-invitations/preview")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OrganizationInvitationPreviewResponseBodyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PreviewInvitation([FromQuery] string token, CancellationToken cancellationToken)
    {
        var result = await organizationService.GetInvitationPreviewAsync(token, cancellationToken);

        return Ok(new OrganizationInvitationPreviewResponseBodyDto(
            result.OrganizationId,
            result.OrganizationName,
            result.Email,
            result.Role,
            result.ExpiresAt,
            result.IsAccepted,
            result.IsExpired));
    }

    [HttpGet("{organizationId:guid}/members")]
    [ProducesResponseType(typeof(IReadOnlyCollection<OrganizationMemberResponseBodyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMembers(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var result = await organizationService.GetOrganizationMembersAsync(
            organizationId,
            GetAuthenticatedUserId(User),
            cancellationToken);

        var response = result.Select(member => new OrganizationMemberResponseBodyDto(
                member.UserId,
                member.FirstName,
                member.LastName,
                member.Role))
            .ToList();

        return Ok(response);
    }

    private static Guid GetAuthenticatedUserId(ClaimsPrincipal user)
    {
        var userIdRaw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            throw new UnauthorizedException("Invalid authenticated user id.");
        }

        return userId;
    }
}
