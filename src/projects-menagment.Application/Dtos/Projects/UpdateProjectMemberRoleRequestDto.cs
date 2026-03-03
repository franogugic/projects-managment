namespace projects_menagment.Application.Dtos.Projects;

public sealed record UpdateProjectMemberRoleRequestDto(
    Guid ProjectId,
    Guid UserId,
    string? Role);
