using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class addCriteriasDefaultValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "approved_criteria_labels",
                table: "promotions",
                type: "text",
                nullable: false,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "approved_criteria_labels",
                table: "promotions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "[]");
        }
    }
}
