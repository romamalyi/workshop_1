using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddPromotionCriteriaIdToPromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "promotion_criteria_id",
                table: "promotions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_promotions_promotion_criteria_id",
                table: "promotions",
                column: "promotion_criteria_id");

            migrationBuilder.AddForeignKey(
                name: "fk_promotions_promotion_criteria_promotion_criteria_id",
                table: "promotions",
                column: "promotion_criteria_id",
                principalTable: "promotion_criteria",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_promotions_promotion_criteria_promotion_criteria_id",
                table: "promotions");

            migrationBuilder.DropIndex(
                name: "ix_promotions_promotion_criteria_id",
                table: "promotions");

            migrationBuilder.DropColumn(
                name: "promotion_criteria_id",
                table: "promotions");
        }
    }
}
