namespace projects_menagment.Application.Dtos.Projects;

public sealed record ProjectListItemDto(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string? Description,
    DateTime? Deadline,
    decimal Budget,
    string Status,
    int TotalTasksCount,
    int FinishedTasksCount,
    decimal ProgressPercentage,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    bool IsArchived);
