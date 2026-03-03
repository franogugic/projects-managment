using projects_menagment.Application.Dtos.Projects;

namespace projects_menagment.Application.Interfaces.Services;

public interface IProjectService
{
    Task<CreateProjectResponseDto> CreateAsync(
        Guid organizationId,
        CreateProjectRequestDto request,
        Guid createdByUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProjectListItemDto>> GetByOrganizationIdAsync(
        Guid organizationId,
        Guid requestUserId,
        CancellationToken cancellationToken);

    Task<UpdateProjectResponseDto> UpdateAsync(
        Guid organizationId,
        UpdateProjectRequestDto request,
        Guid requestUserId,
        CancellationToken cancellationToken);

    Task<ArchiveProjectResponseDto> ArchiveAsync(
        Guid organizationId,
        Guid projectId,
        Guid requestUserId,
        CancellationToken cancellationToken);

    Task<AddProjectMemberResponseDto> AddMemberAsync(
        Guid organizationId,
        AddProjectMemberRequestDto request,
        Guid requestUserId,
        CancellationToken cancellationToken);

    Task<UpdateProjectMemberRoleResponseDto> UpdateMemberRoleAsync(
        Guid organizationId,
        UpdateProjectMemberRoleRequestDto request,
        Guid requestUserId,
        CancellationToken cancellationToken);

    Task<RemoveProjectMemberResponseDto> RemoveMemberAsync(
        Guid organizationId,
        Guid projectId,
        Guid userId,
        Guid requestUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProjectMemberDto>> GetMembersAsync(
        Guid organizationId,
        Guid projectId,
        Guid requestUserId,
        CancellationToken cancellationToken);
}
