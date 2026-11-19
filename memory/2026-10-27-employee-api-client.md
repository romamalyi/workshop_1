# Employee API client caches the employee list

2026-10-27 · dev-b

`EmployeeApiClient.GetAllAsync` returns the whole employee list and caches it in `IMemoryCache`:

- key `employee-api:all`, lifetime `EmployeeApi:CacheExpirationMinutes` (default 60);
- a second copy under `employee-api:all:stale`, kept 24 hours. If a fetch fails and the stale copy exists, it is served with a warning in the log. With no copy at all, the exception goes to the caller;
- a static semaphore stops concurrent refreshes; `GetByIdAsync` reads from the same list.

So an Employee API outage shorter than a day doesn't reach users; a longer one shows up as errors. And calling `GetAllAsync` several times in one request is cheap: don't pass an already fetched list through services just to save a call. `OnboardingFlowData` catches a directory failure and shows flows without warnings.
