using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddCarriedFromPromotionForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_employee_activities_carried_from_promotion_id",
                table: "employee_activities",
                column: "carried_from_promotion_id");

            migrationBuilder.AddForeignKey(
                name: "fk_employee_activities_promotions_carried_from_promotion_id",
                table: "employee_activities",
                column: "carried_from_promotion_id",
                principalTable: "promotions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_employee_activities_promotions_carried_from_promotion_id",
                table: "employee_activities");

            migrationBuilder.DropIndex(
                name: "ix_employee_activities_carried_from_promotion_id",
                table: "employee_activities");
        }
    }
}
