using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddPromotionHolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "promotion_holder_id",
                table: "promotions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "sh");

            migrationBuilder.Sql(
                "ALTER TABLE promotions ALTER COLUMN promotion_holder_id DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "promotion_holder_id",
                table: "promotions");
        }
    }
}
