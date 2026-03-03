namespace projects_menagment.Application.Dtos.Projects;

public sealed record CreateProjectRequestDto(
    string Name,
    string? Description,
    DateTime? Deadline,
    decimal Budget,
    string? Status);
