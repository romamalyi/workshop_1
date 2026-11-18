using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddOnboardingFlowTaskFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "onboarding_flow_task_fields",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    task_id = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    field_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_flow_task_fields", x => x.id);
                    table.ForeignKey(
                        name: "fk_onboarding_flow_task_fields_onboarding_flow_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "onboarding_flow_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_flow_task_fields_task_id",
                table: "onboarding_flow_task_fields",
                column: "task_id");

            // Existing tasks never stored their fields, so they take their template's current ones.
            migrationBuilder.Sql("""
                INSERT INTO onboarding_flow_task_fields (task_id, sort_order, field_type, label)
                SELECT t.id, f.sort_order, f.field_type, f.label
                FROM onboarding_flow_tasks t
                JOIN onboarding_task_template_fields f ON f.template_id = t.template_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "onboarding_flow_task_fields");
        }
    }
}
