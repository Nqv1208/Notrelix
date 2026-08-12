---
document_id: FE-ADR-005
document_type: architecture-decision
status: Accepted
owner: frontend-architecture
applies_to:
  - frontend-authentication
  - frontend-session-management
  - frontend-cookie-auth
  - frontend-auth-refresh
  - frontend-session-expiry
  - frontend-auth-navigation
  - frontend-csrf-client-contract
evidence:
  - frontend/packages/foundation/contracts/src/client/api-client.ts
  - frontend/packages/foundation/contracts/src/client/csrf.ts
  - frontend/packages/features/auth/
  - frontend/packages/runtimes/web/src/runtime/session-event-bus.ts
  - frontend/apps/web/src/main.tsx
  - frontend/apps/web/src/providers/app-providers.tsx
  - frontend/apps/web/src/composition/application-services.ts
  - backend/src/Notrelix.Infrastructure/Auth/Csrf/CsrfProtector.cs
  - frontend/docs/architecture/api-and-contracts.md
  - frontend/docs/architecture/hosts-composition-routing.md
review_on:
  - frontend-auth-session-model-change
  - frontend-cookie-auth-change
  - frontend-token-storage-change
  - frontend-auth-refresh-change
  - frontend-session-expiry-flow-change
  - frontend-csrf-contract-change
  - frontend-auth-navigation-boundary-change
---

# FE-ADR-005 — Auth Session Model

## ID

`FE-ADR-005`

## Status

**Accepted**

## Date

**2026-07-09**

This date is preserved from the original ADR.

## Owners

**Current stewardship:** `frontend-architecture`

**Historical decision owner/authorship:** Not recorded explicitly in the original ADR.

Current stewardship does not imply historical authorship.

---

# Context

The original ADR addressed authentication/session architecture for the Notrelix client against the ASP.NET Core backend.

It recorded these historical conditions:

```text
backend uses secure cookie-based session authentication
tokens are HttpOnly rather than JavaScript-managed bearer tokens
unsafe requests require CSRF protection
401 can trigger POST /auth/refresh
refresh failure must route the user to sign-in
```

The frontend therefore needed clear boundaries for:

```text
credential ownership
HTTP client behavior
refresh coordination
session-expired signaling
feature auth state
navigation
browser/runtime adapters
```

---

# Decision

The original accepted decision contains four major architecture choices.

## 1. No JavaScript-managed access-token storage

The frontend does not store/read/manage access tokens in:

```text
memory
localStorage
other JavaScript-managed client token stores
```

for the browser session model.

Authenticated browser requests use:

```text
credentials: "include"
```

so the browser supplies the backend session cookie.

## 2. Decoupled navigation

Auth/session packages should not own the web router or app route tree.

Session-expiry/navigation response should be handled through an outward event/callback/composition pattern.

## 3. Decoupled API client/interceptors

The generic API client owns:

```text
credentials
refresh coordination
CSRF transport injection
generic auth failure handling
```

rather than duplicating those behaviors in feature components.

## 4. Typed session-expiry flow

Refresh/session failure produces a typed `SessionExpiredEvent` that can flow through runtime/application lifecycle and be handled by the web composition root.

---

# Durable identity of the decision

The durable identity of `FE-ADR-005` is:

> Browser authentication uses backend-controlled cookie sessions, not JavaScript-managed bearer-token storage; generic auth transport and refresh are centralized; session expiry is signaled through typed outward contracts so feature auth logic does not own application navigation.

The durable decision is **not**:

```text
one exact file path
one exact event-bus implementation
one exact CSRF cookie/header spelling
one exact router version
one exact sign-in URL
```

Those details can evolve while the decision remains intact, provided the security/boundary model remains the same.

---

# Historical CSRF wording

The original ADR stated:

```text
CSRF protection via X-XSRF-TOKEN or X-CSRF-TOKEN headers
```

for unsafe methods.

That statement is preserved as **historical record**.

It is not automatically the current backend contract.

