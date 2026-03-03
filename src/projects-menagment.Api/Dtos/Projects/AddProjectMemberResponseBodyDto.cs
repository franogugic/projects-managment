namespace projects_menagment.Api.Dtos.Projects;

public sealed record AddProjectMemberResponseBodyDto(
    Guid Id,
    Guid ProjectId,
    Guid UserId,
    string Role,
    DateTime CreatedAt);
