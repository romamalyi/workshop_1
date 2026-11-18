using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class RemovePromotionSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "promotion_record_steps");

            migrationBuilder.DropTable(
                name: "promotion_steps");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "promotion_steps",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_steps", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "promotion_record_steps",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    promotion_id = table.Column<int>(type: "integer", nullable: false),
                    step_id = table.Column<int>(type: "integer", nullable: false),
                    done_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_done = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_record_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_promotion_record_steps_promotion_steps_step_id",
                        column: x => x.step_id,
                        principalTable: "promotion_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_promotion_record_steps_promotions_promotion_id",
                        column: x => x.promotion_id,
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "promotion_steps",
                columns: new[] { "id", "description", "sort_order", "title" },
                values: new object[,]
                {
                    { 1, "Gather feedback from colleagues and project leads.", 1, "Collect peer feedback" },
                    { 2, "Hold a 1:1 review meeting with the employee.", 2, "Conduct performance review" },
                    { 3, "Get sign-off from the department head.", 3, "Approve with management" },
                    { 4, "Draft the official promotion letter.", 4, "Prepare promotion letter" },
                    { 5, "Notify the team about the promotion.", 5, "Send announcement email" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_promotion_record_steps_promotion_id_step_id",
                table: "promotion_record_steps",
                columns: new[] { "promotion_id", "step_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_promotion_record_steps_step_id",
                table: "promotion_record_steps",
                column: "step_id");
        }
    }
}
