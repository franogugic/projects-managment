using projects_menagment.Domain.Enums;

namespace projects_menagment.Domain.Entities;

public sealed class ProjectTask
{
    private ProjectTask()
    {
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ProjectTaskStatus Status { get; private set; }
    public TaskPriority Priority { get; private set; }
    public Guid? AssigneeUserId { get; private set; }
    public DateTime? DueDate { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid? CompletedByUserId { get; private set; }
    public string? CompletionNote { get; private set; }

    public static ProjectTask Create(
        Guid projectId,
        string title,
        Guid createdByUserId,
        TaskPriority priority = TaskPriority.Medium,
        string? description = null,
        Guid? assigneeUserId = null,
        DateTime? dueDate = null)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id is required.", nameof(projectId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Task title is required.", nameof(title));
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("Created by user id is required.", nameof(createdByUserId));
        }

        return new ProjectTask
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = title.Trim(),
            Description = description?.Trim(),
            Status = ProjectTaskStatus.Todo,
            Priority = priority,
            AssigneeUserId = assigneeUserId,
            DueDate = dueDate,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            IsDeleted = false,
            CompletedAt = null,
            CompletedByUserId = null,
            CompletionNote = null
        };
    }
}
