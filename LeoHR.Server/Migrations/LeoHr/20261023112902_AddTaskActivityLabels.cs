using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddTaskActivityLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "checklist_item_text",
                table: "onboarding_flow_task_activities",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "field_label",
                table: "onboarding_flow_task_activities",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "checklist_item_text",
                table: "onboarding_flow_task_activities");

            migrationBuilder.DropColumn(
                name: "field_label",
                table: "onboarding_flow_task_activities");
        }
    }
}
