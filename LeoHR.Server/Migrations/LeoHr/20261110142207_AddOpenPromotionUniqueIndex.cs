using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddOpenPromotionUniqueIndex : Migration
    {
        // Per employee keeps the Ready promotion, otherwise the oldest; the rest are soft-deleted by the system
        // (never hard-deleted: that would turn carry-over clones back into standalone activities).
        public const string SoftDeleteDuplicateOpenPromotionsSql = """
            UPDATE promotions AS p
            SET is_deleted = true, updated_at = now(), updated_by = NULL
            FROM (
                SELECT id, row_number() OVER (
                    PARTITION BY employee_id ORDER BY (status = 'Ready') DESC, id) AS rank
                FROM promotions
                WHERE status IN ('InProgress', 'Ready') AND is_deleted = false
            ) AS ranked
            WHERE p.id = ranked.id AND ranked.rank > 1;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(SoftDeleteDuplicateOpenPromotionsSql);

            migrationBuilder.CreateIndex(
                name: "ix_promotions_employee_id_open",
                table: "promotions",
                column: "employee_id",
                unique: true,
                filter: "\"status\" IN ('InProgress', 'Ready') AND \"is_deleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_promotions_employee_id_open",
                table: "promotions");
        }
    }
}
