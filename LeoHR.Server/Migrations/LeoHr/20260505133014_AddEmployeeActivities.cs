using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddEmployeeActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_activities",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    activity_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    accomplished_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    promotion_id = table.Column<int>(type: "integer", nullable: true),
                    points_snapshot = table.Column<int>(type: "integer", nullable: true),
                    created_by_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    approved_by_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_activities", x => x.id);
                    table.ForeignKey(
                        name: "fk_employee_activities_activities_activity_id",
                        column: x => x.activity_id,
                        principalTable: "activities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_employee_activities_promotions_promotion_id",
                        column: x => x.promotion_id,
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_employee_activities_activity_id",
                table: "employee_activities",
                column: "activity_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_activities_employee_id_is_active",
                table: "employee_activities",
                columns: new[] { "employee_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_activities_promotion_id",
                table: "employee_activities",
                column: "promotion_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_activities");
        }
    }
}
