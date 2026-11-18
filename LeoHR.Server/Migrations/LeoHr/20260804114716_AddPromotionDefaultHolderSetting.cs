using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddPromotionDefaultHolderSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add NOT NULL with a temporary default only to backfill existing rows to the
            // setting's default, then drop it so the column carries no persistent DB-level default.
            migrationBuilder.AddColumn<string>(
                name: "promotion_default_holder",
                table: "app_settings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "sh");

            migrationBuilder.Sql(
                "ALTER TABLE app_settings ALTER COLUMN promotion_default_holder DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "promotion_default_holder",
                table: "app_settings");
        }
    }
}
