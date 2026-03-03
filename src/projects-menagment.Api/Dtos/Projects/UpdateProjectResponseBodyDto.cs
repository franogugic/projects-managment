namespace projects_menagment.Api.Dtos.Projects;

public sealed record UpdateProjectResponseBodyDto(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string? Description,
    DateTime? Deadline,
    decimal Budget,
    string Status,
    int TotalTasksCount,
    int FinishedTasksCount,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    bool IsArchived);