Current producer evidence must be evaluated separately.

---

# Current backend CSRF contract

Current backend `CsrfProtector` defines:

```text
cookie:
csrf_token

header:
X-CSRF-Token

unsafe methods:
POST
PUT
PATCH
DELETE
```

It implements a Double Submit Cookie pattern.

The cookie is intentionally JavaScript-readable because the client must echo the token in the header.

---

# Current frontend CSRF implementation

Current frontend `getCsrfToken()` reads:

```text
<meta name="csrf-token">
or
XSRF-TOKEN cookie
```

and current generic API client sends:

```text
X-XSRF-TOKEN
```

for unsafe methods when a token is present.

This does not match the current backend producer contract:

```text
csrf_token
+
X-CSRF-Token
```

---

# FE-ADR-005-D1 — Current CSRF implementation is SOURCE_DEBT / CONTRACT DRIFT

The accepted auth-session architecture does not require the historical `X-XSRF-TOKEN` spelling.

The current backend contract is authoritative for the active browser anti-forgery protocol.

Therefore the current frontend mismatch is classified:

```text
SOURCE_DEBT
+
CONTRACT_CHANGE reconciliation required
```

Target if the backend contract remains unchanged:

```text
frontend reads csrf_token
frontend sends X-CSRF-Token
```

This source debt does **not** require superseding `FE-ADR-005`.

---

# FE-ADR-005-I1 — Browser access/refresh credentials remain JavaScript-inaccessible

The frontend MUST NOT migrate to:

```text
localStorage access token
sessionStorage access token
global in-memory bearer token
```

under the accepted decision.

A future shift to JavaScript-managed bearer tokens would be a consequential auth architecture change.

---

# Current API-client alignment

Current `createNotrelixClient()` uses:

```text
credentials: "include"
```

for normal API requests.

Its refresh request also uses:

```text
POST /auth/refresh
credentials: "include"
```

This aligns with the cookie-session decision.

---

# Single-flight refresh

Current API client maintains an instance-scoped:

```text
refreshPromise
```

so concurrent 401 responses share one refresh attempt.

After successful refresh, the original request is retried once.

---

# FE-ADR-005-I2 — Refresh is centralized and bounded

Feature/API wrappers MUST NOT each implement independent:

```text
401 → refresh → retry
```

logic.

The accepted model centralizes refresh in the generic client/session boundary.

Retry MUST remain bounded to avoid refresh loops.

---

# Refresh failure

Current API client maps refresh failure to a typed auth/network `AppError` and invokes:

```text
onSessionExpired(...)
```

with a typed `SessionExpiredEvent`.

Current event reasons include:

```text
refresh-rejected
refresh-network-failure
session-revoked
```

as the current type contract.

---

# FE-ADR-005-I3 — Session expiry is a typed outward event

Low-level transport/session code reports session failure outward.

It does not directly import:

```text
TanStack Router
Expo Router
app route modules
```

to navigate.

The host/application lifecycle decides the navigation response.

---

# Runtime session-event bus

Current `runtime-web` contains:

```text
createSessionEventBus()
```

with:

```text
publish
subscribe
clear
```

over the typed `SessionExpiredEvent`.

This is current evidence aligned with the event-driven boundary.

---

# Web composition root

Current `apps/web/src/main.tsx` injects:

```text
navigateToSignedOut
```

into `createWebApplicationServices()`.

The callback:

```text
sanitizes the current internal return URL
navigates to /sign-in
passes the sanitized redirect
```

through the app-owned TanStack Router.

This is strongly aligned with the accepted outward-navigation pattern.

---

# Application lifecycle

Current web application services receive:

```text
runtime.sessionEvents
navigateToSignedOut
queryClient
realtime
```

through the application lifecycle composition.

This keeps router knowledge at the app/composition edge instead of in the generic API client.

---

# FE-ADR-005-I4 — App owns auth-failure navigation

The composition root MAY decide:

```text
sign-in route
redirect behavior
replace/push
host-specific navigation
```

