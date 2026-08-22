---
document_id: ADR-005
document_type: architecture-decision
status: Accepted
owner: backend-architecture
applies_to:
  - backend
  - backend-api
  - backend-security
  - browser-authentication
  - csrf
evidence:
  - backend/docs/decisions/ADR-003-csrf-protection.md
  - backend/src/Notrelix.Infrastructure/Auth/Csrf/CsrfProtector.cs
  - backend/src/Notrelix.API/Middleware/CsrfValidationMiddleware.cs
  - backend/src/Notrelix.API/Endpoints/Identity/Auth/
  - backend/src/Notrelix.API/appsettings.json
  - frontend/packages/foundation/contracts/src/client/csrf.ts
  - frontend/packages/foundation/contracts/src/client/api-client.ts
  - frontend/docs/decisions/FE-ADR-005-auth-session-model.md
review_on:
  - csrf-model-change
  - browser-credential-model-change
  - auth-cookie-samesite-change
  - frontend-api-origin-model-change
---

# ADR-005: Cross-Origin CSRF Bootstrap Protocol

## ID

`ADR-005`

## Status

Accepted

## Date

2026-08-22

## Owners

Current stewardship:

- `backend-architecture`

---

# Context

`ADR-003` accepted the Double Submit Cookie pattern for browser CSRF protection. Its transport assumptions were recorded under a deployment model where the browser client reads the CSRF token cookie directly with JavaScript and echoes it into a request header.

That assumption conflicts with the supported cross-origin topology:

```text
frontend origin ≠ API origin
CSRF cookie is host-scoped to the API origin
browser JavaScript on the frontend origin cannot read an API-host cookie
```

The current source therefore contains an incompatible cross-stack contract:

Backend (ADR-003 implementation):

```text
cookie: csrf_token (host-scoped, SameSite=Strict)
header: X-CSRF-Token
middleware issues the cookie implicitly on every GET
frontend must read the cookie to participate
```

Frontend (current source):

```text
reads <meta name="csrf-token"> or an XSRF-TOKEN cookie
sends X-XSRF-TOKEN
refresh path uses a raw fetch that sends no CSRF header at all
```

Neither spelling matches, and renaming only the frontend header cannot work: a cross-origin SPA cannot read the API host-scoped cookie regardless of its name. Additionally, `SameSite=Strict` on the CSRF cookie would prevent the browser from sending the cookie on legitimate cross-site mutations, breaking the Double Submit comparison entirely.

The problem was classified in the Phase 13 closure of the Identity & Accounts workstream as `SOURCE_DEBT` + required `CONTRACT_CHANGE` reconciliation (`FE-ADR-005-D1` records the frontend side).

The Double Submit Cookie pattern itself remains sound for this threat model. What changes is how the token reaches the client and which requests the protection applies to.

---

# Decision

## Protocol

The browser anti-forgery protocol is:

```text
bootstrap (safe):
  GET /api/v1/auth/csrf
  → server generates cryptographically random token
  → Set-Cookie: csrf_token=<token>
  → response body carries the same token

mutation (unsafe browser request):
  Cookie: csrf_token=<token>        ← attached by the browser automatically
  X-CSRF-Token: <token>             ← sent by the client from memory
  → fixed-time equality validation
```

The client reads the token from the **response body**, never from the API-host cookie. Because both values originate from one bootstrap response, cross-origin clients participate without reading API cookies.

## Bootstrap endpoint ownership

One safe GET bootstrap endpoint lives in the existing Identity/Auth endpoint group (`/api/v1/auth/csrf`). No new top-level API group is created solely for CSRF. The endpoint is public: it must be callable before authentication so that login and other session-establishing mutations can be protected.

Response shape is minimal and typed:

```json
{
  "token": "..."
}
```

The same generated token value is placed in the `csrf_token` cookie.

## Cookie policy

```text
production:
  HttpOnly = true
  Secure   = true
  SameSite = None
  Path     = /
  host-scoped (no Domain widening)

development:
  HttpOnly = true
  Secure   = false
  SameSite = Lax
  Path     = /
```

The cookie is now `HttpOnly`. Under ADR-003 the cookie had to be JavaScript-readable because the client copied it into the header; under this decision the client receives the token from the bootstrap response body, so exposing it to JavaScript is no longer necessary and is removed as an XSS hardening measure.

`SameSite=None` (with `Secure`) in production is required so the browser actually attaches the CSRF cookie to legitimate cross-origin mutations from the supported frontend topology. The cookie `Domain` MUST NOT be widened to sibling subdomains merely to let JavaScript read it — JavaScript no longer needs to read it at all.

