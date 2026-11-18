using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class RemoveEmployeeActivityIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Deactivated rows become deletes; left in place they would count as live once the column is gone.
            migrationBuilder.Sql("DELETE FROM employee_activities WHERE NOT is_active;");
            migrationBuilder.DropColumn(
                name: "is_active",
                table: "employee_activities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The deleted rows are not restored; every remaining row is live.
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "employee_activities",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }
    }
}
