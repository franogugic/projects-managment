using projects_menagment.Domain.Entities;
using projects_menagment.Application.Dtos.Tasks;

namespace projects_menagment.Application.Interfaces.Repositories;

public interface IProjectTaskRepository
{
    Task AddAsync(ProjectTask task, CancellationToken cancellationToken);
    Task<ProjectTask?> GetForUpdateByIdAsync(Guid taskId, CancellationToken cancellationToken);
    Task UpdateAsync(ProjectTask task, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ProjectTaskListItemDto>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken);
}