after receiving a session-expired signal.

The generic contracts/runtime MUST NOT hard-code app router implementation.

---

# AuthProvider historical rule

The original ADR stated that:

```text
AuthProvider accepts no props except children
```

and navigation on auth failure is external.

Current source uses:

```text
createAuthProvider({ api, endpoints })
```

to create a provider component.

The returned provider accepts:

```text
{ children }
```

only.

This preserves the intended provider runtime shape while moving construction dependencies into a factory.

---

# FE-ADR-005-I5 — Provider construction dependencies are injected outside render usage

A factory may receive:

```text
api
endpoints
```

to construct the provider behavior.

The rendered `AuthProvider` itself need not accept router/navigation props.

This is consistent with the durable decision when navigation remains external.

---

# Current feature-auth source alignment issue

The original ADR also stated:

```text
@notrelix/features-auth
must remain router-independent
must not depend on TanStack Router
must not hard-code route paths such as /sign-in
```

Current source does not fully satisfy this.

Current `@notrelix/features-auth/package.json` declares:

```text
@tanstack/react-router
```

as a dependency.

Current `login-form.tsx` imports:

```text
Link from @tanstack/react-router
```

and hard-codes route targets such as:

```text
/forgot-password
/sign-up
```

Current `use-login.ts` uses injected `navigate(...)` but still hard-codes:

```text
/home
```

as its fallback destination.

---

# FE-ADR-005-D2 — Feature-auth router independence currently has SOURCE_DEBT

This is a direct current-source conflict with the accepted decoupled-navigation rule.

Classification:

```text
SOURCE_DEBT
```

The target architecture remains:

```text
feature auth
→ router-independent navigation contract

app/web adapter/composition
→ TanStack Router
→ concrete route names/redirect policy
```

unless a future ADR intentionally changes the auth package boundary.

---

# Why this does not supersede FE-ADR-005

A source implementation violating an accepted decision is not evidence that the decision has changed.

There is currently no frontend ADR that says:

```text
features-auth now intentionally owns TanStack Router and app routes
```

Therefore:

```text
status stays Accepted
source is repaired toward decision/current architecture
```

rather than rewriting this ADR to bless current coupling.

---

# Current architecture interpretation

Current frontend architecture further refines the accepted decision:

```text
contracts client
→ generic HTTP / refresh / CSRF transport

runtime-web
→ browser/runtime adapters + session-event mechanism

features-auth
→ auth feature state/forms/semantics

apps/web
→ router/navigation composition

backend
→ authentication + authorization authority
```

The browser client is untrusted.

Frontend route guards/feature UI do not replace backend authorization.

---

# FE-ADR-005-I6 — Authentication and authorization remain distinct

Cookie/session validity answers:

```text
who is the principal?
```

It does not answer:

```text
may this principal mutate resource X?
```

Backend Application/security architecture remains authoritative for protected resource authorization.

---

# CSRF and authentication

CSRF protection is a browser request-forgery defense.

It is not authentication itself.

A valid CSRF token does not grant resource permission.

---

# FE-ADR-005-I7 — CSRF success does not imply authorization

The browser request flow is conceptually:

```text
session/authentication
+
CSRF validation for unsafe cookie-authenticated request
+
backend authorization
+
business validation
```

These checks remain separate.

---

# Mobile implications

The accepted ADR was written around the browser cookie-session model.

Mobile uses a separate runtime and may require platform-specific credential storage/transport.

The original ADR claimed `features-auth` portability because it should not assume web router structure.

That portability goal remains architectural intent.

---

# FE-ADR-005-I8 — Browser cookie mechanism does not leak into product/auth core

Do not force:

```text
document.cookie
browser CSRF helper
TanStack Router
```

into auth core shared with mobile.

Platform-specific transport remains outward.

---

# Current portability caveat

Current `features-auth` package includes:

```text
core
web
```

exports and web UI code.

Its core remains a separate exported surface.

However, the package-level dependency on TanStack Router and web components means the whole package is not purely platform-neutral.

