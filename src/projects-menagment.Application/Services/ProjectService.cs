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

        var creatorProjectMember = ProjectMember.Create(
            project.Id,
            createdByUserId,
            ProjectMemberRole.Menager);
        await projectMemberRepository.AddAsync(creatorProjectMember, cancellationToken);

        logger.LogInformation(
            "Project {ProjectId} created in organization {OrganizationId} by user {UserId}; creator added as project member with role MENAGER",
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
            project.TotalSpentAmount,
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

    public async Task<ProjectListItemDto> GetByIdAsync(
        Guid organizationId,
        Guid projectId,
        Guid requestUserId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ValidationException("Organization id is required.");
        }

        if (projectId == Guid.Empty)
        {
            throw new ValidationException("Project id is required.");
        }

        if (requestUserId == Guid.Empty)
        {
            throw new ValidationException("Request user id is required.");
        }

        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null || project.OrganizationId != organizationId)
        {
            throw new NotFoundException("Project was not found.");
        }

        var userRole = await organizationMemberRepository.GetUserRoleInOrganizationAsync(
            organizationId,
            requestUserId,
            cancellationToken);

        if (userRole is null)
        {
            throw new ForbiddenException("User is not a member of this organization.");
        }

        var progress = project.TotalTasksCount == 0
            ? 0m
            : Math.Round((decimal)project.FinishedTasksCount * 100m / project.TotalTasksCount, 2);

        return new ProjectListItemDto(
            project.Id,
            project.OrganizationId,
            project.Name,
            project.Description,
            project.Deadline,
            project.Budget,
            project.Status.ToString().ToUpperInvariant(),
            project.TotalTasksCount,
            project.FinishedTasksCount,
            progress,
            project.TotalSpentAmount,
            project.CreatedByUserId,
            project.CreatedAt,
            project.IsArchived);
    }

    public async Task<UpdateProjectResponseDto> UpdateAsync(
        Guid organizationId,
        UpdateProjectRequestDto request,
        Guid requestUserId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ValidationException("Organization id is required.");
        }

        if (request.ProjectId == Guid.Empty)
        {
            throw new ValidationException("Project id is required.");
        }

        if (requestUserId == Guid.Empty)
        {
            throw new ValidationException("Request user id is required.");
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

        var project = await projectRepository.GetForUpdateByIdAsync(request.ProjectId, cancellationToken);
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
            throw new ForbiddenException("Only OWNER or MENAGER can update projects.");
        }

        var status = ParseProjectStatus(request.Status);
        project.UpdateDetails(
            name,
            request.Description,
            request.Deadline,
            request.Budget,
            status);

        await projectRepository.UpdateAsync(project, cancellationToken);

        logger.LogInformation(
            "Project {ProjectId} updated in organization {OrganizationId} by user {UserId}",
            project.Id,
            organizationId,
            requestUserId);

        return new UpdateProjectResponseDto(
            project.Id,
            project.OrganizationId,
            project.Name,
            project.Description,
            project.Deadline,
            project.Budget,
            project.Status.ToString().ToUpperInvariant(),
            project.TotalTasksCount,
            project.FinishedTasksCount,
            project.TotalSpentAmount,
            project.CreatedByUserId,
            project.CreatedAt,
            project.IsArchived);
    }

    public async Task<ArchiveProjectResponseDto> ArchiveAsync(
        Guid organizationId,
        Guid projectId,
        Guid requestUserId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ValidationException("Organization id is required.");
        }

        if (projectId == Guid.Empty)
        {
            throw new ValidationException("Project id is required.");
        }

        if (requestUserId == Guid.Empty)
        {
            throw new ValidationException("Request user id is required.");
        }

        var project = await projectRepository.GetForUpdateByIdAsync(projectId, cancellationToken);
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
            throw new ForbiddenException("Only OWNER or MENAGER can archive projects.");
        }

        if (project.IsArchived)
        {
            throw new ConflictException("Project is already archived.");
        }

        if (project.Status != ProjectStatus.Completed)
        {
            throw new ConflictException("Only COMPLETED projects can be archived.");
        }

        project.Archive();
        await projectRepository.UpdateAsync(project, cancellationToken);

        logger.LogInformation(
            "Project {ProjectId} archived in organization {OrganizationId} by user {UserId}",
            project.Id,
            organizationId,
            requestUserId);

        return new ArchiveProjectResponseDto(
            project.Id,
            project.OrganizationId,
            project.Status.ToString().ToUpperInvariant(),
            project.IsArchived);
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

    public async Task<UpdateProjectMemberRoleResponseDto> UpdateMemberRoleAsync(
        Guid organizationId,
        UpdateProjectMemberRoleRequestDto request,
        Guid requestUserId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ValidationException("Organization id is required.");
        }

        if (request.ProjectId == Guid.Empty)
        {
            throw new ValidationException("Project id is required.");
        }

        if (request.UserId == Guid.Empty)
        {
            throw new ValidationException("Target user id is required.");
        }

        if (requestUserId == Guid.Empty)
        {
            throw new ValidationException("Request user id is required.");
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
            throw new ForbiddenException("Only OWNER or MENAGER can update project member role.");
        }

        var member = await projectMemberRepository.GetForUpdateAsync(request.ProjectId, request.UserId, cancellationToken);
        if (member is null)
        {
            throw new NotFoundException("Project member was not found.");
        }

        var role = ParseProjectMemberRole(request.Role);
        if (request.UserId == project.CreatedByUserId && role != ProjectMemberRole.Menager)
        {
            throw new ConflictException("Project creator role cannot be changed from MENAGER.");
        }

        if (member.Role == ProjectMemberRole.Menager && role != ProjectMemberRole.Menager)
        {
            var managersCount = await projectMemberRepository.CountByProjectIdAndRoleAsync(
                request.ProjectId,
                ProjectMemberRole.Menager,
                cancellationToken);

            if (managersCount <= 1)
            {
                throw new ConflictException("Project must have at least one MENAGER.");
            }
        }

        member.ChangeRole(role);
        await projectMemberRepository.UpdateAsync(member, cancellationToken);

        logger.LogInformation(
            "Project member role updated for user {TargetUserId} in project {ProjectId} by user {RequestUserId} to {Role}",
            request.UserId,
            request.ProjectId,
            requestUserId,
            role);

        return new UpdateProjectMemberRoleResponseDto(
            member.Id,
            member.ProjectId,
            member.UserId,
            member.Role.ToString().ToUpperInvariant(),
            member.CreatedAt);
    }

    public async Task<RemoveProjectMemberResponseDto> RemoveMemberAsync(
        Guid organizationId,
        Guid projectId,
        Guid userId,
        Guid requestUserId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ValidationException("Organization id is required.");
        }

        if (projectId == Guid.Empty)
        {
            throw new ValidationException("Project id is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new ValidationException("Target user id is required.");
        }

        if (requestUserId == Guid.Empty)
        {
            throw new ValidationException("Request user id is required.");
        }

        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
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
            throw new ForbiddenException("Only OWNER or MENAGER can remove project members.");
        }

        var member = await projectMemberRepository.GetForUpdateAsync(projectId, userId, cancellationToken);
        if (member is null)
        {
            throw new NotFoundException("Project member was not found.");
        }

        if (userId == project.CreatedByUserId)
        {
            throw new ConflictException("Project creator cannot be removed from project members.");
        }

        if (member.Role == ProjectMemberRole.Menager)
        {
            var managersCount = await projectMemberRepository.CountByProjectIdAndRoleAsync(
                projectId,
                ProjectMemberRole.Menager,
                cancellationToken);

            if (managersCount <= 1)
            {
                throw new ConflictException("Project must have at least one MENAGER.");
            }
        }

        await projectMemberRepository.RemoveAsync(member, cancellationToken);

        logger.LogInformation(
            "Project member removed for user {TargetUserId} from project {ProjectId} by user {RequestUserId}",
            userId,
            projectId,
            requestUserId);

        return new RemoveProjectMemberResponseDto(projectId, userId, true);
    }

    public async Task<IReadOnlyCollection<ProjectMemberDto>> GetMembersAsync(
        Guid organizationId,
        Guid projectId,
        Guid requestUserId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ValidationException("Organization id is required.");
        }

        if (projectId == Guid.Empty)
        {
            throw new ValidationException("Project id is required.");
        }

        if (requestUserId == Guid.Empty)
        {
            throw new ValidationException("Request user id is required.");
        }

        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null || project.OrganizationId != organizationId)
        {
            throw new NotFoundException("Project was not found.");
        }

        var requesterRole = await organizationMemberRepository.GetUserRoleInOrganizationAsync(
            organizationId,
            requestUserId,
            cancellationToken);

        if (requesterRole is null)
        {
            throw new ForbiddenException("User is not a member of this organization.");
        }

        var members = await projectMemberRepository.GetByProjectIdAsync(projectId, cancellationToken);

        logger.LogInformation(
            "Fetched {Count} project members for project {ProjectId} in organization {OrganizationId} requested by user {UserId}",
            members.Count,
            projectId,
            organizationId,
            requestUserId);

        return members;
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
            "INPROGRESS" => ProjectStatus.InProgress,
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
