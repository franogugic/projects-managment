using projects_menagment.Application.Dtos.Projects;

namespace projects_menagment.Application.Interfaces.Services;

public interface IProjectService
{
    Task<CreateProjectResponseDto> CreateAsync(
        Guid organizationId,
        CreateProjectRequestDto request,
        Guid createdByUserId,
        CancellationToken cancellationToken);
}
