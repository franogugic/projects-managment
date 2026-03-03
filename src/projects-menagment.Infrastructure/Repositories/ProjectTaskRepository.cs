using Microsoft.EntityFrameworkCore;
using projects_menagment.Application.Dtos.Tasks;
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

    public async Task<ProjectTask?> GetForUpdateByIdAsync(Guid taskId, CancellationToken cancellationToken)
    {
        return await dbContext.ProjectTasks
            .FirstOrDefaultAsync(task => task.Id == taskId && !task.IsDeleted, cancellationToken);
    }

    public async Task UpdateAsync(ProjectTask task, CancellationToken cancellationToken)
    {
        dbContext.ProjectTasks.Update(task);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProjectTaskListItemDto>> GetByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ProjectTasks
            .AsNoTracking()
            .Where(task => task.ProjectId == projectId && !task.IsDeleted)
            .OrderByDescending(task => task.CreatedAt)
            .Select(task => new ProjectTaskListItemDto(
                task.Id,
                task.ProjectId,
                task.AssigneeUserId,
                task.Title,
                task.Description,
                task.DueDate,
                task.Status.ToString().ToUpperInvariant(),
                task.Priority.ToString().ToUpperInvariant(),
                task.SpentAmount,
                task.CreatedByUserId,
                task.CreatedAt,
                task.CompletedAt,
                task.CompletionNote))
            .ToListAsync(cancellationToken);
    }
}
