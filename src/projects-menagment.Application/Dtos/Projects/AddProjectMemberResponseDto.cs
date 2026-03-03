namespace projects_menagment.Application.Dtos.Projects;

public sealed record AddProjectMemberResponseDto(
    Guid Id,
    Guid ProjectId,
    Guid UserId,
    string Role,
    DateTime CreatedAt);
