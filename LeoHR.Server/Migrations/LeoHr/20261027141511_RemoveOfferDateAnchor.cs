using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class RemoveOfferDateAnchor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Flow tasks never got this anchor (such templates were skipped), so only templates change; the days and direction stay.
            migrationBuilder.Sql("UPDATE onboarding_task_templates SET due_anchor = 'StartDate' WHERE due_anchor = 'OfferDate';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data changes in Up() are not reversible: templates that used the offer date stay on the start date.
        }
    }
}
