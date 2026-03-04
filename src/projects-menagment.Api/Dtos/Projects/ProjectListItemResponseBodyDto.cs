namespace projects_menagment.Api.Dtos.Projects;

public sealed record ProjectListItemResponseBodyDto(
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
    decimal TotalSpentAmount,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    bool IsArchived);