Token lifetime remains bounded by the cookie `MaxAge`; renewal happens by calling the bootstrap endpoint again. Each bootstrap call generates a fresh token and overwrites the cookie.

## Applicability classification

CSRF validation applies when all of the following hold:

```text
Security:Csrf:Enabled = true
AND the request method is state-changing (POST / PUT / PATCH / DELETE)
AND the request relies on ambient browser credentials
```

A request relies on ambient browser credentials when it presents no explicit non-ambient credential in the `Authorization` header. Requests presenting an explicit `Authorization` credential — canonical API-token principals (`ntk_v1.` prefix bearer secrets) and any other explicitly presented bearer credential used by native/non-browser clients — are outside the browser CSRF threat model, because a cross-site attacker cannot cause a victim's browser to attach an `Authorization` header.

Public unsafe endpoints are not blanket-exempt. Operations that establish or mutate ambient cookie session state (login, refresh, logout and equivalents) are CSRF-required for the ambient browser mode because they are exactly the operations login-CSRF and session-fixation attacks target.

The classification is derived from request evidence and semantic metadata. It MUST NOT be implemented as a hard-coded list of endpoint route strings.

## Middleware behavior

Implicit CSRF-cookie issuance on every GET is removed. The middleware validates unsafe ambient-browser requests only, per the applicability classification above, and delegates token generation/comparison to the Infrastructure CSRF protector.

Safe requests never require or consume CSRF material.

## Error mapping

CSRF failures use the canonical API ProblemDetails error writer with a stable security error code, correlation metadata consistent with API policy, and no token material in responses or logs. A middleware-local anonymous JSON shape bypassing the canonical writer is prohibited.

## Frontend participation

The shared frontend transport owns an instance-scoped, memory-only CSRF token provider:

```text
ensureCsrfToken():
  reuse in-memory token if present
  else single-flight GET /api/v1/auth/csrf with credentials: include
```

Unsafe requests attach `X-CSRF-Token` from that provider. The refresh request reuses the same CSRF-aware low-level primitive instead of a raw fetch branch. On a stale/rotated-token rejection the client clears its in-memory token, re-bootstraps once, and retries at most once; unbounded retry loops are prohibited. No localStorage/sessionStorage/meta-tag/document-cookie dependence is permitted.

Frontend detail is owned by the frontend auth/session model (`FE-ADR-005`, which does not require supersession for this correction).

## Non-browser principal behavior

API-token and other explicit-header credentials are never subject to browser CSRF validation. They remain fully subject to normal authentication and Governance authorization.

## Feature-flag rollout

Enablement remains controlled by `Security:Csrf:Enabled`, default-safe disabled during implementation. Production enablement occurs only after cross-stack integration proof passes. Disabling the flag is an operational rollout mechanism, not evidence that unprotected mutation is safe.

---

# Decision invariants

### CSRF and authorization remain separate

Passing CSRF grants no resource permission. Application authorization still decides every protected operation.

### One convention

`csrf_token` + `X-CSRF-Token` is the only accepted wire spelling. Parallel legacy conventions (`XSRF-TOKEN`, `X-XSRF-TOKEN`) must not be kept alive as a second mechanism.

### Token is not persisted client-side

The frontend keeps the token in memory only. localStorage/sessionStorage/persistent adapters are forbidden storage locations for CSRF tokens.

### Bootstrap is the only issuance path

The middleware does not silently issue cookies on unrelated GETs; only the bootstrap endpoint (and any future explicitly designed issuance surface) sets the cookie.

### Explicit credentials stay outside browser CSRF

Non-ambient credentialed requests are never rejected for missing browser CSRF material.

---

# Alternatives Considered

### Alternative A — Keep ADR-003 transport, rename frontend header/cookie only

Rejected. A cross-origin SPA cannot read the API host-scoped cookie, so the frontend could never learn the token value. The mismatch is structural, not cosmetic.

### Alternative B — Synchronizer tokens (server-side session/token store)

Rejected for the same reasons recorded in ADR-003: it would introduce server-side CSRF state management incompatible with the stateless JWT-oriented authentication model, adding storage and latency solely for token lifecycle.

### Alternative C — HTML `<meta name="csrf-token">` bootstrap injected by the API

Rejected. It couples the JSON API to HTML rendering concerns, requires every entry page to pass through a server-rendered surface, and breaks for API-only consumers. It also cannot protect flows that begin without a full page load from the API origin.

