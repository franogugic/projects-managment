namespace projects_menagment.Api.Dtos.Projects;

public sealed record RemoveProjectMemberResponseBodyDto(
    Guid ProjectId,
    Guid UserId,
    bool Removed);
