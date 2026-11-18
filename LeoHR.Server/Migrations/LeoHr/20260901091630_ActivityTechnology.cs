using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class ActivityTechnology : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "technology",
                table: "activities",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_activities_technology",
                table: "activities",
                column: "technology");

            migrationBuilder.Sql(
                "SELECT setval(pg_get_serial_sequence('activities', 'id'), (SELECT MAX(id) FROM activities));");

            migrationBuilder.Sql(@"
                INSERT INTO activities (category, name, within_period, technology, points, is_active, created_at, updated_at)
                VALUES
                  ('SelfCourses',    'Udemy: Master the Coding Interview (Data Structures + Algorithms)',         'OverallActive', 'WebUi',   0, true, TIMESTAMPTZ '2026-01-01T00:00:00Z', TIMESTAMPTZ '2026-01-01T00:00:00Z'),
                  ('SelfCourses',    'Udemy: React - The Complete Guide, or Udemy: Angular - The Complete Guide', 'OverallActive', 'WebUi',   0, true, TIMESTAMPTZ '2026-01-01T00:00:00Z', TIMESTAMPTZ '2026-01-01T00:00:00Z'),
                  ('SelfCourses',    'The Odin Project: Ruby course',                                             'OverallActive', 'Ruby',    0, true, TIMESTAMPTZ '2026-01-01T00:00:00Z', TIMESTAMPTZ '2026-01-01T00:00:00Z'),
                  ('SelfCourses',    'The Odin Project: Full Stack Ruby on Rails course',                         'OverallActive', 'Ruby',    0, true, TIMESTAMPTZ '2026-01-01T00:00:00Z', TIMESTAMPTZ '2026-01-01T00:00:00Z'),
                  ('SdoActivities',  'Contribution to an open-source Ruby project',                               'OverallActive', 'Ruby',    0, true, TIMESTAMPTZ '2026-01-01T00:00:00Z', TIMESTAMPTZ '2026-01-01T00:00:00Z'),
                  ('SelfCourses',    'Udemy: iOS 13 App Development Bootcamp, or Udemy: Android Kotlin Developer', 'OverallActive', 'Flutter', 0, true, TIMESTAMPTZ '2026-01-01T00:00:00Z', TIMESTAMPTZ '2026-01-01T00:00:00Z'),
                  ('SelfCourses',    'LinkedIn Learning: Become a PHP Developer (learning path)',                 'OverallActive', 'Php',     0, true, TIMESTAMPTZ '2026-01-01T00:00:00Z', TIMESTAMPTZ '2026-01-01T00:00:00Z'),
                  ('Certifications', 'Zend PHP Certification',                                                    'OverallActive', 'Php',     0, true, TIMESTAMPTZ '2026-01-01T00:00:00Z', TIMESTAMPTZ '2026-01-01T00:00:00Z'),
                  ('Certifications', 'AWS, Symfony, Laravel or Zend certification',                               'OverallActive', 'Php',     0, true, TIMESTAMPTZ '2026-01-01T00:00:00Z', TIMESTAMPTZ '2026-01-01T00:00:00Z'),
                  ('SelfCourses',    'Stanford CS193p: Developing iOS Apps with Swift course',                    'OverallActive', 'Ios',     0, true, TIMESTAMPTZ '2026-01-01T00:00:00Z', TIMESTAMPTZ '2026-01-01T00:00:00Z'),
                  ('SelfCourses',    'objc.io: App Architecture (book)',                                          'OverallActive', 'Ios',     0, true, TIMESTAMPTZ '2026-01-01T00:00:00Z', TIMESTAMPTZ '2026-01-01T00:00:00Z'),
                  ('SdoActivities',  'Contribution to an open-source iOS-related project',                        'OverallActive', 'Ios',     0, true, TIMESTAMPTZ '2026-01-01T00:00:00Z', TIMESTAMPTZ '2026-01-01T00:00:00Z'),
                  ('SelfCourses',    'objc.io: Advanced Swift (book), published App Store app',                   'OverallActive', 'Ios',     0, true, TIMESTAMPTZ '2026-01-01T00:00:00Z', TIMESTAMPTZ '2026-01-01T00:00:00Z'),
                  ('SelfCourses',    'objc.io: Functional Swift (book)',                                          'OverallActive', 'Ios',     0, true, TIMESTAMPTZ '2026-01-01T00:00:00Z', TIMESTAMPTZ '2026-01-01T00:00:00Z');");

            migrationBuilder.Sql(
                "SELECT setval(pg_get_serial_sequence('promotion_criteria', 'id'), (SELECT MAX(id) FROM promotion_criteria));");

            migrationBuilder.Sql(@"
                INSERT INTO promotion_criteria
                  (technology, seniority, sub_level, work_experience_from_months, work_experience_to_months,
                   required_experience, english_level, required_activity_id, ke_required, activities_score,
                   is_active, created_at, updated_at)
                SELECT
                  v.technology, v.seniority, v.sub_level, v.wx_from, v.wx_to,
                  v.required_experience, v.english_level,
                  (SELECT a.id FROM activities a WHERE a.name = v.required_activity_name),
                  v.ke_required, v.activities_score,
                  true, TIMESTAMPTZ '2026-01-01T00:00:00Z', TIMESTAMPTZ '2026-01-01T00:00:00Z'
                FROM (VALUES
                  ('WebUi',   'Trainee',      'None',      0,  3, NULL,                            'IntermediateLow',    NULL,                                                                          'None',   0),
                  ('WebUi',   'Junior',       'Associate', 3,  6, NULL,                            'Intermediate',       NULL,                                                                          'None',   0),
                  ('WebUi',   'Junior',       'None',      6, 12, NULL,                            'Intermediate',       NULL,                                                                          'Junior', 10),
                  ('WebUi',   'Junior',       'Strong',   12, 18, '6m frontend',                   'Intermediate',       'Udemy: Master the Coding Interview (Data Structures + Algorithms)',             'None',   20),
                  ('WebUi',   'Intermediate', 'Associate',18, 24, NULL,                            'IntermediateStrong', 'Udemy: React - The Complete Guide, or Udemy: Angular - The Complete Guide',     'None',   30),
                  ('WebUi',   'Intermediate', 'None',     24, 30, NULL,                            'IntermediateStrong', NULL,                                                                          'Middle', 40),
                  ('WebUi',   'Intermediate', 'Strong',   30, 36, '2y frontend',                   'IntermediateStrong', NULL,                                                                          'None',   60),
                  ('WebUi',   'Senior',       'Associate',36, 42, NULL,                            'UpperIntermediate',  NULL,                                                                          'None',   80),
                  ('WebUi',   'Senior',       'None',     42, 48, NULL,                            'UpperIntermediate',  NULL,                                                                          'Senior', 100),
                  ('WebUi',   'Senior',       'Strong',   48, 54, NULL,                            'UpperIntermediate',  'AWS Certified Developer - Associate',                                          'None',   120),

                  ('Ruby',    'Trainee',      'None',      0,  3, NULL,                            'IntermediateLow',    NULL,                                                                          'None',   0),
                  ('Ruby',    'Junior',       'Associate', 3,  6, NULL,                            'Intermediate',       'The Odin Project: Ruby course',                                                'None',   0),
                  ('Ruby',    'Junior',       'None',      6, 12, NULL,                            'Intermediate',       NULL,                                                                          'Junior', 10),
                  ('Ruby',    'Junior',       'Strong',   12, 18, '3m frontend, 6m backend',       'Intermediate',       'The Odin Project: Full Stack Ruby on Rails course',                            'None',   20),
                  ('Ruby',    'Intermediate', 'Associate',18, 24, NULL,                            'IntermediateStrong', NULL,                                                                          'None',   30),
                  ('Ruby',    'Intermediate', 'None',     24, 30, NULL,                            'IntermediateStrong', NULL,                                                                          'Middle', 40),
                  ('Ruby',    'Intermediate', 'Strong',   30, 36, '1y frontend, 1y backend',       'IntermediateStrong', 'Contribution to an open-source Ruby project',                                  'None',   60),
                  ('Ruby',    'Senior',       'Associate',36, 42, NULL,                            'UpperIntermediate',  'AWS Certified Developer - Associate',                                          'None',   80),
                  ('Ruby',    'Senior',       'None',     42, 48, NULL,                            'UpperIntermediate',  NULL,                                                                          'Senior', 100),
                  ('Ruby',    'Senior',       'Strong',   48, 54, '2y BE, 0.5y any FE framework',  'UpperIntermediate',  'AWS Certified Solutions Architect - Associate',                                'None',   120),

                  ('Flutter', 'Trainee',      'None',      0,  3, NULL,                            'IntermediateLow',    NULL,                                                                          'None',   0),
                  ('Flutter', 'Junior',       'Associate', 3,  6, NULL,                            'Intermediate',       NULL,                                                                          'None',   0),
                  ('Flutter', 'Junior',       'None',      6, 12, '0.5y any mobile technology',    'Intermediate',       NULL,                                                                          'Junior', 10),
                  ('Flutter', 'Junior',       'Strong',   12, 18, '0.5y Dart, 0.5y Flutter',       'Intermediate',       NULL,                                                                          'None',   20),
                  ('Flutter', 'Intermediate', 'Associate',18, 24, NULL,                            'IntermediateStrong', 'Udemy: iOS 13 App Development Bootcamp, or Udemy: Android Kotlin Developer',    'None',   30),
                  ('Flutter', 'Intermediate', 'None',     24, 30, NULL,                            'IntermediateStrong', NULL,                                                                          'Middle', 40),
                  ('Flutter', 'Intermediate', 'Strong',   30, 36, '2y Dart, 2y Flutter',           'IntermediateStrong', NULL,                                                                          'None',   60),
                  ('Flutter', 'Senior',       'Associate',36, 42, NULL,                            'UpperIntermediate',  NULL,                                                                          'None',   80),
                  ('Flutter', 'Senior',       'None',     42, 48, NULL,                            'UpperIntermediate',  NULL,                                                                          'Senior', 100),
                  ('Flutter', 'Senior',       'Strong',   48, 54, NULL,                            'UpperIntermediate',  NULL,                                                                          'None',   120),

                  ('Php',     'Trainee',      'None',      0,  3, NULL,                            'IntermediateLow',    NULL,                                                                          'None',   0),
                  ('Php',     'Junior',       'Associate', 3,  6, NULL,                            'Intermediate',       NULL,                                                                          'None',   0),
                  ('Php',     'Junior',       'None',      6, 12, NULL,                            'Intermediate',       NULL,                                                                          'Junior', 10),
                  ('Php',     'Junior',       'Strong',   12, 18, '6m frontend',                   'Intermediate',       'LinkedIn Learning: Become a PHP Developer (learning path)',                    'None',   20),
                  ('Php',     'Intermediate', 'Associate',18, 24, NULL,                            'IntermediateStrong', NULL,                                                                          'None',   30),
                  ('Php',     'Intermediate', 'None',     24, 30, NULL,                            'IntermediateStrong', NULL,                                                                          'Middle', 40),
                  ('Php',     'Intermediate', 'Strong',   30, 36, '1y FE, 1y BE',                  'IntermediateStrong', NULL,                                                                          'None',   60),
                  ('Php',     'Senior',       'Associate',36, 42, NULL,                            'UpperIntermediate',  'Zend PHP Certification',                                                       'None',   80),
                  ('Php',     'Senior',       'None',     42, 48, NULL,                            'UpperIntermediate',  NULL,                                                                          'Senior', 100),
                  ('Php',     'Senior',       'Strong',   48, 54, '1y FE, 3y BE',                  'UpperIntermediate',  'AWS, Symfony, Laravel or Zend certification',                                  'None',   120),

                  ('Ios',     'Trainee',      'None',      0,  3, NULL,                            'IntermediateLow',    NULL,                                                                          'None',   0),
                  ('Ios',     'Junior',       'Associate', 3,  6, NULL,                            'Intermediate',       'Stanford CS193p: Developing iOS Apps with Swift course',                       'None',   0),
                  ('Ios',     'Junior',       'None',      6, 12, NULL,                            'Intermediate',       NULL,                                                                          'Junior', 10),
                  ('Ios',     'Junior',       'Strong',   12, 18, '6m frontend',                   'Intermediate',       NULL,                                                                          'None',   20),
                  ('Ios',     'Intermediate', 'Associate',18, 24, NULL,                            'IntermediateStrong', 'objc.io: App Architecture (book)',                                             'None',   30),
                  ('Ios',     'Intermediate', 'None',     24, 30, NULL,                            'IntermediateStrong', NULL,                                                                          'Middle', 40),
                  ('Ios',     'Intermediate', 'Strong',   30, 36, '2y frontend',                   'IntermediateStrong', 'Contribution to an open-source iOS-related project',                           'None',   60),
                  ('Ios',     'Senior',       'Associate',36, 42, NULL,                            'UpperIntermediate',  'objc.io: Advanced Swift (book), published App Store app',                       'None',   80),
                  ('Ios',     'Senior',       'None',     42, 48, NULL,                            'UpperIntermediate',  NULL,                                                                          'Senior', 100),
                  ('Ios',     'Senior',       'Strong',   48, 54, NULL,                            'UpperIntermediate',  'objc.io: Functional Swift (book)',                                             'None',   120),

                  ('Android', 'Trainee',      'None',      0,  3, NULL,                            'IntermediateLow',    NULL,                                                                          'None',   0),
                  ('Android', 'Junior',       'Associate', 3,  6, NULL,                            'Intermediate',       NULL,                                                                          'None',   0),
                  ('Android', 'Junior',       'None',      6, 12, NULL,                            'Intermediate',       NULL,                                                                          'Junior', 10),
                  ('Android', 'Junior',       'Strong',   12, 18, '0.5y Java, 0.5y Kotlin',        'Intermediate',       NULL,                                                                          'None',   20),
                  ('Android', 'Intermediate', 'Associate',18, 24, '1y Kotlin',                     'IntermediateStrong', NULL,                                                                          'None',   30),
                  ('Android', 'Intermediate', 'None',     24, 30, NULL,                            'IntermediateStrong', NULL,                                                                          'Middle', 40),
                  ('Android', 'Intermediate', 'Strong',   30, 36, '0.5y Java, 2y Kotlin',          'IntermediateStrong', NULL,                                                                          'None',   60),
                  ('Android', 'Senior',       'Associate',36, 42, 'Unit testing, DI frameworks',   'UpperIntermediate',  NULL,                                                                          'None',   80),
                  ('Android', 'Senior',       'None',     42, 48, NULL,                            'UpperIntermediate',  NULL,                                                                          'Senior', 100),
                  ('Android', 'Senior',       'Strong',   48, 54, '1y FE, 3y BE',                  'UpperIntermediate',  NULL,                                                                          'None',   120)
                ) AS v(technology, seniority, sub_level, wx_from, wx_to, required_experience, english_level, required_activity_name, ke_required, activities_score);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM promotion_criteria WHERE technology <> 'Dotnet';");
            migrationBuilder.Sql("DELETE FROM activities WHERE technology IS NOT NULL;");
            migrationBuilder.Sql(
                "SELECT setval(pg_get_serial_sequence('promotion_criteria', 'id'), GREATEST((SELECT MAX(id) FROM promotion_criteria), 1));");
            migrationBuilder.Sql(
                "SELECT setval(pg_get_serial_sequence('activities', 'id'), GREATEST((SELECT MAX(id) FROM activities), 1));");

            migrationBuilder.DropIndex(
                name: "ix_activities_technology",
                table: "activities");

            migrationBuilder.DropColumn(
                name: "technology",
                table: "activities");
        }
    }
}
