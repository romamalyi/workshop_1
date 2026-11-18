using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddEmployeeEventEmailTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Raw SQL, not HasData: HR edits this template, and a later seed change must not overwrite it.
            migrationBuilder.Sql(
                "INSERT INTO email_templates (id, label, subject, body, checklist) VALUES (" +
                "'employee_event', " +
                "'Employee Event Notification', " +
                "'LeoHR notification: {eventType} – {employeeName}', " +
                "'Hello,\n\n" +
                "LeoHR received an update about one of your employees.\n\n" +
                "Employee: {employeeName}\n" +
                "Event: {eventType}\n" +
                "Details: {eventDescription}\n\n" +
                "Please update the relevant external system if needed.\n\n" +
                "— LeoHR', " +
                "'[]'::jsonb) " +
                "ON CONFLICT (id) DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM email_templates WHERE id = 'employee_event';");
        }
    }
}
