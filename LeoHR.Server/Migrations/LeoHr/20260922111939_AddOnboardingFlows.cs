using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddOnboardingFlows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "onboarding_flows",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    position = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    employment_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    responsible_person_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_flows", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "onboarding_flow_tasks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    flow_id = table.Column<int>(type: "integer", nullable: false),
                    template_id = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phase = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    responsible_role_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    due_offset_days = table.Column<int>(type: "integer", nullable: false),
                    due_direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    due_anchor = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    assignee_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_flow_tasks", x => x.id);
                    table.ForeignKey(
                        name: "fk_onboarding_flow_tasks_onboarding_flows_flow_id",
                        column: x => x.flow_id,
                        principalTable: "onboarding_flows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_onboarding_flow_tasks_onboarding_task_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "onboarding_task_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_flow_tasks_flow_id",
                table: "onboarding_flow_tasks",
                column: "flow_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_flow_tasks_template_id",
                table: "onboarding_flow_tasks",
                column: "template_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "onboarding_flow_tasks");

            migrationBuilder.DropTable(
                name: "onboarding_flows");
        }
    }
}
