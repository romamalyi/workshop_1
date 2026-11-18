using System;
using System.Collections.Generic;
using LeoHR.Server.Models.Entities.LeoHr;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    birthday_reminders = table.Column<bool>(type: "boolean", nullable: false),
                    new_hire_alerts = table.Column<bool>(type: "boolean", nullable: false),
                    onboarding_reminders = table.Column<bool>(type: "boolean", nullable: false),
                    pe_due_reminders = table.Column<bool>(type: "boolean", nullable: false),
                    preboarding_task_overdue = table.Column<bool>(type: "boolean", nullable: false),
                    probation_period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    first_pe_after_probation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    default_pe_cadence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    days_before_start_to_notify = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    max_onboarding_hours = table.Column<int>(type: "integer", nullable: false),
                    cc_manager_by_default = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_settings", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "email_templates",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    checklist = table.Column<List<EmailTemplateChecklistItem>>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "onboarding_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "onboarding_steps",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    step_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    action_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_steps", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "promotion_steps",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_steps", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "promotions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    from_role = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    to_role = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "onboarding_record_steps",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    record_id = table.Column<int>(type: "integer", nullable: false),
                    step_id = table.Column<int>(type: "integer", nullable: false),
                    is_done = table.Column<bool>(type: "boolean", nullable: false),
                    done_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    action_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_record_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_onboarding_record_steps_onboarding_records_record_id",
                        column: x => x.record_id,
                        principalTable: "onboarding_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_onboarding_record_steps_onboarding_steps_step_id",
                        column: x => x.step_id,
                        principalTable: "onboarding_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "promotion_record_steps",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    promotion_id = table.Column<int>(type: "integer", nullable: false),
                    step_id = table.Column<int>(type: "integer", nullable: false),
                    is_done = table.Column<bool>(type: "boolean", nullable: false),
                    done_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_record_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_promotion_record_steps_promotion_steps_step_id",
                        column: x => x.step_id,
                        principalTable: "promotion_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_promotion_record_steps_promotions_promotion_id",
                        column: x => x.promotion_id,
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "onboarding_steps",
                columns: new[] { "id", "action_label", "description", "sort_order", "step_type", "title" },
                values: new object[,]
                {
                    { 1, "Send Email", "Send a personalized welcome email to the new hire.", 1, "Action", "Send welcome email" },
                    { 2, null, "Prepare laptop, monitor and necessary equipment.", 2, "Manual", "Set up workstation" },
                    { 3, null, "Create accounts in Slack, Jira, GitLab and other tools.", 3, "Manual", "Create accounts" },
                    { 4, null, "Pair the new hire with an experienced team member.", 4, "Manual", "Assign a buddy" },
                    { 5, null, "Book 1:1s with team leads and key stakeholders.", 5, "Manual", "Schedule intro meetings" },
                    { 6, "Send Docs", "Send the company policies and employee handbook.", 6, "Action", "Share company handbook" },
                    { 7, null, "Ensure all legal and HR documents are signed.", 7, "Manual", "Complete HR paperwork" },
                    { 8, null, "Schedule a check-in call at the end of the first week.", 8, "Manual", "First week check-in" }
                });

            migrationBuilder.InsertData(
                table: "promotion_steps",
                columns: new[] { "id", "description", "sort_order", "title" },
                values: new object[,]
                {
                    { 1, "Gather feedback from colleagues and project leads.", 1, "Collect peer feedback" },
                    { 2, "Hold a 1:1 review meeting with the employee.", 2, "Conduct performance review" },
                    { 3, "Get sign-off from the department head.", 3, "Approve with management" },
                    { 4, "Draft the official promotion letter.", 4, "Prepare promotion letter" },
                    { 5, "Notify the team about the promotion.", 5, "Send announcement email" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_record_steps_record_id_step_id",
                table: "onboarding_record_steps",
                columns: new[] { "record_id", "step_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_record_steps_step_id",
                table: "onboarding_record_steps",
                column: "step_id");

            migrationBuilder.CreateIndex(
                name: "ix_promotion_record_steps_promotion_id_step_id",
                table: "promotion_record_steps",
                columns: new[] { "promotion_id", "step_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_promotion_record_steps_step_id",
                table: "promotion_record_steps",
                column: "step_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DropTable(
                name: "email_templates");

            migrationBuilder.DropTable(
                name: "onboarding_record_steps");

            migrationBuilder.DropTable(
                name: "promotion_record_steps");

            migrationBuilder.DropTable(
                name: "onboarding_records");

            migrationBuilder.DropTable(
                name: "onboarding_steps");

            migrationBuilder.DropTable(
                name: "promotion_steps");

            migrationBuilder.DropTable(
                name: "promotions");
        }
    }
}
