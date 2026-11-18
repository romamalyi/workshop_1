import { test as setup } from '@playwright/test';
import * as fs from 'node:fs';
import * as path from 'node:path';

const AUTH_FILE = path.join(__dirname, 'playwright', '.auth', 'user.json');

function decodeJwtPayload(jwt: string): Record<string, any> {
  const parts = jwt.split('.');
  if (parts.length !== 3) {
    throw new Error('LEOHR_E2E_ACCESS_TOKEN is not a JWT — it may have been truncated.');
  }
  return JSON.parse(Buffer.from(parts[1], 'base64url').toString('utf8'));
}

setup('authenticate', async () => {
  const accessToken = process.env.LEOHR_E2E_ACCESS_TOKEN;

  // Local runs reuse the session captured by `playwright codegen` instead.
  if (!accessToken) {
    if (!fs.existsSync(AUTH_FILE)) {
      throw new Error(
        `No saved session at ${AUTH_FILE} and no LEOHR_E2E_ACCESS_TOKEN set.\n` +
          'Capture one locally (see e2e/README.md) or set the CI variable.'
      );
    }
    setup.skip(true, 'Reusing the locally captured session.');
    return;
  }

  const claims = decodeJwtPayload(accessToken);
  const expiresAt = Number(claims.exp) * 1000;

  if (expiresAt < Date.now()) {
    throw new Error(
      `LEOHR_E2E_ACCESS_TOKEN expired on ${new Date(expiresAt).toISOString()} — ` +
        'mint a new one (see e2e/README.md).'
    );
  }

  const audience = [claims.aud].flat();
  if (!audience.includes('LeoHR')) {
    throw new Error(`Token audience is [${audience.join(', ')}] — expected "LeoHR".`);
  }

  // id_token_claims_obj is needed even without an id_token: loadUserProfile() rejects
  // unless a stored `sub` matches the one /connect/userinfo returns.
  const entries = [
    { name: 'access_token', value: accessToken },
    { name: 'expires_at', value: String(expiresAt) },
    { name: 'access_token_stored_at', value: String(Date.now()) },
    { name: 'id_token_claims_obj', value: JSON.stringify({ sub: claims.sub }) },
    { name: 'granted_scopes', value: JSON.stringify([claims.scope].flat()) },
  ];

  const origin = new URL(process.env.BASE_URL ?? 'https://test.leohr.leobit.co').origin;

  fs.mkdirSync(path.dirname(AUTH_FILE), { recursive: true });
  fs.writeFileSync(
    AUTH_FILE,
    JSON.stringify({ cookies: [], origins: [{ origin, localStorage: entries }] }, null, 2)
  );

  const daysLeft = Math.round((expiresAt - Date.now()) / 86_400_000);
  console.log(`Authenticated as "${claims.sub}" for ${origin} (token valid ${daysLeft} more days)`);
});
