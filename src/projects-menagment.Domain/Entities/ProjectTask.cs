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
    public decimal SpentAmount { get; private set; }
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
            SpentAmount = 0m,
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

    public void MarkInProgress(DateTime nowUtc)
    {
        if (Status == ProjectTaskStatus.Done)
        {
            throw new InvalidOperationException("Completed task cannot be moved back to IN_PROGRESS.");
        }

        Status = ProjectTaskStatus.InProgress;
        UpdatedAt = nowUtc;
    }

    public void MarkDone(Guid completedByUserId, string completionNote, decimal spentAmount, DateTime nowUtc)
    {
        if (completedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Completed by user id is required.", nameof(completedByUserId));
        }

        if (string.IsNullOrWhiteSpace(completionNote))
        {
            throw new ArgumentException("Completion note is required.", nameof(completionNote));
        }

        if (spentAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spentAmount), "Task spent amount must not be negative.");
        }

        if (Status == ProjectTaskStatus.Done)
        {
            throw new InvalidOperationException("Task is already completed.");
        }

        Status = ProjectTaskStatus.Done;
        SpentAmount = spentAmount;
        CompletedByUserId = completedByUserId;
        CompletedAt = nowUtc;
        CompletionNote = completionNote.Trim();
        UpdatedAt = nowUtc;
    }
}
