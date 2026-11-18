using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddRnDActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO activities (category, name, within_period, points, is_active, created_at, updated_at)
                VALUES 
                    ('RnD', 'Claude Certified Developer – Foundations', 'OverallActive', 30, true, NOW(), NOW()),
                    ('RnD', 'Claude Certified Architect – Foundations', 'OverallActive', 40, true, NOW(), NOW()),
                    ('RnD', 'Claude Certified Architect – Professional', 'OverallActive', 50, true, NOW(), NOW()),
                    ('RnD', 'Claude Certification Bonus - until November 1 2026', 'OverallActive', 20, true, NOW(), NOW()),
                    ('RnD', 'PoC Development', 'TwelveMonths', 20, true, NOW(), NOW());
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM activities 
                WHERE category = 'RnD' 
                AND name IN (
                    'Claude Certified Developer – Foundations',
                    'Claude Certified Architect – Foundations',
                    'Claude Certified Architect – Professional',
                    'Claude Certification Bonus - until November 1 2026',
                    'PoC Development'
                );
            ");
        }
    }
}
