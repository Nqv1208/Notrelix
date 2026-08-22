---
document_id: ADR-003
document_type: architecture-decision
status: Superseded
owner: backend-architecture
superseded_by: ADR-005-csrf-cross-origin-bootstrap.md
applies_to:
  - backend
  - backend-api
  - backend-security
  - browser-authentication
  - csrf
evidence:
  - backend/docs/architecture/api-and-contracts.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/src/Notrelix.API/Middleware/CsrfValidationMiddleware.cs
  - backend/src/Notrelix.Infrastructure/Auth/Csrf/CsrfProtector.cs
  - backend/src/Notrelix.API/appsettings.json
  - backend/tests/Notrelix.API.Tests/
review_on:
  - decision-superseded
  - browser-credential-model-change
  - csrf-model-change
  - auth-cookie-samesite-change
  - frontend-api-origin-model-change
---

# ADR-003: CSRF Protection via Double Submit Cookie

## ID

`ADR-003`

## Status

Superseded by [ADR-005 — Cross-Origin CSRF Bootstrap Protocol](ADR-005-csrf-cross-origin-bootstrap.md)

This record is preserved as historical decision evidence. Its Double Submit Cookie pattern and feature-flag rollout model remain in force; its transport assumptions (JavaScript-readable cookie, implicit per-GET issuance, `SameSite=Strict` cookie behavior) are superseded by ADR-005.

## Date

`2026-08-11`

Historical note:

```text
The original ADR did not contain an explicit Date section.
This date is recovered from the Git history entry that introduced/preserved
the ADR in the current documentation refoundation commit.
```

## Owners

Current stewardship:

- `backend-architecture`

Historical authorship/owner:

```text
Not recorded explicitly in the original ADR.
```

This normalized record does not infer historical authorship.

---

## Context

The original ADR records the browser deployment/authentication assumption that motivated the decision:

```text
frontend and API are cross-origin
+
authentication cookies require SameSite=None
```

With cross-origin cookie-authenticated requests, the browser can attach authentication cookies to a state-changing request initiated by another site under conditions where CSRF protection is required.

The backend therefore needs a mechanism that can distinguish:

```text
a legitimate browser mutation from the Notrelix frontend
```

from:

```text
a cross-site request that rides the user's authentication cookie
```

without turning Application resource authorization into the CSRF mechanism.

The original ADR also records that Notrelix does not use a traditional server-side web session whose session record could naturally store a synchronizer token.

The security model therefore needed a browser anti-forgery mechanism compatible with:

```text
SPA frontend
cross-origin API
cookie-authenticated browser flow
stateless JWT-oriented authentication model
```

as documented by the original decision.

---

## Decision

Notrelix uses the **Double Submit Cookie** pattern for the browser CSRF boundary covered by this ADR.

### Token issuance

On eligible GET requests, the API ensures a CSRF token cookie exists.

Current implementation uses:

```text
cookie name
→ csrf_token
```

The token is intentionally readable by JavaScript because the SPA must copy the token into a request header.

### Mutation request

For state-changing methods:

```text
POST
PUT
PATCH
DELETE
```

the frontend sends the cookie's token value in:

```text
X-CSRF-Token
```

### Validation

`CsrfValidationMiddleware` delegates comparison to the Infrastructure CSRF protector.

The current protector checks:

```text
csrf_token cookie exists
X-CSRF-Token header exists
cookie token == header token
```

using a fixed-time comparison.

If validation fails, the API rejects the request with:

```text
403 Forbidden
```

and a CSRF-specific safe problem/error response.

### Cookie properties

Current `CsrfProtector` configures the CSRF cookie with properties including:

```text
HttpOnly = false
SameSite = Strict
Path = /
bounded MaxAge
Secure in Production
```

The CSRF cookie is separate from the authentication cookie.

The original ADR explicitly records that the authentication cookie can remain `SameSite=None` for the cross-origin frontend/API model while the CSRF token cookie itself uses stricter behavior.

