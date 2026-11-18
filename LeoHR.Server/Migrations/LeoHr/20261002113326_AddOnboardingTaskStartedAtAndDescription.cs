using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddOnboardingTaskStartedAtAndDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "onboarding_flow_tasks",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "started_at",
                table: "onboarding_flow_tasks",
                type: "timestamp with time zone",
                nullable: true);

            // Tasks generated before this column existed take the description their template has now.
            migrationBuilder.Sql(
                """
                UPDATE onboarding_flow_tasks t
                SET description = tpl.description
                FROM onboarding_task_templates tpl
                WHERE t.template_id = tpl.id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description",
                table: "onboarding_flow_tasks");

            migrationBuilder.DropColumn(
                name: "started_at",
                table: "onboarding_flow_tasks");
        }
    }
}
