using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeoHR.Server.Migrations.LeoHr
{
    /// <inheritdoc />
    public partial class AddEnglishEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Raw SQL, not HasData: HR edits these templates, and a later seed change must not overwrite them.
            migrationBuilder.Sql("""
                INSERT INTO email_templates (id, label, subject, body, checklist) VALUES (
                'english_level_changed',
                'English Level Changed',
                'Congratulations on Your New English Level!',
                $body$<p>Dear {employeeName},</p>

                <p>
                    Congratulations on completing your English level assessment! We sincerely appreciate the
                    time, effort, and consistency you've invested in developing your language skills. Your
                    progress reflects real commitment to your professional growth.
                </p>

                <p>
                    We're pleased to confirm that your English level has been updated to {currentLevel}.
                    As a next step, we'd like to share a short summary of your key strengths and areas for
                    further development, along with personalized recommendations to help you continue progressing.
                </p>

                <p>
                    <strong>Current Level:</strong> {currentLevel}<br />
                    <strong>Previous Level:</strong> {previousLevel}
                </p>

                <p><strong>Strengths &ndash; Skills at a High Level:</strong></p>
                {strengths}

                <p><strong>Priority Areas for Improvement:</strong></p>
                {improvementAreas}

                <p>
                    We're proud of your progress and confident this achievement will continue to support your
                    professional growth.
                </p>
                <p><strong>KEEP UP THE GREAT WORK ON YOUR ENGLISH JOURNEY!</strong></p>$body$,
                '[]'::jsonb)
                ON CONFLICT (id) DO NOTHING;
                """);

            migrationBuilder.Sql("""
                INSERT INTO email_templates (id, label, subject, body, checklist) VALUES (
                'english_group_assigned',
                'English Group Assigned',
                'Confirmation of Enrollment in English Classes',
                $body$<p>Dear {employeeName},</p>

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

                <p>Looking forward to seeing you in class!</p>$body$,
                '[]'::jsonb)
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM email_templates WHERE id IN ('english_level_changed', 'english_group_assigned');");
        }
    }
}