This is acceptable only if:

```text
core remains safe
and
web entrypoint remains web-specific
```

but it does not satisfy the broad original statement that the entire package is router-independent.

The current architecture should continue moving routing concerns outward.

---

# FE-ADR-005-I9 — Package-level convenience does not redefine portable core

`@notrelix/features-auth/core` should remain independent from:

```text
web router
DOM
browser credential storage
```

even if the package also exposes web-specific UI entrypoints.

---

# Navigation dependency pattern

Current `use-login.ts` already uses an injected `NavigationDeps` abstraction:

```text
navigate(...)
getSearchParams()
```

This is directionally aligned with the accepted callback pattern.

The remaining hard-coded destinations should be owned/configured at the appropriate host/feature adapter boundary rather than embedded as app route policy.

---

# FE-ADR-005-I10 — Navigation capability and route policy are separate

Reusable auth behavior may request:

```text
navigate to successful-login destination
navigate to recovery destination
```

through a contract.

The app owns the actual route structure.

---

# Return URL security

Current app composition sanitizes the current return URL before passing it into sign-in navigation.

This is current security evidence.

---

# FE-ADR-005-I11 — Return/redirect input is untrusted

A redirect target MUST be validated/sanitized before navigation.

Do not permit arbitrary external return URLs from:

```text
query string
provider state
stored browser state
```

without an approved allowlist/internal-path policy.

---

# Session generation

Current Auth context exposes:

```text
sessionGeneration
```

derived from the current user identity.

Current realtime lifecycle uses session generation as part of its connection lifecycle.

This demonstrates that auth session state affects broader runtime cleanup/rebinding.

---

# FE-ADR-005-I12 — Principal/session transition is an application lifecycle boundary

Session change can require:

```text
query cache cleanup
realtime reconnect/disconnect
workspace state reset
navigation
```

according to current state/realtime architecture.

Auth feature UI alone does not own all transition effects.

---

# Logout

Logout invalidates the client session.

The host/application lifecycle must ensure old protected state does not remain active for a new/signed-out principal.

---

# FE-ADR-005-I13 — Logout clears protected client lifecycle

After logout/session replacement:

```text
old principal state
MUST NOT
continue as active authoritative UI
```

State/realtime cleanup requirements are defined in their canonical architecture documents.

---

# Auth error handling

Current generic API client normalizes auth failure into `AppError`.

Feature auth can map error semantics to forms/UI.

---

# FE-ADR-005-I14 — Feature UI consumes stable auth/error semantics, not transport prose

Do not branch on:

```text
message.includes(...)
```

for session protocol behavior.

Use typed error/session event contracts.

---

# Alternatives Considered

The original ADR does not contain a formal alternatives section.

However, the accepted statements make several rejected directions recoverable at a high level.

## Alternative A — JavaScript-managed bearer-token storage

The decision explicitly rejected:

```text
frontend-managed access tokens in memory/localStorage
```

in favor of browser HttpOnly cookie sessions.

The original ADR does not contain a detailed threat-model comparison beyond its stated consequence that HttpOnly cookies mitigate JavaScript token access/XSS exposure.

No additional historical tradeoffs are invented.

## Alternative B — Feature package owns navigation

The original decision explicitly rejected auth feature dependency on:

```text
apps/web router
TanStack Router
hard-coded sign-in path
```

for session-expiry navigation.

It chose an outward callback/event approach.

## Other alternatives

Detailed evaluation of:

```text
OAuth-only SPA bearer tokens
BFF architecture variants
sessionStorage tokens
global browser CustomEvent
Redux auth-event bus
```

was not recorded.

These are not inserted retrospectively.

---

# Consequences

The original ADR recorded:

## Secure by default

Session tokens are HttpOnly and not accessible to normal JavaScript.

## Portability

Auth feature behavior is intended to be reusable without assuming one router structure.

## Clear boundaries

The generic HTTP/auth transport sits in contracts/client while runtime-web stays focused on browser runtime adapters.

