using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddPromotionLevelStartSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "level_start_date",
                table: "promotions",
                type: "timestamp with time zone",
                nullable: true);

            // Backfill: for each existing promotion, snapshot the boundary that was in effect while it
            // was active — the close time of the most recent earlier Sent/ClosedSuccessful promotion
            // for the same employee. Uses COALESCE(reference_date, created_at) to match the runtime
            // (GetLastSuccessfulPromotion / CarryOverSurplusPoints), so a legacy row with a null
            // reference_date still contributes a boundary instead of leaving the successor null.
            // No earlier promotion => leave null (all "Overall in level" activities count, matching
            // pre-change behaviour for a first promotion).
            migrationBuilder.Sql("""
                UPDATE promotions p
                SET level_start_date = (
                    SELECT COALESCE(p2.reference_date, p2.created_at)
                    FROM promotions p2
                    WHERE p2.employee_id = p.employee_id
                      AND p2.id <> p.id
                      AND p2.status IN ('Sent', 'ClosedSuccessful')
                      AND COALESCE(p2.reference_date, p2.created_at) < COALESCE(p.reference_date, p.created_at)
                    ORDER BY COALESCE(p2.reference_date, p2.created_at) DESC
                    LIMIT 1
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "level_start_date",
                table: "promotions");
        }
    }
}
