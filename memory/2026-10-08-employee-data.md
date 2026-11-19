# Employee data is personal data

2026-10-08 · dev-a

LeoHR holds employee records, promotions, activities and onboarding data. Under GDPR that is personal data.

- Never log personal fields (names, emails) and never put them in error messages.
- Treat logging, exports and new endpoints as sensitive.
- Only the HR roles that need the data should see it. Flag any change that widens who can.
