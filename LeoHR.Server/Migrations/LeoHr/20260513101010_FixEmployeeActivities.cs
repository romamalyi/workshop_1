using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class FixEmployeeActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE employee_activities ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT TRUE;");

            migrationBuilder.Sql(
                "ALTER TABLE employee_activities ALTER COLUMN accomplished_on TYPE date USING accomplished_on::date;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE employee_activities ALTER COLUMN accomplished_on TYPE timestamp with time zone USING accomplished_on::timestamp with time zone;");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "employee_activities");
        }
    }
}
