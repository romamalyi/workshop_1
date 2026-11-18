using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class ReplaceRequiredAchievementWithActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "required_achievement",
                table: "promotion_criteria");

            migrationBuilder.AddColumn<int>(
                name: "required_activity_id",
                table: "promotion_criteria",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 7,
                column: "required_activity_id",
                value: 2);

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 8,
                column: "required_activity_id",
                value: 3);

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 10,
                column: "required_activity_id",
                value: 3);

            migrationBuilder.CreateIndex(
                name: "ix_promotion_criteria_required_activity_id",
                table: "promotion_criteria",
                column: "required_activity_id");

            migrationBuilder.AddForeignKey(
                name: "fk_promotion_criteria_activities_required_activity_id",
                table: "promotion_criteria",
                column: "required_activity_id",
                principalTable: "activities",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_promotion_criteria_activities_required_activity_id",
                table: "promotion_criteria");

            migrationBuilder.DropIndex(
                name: "ix_promotion_criteria_required_activity_id",
                table: "promotion_criteria");

            migrationBuilder.DropColumn(
                name: "required_activity_id",
                table: "promotion_criteria");

            migrationBuilder.AddColumn<string>(
                name: "required_achievement",
                table: "promotion_criteria",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 7,
                column: "required_achievement",
                value: "Microsoft Azure Developer - Associate (AZ-204) or Microsoft Azure Administrator Associate (AZ-104)");

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 8,
                column: "required_achievement",
                value: "Microsoft Certified: DevOps Engineer Expert (AZ-400)");

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 10,
                column: "required_achievement",
                value: "Microsoft Certified: Azure Solutions Architect Expert (AZ-305)");
        }
    }
}
