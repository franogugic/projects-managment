namespace projects_menagment.Application.Dtos.Projects;

public sealed record UpdateProjectRequestDto(
    Guid ProjectId,
    string? Name,
    string? Description,
    DateTime? Deadline,
    decimal Budget,
    string? Status);
