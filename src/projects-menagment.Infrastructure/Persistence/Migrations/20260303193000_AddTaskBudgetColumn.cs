using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using projects_menagment.Infrastructure.Persistence;

#nullable disable

namespace projects_menagment.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260303193000_AddTaskBudgetColumn")]
public partial class AddTaskBudgetColumn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "spent_amount",
            table: "tasks",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'tasks'
                      AND column_name = 'budget') THEN
                    EXECUTE 'UPDATE tasks SET spent_amount = budget';
                END IF;
            END
            $$;
            """);

        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'tasks'
                      AND column_name = 'budget') THEN
                    ALTER TABLE tasks DROP COLUMN budget;
                END IF;
            END
            $$;
            """);

        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'CK_tasks_budget_non_negative') THEN
                    ALTER TABLE tasks DROP CONSTRAINT "CK_tasks_budget_non_negative";
                END IF;
            END
            $$;
            """);

        migrationBuilder.AddCheckConstraint(
            name: "CK_tasks_spent_amount_non_negative",
            table: "tasks",
            sql: "spent_amount >= 0");

        migrationBuilder.AddColumn<decimal>(
            name: "total_spent_amount",
            table: "projects",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddCheckConstraint(
            name: "CK_projects_total_spent_amount_non_negative",
            table: "projects",
            sql: "total_spent_amount >= 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_projects_total_spent_amount_non_negative",
            table: "projects");

        migrationBuilder.DropColumn(
            name: "total_spent_amount",
            table: "projects");

        migrationBuilder.DropCheckConstraint(
            name: "CK_tasks_spent_amount_non_negative",
            table: "tasks");

        migrationBuilder.DropColumn(
            name: "spent_amount",
            table: "tasks");

        migrationBuilder.AddColumn<decimal>(
            name: "budget",
            table: "tasks",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddCheckConstraint(
            name: "CK_tasks_budget_non_negative",
            table: "tasks",
            sql: "budget >= 0");
    }
}
