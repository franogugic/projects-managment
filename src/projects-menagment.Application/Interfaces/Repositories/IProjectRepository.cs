using projects_menagment.Domain.Entities;

namespace projects_menagment.Application.Interfaces.Repositories;

public interface IProjectRepository
{
    Task AddAsync(Project project, CancellationToken cancellationToken);
    Task<int> CountActiveByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken);
}