## Typed event-driven session failure

Session expiry uses a typed `SessionExpiredEvent` rather than an untyped global browser event.

These historical consequences are preserved.

---

# Current consequences

## Centralized refresh concurrency

Current client uses one instance-scoped refresh promise.

Concurrent 401s do not independently launch refresh requests within the same client instance.

## Host-owned redirect behavior

Current web composition owns sign-in navigation and return URL sanitization.

## Source debt is visible

The current router coupling inside `features-auth` is identified explicitly instead of being normalized into architecture.

## CSRF activation requires contract reconciliation

Browser anti-forgery cannot be safely certified until frontend and backend agree on cookie/header names.

---

# Compatibility / Migration

## Historical migration plan

**Not recorded in the original ADR.**

No detailed migration from bearer-token handling or another auth model is documented.

## Current compatibility contract

The browser path currently assumes:

```text
cookie-authenticated backend
credentials: include
POST /auth/refresh
typed session-expiry callback
browser CSRF for unsafe methods when enabled
```

## CSRF repair migration

If the backend producer contract remains:

```text
csrf_token
X-CSRF-Token
```

the frontend migration should:

```text
1. update CSRF cookie reader
2. emit X-CSRF-Token
3. remove stale XSRF-TOKEN/X-XSRF-TOKEN assumptions
4. test unsafe methods
5. test disabled/enabled rollout states as applicable
6. verify refresh/login/logout behavior
```

No new ADR is required unless the intended anti-forgery architecture changes.

## Router-decoupling repair migration

To restore the accepted boundary:

```text
1. identify route strings in features-auth web code
2. inject route/navigation intent from app/web adapter where needed
3. remove direct TanStack Router dependency from reusable auth surfaces when no longer required
4. keep app route construction in apps/web
5. update dependency/package manifests
6. add architecture tests preventing regression
```

---

# What does not require superseding this ADR

Examples:

```text
refresh endpoint implementation refactor
new SessionExpiredEvent reason
sign-in page visual redesign
route slug change handled at app boundary
AuthProvider factory refactor
cookie name change coordinated with backend without changing cookie-session model
CSRF header name correction to current backend contract
```

provided the durable session/boundary model remains the same.

---

# What can require superseding this ADR

Likely examples:

```text
browser moves to JS-managed bearer access tokens
auth tokens intentionally stored in localStorage/sessionStorage
feature-auth intentionally owns router/navigation architecture
refresh ownership moves out of the generic client into feature components
browser cookie-session model is replaced by another durable auth architecture
```

A mobile-specific credential strategy does not automatically supersede this browser auth decision if it is modeled as a separate runtime concern.

---

# Evidence

## Original ADR

The original record explicitly contains:

```text
Date: 2026-07-09
Status: Accepted
cookie-based HttpOnly session
credentials: include
CSRF on unsafe methods
401 → POST /auth/refresh
failed refresh → sign-in
no in-memory/localStorage access token
decoupled navigation
generic client/interceptor ownership
typed SessionExpiredEvent
```

## Current API client

Current:

```text
frontend/packages/foundation/contracts/src/client/api-client.ts
```

implements:

```text
credentials: include
single-flight refresh
POST /auth/refresh
one bounded retry
typed SessionExpiredEvent callback
```

## Current session-event bus

Current:

```text
frontend/packages/runtimes/web/src/runtime/session-event-bus.ts
```

implements typed:

```text
publish
subscribe
clear
```

for `SessionExpiredEvent`.

## Current web composition

Current:

```text
frontend/apps/web/src/main.tsx
frontend/apps/web/src/composition/application-services.ts
```

keeps concrete sign-in navigation at the app/composition boundary.

## Current AuthProvider

Current returned `AuthProvider` accepts:

```text
children
```

only, while its factory receives API/endpoints.

This remains aligned with the provider-composition intent.

## Current CSRF mismatch evidence

Frontend:

```text
XSRF-TOKEN
X-XSRF-TOKEN
```

Backend:

