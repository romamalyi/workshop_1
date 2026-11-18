using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class RetireClosedSuccessfulStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A successful promotion now ends at Sent, so the ClosedSuccessful value is gone from the enum.
            // Historical rows must be rewritten before the new code reads them, or the string no longer maps.
            migrationBuilder.Sql("UPDATE promotions SET status = 'Sent' WHERE status = 'ClosedSuccessful';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not reversible: once rewritten, those rows are indistinguishable from promotions that were sent.
        }
    }
}
