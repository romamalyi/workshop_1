using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    public partial class AddHistorySettingsToAppSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "show_employment",
                table: "app_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "show_manager",
                table: "app_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "show_mentor",
                table: "app_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "show_payment_mode",
                table: "app_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "show_employment", table: "app_settings");
            migrationBuilder.DropColumn(name: "show_manager",    table: "app_settings");
            migrationBuilder.DropColumn(name: "show_mentor",     table: "app_settings");
            migrationBuilder.DropColumn(name: "show_payment_mode", table: "app_settings");
        }
    }
}
