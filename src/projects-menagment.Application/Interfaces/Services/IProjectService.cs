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

    Task<AddProjectMemberResponseDto> AddMemberAsync(
        Guid organizationId,
        AddProjectMemberRequestDto request,
        Guid requestUserId,
        CancellationToken cancellationToken);
}
