namespace projects_menagment.Application.Dtos.Organizations;

public sealed record InviteOrganizationMemberRequestDto(
    Guid OrganizationId,
    string Email,
    string? Role);
