using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class RefactorPromotionCriteria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "work_experience",
                table: "promotion_criteria");

            migrationBuilder.AddColumn<int>(
                name: "work_experience_from_months",
                table: "promotion_criteria",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "work_experience_to_months",
                table: "promotion_criteria",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "work_experience_from_months", "work_experience_to_months" },
                values: new object[] { 0, 3 });

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 2,
                columns: new[] { "work_experience_from_months", "work_experience_to_months" },
                values: new object[] { 3, 6 });

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 3,
                columns: new[] { "work_experience_from_months", "work_experience_to_months" },
                values: new object[] { 6, 12 });

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 4,
                columns: new[] { "work_experience_from_months", "work_experience_to_months" },
                values: new object[] { 12, 18 });

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 5,
                columns: new[] { "english_level", "work_experience_from_months", "work_experience_to_months" },
                values: new object[] { "IntermediateStrong", 18, 24 });

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 6,
                columns: new[] { "english_level", "work_experience_from_months", "work_experience_to_months" },
                values: new object[] { "IntermediateStrong", 24, 30 });

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 7,
                columns: new[] { "english_level", "work_experience_from_months", "work_experience_to_months" },
                values: new object[] { "IntermediateStrong", 30, 36 });

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 8,
                columns: new[] { "english_level", "work_experience_from_months", "work_experience_to_months" },
                values: new object[] { "UpperIntermediate", 36, 42 });

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 9,
                columns: new[] { "english_level", "work_experience_from_months", "work_experience_to_months" },
                values: new object[] { "UpperIntermediate", 42, 48 });

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 10,
                columns: new[] { "english_level", "work_experience_from_months", "work_experience_to_months" },
                values: new object[] { "UpperIntermediate", 48, 54 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "work_experience_from_months",
                table: "promotion_criteria");

            migrationBuilder.DropColumn(
                name: "work_experience_to_months",
                table: "promotion_criteria");

            migrationBuilder.AddColumn<string>(
                name: "work_experience",
                table: "promotion_criteria",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 1,
                column: "work_experience",
                value: "0-3m");

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 2,
                column: "work_experience",
                value: "3-6m");

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 3,
                column: "work_experience",
                value: "6-12m");

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 4,
                column: "work_experience",
                value: "1-1.5y");

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 5,
                columns: new[] { "english_level", "work_experience" },
                values: new object[] { "IntStrong", "1.5-2y" });

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 6,
                columns: new[] { "english_level", "work_experience" },
                values: new object[] { "IntStrong", "2-2.5y" });

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 7,
                columns: new[] { "english_level", "work_experience" },
                values: new object[] { "IntStrong", "2.5-3y" });

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 8,
                columns: new[] { "english_level", "work_experience" },
                values: new object[] { "UpperInt", "3-3.5y" });

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 9,
                columns: new[] { "english_level", "work_experience" },
                values: new object[] { "UpperInt", "3.5-4y" });

            migrationBuilder.UpdateData(
                table: "promotion_criteria",
                keyColumn: "id",
                keyValue: 10,
                columns: new[] { "english_level", "work_experience" },
                values: new object[] { "UpperInt", "4-4.5y" });
        }
    }
}
