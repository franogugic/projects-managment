namespace projects_menagment.Application.Dtos.Projects;

public sealed record UpdateProjectResponseDto(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string? Description,
    DateTime? Deadline,
    decimal Budget,
    string Status,
    int TotalTasksCount,
    int FinishedTasksCount,
    decimal TotalSpentAmount,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    bool IsArchived);
