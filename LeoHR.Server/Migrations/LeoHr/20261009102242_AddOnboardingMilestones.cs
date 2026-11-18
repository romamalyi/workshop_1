using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddOnboardingMilestones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "onboarding_milestones",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_milestones", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_milestones_name",
                table: "onboarding_milestones",
                column: "name",
                unique: true);

            // Admin-editable later, so seeded here instead of HasData.
            migrationBuilder.Sql("""
                INSERT INTO onboarding_milestones (name, sort_order) VALUES
                    ('Preboarding', 1),
                    ('First Day', 2),
                    ('Learning & Orientation', 3),
                    ('Team Integration', 4),
                    ('Adaptation', 5),
                    ('Final Review', 6),
                    ('Onboarding Completed', 7)
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.AddColumn<int>(
                name: "milestone_id",
                table: "onboarding_task_templates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "milestone_id",
                table: "onboarding_flow_tasks",
                type: "integer",
                nullable: true);

            // Existing rows predate milestones, so any value will do: they all get the first one.
            migrationBuilder.Sql("""
                UPDATE onboarding_task_templates SET milestone_id = (SELECT id FROM onboarding_milestones ORDER BY sort_order LIMIT 1);
                UPDATE onboarding_flow_tasks SET milestone_id = (SELECT id FROM onboarding_milestones ORDER BY sort_order LIMIT 1);
                """);

            migrationBuilder.AlterColumn<int>(
                name: "milestone_id",
                table: "onboarding_task_templates",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "milestone_id",
                table: "onboarding_flow_tasks",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_task_templates_milestone_id",
                table: "onboarding_task_templates",
                column: "milestone_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_flow_tasks_milestone_id",
                table: "onboarding_flow_tasks",
                column: "milestone_id");

            migrationBuilder.AddForeignKey(
                name: "fk_onboarding_flow_tasks_onboarding_milestones_milestone_id",
                table: "onboarding_flow_tasks",
                column: "milestone_id",
                principalTable: "onboarding_milestones",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_onboarding_task_templates_onboarding_milestones_milestone_id",
                table: "onboarding_task_templates",
                column: "milestone_id",
                principalTable: "onboarding_milestones",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_onboarding_flow_tasks_onboarding_milestones_milestone_id",
                table: "onboarding_flow_tasks");

            migrationBuilder.DropForeignKey(
                name: "fk_onboarding_task_templates_onboarding_milestones_milestone_id",
                table: "onboarding_task_templates");

            migrationBuilder.DropIndex(
                name: "ix_onboarding_task_templates_milestone_id",
                table: "onboarding_task_templates");

            migrationBuilder.DropIndex(
                name: "ix_onboarding_flow_tasks_milestone_id",
                table: "onboarding_flow_tasks");

            migrationBuilder.DropColumn(
                name: "milestone_id",
                table: "onboarding_task_templates");

            migrationBuilder.DropColumn(
                name: "milestone_id",
                table: "onboarding_flow_tasks");

            migrationBuilder.DropTable(
                name: "onboarding_milestones");
        }
    }
}
