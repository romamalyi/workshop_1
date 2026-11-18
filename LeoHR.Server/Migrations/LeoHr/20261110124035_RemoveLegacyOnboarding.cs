using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class RemoveLegacyOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "onboarding_record_steps");

            migrationBuilder.DropTable(
                name: "onboarding_records");

            migrationBuilder.DropTable(
                name: "onboarding_steps");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "onboarding_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    employee_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false)
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
                    action_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    step_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_steps", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "onboarding_record_steps",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    record_id = table.Column<int>(type: "integer", nullable: false),
                    step_id = table.Column<int>(type: "integer", nullable: false),
                    action_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    done_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_done = table.Column<bool>(type: "boolean", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_record_steps_record_id_step_id",
                table: "onboarding_record_steps",
                columns: new[] { "record_id", "step_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_record_steps_step_id",
                table: "onboarding_record_steps",
                column: "step_id");
        }
    }
}
