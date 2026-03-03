using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projects_menagment.Application.Dtos.Projects;
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

    public async Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(project => project.Id == projectId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProjectListItemDto>> GetByOrganizationIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var projects = await dbContext.Projects
            .AsNoTracking()
            .Where(project => project.OrganizationId == organizationId)
            .OrderByDescending(project => project.CreatedAt)
            .Select(project => new
            {
                project.Id,
                project.OrganizationId,
                project.Name,
                project.Description,
                project.Deadline,
                project.Budget,
                project.Status,
                project.TotalTasksCount,
                project.FinishedTasksCount,
                project.CreatedByUserId,
                project.CreatedAt,
                project.IsArchived
            })
            .ToListAsync(cancellationToken);

        return projects
            .Select(project =>
            {
                var progress = project.TotalTasksCount == 0
                    ? 0m
                    : Math.Round((decimal)project.FinishedTasksCount * 100m / project.TotalTasksCount, 2);

                return new ProjectListItemDto(
                    project.Id,
                    project.OrganizationId,
                    project.Name,
                    project.Description,
                    project.Deadline,
                    project.Budget,
                    project.Status.ToString().ToUpperInvariant(),
                    project.TotalTasksCount,
                    project.FinishedTasksCount,
                    progress,
                    project.CreatedByUserId,
                    project.CreatedAt,
                    project.IsArchived);
            })
            .ToList();
    }
}
