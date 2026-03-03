using projects_menagment.Application.Dtos.Organizations;

namespace projects_menagment.Application.Interfaces.Services;

public interface IOrganizationService
{
    Task<CreateOrganizationResponseDto> CreateAsync(CreateOrganizationRequestDto request, Guid createdByUserId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<UserOrganizationDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<InviteOrganizationMemberResponseDto> InviteMemberAsync(InviteOrganizationMemberRequestDto request, Guid invitedByUserId, CancellationToken cancellationToken);
    Task<OrganizationInvitationPreviewDto> GetInvitationPreviewAsync(string token, CancellationToken cancellationToken);
    Task<AcceptOrganizationInvitationResponseDto> AcceptInvitationAsync(AcceptOrganizationInvitationRequestDto request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<OrganizationMemberDto>> GetOrganizationMembersAsync(Guid organizationId, Guid requestUserId, CancellationToken cancellationToken);
}
