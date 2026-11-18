using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class RemoveNotActiveAndClosedPromotionStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "promotions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE promotions SET status = 'InProgress' WHERE status = 'NotActive';");
            // Same cleanup as deleting a promotion in the app: ON DELETE SET NULL would turn these into standalone activities.
            migrationBuilder.Sql("""
                DELETE FROM employee_activities
                WHERE carried_from_promotion_id IN (SELECT id FROM promotions WHERE status = 'ClosedUnsuccessful')
                   OR (promotion_id IN (SELECT id FROM promotions WHERE status = 'ClosedUnsuccessful')
                       AND (carried_from_promotion_id IS NOT NULL OR status = 'Planned'));
                """);
            migrationBuilder.Sql("DELETE FROM promotions WHERE status = 'ClosedUnsuccessful';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data changes in Up() are not reversible: deleted Closed promotions and the former NotActive statuses are not restored.
            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "promotions");
        }
    }
}
