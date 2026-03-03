using Microsoft.Extensions.Logging;
using projects_menagment.Application.Dtos.Tasks;
using projects_menagment.Application.Exceptions;
using projects_menagment.Application.Interfaces.Repositories;
using projects_menagment.Application.Interfaces.Services;
using projects_menagment.Domain.Entities;
using projects_menagment.Domain.Enums;

namespace projects_menagment.Application.Services;

public sealed class ProjectTaskService(
    IUserRepository userRepository,
    IProjectRepository projectRepository,
    IProjectMemberRepository projectMemberRepository,
    IProjectTaskRepository projectTaskRepository,
    ILogger<ProjectTaskService> logger) : IProjectTaskService
{
    public async Task<CreateTaskResponseDto> CreateAsync(
        Guid organizationId,
        CreateTaskRequestDto request,
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

        if (request.AssigneeUserId == Guid.Empty)
        {
            throw new ValidationException("Assignee user id is required.");
        }

        if (requestUserId == Guid.Empty)
        {
            throw new ValidationException("Request user id is required.");
        }

        var title = request.Title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Task title is required.");
        }

        if (title.Length > 200)
        {
            throw new ValidationException("Task title must not exceed 200 characters.");
        }

        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null || project.OrganizationId != organizationId)
        {
            throw new NotFoundException("Project was not found.");
        }

        if (project.IsArchived)
        {
            throw new ConflictException("Cannot create task in archived project.");
        }

        var requesterMember = await projectMemberRepository.GetForUpdateAsync(
            request.ProjectId,
            requestUserId,
            cancellationToken);

        if (requesterMember is null)
        {
            throw new ForbiddenException("User is not a member of this project.");
        }

        if (requesterMember.Role != ProjectMemberRole.Menager)
        {
            throw new ForbiddenException("Only MENAGER can create tasks.");
        }

        var assignee = await userRepository.GetByIdAsync(request.AssigneeUserId, cancellationToken);
        if (assignee is null)
        {
            throw new NotFoundException("Assignee user was not found.");
        }

        if (!assignee.IsActive)
        {
            throw new ForbiddenException("Assignee user account is inactive.");
        }

        var assigneeIsProjectMember = await projectMemberRepository.ExistsAsync(
            request.ProjectId,
            request.AssigneeUserId,
            cancellationToken);

        if (!assigneeIsProjectMember)
        {
            throw new ForbiddenException("Assignee user is not a member of this project.");
        }

        var priority = ParsePriority(request.Priority);

        var task = ProjectTask.Create(
            request.ProjectId,
            title,
            requestUserId,
            priority,
            request.Description,
            request.AssigneeUserId,
            request.DueDate);

        await projectTaskRepository.AddAsync(task, cancellationToken);

        project.SetTaskProgress(project.TotalTasksCount + 1, project.FinishedTasksCount);
        await projectRepository.UpdateAsync(project, cancellationToken);

        logger.LogInformation(
            "Task {TaskId} created in project {ProjectId} by user {UserId}; assignee {AssigneeUserId}",
            task.Id,
            request.ProjectId,
            requestUserId,
            request.AssigneeUserId);

        return new CreateTaskResponseDto(
            task.Id,
            task.ProjectId,
            task.AssigneeUserId!.Value,
            task.Title,
            task.Description,
            task.DueDate,
            task.Status.ToString().ToUpperInvariant(),
            task.Priority.ToString().ToUpperInvariant(),
            task.SpentAmount,
            task.CreatedByUserId,
            task.CreatedAt);
    }

    public async Task<IReadOnlyCollection<ProjectTaskListItemDto>> GetByProjectIdAsync(
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

        var requesterIsProjectMember = await projectMemberRepository.ExistsAsync(
            projectId,
            requestUserId,
            cancellationToken);

        if (!requesterIsProjectMember)
        {
            throw new ForbiddenException("User is not a member of this project.");
        }

        var tasks = await projectTaskRepository.GetByProjectIdAsync(projectId, cancellationToken);

        logger.LogInformation(
            "Fetched {Count} tasks for project {ProjectId} requested by user {UserId}",
            tasks.Count,
            projectId,
            requestUserId);

        return tasks;
    }

    public async Task<UpdateProjectTaskStatusResponseDto> UpdateStatusAsync(
        Guid organizationId,
        Guid projectId,
        UpdateProjectTaskStatusRequestDto request,
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

        if (request.TaskId == Guid.Empty)
        {
            throw new ValidationException("Task id is required.");
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

        var task = await projectTaskRepository.GetForUpdateByIdAsync(request.TaskId, cancellationToken);
        if (task is null || task.ProjectId != projectId)
        {
            throw new NotFoundException("Task was not found.");
        }

        var requesterMember = await projectMemberRepository.GetForUpdateAsync(projectId, requestUserId, cancellationToken);
        if (requesterMember is null)
        {
            throw new ForbiddenException("User is not a member of this project.");
        }

        var canManageTask = requesterMember.Role == ProjectMemberRole.Menager || task.AssigneeUserId == requestUserId;
        if (!canManageTask)
        {
            throw new ForbiddenException("Only MENAGER or assigned user can update task status.");
        }

        var requestedStatus = ParseUpdatableStatus(request.Status);
        switch (requestedStatus)
        {
            case ProjectTaskStatus.InProgress:
                if (task.Status == ProjectTaskStatus.Done)
                {
                    throw new ConflictException("Completed task cannot be moved back to IN_PROGRESS.");
                }

                task.MarkInProgress(DateTime.UtcNow);
                break;
            case ProjectTaskStatus.Done:
            {
                if (task.Status == ProjectTaskStatus.Done)
                {
                    throw new ConflictException("Task is already completed.");
                }

                if (string.IsNullOrWhiteSpace(request.CompletionNote))
                {
                    throw new ValidationException("Completion note is required when marking task as DONE.");
                }

                if (!request.SpentAmount.HasValue)
                {
                    throw new ValidationException("Spent amount is required when marking task as DONE.");
                }

                if (request.SpentAmount.Value < 0)
                {
                    throw new ValidationException("Task spent amount must not be negative.");
                }

                var previousSpentAmount = task.SpentAmount;
                task.MarkDone(requestUserId, request.CompletionNote, request.SpentAmount.Value, DateTime.UtcNow);
                project.SetTaskProgress(project.TotalTasksCount, project.FinishedTasksCount + 1);
                project.SetTotalSpentAmount(project.TotalSpentAmount + (task.SpentAmount - previousSpentAmount));
                break;
            }
            default:
                throw new ValidationException("Task status can only be updated to IN_PROGRESS or DONE.");
        }

        await projectTaskRepository.UpdateAsync(task, cancellationToken);
        await projectRepository.UpdateAsync(project, cancellationToken);

        return new UpdateProjectTaskStatusResponseDto(
            task.Id,
            task.ProjectId,
            task.AssigneeUserId,
            task.Title,
            task.Description,
            task.DueDate,
            task.Status.ToString().ToUpperInvariant(),
            task.Priority.ToString().ToUpperInvariant(),
            task.SpentAmount,
            task.CreatedByUserId,
            task.CreatedAt,
            task.CompletedAt,
            task.CompletedByUserId,
            task.CompletionNote);
    }

    private static TaskPriority ParsePriority(string? priority)
    {
        if (string.IsNullOrWhiteSpace(priority))
        {
            return TaskPriority.Medium;
        }

        return priority.Trim().ToUpperInvariant() switch
        {
            "LOW" => TaskPriority.Low,
            "MEDIUM" => TaskPriority.Medium,
            "HIGH" => TaskPriority.High,
            _ => throw new ValidationException("Invalid task priority. Allowed values: LOW, MEDIUM, HIGH.")
        };
    }

    private static ProjectTaskStatus ParseUpdatableStatus(string? status)
    {
        return status?.Trim().ToUpperInvariant() switch
        {
            "IN_PROGRESS" => ProjectTaskStatus.InProgress,
            "INPROGRESS" => ProjectTaskStatus.InProgress,
            "DONE" => ProjectTaskStatus.Done,
            _ => throw new ValidationException("Invalid task status. Allowed values: IN_PROGRESS, DONE.")
        };
    }
}
