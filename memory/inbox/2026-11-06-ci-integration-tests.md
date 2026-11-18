# CI runs the backend integration tests

2026-11-06 · dev-c

The `integration-test` job in `.gitlab-ci.yml` runs `dotnet test LeoHR.Server.IntegrationTests` on every MR and on `develop`; a failing test fails the pipeline.

- Our runners can't run Docker inside a job, so Testcontainers can't start Postgres there. The job uses `postgres:16` as a GitLab service and sets `LEOHR_TEST_CONNECTION_STRING`. When it's set, `LeoHrApiFactory` uses that database; on a developer machine the tests start their own container.
- `dotnet test` runs with `-p:ShouldRunNpmInstall=false`, because the SDK image has no Node and the client `.esproj` would try `npm install`.
- No `retry:` on the job, so a flaky test isn't hidden.
- The job also runs on `develop` and `master`. On `develop` the Test deploy doesn't wait for it (the MR is the gate); on `master` `deploy-to-production` needs it, so a failing test blocks Production.

Rejected: Docker-in-Docker or mounting the Docker socket. Both need a runner admin change and give every job control of the host.
