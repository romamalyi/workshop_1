# 409 is only for duplicates

2026-10-19 · dev-b

Every duplicate-resource failure returns 409 on every endpoint: `ResultExtensions.DetermineStatusCode` maps `DuplicateResourceError` to `Conflict` (body has `errorCode: DUPLICATE_RESOURCE`). Everything else that breaks a business rule is `BusinessRuleError`, which is 400.

- Why: IPR-4026 added a `ConflictError` just so a duplicate type name returned 409, while other duplicates returned 400. The same kind of error had different codes on different endpoints. IPR-4040 deleted `ConflictError`.
- Rules that look like duplicates but aren't (`OverallActiveSubmittedOnce`, `AlreadyCompletedInLevel`) stay 400 with `BusinessRuleError`.
- Don't bring back a separate conflict/in-use error class for 409.
