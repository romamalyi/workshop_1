using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class MoveOnboardingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "birthday_reminders",
                table: "app_settings");

            migrationBuilder.DropColumn(
                name: "days_before_start_to_notify",
                table: "app_settings");

            migrationBuilder.DropColumn(
                name: "default_pe_cadence",
                table: "app_settings");

            migrationBuilder.DropColumn(
                name: "first_pe_after_probation",
                table: "app_settings");

            migrationBuilder.DropColumn(
                name: "max_onboarding_hours",
                table: "app_settings");

            migrationBuilder.DropColumn(
                name: "new_hire_alerts",
                table: "app_settings");

            migrationBuilder.DropColumn(
                name: "onboarding_reminders",
                table: "app_settings");

            migrationBuilder.DropColumn(
                name: "pe_due_reminders",
                table: "app_settings");

            migrationBuilder.DropColumn(
                name: "preboarding_task_overdue",
                table: "app_settings");

            migrationBuilder.DropColumn(
                name: "probation_period",
                table: "app_settings");

            migrationBuilder.CreateTable(
                name: "onboarding_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    days_before_start_to_notify = table.Column<int>(type: "integer", nullable: false),
                    weekly_digest_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    overdue_alerts_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    probation_months = table.Column<int>(type: "integer", nullable: false),
                    max_onboarding_hours = table.Column<int>(type: "integer", nullable: false),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_settings", x => x.id);
                });

            // HR edits these, so they're seeded once here rather than with HasData.
            migrationBuilder.Sql(
                "INSERT INTO onboarding_settings (id, days_before_start_to_notify, weekly_digest_enabled, overdue_alerts_enabled, probation_months, max_onboarding_hours, created_at) " +
                "VALUES (1, 14, FALSE, FALSE, 3, 15, now()) ON CONFLICT (id) DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "onboarding_settings");

            migrationBuilder.AddColumn<bool>(
                name: "birthday_reminders",
                table: "app_settings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "days_before_start_to_notify",
                table: "app_settings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "14 days");

            migrationBuilder.AddColumn<string>(
                name: "default_pe_cadence",
                table: "app_settings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "6 months");

            migrationBuilder.AddColumn<string>(
                name: "first_pe_after_probation",
                table: "app_settings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "3 months");

            migrationBuilder.AddColumn<int>(
                name: "max_onboarding_hours",
                table: "app_settings",
                type: "integer",
                nullable: false,
                defaultValue: 15);

            migrationBuilder.AddColumn<bool>(
                name: "new_hire_alerts",
                table: "app_settings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "onboarding_reminders",
                table: "app_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "pe_due_reminders",
                table: "app_settings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "preboarding_task_overdue",
                table: "app_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "probation_period",
                table: "app_settings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "3 months");
        }
    }
}
