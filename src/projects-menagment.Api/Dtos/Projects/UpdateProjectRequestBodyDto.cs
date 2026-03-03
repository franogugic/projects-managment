namespace projects_menagment.Api.Dtos.Projects;

public sealed record UpdateProjectRequestBodyDto(
    string? Name,
    string? Description,
    DateTime? Deadline,
    decimal Budget,
    string? Status);
