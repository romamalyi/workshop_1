# The English app sends no email

2026-10-28 · teammate (no author recorded) · LeoHR side verified 2026-11-18

`leo-english-blazor` has no email code at all. It reports events to LeoHR (`POST api/integrations/events`), and LeoHR sends every English email from a template HR can edit:

- level changed → `EnglishLevelChanged` → template `english_level_changed`;
- student added to a group → `EnglishGroupAssigned` → template `english_group_assigned`.

Things you can't see from this repo:

- The English app calls LeoHR after its own save, once, with no retry. If LeoHR is down, that email is lost.
- It sends plain text; LeoHR builds all the HTML. Lists are joined with `\n`, empty optional values are left out, a cleared level is sent as `"-"`.
- A new English email means a new LeoHR event type and template, not code in the English app.
- Release order: LeoHR first, then the English app, or group events fail with 400.

Decided in IPR-4091.
