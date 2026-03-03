namespace projects_menagment.Application.Dtos.Projects;

public sealed record UpdateProjectMemberRoleResponseDto(
    Guid Id,
    Guid ProjectId,
    Guid UserId,
    string Role,
    DateTime CreatedAt);