### Alternative D — SameSite=Strict authentication cookies instead of CSRF tokens

Rejected. The supported deployment model requires cross-origin frontend → API mutation; strict SameSite on the authentication cookie would break the product's own legitimate traffic (same reason recorded in ADR-003 against Lax).

---

# Consequences

### Positive

- Cross-origin browser clients can obtain and return the token without reading API-host cookies.
- The CSRF cookie stops being JavaScript-readable (`HttpOnly`), removing an unnecessary XSS exposure.
- Implicit per-GET cookie issuance disappears; issuance is explicit and observable.
- Validation scope is evidence-based: ambient browser mutations are protected while API-token/native traffic is unaffected.
- Frontend refresh/mutation paths converge on one CSRF-aware transport primitive.
- Rollout remains flag-controlled and reversible per environment.

### Negative / operational

- Every unsafe browser flow gains a bootstrap dependency; clients that skip bootstrap will be rejected once the flag is enabled.
- Stale tokens (expired cookie, rotated value) surface as 403s that clients must recover from deterministically (bounded re-bootstrap).
- `SameSite=None` makes the CSRF cookie travel with more cross-site requests than `Strict` would allow; this is required for the protocol to function and is safe because the cookie alone grants nothing without the matching header.

### Security boundary consequence

The frontend must implement the bootstrap-and-echo contract for ambient mutation to work while protection is enabled. Enabling backend protection before supported browser clients send the header will reject legitimate mutations; the rollout sequence below governs ordering.

---

# Compatibility / Migration

The compatibility sequence is:

```text
1. backend implements bootstrap + classifier + canonical error mapping (flag off)
2. frontend adopts the bootstrap provider and X-CSRF-Token emission
3. cross-stack integration proof (bootstrap → login → mutation → refresh → mutation)
4. enable Security:Csrf:Enabled in the intended environment
5. monitor/recover by configuration if rollout fails
```

No database migration is required. No public REST contract breaks: the bootstrap endpoint is additive, and previously unprotected requests gain a requirement only when the operator enables the flag.

If the browser credential model, origin topology, or authentication-cookie SameSite requirements change materially, this decision's assumptions must be reevaluated and superseded if the foundation changes.

---

# Evidence

## Canonical current architecture

- `backend/docs/architecture/api-and-contracts.md`
- `backend/docs/architecture/security-tenancy-authorization.md`
- `backend/docs/operations/configuration-and-runtime.md`

## Source

- `backend/src/Notrelix.API/Middleware/CsrfValidationMiddleware.cs`
- `backend/src/Notrelix.Infrastructure/Auth/Csrf/CsrfProtector.cs`
- `backend/src/Notrelix.API/Endpoints/Identity/Auth/` (bootstrap endpoint registration)
- `backend/src/Notrelix.API/appsettings.json` (`Security:Csrf:Enabled`)
- `frontend/packages/foundation/contracts/src/client/csrf.ts`
- `frontend/packages/foundation/contracts/src/client/api-client.ts`

## Tests / gates

Primary proof surfaces:

```text
backend/tests/Notrelix.API.Tests/Identity/Auth/Csrf*      (host-level protocol tests)
backend/tests/Notrelix.Architecture.Tests/                 (applicability inventory gate)
frontend contracts package client tests                    (transport behavior)
```

Expected proof includes:

```text
disabled flag → middleware does not interfere
bootstrap → body token + csrf_token cookie, equal values
production cookie policy attributes (Secure/SameSite/Path/HttpOnly)
safe GET → no validation, no implicit issuance
unsafe ambient + valid pair → continues
unsafe ambient + missing cookie/header/mismatch → canonical 403 ProblemDetails
Authorization-credentialed unsafe request → not browser-CSRF rejected
frontend single-flight bootstrap, memory-only token, header emission
refresh uses the shared CSRF-aware primitive
bounded recovery on stale token
```

---

# Supersedes

`ADR-003-csrf-protection.md`

Superseded specifically where its assumptions require a JavaScript-readable CSRF cookie, implicit per-GET issuance, and `SameSite=Strict` cookie behavior. The Double Submit Cookie pattern itself, fixed-time comparison, state-changing-method coverage, and feature-flag rollout model are preserved and carried forward by this decision.

---

# Superseded By

`None`

---

# Normalization note

This ADR is created as part of the Phase 13 closure execution (`P13-CSRF-01`). It preserves ADR-003's historical record unchanged apart from bidirectional supersession metadata, as required by backend decision governance.
