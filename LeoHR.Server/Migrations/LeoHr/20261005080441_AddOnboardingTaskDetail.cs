using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddOnboardingTaskDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "note_value",
                table: "onboarding_flow_task_fields",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "person_id",
                table: "onboarding_flow_task_fields",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "template_field_id",
                table: "onboarding_flow_task_fields",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "onboarding_flow_task_checked_items",
                columns: table => new
                {
                    task_field_id = table.Column<int>(type: "integer", nullable: false),
                    checklist_item_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_flow_task_checked_items", x => new { x.task_field_id, x.checklist_item_id });
                    table.ForeignKey(
                        name: "fk_onboarding_flow_task_checked_items_onboarding_flow_task_fie",
                        column: x => x.task_field_id,
                        principalTable: "onboarding_flow_task_fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_onboarding_flow_task_checked_items_onboarding_task_template",
                        column: x => x.checklist_item_id,
                        principalTable: "onboarding_task_template_checklist_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_flow_task_fields_template_field_id",
                table: "onboarding_flow_task_fields",
                column: "template_field_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_flow_task_checked_items_checklist_item_id",
                table: "onboarding_flow_task_checked_items",
                column: "checklist_item_id");

            migrationBuilder.AddForeignKey(
                name: "fk_onboarding_flow_task_fields_onboarding_task_template_fields",
                table: "onboarding_flow_task_fields",
                column: "template_field_id",
                principalTable: "onboarding_task_template_fields",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("""
                UPDATE onboarding_flow_task_fields tf
                SET template_field_id = f.id
                FROM onboarding_flow_tasks t
                JOIN onboarding_task_template_fields f ON f.template_id = t.template_id
                WHERE tf.task_id = t.id
                  AND f.sort_order = tf.sort_order
                  AND f.field_type = tf.field_type;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_onboarding_flow_task_fields_onboarding_task_template_fields",
                table: "onboarding_flow_task_fields");

            migrationBuilder.DropTable(
                name: "onboarding_flow_task_checked_items");

            migrationBuilder.DropIndex(
                name: "ix_onboarding_flow_task_fields_template_field_id",
                table: "onboarding_flow_task_fields");

            migrationBuilder.DropColumn(
                name: "note_value",
                table: "onboarding_flow_task_fields");

            migrationBuilder.DropColumn(
                name: "person_id",
                table: "onboarding_flow_task_fields");

            migrationBuilder.DropColumn(
                name: "template_field_id",
                table: "onboarding_flow_task_fields");
        }
    }
}
