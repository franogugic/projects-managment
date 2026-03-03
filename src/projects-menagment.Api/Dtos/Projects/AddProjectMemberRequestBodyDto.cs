namespace projects_menagment.Api.Dtos.Projects;

public sealed record AddProjectMemberRequestBodyDto(
    Guid UserId,
    string? Role);
