using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddPromotionReadyEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ready_email_sent_at",
                table: "promotions",
                type: "timestamp with time zone",
                nullable: true);

            // Promotions that reached Ready before this release don't email HR on deploy.
            migrationBuilder.Sql(
                "UPDATE promotions SET ready_email_sent_at = now() WHERE status IN ('Ready', 'Sent');");

            migrationBuilder.Sql(
                "INSERT INTO email_templates (id, label, subject, body, checklist, to_recipients, cc_recipients) VALUES (" +
                "'promotion_ready', " +
                "$t$Promotion Ready to Send$t$, " +
                "$t${firstName} {lastName}'s promotion is ready to send$t$, " +
                "$t$Hi,\n\n{firstName} {lastName} has met all the criteria for the promotion to {toRole}, and the promotion is ready to send.\n\nOpen the promotion: {promotionLink}\n\nLeoHR$t$, " +
                "$t$[]$t$::jsonb, " +
                "ARRAY['HrManager'], " +
                "'{}') " +
                "ON CONFLICT (id) DO NOTHING;");

            migrationBuilder.Sql(
                "INSERT INTO email_templates (id, label, subject, body, checklist, to_recipients, cc_recipients) VALUES (" +
                "'knowledge_evaluation_requested', " +
                "$t$Knowledge Evaluation Requested$t$, " +
                "$t${firstName} {lastName} requested a knowledge evaluation$t$, " +
                "$t$Hi,\n\n{firstName} {lastName} requested a knowledge evaluation {requestedDate}.\n\nComment: {comment}\n\nOpen the promotion: {promotionLink}\n\nLeoHR$t$, " +
                "$t$[]$t$::jsonb, " +
                "ARRAY['HrManager', 'PromotionHolder'], " +
                "'{}') " +
                "ON CONFLICT (id) DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM email_templates WHERE id IN ('promotion_ready', 'knowledge_evaluation_requested');");

            migrationBuilder.DropColumn(
                name: "ready_email_sent_at",
                table: "promotions");
        }
    }
}
