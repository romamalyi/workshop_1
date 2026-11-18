const { env } = require('process');

// When the full stack runs locally (IDE / dotnet run), these env vars point the proxy
// at the local backend. When a frontend dev runs `ng serve` on its own with no backend,
// there are no such vars, so we fall back to the deployed test backend — that way the UI
// works standalone against the same test environment used for /connect below.
const localBackend = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : null;

const apiTarget = localBackend ?? 'https://test.leohr.leobit.co';

const PROXY_CONFIG = [
  {
    context: ["/api"],
    target: apiTarget,
    // Rewrite the Host header only when hitting the remote test backend.
    changeOrigin: !localBackend,
    secure: false
  },
  {
    // XHR-only: token exchange, userinfo, JWKS — NOT /connect/authorize (browser redirect, goes directly to identity server)
    context: ["/connect/token", "/connect/userinfo", "/connect/revocation", "/.well-known/openid-configuration/jwks"],
    target: "https://test.identity.leobit.co",
    changeOrigin: true,
    secure: false
  }
];

module.exports = PROXY_CONFIG;

