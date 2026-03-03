using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using projects_menagment.Domain.Entities;
using projects_menagment.Domain.Enums;

namespace projects_menagment.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(project => project.Id);
        builder.Property(project => project.Id)
            .HasColumnName("id");

        builder.Property(project => project.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(project => project.Name)
            .HasColumnName("name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(project => project.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(project => project.Deadline)
            .HasColumnName("deadline")
            .HasColumnType("timestamp with time zone");

        builder.Property(project => project.Budget)
            .HasColumnName("budget")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(project => project.Status)
            .HasColumnName("status")
            .HasConversion(
                value => value.ToString().ToUpperInvariant(),
                dbValue => ParseStatus(dbValue))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(project => project.TotalTasksCount)
            .HasColumnName("total_tasks_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(project => project.FinishedTasksCount)
            .HasColumnName("finished_tasks_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(project => project.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(project => project.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(project => project.IsArchived)
            .HasColumnName("is_archived")
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(project => project.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(project => project.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(project => project.OrganizationId);
        builder.HasIndex(project => project.CreatedByUserId);
        builder.HasIndex(project => new { project.OrganizationId, project.Status, project.IsArchived })
            .HasDatabaseName("IX_projects_org_status_archived");

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_projects_budget_non_negative", "budget >= 0");
            tableBuilder.HasCheckConstraint("CK_projects_total_tasks_non_negative", "total_tasks_count >= 0");
            tableBuilder.HasCheckConstraint("CK_projects_finished_tasks_non_negative", "finished_tasks_count >= 0");
            tableBuilder.HasCheckConstraint("CK_projects_finished_lte_total", "finished_tasks_count <= total_tasks_count");
        });
    }

    private static ProjectStatus ParseStatus(string dbValue)
    {
        return dbValue.Trim().ToUpperInvariant() switch
        {
            "PLANNED" => ProjectStatus.Planned,
            "IN_PROGRESS" => ProjectStatus.InProgress,
            "ON_HOLD" => ProjectStatus.OnHold,
            "COMPLETED" => ProjectStatus.Completed,
            "CANCELLED" => ProjectStatus.Cancelled,
            _ => throw new InvalidOperationException($"Unsupported project status value '{dbValue}'.")
        };
    }
}
