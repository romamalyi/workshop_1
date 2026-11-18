using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddOnboardingTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "onboarding_type_id",
                table: "onboarding_flows",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "onboarding_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "onboarding_type_templates",
                columns: table => new
                {
                    type_id = table.Column<int>(type: "integer", nullable: false),
                    template_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_type_templates", x => new { x.type_id, x.template_id });
                    table.ForeignKey(
                        name: "fk_onboarding_type_templates_onboarding_task_templates_templat",
                        column: x => x.template_id,
                        principalTable: "onboarding_task_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_onboarding_type_templates_onboarding_types_type_id",
                        column: x => x.type_id,
                        principalTable: "onboarding_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_flows_onboarding_type_id",
                table: "onboarding_flows",
                column: "onboarding_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_type_templates_template_id",
                table: "onboarding_type_templates",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_types_name",
                table: "onboarding_types",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_onboarding_flows_onboarding_types_onboarding_type_id",
                table: "onboarding_flows",
                column: "onboarding_type_id",
                principalTable: "onboarding_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_onboarding_flows_onboarding_types_onboarding_type_id",
                table: "onboarding_flows");

            migrationBuilder.DropTable(
                name: "onboarding_type_templates");

            migrationBuilder.DropTable(
                name: "onboarding_types");

            migrationBuilder.DropIndex(
                name: "ix_onboarding_flows_onboarding_type_id",
                table: "onboarding_flows");

            migrationBuilder.DropColumn(
                name: "onboarding_type_id",
                table: "onboarding_flows");
        }
    }
}
