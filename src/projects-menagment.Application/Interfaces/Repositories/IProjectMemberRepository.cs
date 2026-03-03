using projects_menagment.Domain.Entities;

namespace projects_menagment.Application.Interfaces.Repositories;

public interface IProjectMemberRepository
{
    Task<bool> ExistsAsync(Guid projectId, Guid userId, CancellationToken cancellationToken);
    Task AddAsync(ProjectMember member, CancellationToken cancellationToken);
}
