using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using projects_menagment.Domain.Entities;
using projects_menagment.Domain.Enums;

namespace projects_menagment.Infrastructure.Persistence.Configurations;

public sealed class ProjectTaskConfiguration : IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        builder.ToTable("tasks");

        builder.HasKey(task => task.Id);
        builder.Property(task => task.Id)
            .HasColumnName("id");

        builder.Property(task => task.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(task => task.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(task => task.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(task => task.Status)
            .HasColumnName("status")
            .HasConversion(
                value => value.ToString().ToUpperInvariant(),
                dbValue => ParseStatus(dbValue))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(task => task.Priority)
            .HasColumnName("priority")
            .HasConversion(
                value => value.ToString().ToUpperInvariant(),
                dbValue => ParsePriority(dbValue))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(task => task.SpentAmount)
            .HasColumnName("spent_amount")
            .HasColumnType("numeric(18,2)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(task => task.AssigneeUserId)
            .HasColumnName("assignee_user_id");

        builder.Property(task => task.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("timestamp with time zone");

        builder.Property(task => task.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(task => task.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(task => task.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(task => task.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(task => task.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(task => task.CompletedByUserId)
            .HasColumnName("completed_by_user_id");

        builder.Property(task => task.CompletionNote)
            .HasColumnName("completion_note")
            .HasColumnType("text");

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(task => task.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(task => task.AssigneeUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(task => task.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(task => task.CompletedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(task => task.ProjectId);
        builder.HasIndex(task => task.AssigneeUserId);
        builder.HasIndex(task => task.DueDate);
        builder.HasIndex(task => new { task.ProjectId, task.Status })
            .HasDatabaseName("IX_tasks_project_status");

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_tasks_status", "status IN ('TODO', 'IN_PROGRESS', 'DONE')");
            tableBuilder.HasCheckConstraint("CK_tasks_priority", "priority IN ('LOW', 'MEDIUM', 'HIGH')");
            tableBuilder.HasCheckConstraint("CK_tasks_spent_amount_non_negative", "spent_amount >= 0");
        });
    }

    private static ProjectTaskStatus ParseStatus(string dbValue)
    {
        return dbValue.Trim().ToUpperInvariant() switch
        {
            "TODO" => ProjectTaskStatus.Todo,
            "IN_PROGRESS" => ProjectTaskStatus.InProgress,
            "INPROGRESS" => ProjectTaskStatus.InProgress,
            "DONE" => ProjectTaskStatus.Done,
            _ => throw new InvalidOperationException($"Unsupported task status value '{dbValue}'.")
        };
    }

    private static TaskPriority ParsePriority(string dbValue)
    {
        return dbValue.Trim().ToUpperInvariant() switch
        {
            "LOW" => TaskPriority.Low,
            "MEDIUM" => TaskPriority.Medium,
            "HIGH" => TaskPriority.High,
            _ => throw new InvalidOperationException($"Unsupported task priority value '{dbValue}'.")
        };
    }
}
