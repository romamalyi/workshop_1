using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddPromotionCriteria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "promotion_criteria",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    technology = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    seniority = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    sub_level = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    work_experience = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    required_experience = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    english_level = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    required_achievement = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ke_required = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    activities_score = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_criteria", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "promotion_criteria",
                columns: new[] { "id", "activities_score", "created_at", "english_level", "is_active", "ke_required", "required_achievement", "required_experience", "seniority", "sub_level", "technology", "updated_at", "work_experience" },
                values: new object[,]
                {
                    { 1,  0,   new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "IntermediateLow", true, "None",   null,                                                                                                                        null,                            "Trainee",      "None",      "Dotnet", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "0-3m"     },
                    { 2,  0,   new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Intermediate",    true, "None",   null,                                                                                                                        null,                            "Junior",       "Associate", "Dotnet", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "3-6m"     },
                    { 3,  10,  new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Intermediate",    true, "Junior", null,                                                                                                                        null,                            "Junior",       "None",      "Dotnet", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "6-12m"    },
                    { 4,  20,  new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Intermediate",    true, "None",   null,                                                                                                                        "6m front end\n6m backend",      "Junior",       "Strong",    "Dotnet", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "1-1.5y"   },
                    { 5,  30,  new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "IntStrong",       true, "None",   null,                                                                                                                        null,                            "Intermediate", "Associate", "Dotnet", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "1.5-2y"   },
                    { 6,  40,  new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "IntStrong",       true, "Middle", null,                                                                                                                        null,                            "Intermediate", "None",      "Dotnet", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "2-2.5y"   },
                    { 7,  60,  new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "IntStrong",       true, "None",   "Microsoft Azure Developer - Associate (AZ-204) or Microsoft Azure Administrator Associate (AZ-104)",                        "1y front end\n1y backend",      "Intermediate", "Strong",    "Dotnet", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "2.5-3y"   },
                    { 8,  80,  new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "UpperInt",        true, "None",   "Microsoft Certified: DevOps Engineer Expert (AZ-400)",                                                                      null,                            "Senior",       "Associate", "Dotnet", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "3-3.5y"   },
                    { 9,  100, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "UpperInt",        true, "Senior", null,                                                                                                                        null,                            "Senior",       "None",      "Dotnet", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "3.5-4y"   },
                    { 10, 120, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "UpperInt",        true, "None",   "Microsoft Certified: Azure Solutions Architect Expert (AZ-305)",                                                            null,                            "Senior",       "Strong",    "Dotnet", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "4-4.5y"   }
                });

            migrationBuilder.CreateIndex(
                name: "ix_promotion_criteria_technology_seniority_sub_level",
                table: "promotion_criteria",
                columns: new[] { "technology", "seniority", "sub_level" },
                unique: true,
                filter: "\"is_active\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "promotion_criteria");
        }
    }
}
