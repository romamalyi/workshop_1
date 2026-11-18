# Flow warnings come from one place

2026-10-27 · teammate (no author recorded) · verified 2026-11-18

Every place that says "this flow needs attention" uses `FlowWarnings.Build(flow, isCompleted, context)` with the context from `OnboardingFlowData.LoadWarningContext()`:

- the flows list and the flow detail (`warnings` on `OnboardingFlowDto`, via `OnboardingFlowMapper`);
- the banner and row icons on the Flows page (client side, from the same `warnings`);
- the count in Settings → Flow Responsible Person, through `IOnboardingFlowWarnings.CountFlowsWithWarningsAsync` (a `Shared/Onboarding/` interface implemented by `OnboardingFlowWarningsService`).

Don't count warnings with a separate query. The Settings line used to, skipped one warning kind, and showed a different number from the Flows banner (IPR-4047, IPR-4077).
