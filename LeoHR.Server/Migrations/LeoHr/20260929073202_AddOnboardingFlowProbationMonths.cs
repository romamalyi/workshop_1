using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddOnboardingFlowProbationMonths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "probation_months",
                table: "onboarding_flows",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Existing flows never stored their probation period, so they take the current setting.
            migrationBuilder.Sql(
                "UPDATE onboarding_flows SET probation_months = " +
                "COALESCE((SELECT probation_months FROM onboarding_settings WHERE id = 1), 3);");

            migrationBuilder.Sql("ALTER TABLE onboarding_flows ALTER COLUMN probation_months DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "probation_months",
                table: "onboarding_flows");
        }
    }
}
