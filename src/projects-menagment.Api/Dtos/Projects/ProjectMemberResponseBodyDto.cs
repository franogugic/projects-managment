namespace projects_menagment.Api.Dtos.Projects;

public sealed record ProjectMemberResponseBodyDto(
    Guid UserId,
    string FirstName,
    string LastName,
    string Role,
    DateTime AddedAt);
