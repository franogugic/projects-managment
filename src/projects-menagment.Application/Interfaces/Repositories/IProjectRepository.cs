using projects_menagment.Application.Dtos.Projects;
using projects_menagment.Domain.Entities;

namespace projects_menagment.Application.Interfaces.Repositories;

public interface IProjectRepository
{
    Task AddAsync(Project project, CancellationToken cancellationToken);
    Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken);
    Task<int> CountActiveByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ProjectListItemDto>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken);
}
