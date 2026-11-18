using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddEmployeeActivityStatusCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOT VALID skips existing rows — safe to add even with legacy invalid data; new writes are constrained immediately.
            migrationBuilder.Sql(
                "ALTER TABLE employee_activities ADD CONSTRAINT ck_employee_activities_status " +
                "CHECK (status IN ('Planned', 'SelfReported', 'Approved')) NOT VALID;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE employee_activities DROP CONSTRAINT ck_employee_activities_status;");
        }
    }
}
