namespace projects_menagment.Application.Dtos.Projects;

public sealed record AddProjectMemberRequestDto(
    Guid ProjectId,
    Guid UserId,
    string? Role);
