using Microsoft.EntityFrameworkCore;
using projects_menagment.Application.Dtos.Projects;
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

    public async Task<IReadOnlyCollection<ProjectMemberDto>> GetByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var members = await dbContext.ProjectMembers
            .AsNoTracking()
            .Where(member => member.ProjectId == projectId)
            .Join(
                dbContext.Users.AsNoTracking(),
                member => member.UserId,
                user => user.Id,
                (member, user) => new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    member.Role,
                    member.CreatedAt
                })
            .OrderBy(member => member.FirstName)
            .ThenBy(member => member.LastName)
            .ToListAsync(cancellationToken);

        return members
            .Select(member => new ProjectMemberDto(
                member.Id,
                member.FirstName,
                member.LastName,
                member.Role.ToString().ToUpperInvariant(),
                member.CreatedAt))
            .ToList();
    }
}
