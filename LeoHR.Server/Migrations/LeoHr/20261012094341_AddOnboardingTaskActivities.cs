using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddOnboardingTaskActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "onboarding_flow_task_activities",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    task_id = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    from_assignee_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    to_assignee_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_flow_task_activities", x => x.id);
                    table.ForeignKey(
                        name: "fk_onboarding_flow_task_activities_onboarding_flow_tasks_task_",
                        column: x => x.task_id,
                        principalTable: "onboarding_flow_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_flow_task_activities_task_id",
                table: "onboarding_flow_task_activities",
                column: "task_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "onboarding_flow_task_activities");
        }
    }
}