```text
csrf_token
X-CSRF-Token
```

This is current source/contract drift.

## Current router-coupling evidence

Current `@notrelix/features-auth`:

```text
declares @tanstack/react-router
imports Link from @tanstack/react-router
hard-codes /forgot-password
hard-codes /sign-up
hard-codes /home fallback
```

This is current source debt against the accepted navigation-decoupling rule.

---

# Evidence interpretation

Current evidence supports:

```text
cookie-session model
→ aligned

credentials include
→ aligned

centralized refresh
→ aligned

typed session expiry
→ aligned

app-owned signed-out redirect callback
→ aligned

AuthProvider children-only runtime API
→ aligned

CSRF wire spelling
→ SOURCE_DEBT / CONTRACT DRIFT

feature-auth router independence
→ SOURCE_DEBT
```

The decision is therefore still `Accepted`, with implementation debt that must be repaired.

---

# Current known alignment status

At normalization time:

```text
Decision:
cookie-session / centralized refresh / typed outward session expiry
→ still implemented

No superseding ADR
→ found

Therefore:
Status = Accepted
```

Current source deviations do not alter the decision status automatically.

---

# Historical fidelity notes

This normalization does not claim:

- original decision owner;
- exact original CSRF cookie name;
- that both historical CSRF header names were simultaneously valid backend contract;
- that current source paths existed on 2026-07-09;
- that current SessionExpiredEvent reasons were all present originally;
- that current feature-auth router coupling is accepted architecture;
- that current CSRF source mismatch is historical intent.

---

# Relationship to current architecture

Read:

```text
../architecture/hosts-composition-routing.md
../architecture/api-and-contracts.md
../architecture/state-query-mutations.md
../architecture/realtime.md
../architecture/dependency-boundaries.md
```

for current operating rules.

Use this ADR for historical rationale behind the auth/session foundation.

---

# Security relationship

Frontend authentication is one layer.

Backend remains authoritative for:

```text
credential validation
resource authorization
tenant isolation
business permission
```

A browser route guard, cookie presence, or valid CSRF token does not replace those controls.

---

# FE-ADR-005-I15 — Client cannot self-authorize protected operations

The frontend may:

```text
hide
disable
redirect
```

for UX.

The backend must still authorize the actual protected operation.

---

# Testing obligations

Changes to this architecture should include, as applicable:

```text
API client refresh tests
single-flight refresh tests
session-expired callback tests
return URL sanitization tests
logout/principal cleanup tests
CSRF emitted-header tests
enabled backend CSRF integration test
router-dependency architecture test
feature-auth core portability test
```

---

# FE-ADR-005-I16 — Auth architecture needs negative-path evidence

A happy successful login test is insufficient.

Critical failures include:

```text
refresh rejected
refresh network failure
invalid redirect
missing/invalid CSRF
logout stale state
route coupling regression
```

---

# Review triggers

Review this ADR when proposing:

```text
browser token-storage change
cookie-session replacement
refresh ownership change
session-expiry signaling change
auth feature router ownership change
browser anti-forgery architecture change
principal lifecycle foundation change
```

Routine sign-in UI changes do not reopen the decision.

---

# Supersedes

**None.**

No earlier frontend ADR is recorded as superseded by `FE-ADR-005`.

---

# Superseded By

**None.**

At normalization time, no recorded frontend ADR supersedes `FE-ADR-005`.

---

# Normalization note

This normalization preserves:

```text
Date
Status
cookie-session decision
no JavaScript access-token storage
decoupled navigation intent
centralized API client/refresh intent
typed SessionExpiredEvent flow
recorded consequences
```

It adds:

```text
Owners
decision identity
current source evidence
backend CSRF producer evidence
CSRF source-debt classification
feature-auth router source-debt classification
Alternatives Considered
Compatibility / Migration
Supersedes
Superseded By
testing/review obligations
```

Historical protocol wording is preserved as historical record rather than rewritten into today's backend contract.

The accepted decision itself has not been changed.
