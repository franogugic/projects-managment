using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using projects_menagment.Domain.Entities;
using projects_menagment.Domain.Enums;

namespace projects_menagment.Infrastructure.Persistence.Configurations;

public sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("project_members");

        builder.HasKey(member => member.Id);

        builder.Property(member => member.Id)
            .HasColumnName("id");

        builder.Property(member => member.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(member => member.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(member => member.Role)
            .HasColumnName("role")
            .HasConversion(
                role => role.ToString().ToUpperInvariant(),
                dbValue => ParseProjectMemberRole(dbValue))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(member => member.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(member => member.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(member => member.ProjectId);
        builder.HasIndex(member => member.UserId);
        builder.HasIndex(member => new { member.ProjectId, member.UserId })
            .IsUnique();
    }

    private static ProjectMemberRole ParseProjectMemberRole(string dbValue)
    {
        return dbValue.Trim().ToUpperInvariant() switch
        {
            "MENAGER" => ProjectMemberRole.Menager,
            "MANAGER" => ProjectMemberRole.Menager,
            "EMPLOYEE" => ProjectMemberRole.Employee,
            _ => throw new InvalidOperationException($"Unsupported project member role value '{dbValue}'.")
        };
    }
}
