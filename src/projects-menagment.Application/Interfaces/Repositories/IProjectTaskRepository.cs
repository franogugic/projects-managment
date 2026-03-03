using projects_menagment.Domain.Entities;

namespace projects_menagment.Application.Interfaces.Repositories;

public interface IProjectTaskRepository
{
    Task AddAsync(ProjectTask task, CancellationToken cancellationToken);
}
