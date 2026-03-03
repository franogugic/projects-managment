using projects_menagment.Domain.Enums;

namespace projects_menagment.Domain.Entities;

public sealed class Project
{
    private Project()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime? Deadline { get; private set; }
    public decimal Budget { get; private set; }
    public ProjectStatus Status { get; private set; }
    public int TotalTasksCount { get; private set; }
    public int FinishedTasksCount { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsArchived { get; private set; }

    public static Project Create(
        Guid organizationId,
        string name,
        Guid createdByUserId,
        decimal budget,
        string? description = null,
        DateTime? deadline = null,
        ProjectStatus status = ProjectStatus.Planned)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("Organization id is required.", nameof(organizationId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name is required.", nameof(name));
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("Created by user id is required.", nameof(createdByUserId));
        }

        if (budget < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(budget), "Budget must not be negative.");
        }

        return new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Deadline = deadline,
            Budget = budget,
            Status = status,
            TotalTasksCount = 0,
            FinishedTasksCount = 0,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            IsArchived = false
        };
    }

    public void SetTaskProgress(int totalTasksCount, int finishedTasksCount)
    {
        if (totalTasksCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalTasksCount), "Total tasks count must not be negative.");
        }

        if (finishedTasksCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(finishedTasksCount), "Finished tasks count must not be negative.");
        }

        if (finishedTasksCount > totalTasksCount)
        {
            throw new ArgumentException("Finished tasks count must be less than or equal to total tasks count.");
        }

        TotalTasksCount = totalTasksCount;
        FinishedTasksCount = finishedTasksCount;
    }

    public void UpdateDetails(
        string name,
        string? description,
        DateTime? deadline,
        decimal budget,
        ProjectStatus status)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name is required.", nameof(name));
        }

        if (budget < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(budget), "Budget must not be negative.");
        }

        Name = name.Trim();
        Description = description?.Trim();
        Deadline = deadline;
        Budget = budget;
        Status = status;
    }

    public void Archive()
    {
        IsArchived = true;
    }
}
