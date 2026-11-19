# IPR-4071: Flows page search and filters

2026-10-31 · dev-c

**Now:** the Onboarding Flows page shows every flow in one list, with no search and no filters. As the list grows, HR has to scroll to find a flow.

**To do:**

- Search box that filters the list by the new hire's name as you type.
- Status filter, one or more of: Created, Created — Unresolved, Completed.
- Responsible HR filter (the flow's responsible person, from IPR-4031).
- Default view: only active flows (Created and Created — Unresolved). Completed flows show only when selected.
- Use the shared multi-select filter component (IPR-4009), the same as the HR filter on Employees and Promotions.
- Search and filters combine.
- When nothing matches, show "No flows match your filters" instead of "No onboarding flows yet".
- Keep it simple: filter in the browser unless the list is too big for that.
