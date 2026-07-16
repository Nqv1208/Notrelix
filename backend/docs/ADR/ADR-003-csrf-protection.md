# ADR-003: CSRF Protection via Double Submit Cookie

## Status

Accepted

## Context

Notrelix uses `SameSite=None` cookies in production for cross-origin API access from the frontend. This makes the application vulnerable to CSRF attacks — a malicious site can make authenticated requests using the user's cookies.

Traditional Synchronizer Token patterns require server-side session state, which Notrelix doesn't use (stateless JWT). Synchronizer Token would require storing tokens in Redis or the database.

## Decision

We implement the **Double Submit Cookie** pattern:

1. On any GET request, the API sets a `csrf_token` cookie (readable by JavaScript, NOT HttpOnly).
2. On state-changing requests (POST/PUT/PATCH/DELETE), the frontend reads the cookie and sends the same value in the `X-CSRF-Token` header.
3. `CsrfValidationMiddleware` compares cookie value vs header value using constant-time comparison.
4. If they don't match, the request is rejected with 403.

### Why Double Submit?

- **No server-side state**: Token is generated randomly per request, validated by equality check.
- **Works with SPA**: Frontend reads cookie via `document.cookie`, sets header in fetch/axios interceptor.
- **Feature-flagged**: `Security:Csrf:Enabled` (default: `false`) allows rollback.
- **Scope**: Only applies to state-changing methods. GET requests set the cookie.

### Why not Synchronizer Token?

- Requires server-side session storage (Redis/DB) — adds latency and state management.
- Notrelix is designed as stateless API with JWT cookies.

### Why not SameSite=Lax?

- Frontend and API are on different origins (cross-origin requests require `SameSite=None`).
- `SameSite=Lax` would block cross-origin POST requests.

## Implementation

- `CsrfProtector` (Infrastructure): Token generation, cookie setting, constant-time validation.
- `CsrfValidationMiddleware` (API): Sets cookie on GET, validates on state-changing requests.
- Configuration: `Security:Csrf:Enabled` in appsettings.json (default: false).
- Frontend: Read `csrf_token` cookie, set `X-CSRF-Token` header on all mutation requests.

## Consequences

- CSRF protection is opt-in via feature flag for safe rollout.
- Constant-time comparison prevents timing attacks.
- `SameSite=Strict` on the CSRF cookie itself (not the auth cookie) prevents cookie being sent on cross-site navigation.
- Auth cookies remain `SameSite=None` (required for cross-origin API access).
