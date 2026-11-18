# LeoHR E2E tests

Playwright + TypeScript end-to-end tests, targeting the shared Test environment
(`https://test.leohr.leobit.co`).

## Setup

```
cd e2e
npm install
```

### Authentication

LeoHR logs in via Google SSO, so tests reuse a saved session instead of logging in each run.

**Locally**, capture yours once:

```
npx playwright codegen https://test.leohr.leobit.co --save-storage=playwright/.auth/user.json
```

Log in with your own account when the browser opens, then close it once you're back on LeoHR.
This saves your session to `playwright/.auth/user.json`, which `playwright.config.ts` reuses for
every run. The file is gitignored — never commit or share it. If tests start redirecting to a
login page, your session expired; re-run the command.

**In CI** there's no browser to log in with, so `auth.setup.ts` builds that same file from a
long-lived access token held in the `LEOHR_E2E_ACCESS_TOKEN` CI variable. To mint one:

1. In the Identity admin tool, raise the e2e client's **Access Token Lifetime** (seconds).
2. Log into https://test.leohr.leobit.co and copy `access_token` from localStorage
   (DevTools → Application → Local Storage).
3. Set the lifetime back if you changed it on a shared client.
4. Save the token as a **masked + protected** GitLab CI variable named
   `LEOHR_E2E_ACCESS_TOKEN`.

Test environment only — never use a Production token here. The job fails with a clear message
when the token expires, and minting a replacement is the same four steps.

## Running tests

```
npm test           # run all tests, all browsers
npm run test:ui    # interactive UI mode — step through a test and inspect the page
npm run test:headed # run with a visible browser window
npm run report      # open the last HTML report
```


