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
}
