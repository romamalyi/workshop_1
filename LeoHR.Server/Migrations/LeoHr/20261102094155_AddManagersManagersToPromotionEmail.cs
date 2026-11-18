using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddManagersManagersToPromotionEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A role HR already put in To or CC is left where it is; a role must not be in both.
            migrationBuilder.Sql("""
                UPDATE email_templates
                SET cc_recipients = cc_recipients || ARRAY(
                    SELECT role FROM unnest(ARRAY['HrManagersManager', 'ProjectManagersManager']) AS role
                    WHERE NOT role = ANY(to_recipients) AND NOT role = ANY(cc_recipients))
                WHERE id = 'promotion';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE email_templates
                SET to_recipients = array_remove(array_remove(to_recipients, 'HrManagersManager'), 'ProjectManagersManager'),
                    cc_recipients = array_remove(array_remove(cc_recipients, 'HrManagersManager'), 'ProjectManagersManager');
                """);
        }
    }
}
