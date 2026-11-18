using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddPromotionTechnologyAndEmployeeExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "technology",
                table: "promotions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Dotnet");

            migrationBuilder.AddColumn<DateTime>(
                name: "reference_date",
                table: "promotions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.DropTable(name: "promotion_level_requirements");

            migrationBuilder.CreateTable(
                name: "employee_extensions",
                columns: table => new
                {
                    employee_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    level_start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_extensions", x => x.employee_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "technology", table: "promotions");
            migrationBuilder.DropColumn(name: "reference_date", table: "promotions");
            migrationBuilder.DropTable(name: "employee_extensions");
        }
    }
}
