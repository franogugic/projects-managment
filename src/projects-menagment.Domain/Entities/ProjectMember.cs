using projects_menagment.Domain.Enums;

namespace projects_menagment.Domain.Entities;

public sealed class ProjectMember
{
    private ProjectMember()
    {
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public ProjectMemberRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static ProjectMember Create(
        Guid projectId,
        Guid userId,
        ProjectMemberRole role = ProjectMemberRole.Employee)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id is required.", nameof(projectId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        return new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void ChangeRole(ProjectMemberRole role)
    {
        Role = role;
    }
}
