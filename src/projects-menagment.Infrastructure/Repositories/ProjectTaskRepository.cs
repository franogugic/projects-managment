using projects_menagment.Application.Interfaces.Repositories;
using projects_menagment.Domain.Entities;
using projects_menagment.Infrastructure.Persistence;

namespace projects_menagment.Infrastructure.Repositories;

public sealed class ProjectTaskRepository(AppDbContext dbContext) : IProjectTaskRepository
{
    public async Task AddAsync(ProjectTask task, CancellationToken cancellationToken)
    {
        dbContext.ProjectTasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
