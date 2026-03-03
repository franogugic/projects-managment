using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projects_menagment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    budget = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    total_tasks_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    finished_tasks_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                    table.CheckConstraint("CK_projects_budget_non_negative", "budget >= 0");
                    table.CheckConstraint("CK_projects_finished_lte_total", "finished_tasks_count <= total_tasks_count");
                    table.CheckConstraint("CK_projects_finished_tasks_non_negative", "finished_tasks_count >= 0");
                    table.CheckConstraint("CK_projects_total_tasks_non_negative", "total_tasks_count >= 0");
                    table.ForeignKey(
                        name: "FK_projects_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_projects_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_projects_created_by_user_id",
                table: "projects",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_projects_org_status_archived",
                table: "projects",
                columns: new[] { "organization_id", "status", "is_archived" });

            migrationBuilder.CreateIndex(
                name: "IX_projects_organization_id",
                table: "projects",
                column: "organization_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "projects");
        }
    }
}