### Configuration / rollout

The mechanism is controlled by:

```text
Security:Csrf:Enabled
```

The original decision deliberately made CSRF enablement rollout-controllable.

Feature configuration changes the runtime mechanism state.

It does not change the underlying security model that cookie-authenticated cross-origin state-changing browser requests require an approved CSRF protection path.

---

## Decision invariants

### CSRF and authorization are separate

CSRF verifies the browser request-origin/token relationship.

Application authorization still determines:

```text
may this principal perform this action on this resource?
```

Passing CSRF does not grant resource permission.

### State-changing requests are protected

The covered browser credential mode must not allow mutation solely because the authentication cookie is attached.

### Token values must match

A state-changing request requires both:

```text
csrf_token cookie
+
X-CSRF-Token header
```

with an accepted equality check.

### Token is readable by the SPA

The CSRF token cookie cannot be `HttpOnly` in this double-submit design because the frontend must copy it into the header.

That property does not apply to the authentication cookie by implication.

### Safe methods do not become business mutation paths

GET is used to establish/read the CSRF token and remains safe from business mutation according to API architecture.

### Configuration rollback is not architecture bypass

Disabling the feature flag is an operational rollout mechanism.

It is not evidence that cookie-authenticated cross-origin mutation is safe without another anti-CSRF mechanism.

---

## Alternatives Considered

### Alternative A — Synchronizer Token pattern

The original ADR explicitly records this alternative.

Concept:

```text
server creates anti-CSRF token
server stores session-associated token state
client sends token back
server validates against stored state
```

#### Benefits

- established anti-CSRF pattern;
- server-side token/session association.

#### Costs / reasons not chosen

The original ADR records that it would require server-side session/token storage such as:

```text
Redis
database
```

for an API architecture designed around stateless JWT-oriented authentication.

That would add server-side state management and latency solely for the CSRF token lifecycle.

The accepted Double Submit approach avoids this additional server-side session state.

### Alternative B — Use `SameSite=Lax`

The original ADR explicitly records this alternative.

#### Potential benefit

A stricter authentication-cookie SameSite policy can reduce CSRF exposure for some browser navigation/request patterns.

#### Reason not chosen

The original deployment model requires cross-origin frontend → API mutation requests.

The ADR records that `SameSite=Lax` would not support the required cross-origin POST behavior.

Therefore it was incompatible with the accepted frontend/API origin model.

### Other alternatives

```text
Not recorded in the original ADR.
```

This normalization does not invent historical consideration of:

```text
SameSite=Strict authentication cookies
custom Origin-only validation
synchronizer token variants
framework antiforgery middleware
token-in-local-storage auth
same-origin reverse-proxy deployment
```

because those alternatives were not recorded in the original ADR.

---

## Consequences

### Positive

The original ADR records:

- no server-side CSRF token/session store is required;
- the pattern works with the SPA because JavaScript can read the CSRF cookie and send the header;
- rollout can be controlled through `Security:Csrf:Enabled`;
- state-changing methods receive explicit validation;
- fixed-time token comparison is used;
- the authentication cookie can remain compatible with the cross-origin API model.

### Security boundary consequence

The frontend must participate in the contract:

```text
read csrf_token cookie
→ send X-CSRF-Token on mutation
```

A frontend/client that does not implement the header cannot use this cookie-authenticated mutation path while protection is enabled.

### Operational consequence

CSRF enablement and frontend support must be rollout-compatible.

Enabling the backend protection before supported browser clients send the header can reject legitimate mutations.

### Architectural consequence

The current design assumes:

```text
cross-origin browser frontend
+
cookie-based authenticated mutation
```

If that trust/deployment model changes materially, the ADR assumptions must be reevaluated.

---

## Compatibility / Migration

The original ADR explicitly made the mechanism feature-flagged for rollout.

The compatibility sequence implied by the decision is:

