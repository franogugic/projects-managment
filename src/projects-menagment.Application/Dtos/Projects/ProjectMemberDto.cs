namespace projects_menagment.Application.Dtos.Projects;

public sealed record ProjectMemberDto(
    Guid UserId,
    string FirstName,
    string LastName,
    string Role,
    DateTime AddedAt);
