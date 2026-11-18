using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddTeacherToGroupAssignedEmail : Migration
    {
        private const string TeacherLine = "<p><strong>Teacher:</strong> {teacherName}</p>";
        private const string Anchor = "<p>Here are some key guidelines for the course.</p>";

        // The body as AddEnglishEmailTemplates seeded it; line endings are ignored, as they depend on the checkout.
        private const string SeededBody = """
                <p>Dear {employeeName},</p>

                <p>
                    I am pleased to inform you that you have been successfully enrolled in the
                    English group <strong>{groupName}</strong> at Leobit.
                </p>

                <p>Here are some key guidelines for the course.</p>

                <p>
                    <strong>Class Schedule:</strong> Sessions will be held on {schedule}.
                    Group training sessions take place {sessionsPerWeek} a week.
                    Please make sure to attend on time.
                </p>

                <p>Your active and responsible participation is expected, including:</p>

                <ul>
                    <li>
                        <strong>Punctual attendance</strong> &ndash; Joining sessions on time as per the
                        pre-scheduled timetable. Any changes to the schedule can be made upon mutual agreement.
                    </li>
                    <li>
                        <strong>Regular participation</strong> &ndash; You may not miss more than two sessions
                        per month, except for valid reasons such as vacation or sick leave. In such cases,
                        the instructor must be informed in advance, no later than one day before the scheduled session.
                    </li>
                    <li>
                        <strong>Lesson preparation</strong> &ndash; Completing all assigned homework in full and
                        coming prepared for each session.
                    </li>
                </ul>

                <p><strong>Important:</strong></p>
                <ul>
                    <li>
                        The English teacher reserves the right to exclude an employee from the course in the
                        event of repeated violations of the above-mentioned rules.
                    </li>
                    <li>
                        Hours spent in English training are not counted as working hours and must be compensated
                        for on the same day in proportion to the time spent on the training.
                    </li>
                </ul>

                <p>If you have any questions, feel free to reach out.</p>

                <p>Looking forward to seeing you in class!</p>
                """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A body HR has edited is left alone.
            migrationBuilder.Sql($"""
                UPDATE email_templates
                SET body = replace(body, '{Anchor}', '{TeacherLine}' || E'\n\n' || '{Anchor}')
                WHERE id = 'english_group_assigned'
                  AND replace(body, E'\r', '') = replace($seeded${SeededBody}$seeded$, E'\r', '');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                UPDATE email_templates
                SET body = replace(body, '{TeacherLine}' || E'\n\n', '')
                WHERE id = 'english_group_assigned'
                  AND replace(replace(body, E'\r', ''), '{TeacherLine}' || E'\n\n', '') = replace($seeded${SeededBody}$seeded$, E'\r', '');
                """);
        }
    }
}
