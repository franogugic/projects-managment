using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projects_menagment.Application.Interfaces.Repositories;
using projects_menagment.Domain.Entities;
using projects_menagment.Infrastructure.Persistence;

namespace projects_menagment.Infrastructure.Repositories;

public sealed class ProjectRepository(
    AppDbContext dbContext,
    ILogger<ProjectRepository> logger) : IProjectRepository
{
    public async Task AddAsync(Project project, CancellationToken cancellationToken)
    {
        logger.LogDebug("Persisting project {ProjectName} for organization {OrganizationId}", project.Name, project.OrganizationId);
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountActiveByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return await dbContext.Projects
            .AsNoTracking()
            .CountAsync(project => project.OrganizationId == organizationId && !project.IsArchived, cancellationToken);
    }
}
