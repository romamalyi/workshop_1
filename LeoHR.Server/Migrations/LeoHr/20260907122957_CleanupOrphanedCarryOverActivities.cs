using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class CleanupOrphanedCarryOverActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Deactivate carry-over clones left behind by deleted promotions. Their target promotion is
            // gone (promotion_id is null via ON DELETE SET NULL) but the derived row survived, so
            // repeated create/delete cycles piled up duplicates on the employee's activity list.
            // Soft delete (is_active = false) for consistency with the rest of employee_activities
            // and to keep the audit trail.
            migrationBuilder.Sql("""
                UPDATE employee_activities
                SET is_active = false, updated_at = now()
                WHERE carried_from_promotion_id IS NOT NULL
                  AND promotion_id IS NULL
                  AND is_active;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data cleanup — nothing to roll back.
        }
    }
}
