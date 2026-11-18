using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class StopSeedingActivitiesAndPromotionCriteria : Migration
    {
        // Intentionally a no-op: EF's auto-generated body would delete/reinsert the now-unowned
        // activities/promotion_criteria rows, wiping out any HR edits made since the original seed.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
