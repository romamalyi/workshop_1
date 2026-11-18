using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class BackfillLeobitAcademyActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Leobit Academy (leobit.rnd-hub.web) is the only integration that recorded activities so far.
            migrationBuilder.Sql(
                "UPDATE employee_activities SET approved_by_name = 'Leobit Academy' " +
                "WHERE approved_by_id IS NULL AND approved_by_name = 'External integration';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE employee_activities SET approved_by_name = 'External integration' " +
                "WHERE approved_by_id IS NULL AND approved_by_name = 'Leobit Academy';");
        }
    }
}
