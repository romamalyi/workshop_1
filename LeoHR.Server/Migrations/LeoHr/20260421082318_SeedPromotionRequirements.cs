using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class SeedPromotionRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "promotion_level_requirements",
                columns: table => new
                {
                    id          = table.Column<int>(type: "integer", nullable: false)
                                       .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    from_role   = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    to_role     = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order  = table.Column<int>(type: "integer", nullable: false),
                    type        = table.Column<string>(type: "character varying(30)",  maxLength: 30,  nullable: false),
                    title       = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    min_points  = table.Column<int>(type: "integer", nullable: true),
                },
                constraints: table => table.PrimaryKey("pk_promotion_level_requirements", x => x.id));

            migrationBuilder.CreateIndex(
                name: "ix_promotion_level_requirements_from_role_to_role",
                table: "promotion_level_requirements",
                columns: ["from_role", "to_role"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "promotion_level_requirements");
        }
    }
}
