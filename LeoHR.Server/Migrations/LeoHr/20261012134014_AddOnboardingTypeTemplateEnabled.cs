using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    public partial class AddOnboardingTypeTemplateEnabled : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE onboarding_types SET name = LEFT(name, 100);");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "onboarding_types",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<bool>(
                name: "is_enabled",
                table: "onboarding_type_templates",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_enabled",
                table: "onboarding_type_templates");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "onboarding_types",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
        }
    }
}
