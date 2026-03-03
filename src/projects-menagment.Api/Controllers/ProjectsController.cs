using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projects_menagment.Api.Dtos.Common;
using projects_menagment.Api.Dtos.Projects;
using projects_menagment.Application.Dtos.Projects;
using projects_menagment.Application.Exceptions;
using projects_menagment.Application.Interfaces.Services;

namespace projects_menagment.Api.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/projects")]
[Authorize]
public sealed class ProjectsController(
    IProjectService projectService,
    ILogger<ProjectsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<ProjectListItemResponseBodyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByOrganizationId(Guid organizationId, CancellationToken cancellationToken)
    {
        var result = await projectService.GetByOrganizationIdAsync(
            organizationId,
            GetAuthenticatedUserId(User),
            cancellationToken);

        var response = result.Select(project => new ProjectListItemResponseBodyDto(
                project.Id,
                project.OrganizationId,
                project.Name,
                project.Description,
                project.Deadline,
                project.Budget,
                project.Status,
                project.TotalTasksCount,
                project.FinishedTasksCount,
                project.ProgressPercentage,
                project.CreatedByUserId,
                project.CreatedAt,
                project.IsArchived))
            .ToList();

        return Ok(response);
    }

    [HttpGet("{projectId:guid}/members")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ProjectMemberResponseBodyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMembers(
        Guid organizationId,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var result = await projectService.GetMembersAsync(
            organizationId,
            projectId,
            GetAuthenticatedUserId(User),
            cancellationToken);

        var response = result
            .Select(member => new ProjectMemberResponseBodyDto(
                member.UserId,
                member.FirstName,
                member.LastName,
                member.Role,
                member.AddedAt))
            .ToList();

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateProjectResponseBodyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        Guid organizationId,
        [FromBody] CreateProjectRequestBodyDto? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ValidationException("Request body is required.");
        }

        var authenticatedUserId = GetAuthenticatedUserId(User);
        logger.LogInformation(
            "Processing project create request for organization {OrganizationId} by user {UserId}",
            organizationId,
            authenticatedUserId);

        var result = await projectService.CreateAsync(
            organizationId,
            new CreateProjectRequestDto(
                request.Name ?? string.Empty,
                request.Description,
                request.Deadline,
                request.Budget,
                request.Status),
            authenticatedUserId,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new CreateProjectResponseBodyDto(
            result.Id,
            result.OrganizationId,
            result.Name,
            result.Description,
            result.Deadline,
            result.Budget,
            result.Status,
            result.TotalTasksCount,
            result.FinishedTasksCount,
            result.CreatedByUserId,
            result.CreatedAt,
            result.IsArchived));
    }

    [HttpPost("{projectId:guid}/members")]
    [ProducesResponseType(typeof(AddProjectMemberResponseBodyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddMember(
        Guid organizationId,
        Guid projectId,
        [FromBody] AddProjectMemberRequestBodyDto? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ValidationException("Request body is required.");
        }

        var authenticatedUserId = GetAuthenticatedUserId(User);
        logger.LogInformation(
            "Processing add project member request for organization {OrganizationId}, project {ProjectId} by user {UserId}",
            organizationId,
            projectId,
            authenticatedUserId);

        var result = await projectService.AddMemberAsync(
            organizationId,
            new AddProjectMemberRequestDto(
                projectId,
                request.UserId,
                request.Role),
            authenticatedUserId,
            cancellationToken);

        return Ok(new AddProjectMemberResponseBodyDto(
            result.Id,
            result.ProjectId,
            result.UserId,
            result.Role,
            result.CreatedAt));
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