```text
1. backend supports the CSRF cookie/header mechanism
2. frontend learns to read csrf_token and send X-CSRF-Token
3. verify mutation requests work with protection
4. enable Security:Csrf:Enabled in the intended environment
5. monitor/recover by configuration if rollout fails
```

The exact rollout order/environment is a Delivery/Operations concern.

A material browser credential/origin change can invalidate the assumptions behind this ADR.

Examples:

```text
move frontend/API to same-origin
replace cookie authentication with a different browser credential model
change auth-cookie SameSite requirements
introduce another accepted anti-forgery architecture
```

Such a change should trigger security review and, if the foundation changes, a superseding ADR.

No persisted database migration is inherently required by this decision.

---

## Evidence

### Canonical current architecture

- `backend/docs/architecture/api-and-contracts.md`
- `backend/docs/architecture/security-tenancy-authorization.md`
- `backend/docs/operations/configuration-and-runtime.md`

### Current source

- `backend/src/Notrelix.API/Middleware/CsrfValidationMiddleware.cs`
  - feature-gated by `Security:Csrf:Enabled`;
  - establishes a token cookie on GET when absent;
  - validates state-changing methods;
  - returns 403 on invalid/missing token.
- `backend/src/Notrelix.Infrastructure/Auth/Csrf/CsrfProtector.cs`
  - uses cryptographically random token bytes;
  - writes `csrf_token`;
  - reads `X-CSRF-Token`;
  - uses fixed-time equality;
  - recognizes POST/PUT/PATCH/DELETE as state-changing;
  - currently marks the CSRF cookie JavaScript-readable and `SameSite=Strict`.
- `backend/src/Notrelix.API/appsettings.json`
  - contains the CSRF runtime configuration section/default.
- environment-specific configuration may override the current rollout state.

### Tests / gates

Primary current test project:

- `backend/tests/Notrelix.API.Tests/`

Expected proof for the accepted decision includes:

```text
protection disabled path
GET establishes token when required
state-changing request missing cookie/header is rejected
mismatch is rejected
matching cookie/header is accepted to continue
safe methods are not treated as protected mutations
public error is safe
```

Integration/security proof may additionally cover the actual browser authentication/cookie host behavior where required.

---

## Supersedes

`None`

The original ADR does not record an older backend ADR superseded by this decision.

---

## Superseded By

[ADR-005 — Cross-Origin CSRF Bootstrap Protocol](ADR-005-csrf-cross-origin-bootstrap.md)

ADR-005 preserves the Double Submit Cookie pattern, fixed-time comparison, state-changing-method coverage, and feature-flag rollout model accepted here.

It supersedes the transport assumptions recorded in this ADR:

```text
JavaScript-readable CSRF cookie (HttpOnly = false)
implicit per-GET token-cookie issuance by middleware
SameSite=Strict CSRF cookie behavior
```

because a cross-origin SPA cannot read the API host-scoped cookie, making those assumptions structurally incompatible with the supported deployment topology.

---

## Historical normalization note

This file was normalized to the current ADR schema while preserving the accepted historical meaning.

The normalization adds:

```text
metadata
ID
recoverable date
current stewardship
structured alternatives/consequences
compatibility/migration
current source/test evidence
supersession fields
```

It intentionally keeps only the alternatives recorded in the original ADR:

```text
Synchronizer Token
SameSite=Lax
```

and marks other historical alternatives as unrecorded rather than fabricating rationale.

---

## Decision-change trigger

A superseding ADR should be considered if Notrelix materially changes:

```text
the browser authentication credential model
the cross-origin frontend/API deployment assumption
the authentication-cookie SameSite requirement
the anti-forgery token architecture
the responsibility split between frontend/API/Infrastructure for CSRF
```

Routine fixes such as:

```text
refactoring the protector implementation
changing safe error formatting
adjusting non-semantic option binding
renaming implementation classes while preserving the contract
```

do not automatically require a new ADR.
