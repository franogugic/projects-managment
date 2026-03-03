namespace projects_menagment.Application.Dtos.Projects;

public sealed record RemoveProjectMemberResponseDto(
    Guid ProjectId,
    Guid UserId,
    bool Removed);
