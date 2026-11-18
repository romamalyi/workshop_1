# "Today" in server filters comes from the browser

2026-10-28 · dev-c

When the server filters or counts by a date relative to today (Overdue, Due today), the client sends its local date as `today=yyyy-MM-dd`. The server uses `DateOnly.FromDateTime(DateTime.UtcNow)` only when the parameter is missing.

Why: the status pills on each row are computed in the browser from the local date. With the server's UTC date, between about 00:00 and 03:00 Kyiv time the Overdue tab and its counts disagree with the pills.

Rejected: server UTC, like the rest of the server. Simpler, but visibly wrong for a few hours a day.

First used on the onboarding Tasks page (`OnboardingTaskFilterModel.Today`).
