# Employee API client caches the employee list

2026-10-27 · dev-b · re-checked 2026-11-18

`EmployeeApiClient.GetAllAsync` returns the whole employee list. With feature flag `EmployeeDataCache` on, it caches it in `IMemoryCache`:

- key `employee-api:all`, lifetime `EmployeeApi:CacheExpirationMinutes` (default 60);
- a second copy under `employee-api:all:stale`, kept 12 hours. If a fetch fails and the stale copy exists, it is served with a warning in the log. With no copy at all, the exception goes to the caller;
- a static semaphore stops concurrent refreshes; `GetByIdAsync` reads from the same list.

With the flag off, the list is fetched once per client instance (per request) and there is no stale fallback.

Either way, calling `GetAllAsync` several times in one request is cheap. Don't pass an already fetched list through services just to save a call. `OnboardingFlowData` catches a directory failure and shows flows without warnings.
