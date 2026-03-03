using projects_menagment.Application.Dtos.Projects;
using projects_menagment.Domain.Entities;
using projects_menagment.Domain.Enums;

namespace projects_menagment.Application.Interfaces.Repositories;

public interface IProjectMemberRepository
{
    Task<bool> ExistsAsync(Guid projectId, Guid userId, CancellationToken cancellationToken);
    Task AddAsync(ProjectMember member, CancellationToken cancellationToken);
    Task<ProjectMember?> GetForUpdateAsync(Guid projectId, Guid userId, CancellationToken cancellationToken);
    Task UpdateAsync(ProjectMember member, CancellationToken cancellationToken);
    Task RemoveAsync(ProjectMember member, CancellationToken cancellationToken);
    Task<int> CountByProjectIdAndRoleAsync(Guid projectId, ProjectMemberRole role, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ProjectMemberDto>> GetByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken);
}
