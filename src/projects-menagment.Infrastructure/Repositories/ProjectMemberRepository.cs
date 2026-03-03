using Microsoft.EntityFrameworkCore;
using projects_menagment.Application.Interfaces.Repositories;
using projects_menagment.Domain.Entities;
using projects_menagment.Infrastructure.Persistence;

namespace projects_menagment.Infrastructure.Repositories;

public sealed class ProjectMemberRepository(AppDbContext dbContext) : IProjectMemberRepository
{
    public async Task<bool> ExistsAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.ProjectMembers
            .AsNoTracking()
            .AnyAsync(member => member.ProjectId == projectId && member.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(ProjectMember member, CancellationToken cancellationToken)
    {
        dbContext.ProjectMembers.Add(member);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
