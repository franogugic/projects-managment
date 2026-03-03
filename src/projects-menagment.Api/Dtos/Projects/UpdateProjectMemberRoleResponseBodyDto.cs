namespace projects_menagment.Api.Dtos.Projects;

public sealed record UpdateProjectMemberRoleResponseBodyDto(
    Guid Id,
    Guid ProjectId,
    Guid UserId,
    string Role,
    DateTime CreatedAt);
