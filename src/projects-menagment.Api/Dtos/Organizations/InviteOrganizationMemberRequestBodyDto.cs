namespace projects_menagment.Api.Dtos.Organizations;

public sealed record InviteOrganizationMemberRequestBodyDto(
    string? Email,
    string? Role);
