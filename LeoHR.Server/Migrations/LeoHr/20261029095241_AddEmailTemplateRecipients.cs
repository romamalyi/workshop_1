using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddEmailTemplateRecipients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "cc_recipients",
                table: "email_templates",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string[]>(
                name: "to_recipients",
                table: "email_templates",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.Sql("""
                UPDATE email_templates SET to_recipients = ARRAY['Employee', 'ProjectManager', 'HrManager', 'PromotionHolder']
                WHERE id = 'promotion';
                UPDATE email_templates SET to_recipients = ARRAY['Employee'], cc_recipients = ARRAY['HrManager']
                WHERE id = 'english_level_changed';
                UPDATE email_templates SET to_recipients = ARRAY['Employee']
                WHERE id = 'english_group_assigned';
                UPDATE email_templates SET to_recipients = ARRAY['HrManager']
                WHERE id = 'employee_event';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cc_recipients",
                table: "email_templates");

            migrationBuilder.DropColumn(
                name: "to_recipients",
                table: "email_templates");
        }
    }
}
