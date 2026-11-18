using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddOnboardingTaskTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "onboarding_task_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phase = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    due_offset_days = table.Column<int>(type: "integer", nullable: false),
                    due_direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    due_anchor = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    applies_to = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    responsible_role_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_task_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "onboarding_task_template_fields",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    template_id = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    field_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    hint = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_task_template_fields", x => x.id);
                    table.ForeignKey(
                        name: "fk_onboarding_task_template_fields_onboarding_task_templates_t",
                        column: x => x.template_id,
                        principalTable: "onboarding_task_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "onboarding_task_template_suggested_levels",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    template_id = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_task_template_suggested_levels", x => x.id);
                    table.ForeignKey(
                        name: "fk_onboarding_task_template_suggested_levels_onboarding_task_t",
                        column: x => x.template_id,
                        principalTable: "onboarding_task_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "onboarding_task_template_checklist_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    field_id = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_task_template_checklist_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_onboarding_task_template_checklist_items_onboarding_task_te",
                        column: x => x.field_id,
                        principalTable: "onboarding_task_template_fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_task_template_checklist_items_field_id",
                table: "onboarding_task_template_checklist_items",
                column: "field_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_task_template_fields_template_id",
                table: "onboarding_task_template_fields",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_task_template_suggested_levels_template_id_level",
                table: "onboarding_task_template_suggested_levels",
                columns: new[] { "template_id", "level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_task_templates_phase_title",
                table: "onboarding_task_templates",
                columns: new[] { "phase", "title" },
                unique: true,
                filter: "\"is_active\" = true");

            SeedInitialLibrary(migrationBuilder);
        }

        // Seeded with raw SQL, not HasData: HR edits these rows, so migrations must not own them.
        // Guarded on an empty table so it runs once and is never reconciled against later edits.
        private static void SeedInitialLibrary(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO onboarding_task_templates
                    (title, phase, due_offset_days, due_direction, due_anchor, applies_to, is_active, created_at, updated_at)
                SELECT v.title, v.phase, v.days, v.direction, 'StartDate', v.applies_to, v.is_active, now(), now()
                FROM (VALUES
                    ('Meet mentor',                        'BeforeFirstDay',  3, 'Before', 'Optional', true),
                    ('Give access card',                   'BeforeFirstDay',  1, 'Before', 'AllHires', true),
                    ('Prep notebook & accessories',        'BeforeFirstDay',  2, 'Before', 'AllHires', true),
                    ('Place-ready',                        'BeforeFirstDay',  1, 'Before', 'Optional', true),
                    ('Create Google account',              'BeforeFirstDay',  1, 'Before', 'AllHires', true),
                    ('Fill Public all employee doc',       'BeforeFirstDay',  1, 'Before', 'AllHires', true),
                    ('Prepare welcome kit',                'BeforeFirstDay',  2, 'Before', 'Optional', false),
                    ('Welcome intro & office tour',        'FirstDay',        0, 'After',  'AllHires', true),
                    ('Hand over notebook & access card',   'FirstDay',        0, 'After',  'AllHires', true),
                    ('Sign employment documents',          'FirstDay',        0, 'After',  'AllHires', true),
                    ('End-of-trial performance evaluation','ThirdMonth',     90, 'After',  'AllHires', true)
                ) AS v(title, phase, days, direction, applies_to, is_active)
                WHERE NOT EXISTS (SELECT 1 FROM onboarding_task_templates);
                """);

            migrationBuilder.Sql("""
                INSERT INTO onboarding_task_template_suggested_levels (template_id, level)
                SELECT t.id, 'Trainee'
                FROM onboarding_task_templates t
                WHERE t.title = 'Meet mentor'
                  AND t.phase = 'BeforeFirstDay'
                  AND NOT EXISTS (SELECT 1 FROM onboarding_task_template_suggested_levels);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "onboarding_task_template_checklist_items");

            migrationBuilder.DropTable(
                name: "onboarding_task_template_suggested_levels");

            migrationBuilder.DropTable(
                name: "onboarding_task_template_fields");

            migrationBuilder.DropTable(
                name: "onboarding_task_templates");
        }
    }
}
