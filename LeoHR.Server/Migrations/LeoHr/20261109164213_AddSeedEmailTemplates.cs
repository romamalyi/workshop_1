using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddSeedEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "INSERT INTO email_templates (id, label, subject, body, checklist, to_recipients, cc_recipients) VALUES (" +
                "'welcome', " +
                "$t$Welcome$t$, " +
                "$t$Welcome to Leobit, {firstName}!$t$, " +
                "$t$Dear {firstName},\n\nWe are so delighted to have you in our team! We believe that your skills and talent will make a substantial contribution to our company.\n\nWelcome aboard!\n\nPlease start with the following setup tasks first:\n\n1. Add email signature (instruction will be sent separately):\n   — Position: [your position]\n   — Add your Leobit email and cell phone\n\n2. Update your LinkedIn page — add your position at Leobit and use our corporate banners.\n\n3. Set up your Slack profile — add your photo and position.\n\n4. Set up your Leobit Skills profile — https://skills.leobit.co/list\n\n5. Add to your calendar: Leobit Vacation and Leobit BDays&Holidays.\n\n6. Subscribe to Leobit Instagram, FB page, Telegram.\n\n7. Bookmark Time Tracking — timetracking.leobit.co\n   Report your time starting from your first day ({startDate}).\n\n8. Save Leobit Main Documents folder on your Google Drive.\n\nPlease respond to this email once all tasks are completed (with your new signature).\n\nLooking forward to working with you!\n\nHR Team$t$, " +
                "$t$[{\"Id\": \"c1\", \"Label\": \"Email signature instruction attached\"}, {\"Id\": \"c2\", \"Label\": \"LinkedIn update reminder included\"}, {\"Id\": \"c3\", \"Label\": \"Slack profile setup mentioned\"}, {\"Id\": \"c4\", \"Label\": \"Skills profile link included (skills.leobit.co)\"}, {\"Id\": \"c5\", \"Label\": \"Company calendar subscription mentioned\"}, {\"Id\": \"c6\", \"Label\": \"Time Tracking link included (timetracking.leobit.co)\"}, {\"Id\": \"c7\", \"Label\": \"Social media channels listed\"}, {\"Id\": \"c8\", \"Label\": \"Main Documents folder link shared\"}]$t$::jsonb, " +
                "'{}', " +
                "'{}') " +
                "ON CONFLICT (id) DO NOTHING;");

            migrationBuilder.Sql(
                "INSERT INTO email_templates (id, label, subject, body, checklist, to_recipients, cc_recipients) VALUES (" +
                "'first-day', " +
                "$t$First Day Info$t$, " +
                "$t$Your first day at Leobit — {startDate}$t$, " +
                "$t$Hi {firstName},\n\nYour first day is just around the corner — {startDate}!\n\nHere is what to expect:\n• Arrival time: 9:00 AM\n• Our HR representative will meet you at the office entrance\n• Please bring a valid ID document\n\nIf you have any questions before your start date, do not hesitate to reach out.\n\nYou can also find our HR team in the HR Room — Anna Osoba, Hanna Meleshko.\nFor any office or access questions — contact our Office Manager in the Admin Room.\n\nSee you soon!\n\nHR Team$t$, " +
                "$t$[{\"Id\": \"c1\", \"Label\": \"Start date and arrival time confirmed\"}, {\"Id\": \"c2\", \"Label\": \"Meeting point / entrance mentioned\"}, {\"Id\": \"c3\", \"Label\": \"ID document reminder included\"}, {\"Id\": \"c4\", \"Label\": \"HR and office contacts listed\"}]$t$::jsonb, " +
                "'{}', " +
                "'{}') " +
                "ON CONFLICT (id) DO NOTHING;");

            migrationBuilder.Sql(
                "INSERT INTO email_templates (id, label, subject, body, checklist, to_recipients, cc_recipients) VALUES (" +
                "'documents', " +
                "$t$Documents Request$t$, " +
                "$t$Action required: documents needed before {startDate}$t$, " +
                "$t$Hi {firstName},\n\nTo complete your onboarding before {startDate}, we kindly ask you to prepare and send us the following documents:\n\n• Copy of passport or national ID\n• Tax identification number (TIN)\n• Bank account details for payroll\n• Signed employment contract (if not yet returned)\n• NDA (attached to this email)\n\nPlease send the documents as scanned copies to hr@leobit.com or bring originals on your first day.\n\nThank you!\n\nHR Team$t$, " +
                "$t$[{\"Id\": \"c1\", \"Label\": \"All required documents listed\"}, {\"Id\": \"c2\", \"Label\": \"Submission deadline mentioned\"}, {\"Id\": \"c3\", \"Label\": \"Submission email (hr@leobit.com) specified\"}, {\"Id\": \"c4\", \"Label\": \"NDA attached to email\"}]$t$::jsonb, " +
                "'{}', " +
                "'{}') " +
                "ON CONFLICT (id) DO NOTHING;");

            migrationBuilder.Sql(
                "INSERT INTO email_templates (id, label, subject, body, checklist, to_recipients, cc_recipients) VALUES (" +
                "'tools', " +
                "$t$Tools & Access$t$, " +
                "$t$Your Leobit accounts are being set up$t$, " +
                "$t$Hi {firstName},\n\nWe are currently setting up your work accounts and tools. By {startDate} you will have access to:\n\n• Corporate email (Google Workspace)\n• Slack — our main communication tool\n• Jira & Confluence for project management\n• GitHub / GitLab (if applicable to your role)\n• HR portal (this system)\n\nYou will receive login credentials via your personal email shortly before your start date.\n\nIf you have any questions about the tools we use, do not hesitate to ask.\n\nHR Team$t$, " +
                "$t$[{\"Id\": \"c1\", \"Label\": \"Corporate email (Google Workspace) mentioned\"}, {\"Id\": \"c2\", \"Label\": \"Slack access included\"}, {\"Id\": \"c3\", \"Label\": \"Jira / project tools mentioned\"}, {\"Id\": \"c4\", \"Label\": \"Credentials delivery method specified\"}]$t$::jsonb, " +
                "'{}', " +
                "'{}') " +
                "ON CONFLICT (id) DO NOTHING;");

            migrationBuilder.Sql(
                "INSERT INTO email_templates (id, label, subject, body, checklist, to_recipients, cc_recipients) VALUES (" +
                "'checkin', " +
                "$t$Week 1 Check-in$t$, " +
                "$t$How is your first week going, {firstName}?$t$, " +
                "$t$Hi {firstName},\n\nIt has been a week since you joined us on {startDate} — time flies!\n\nWe wanted to check in and see how everything is going. We would love to hear:\n• How comfortable do you feel in your new role so far?\n• Is there anything you are still missing (access, tools, information)?\n• How is the communication with your team?\n\nFeel free to reply to this email or book a quick call with your HR contact.\n\nWe are here to support you!\n\nHR Team$t$, " +
                "$t$[{\"Id\": \"c1\", \"Label\": \"Sent after the first full work week\"}, {\"Id\": \"c2\", \"Label\": \"Reply-to address is correct\"}, {\"Id\": \"c3\", \"Label\": \"Manager is CC'd\"}]$t$::jsonb, " +
                "'{}', " +
                "'{}') " +
                "ON CONFLICT (id) DO NOTHING;");

            migrationBuilder.Sql(
                "INSERT INTO email_templates (id, label, subject, body, checklist, to_recipients, cc_recipients) VALUES (" +
                "'birthday', " +
                "$t$Birthday Wish$t$, " +
                "$t$Happy Birthday, {firstName}!$t$, " +
                "$t$Hi {firstName},\n\nWishing you a wonderful birthday today! Hope your day is filled with joy and lots of smiles.\n\nBest wishes from the whole Leobit team!\n\nHR Team$t$, " +
                "$t$[]$t$::jsonb, " +
                "'{}', " +
                "'{}') " +
                "ON CONFLICT (id) DO NOTHING;");

            migrationBuilder.Sql(
                "INSERT INTO email_templates (id, label, subject, body, checklist, to_recipients, cc_recipients) VALUES (" +
                "'promotion', " +
                "$t$Promotion Announcement$t$, " +
                "$t$Congratulations to {firstName} {lastName} on their promotion!$t$, " +
                "$t$Dear Team,\n\nWe are thrilled to announce the promotion of {firstName} {lastName}!\n\nEffective immediately, {firstName} has been promoted from {fromRole} to {toRole}.\n\nOver the course of their time at Leobit, {firstName} has demonstrated exceptional dedication, growth, and a strong commitment to both their team and the company's success. This promotion is a reflection of their hard work and the confidence we have in their continued contributions.\n\nPlease join us in congratulating {firstName} on this well-deserved achievement!\n\nHR Team$t$, " +
                "$t$[{\"Id\": \"p1\", \"Label\": \"Employee name and roles are correct\"}, {\"Id\": \"p2\", \"Label\": \"Manager is CC'd\"}, {\"Id\": \"p3\", \"Label\": \"Sent to the full team\"}]$t$::jsonb, " +
                "ARRAY['Employee', 'ProjectManager', 'HrManager', 'PromotionHolder'], " +
                "ARRAY['HrManagersManager', 'ProjectManagersManager']) " +
                "ON CONFLICT (id) DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Most databases had these rows before this migration; deleting them would drop HR's edited templates.
        }
    }
}
