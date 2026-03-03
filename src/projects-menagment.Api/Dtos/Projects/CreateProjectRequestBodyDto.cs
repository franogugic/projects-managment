namespace projects_menagment.Api.Dtos.Projects;

public sealed record CreateProjectRequestBodyDto(
    string? Name,
    string? Description,
    DateTime? Deadline,
    decimal Budget,
    string? Status);
