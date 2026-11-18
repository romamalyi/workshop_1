using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddResponsiblePositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "responsible_position",
                table: "onboarding_flows",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "responsible_positions",
                columns: table => new
                {
                    position = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_responsible_positions", x => x.position);
                });

            migrationBuilder.Sql("INSERT INTO responsible_positions (position) VALUES ('HR Assistant');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "responsible_positions");

            migrationBuilder.DropColumn(
                name: "responsible_position",
                table: "onboarding_flows");
        }
    }
}
