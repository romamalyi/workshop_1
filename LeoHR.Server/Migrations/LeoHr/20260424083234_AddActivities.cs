using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "activities",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    within_period = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_activities", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "activities",
                columns: new[] { "id", "category", "created_at", "is_active", "name", "points", "updated_at", "within_period" },
                values: new object[,]
                {
                    { 1, "SelfCourses", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Udemy course", 10, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SixMonths" },
                    { 2, "Certifications", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "AZ-204 or AZ-104", 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "OverallActive" },
                    { 3, "Certifications", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "AZ-305 or AZ-400", 60, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "OverallActive" },
                    { 4, "Certifications", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Azure AI Engineer Associate (AI-102)", 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "OverallActive" },
                    { 5, "Certifications", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Azure AD-300", 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "OverallActive" },
                    { 6, "Certifications", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Azure AD-420", 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "OverallActive" },
                    { 7, "Certifications", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "AWS Certified Developer - Associate", 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "OverallActive" },
                    { 8, "Certifications", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "AWS Certified Solutions Architect - Associate", 40, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "OverallActive" },
                    { 9, "Certifications", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Certification renew (existing one)", 10, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "OverallActive" },
                    { 10, "Mentorship", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Trainee Mentorship", 20, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "OverallInLevel" },
                    { 11, "Presentations", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Local Presentation", 10, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TwelveMonths" },
                    { 12, "Presentations", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Leobit Presentation (online)", 20, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TwelveMonths" },
                    { 13, "Presentations", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Leobit Presentation (offline)", 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TwelveMonths" },
                    { 14, "Presentations", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Leobit Presentation (hot-topic)", 40, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TwelveMonths" },
                    { 15, "Presentations", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Conference Presentation", 60, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "OverallInLevel" },
                    { 16, "Workshop", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Online workshop", 20, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TwelveMonths" },
                    { 17, "Workshop", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Offline workshop", 40, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TwelveMonths" },
                    { 18, "SdoActivities", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Article (tech/marketing)", 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "OverallInLevel" },
                    { 19, "SdoActivities", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SDO Contributions", 40, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "OverallInLevel" },
                    { 20, "KeConducting", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "1 KE", 10, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TwelveMonths" },
                    { 21, "KeConducting", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "3 KE interview", 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TwelveMonths" },
                    { 22, "KeConducting", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "6 KE interview", 60, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TwelveMonths" },
                    { 23, "Interview", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "1 interview", 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TwelveMonths" },
                    { 24, "Interview", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "3 interview", 20, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TwelveMonths" },
                    { 25, "Interview", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "6 interview", 40, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TwelveMonths" },
                    { 26, "Interview", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "12 interview", 60, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TwelveMonths" },
                    { 27, "Interview", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "24 interview", 100, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TwelveMonths" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_activities_category",
                table: "activities",
                column: "category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activities");
        }
    }
}
