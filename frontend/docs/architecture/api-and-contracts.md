---
document_id: FE-ARCH-API-CONTRACTS
document_type: architecture
status: active
owner: frontend-platform
applies_to:
  - frontend-api
  - frontend-contracts
  - frontend-codegen
  - frontend-api-client
  - frontend-error-contracts
  - frontend-auth-transport
  - frontend-idempotency
evidence:
  - backend/contracts/openapi/notrelix.v1.json
  - artifacts/contracts/realtime.v1.json
  - frontend/tooling/codegen/
  - frontend/packages/foundation/contracts/
  - frontend/packages/runtimes/web/
  - frontend/packages/runtimes/mobile/
  - frontend/turbo.json
  - frontend/package.json
review_on:
  - public-api-contract-change
  - frontend-codegen-change
  - api-client-transport-change
  - auth-session-transport-change
  - csrf-contract-change
  - idempotency-contract-change
  - error-contract-change
  - api-versioning-change
---

# API and Contracts

> **Backend/system producers own public wire contracts. Frontend consumes those contracts through generated artifacts and narrow runtime/client boundaries.**
>
> Generated types are evidence of producer contracts. Product/feature packages may map wire representations into client semantic models, but they MUST NOT independently redefine server API meaning.

This document is the canonical frontend owner for:

- REST/OpenAPI contract consumption;
- realtime generated contract relationship at the wire boundary;
- contract code generation;
- generated versus handwritten client types;
- API client construction;
- endpoint ownership;
- correlation IDs;
- auth/session transport integration;
- browser CSRF transport contract;
- idempotency headers;
- cancellation;
- error/result normalization;
- response parsing;
- version compatibility;
- product API wrappers;
- testing and drift detection.

It does not own:

- backend endpoint implementation;
- backend authorization;
- backend Domain/Application invariants;
- server persistence;
- query/cache ownership;
- realtime reconciliation semantics;
- product business meaning.

---

# 1. Contract architecture objective

The contract boundary should remain:

```text
backend/system producer
        ↓
versioned public contract artifact
        ↓
frontend generator
        ↓
generated frontend wire contract
        ↓
foundation client/runtime
        ↓
product/feature API adapter
        ↓
state/UI consumer
```

The frontend must not reverse this relationship.

---

# 2. FE-API-001 — Backend/system producer owns wire meaning

For backend-owned operations, the frontend MUST NOT independently decide:

```text
route
HTTP method
request field meaning
response field meaning
error meaning
authorization result
concurrency rule
idempotency rule
```

when a producer contract exists or is required.

---

# 3. Current producer inputs

Current frontend code generation consumes producer artifacts including:

```text
backend/contracts/openapi/notrelix.v1.json
artifacts/contracts/realtime.v1.json
```

through Turbo/codegen configuration.

These are source contract inputs.

---

# 4. FE-API-002 — Contract producer input is not replaced by handwritten DTOs

If generated output disagrees with a backend public contract:

```text
inspect producer
inspect generator
regenerate
```

Do not create a second handwritten DTO merely to make the frontend compile.

---

# 5. Codegen tooling

Current tooling package:

```text
@notrelix/codegen
```

runs:

```text
generate:openapi
generate:asyncapi
```

and writes generated contract artifacts into the foundation contract package.

---

# 6. FE-API-003 — Generator owns generated output

Generated files MUST be changed through:

```text
producer/input
or
generator
```

not by direct permanent edits.

If a generated output needs a manual fix, fix the generation pipeline.

---

# 7. Generated output families

Current generated contract tree contains:

```text
generated/rest
generated/realtime
```

under:

```text
packages/foundation/contracts/src/generated
```

The exact generated file inventory is current evidence.

---

# 8. FE-API-004 — REST and realtime wire contracts are distinct but coordinated

REST and realtime may describe the same resource domain through different delivery shapes.

Do not assume:

```text
REST response DTO
=
realtime event DTO
```

unless the producer contracts explicitly define equivalence.

Map each through the correct boundary.

---

# 9. Codegen check

Current root command:

```bash
pnpm codegen:check
```

runs codegen then verifies generated REST output has no uncommitted drift according to current script.

The exact command implementation is executable in `frontend/package.json`.

---

# 10. FE-API-005 — Codegen drift is a merge-blocking contract signal

If producer input changes and committed generated output does not:

```text
contract evidence is stale
```

The normal fix is regenerate/review.

Do not disable drift detection to preserve stale generated files.

---

# 11. Generated versus semantic types

Wire contracts serve transport compatibility.

Client product packages may need a different semantic representation.

Example:

```text
wire date string
→ client parsed/display model

wire union
→ product-specific normalized model
```

when justified.

---

# 12. FE-API-006 — Mapping is allowed; duplication without semantic purpose is not

A handwritten client type SHOULD exist only when it intentionally models:

```text
client semantics
derived view model
normalized state
platform abstraction
```

not because the developer prefers another DTO spelling.

---

# 13. Mapping owner

Wire-to-client mapping belongs at the closest stable contract/product boundary.

Do not map the same response independently in multiple components.

---

# 14. FE-API-007 — One mapping owner per semantic representation

If five screens consume the same normalized Board representation:

```text
map once in the owning product/state/API adapter
```

not five route-local transformations with divergent assumptions.

---

# 15. Contract package

Current package:

```text
@notrelix/contracts
```

exports supported surfaces including:

```text
.
./client
./endpoints
./types
./generated/rest
```

according to current package manifest.

Generated realtime output also exists in the generated tree; public export policy must remain intentional.

---

# 16. FE-API-008 — Package export defines supported frontend contract surface

Consumers MUST use supported contract entrypoints.

Do not deep-import:

```text
@notrelix/contracts/src/generated/...
```

to bypass export decisions.

If a generated contract must become public, expose it intentionally.

---

# 17. API client ownership

Current foundation contracts package contains a reusable Notrelix API client.

The client handles transport concerns such as:

```text
base URL
fetch implementation
credentials
correlation ID
session refresh
idempotency header
CSRF header
response parsing
AppError normalization
```

as current implementation evidence.

---

# 18. FE-API-009 — API client owns generic transport, not product operation semantics

The foundation client MAY know:

```text
HTTP
headers
credentials
refresh protocol
generic errors
```

It SHOULD NOT know:

```text
how moving a Board Item updates cache
how Billing UI interprets plan
how Documents resolve editing conflicts
```

Those belong outward.

---

# 19. Client construction

API client is constructed by runtime/host composition using normalized runtime configuration.

Product packages consume the client or product-specific service wrapper.

---

# 20. FE-API-010 — Components do not construct ad-hoc root API clients

Avoid:

```ts
const client = createNotrelixClient(...)
```

inside arbitrary route/component render.

Long-lived client configuration/session behavior belongs to runtime/host composition.

---

# 21. Endpoint definitions

Current contracts package contains an endpoint-definition surface.

A centralized endpoint helper can reduce route-string duplication.

Generated OpenAPI operations remain the wire contract authority.

---

# 22. FE-API-011 — Endpoint constants do not become a second API specification

A handwritten endpoint table MAY provide runtime convenience.

It MUST NOT silently disagree with:

```text
backend OpenAPI
versioned public endpoint
method semantics
```

If disagreement exists, classify contract/source drift.

---

# 23. Product API wrappers

Current Work Management state contains focused API adapters such as:

```text
board.api.ts
item.api.ts
group.api.ts
field.api.ts
label.api.ts
checklist.api.ts
item-comments.api.ts
```

as current implementation evidence.

These wrappers translate generic client calls into product-specific operations.

---

# 24. FE-API-012 — Product API adapter is thin and typed

A product API adapter SHOULD own:

```text
endpoint selection
request/response types
wire-to-product mapping
operation-specific headers/options
```

It SHOULD NOT own:

```text
React rendering
global routing
unrelated product state
```

---

# 25. Generated client versus thin wrapper

Notrelix MAY use generated operation types with a handwritten thin client/wrapper.

The architectural requirement is contract fidelity.

It does not require every HTTP call to be fully generated implementation code.

---

# 26. FE-API-013 — Handwritten transport wrapper cannot weaken generated type fidelity

Do not turn a generated request/response contract into:

```ts
any;
unknown as SomeDto;
Record<string, any>;
```

without a narrow validated reason.

Preserve typed producer semantics.

---

# 27. Auth credentials

Current generic API client uses browser `credentials: "include"` as current web-oriented transport evidence.

Host/runtime credential behavior may differ on mobile.

Auth/session architecture determines credential source/lifecycle.

---

# 28. FE-API-014 — Product packages do not read credential storage

Product packages should request an authenticated API capability.

They MUST NOT directly read:

```text
browser auth cookie
native secure-store token
refresh token
```

unless the package itself is the approved auth/runtime owner.

---

# 29. 401/session refresh

Current API client implements instance-scoped single-flight refresh behavior before one retry.

This avoids duplicate concurrent refresh attempts within one client instance.

---

# 30. FE-API-015 — Auth refresh is centralized per client/session owner

Do not implement independent:

```text
if 401 then refresh
```

logic in each product API wrapper.

This causes refresh races and divergent session behavior.

---

# 31. Single retry after refresh

Current client performs one retry after a successful refresh and marks it to avoid recursive refresh.

The durable rule is bounded retry.

---

# 32. FE-API-016 — Authentication retry is bounded

A failed refresh/retry MUST NOT loop indefinitely.

Session failure should transition to the auth/session recovery contract.

---

# 33. Session-expired event

Current client can notify an outer `onSessionExpired` callback with a typed event/reason.

This is a good outward dependency direction:

```text
client/session event
→ host/runtime callback
```

rather than importing router directly.

---

# 34. FE-API-017 — Transport reports session failure; host decides navigation

Foundation API client SHOULD NOT import:

```text
TanStack Router
Expo Router
```

to navigate to sign-in.

The host/runtime owns navigation response.

---

# 35. Correlation ID

Current client attaches:

```text
X-Correlation-ID
```

and preserves the same correlation ID through the one refresh retry path.

Correlation helps trace client/server operations.

---

# 36. FE-API-018 — Correlation identity is diagnostic, not idempotency identity

Do not assume:

```text
correlationId
=
idempotencyKey
```

because both are strings.

They serve different contracts.

---

# 37. Correlation generation

Generic client can generate correlation IDs through an injectable function.

Tests can substitute deterministic generation.

---

# 38. FE-API-019 — Correlation IDs should be unique enough for trace linkage

Do not reuse one static correlation ID across unrelated operations merely for convenience.

Preserve it only where operations are part of the same retry/logical request flow.

---

# 39. Idempotency option

Current API request options support:

```text
idempotencyKey
```

and emit:

```text
Idempotency-Key
```

when provided.

---

# 40. FE-API-020 — Idempotency is operation-defined, not globally auto-generated for every mutation

Only operations whose backend contract supports/requires idempotency should supply an idempotency key.

Do not add arbitrary idempotency headers and assume the backend honors them.

---

# 41. Stable idempotency across retry

When retrying the same logical mutation, the same idempotency identity should be reused according to backend contract.

A retry that generates a new key may become a second operation.

---

# 42. FE-API-021 — Logical command identity survives transport retry

For an idempotent command:

```text
logical operation
→ stable idempotency key
→ transport retries
```

unless the backend contract explicitly defines different behavior.

---

# 43. Cancellation

Current generic API request options support `AbortSignal`.

Product/query layers can cancel stale/inapplicable requests.

---

# 44. FE-API-022 — Cancellation is a transport/lifecycle signal, not server rollback

Aborting a client request does not prove the server mutation was never accepted.

For mutation cancellation, reason using the backend operation/idempotency/status contract.

Do not tell the user “nothing happened” solely because fetch aborted.

---

# 45. Response parsing

Current generic client handles:

```text
204 → null-like response
JSON parsing
non-JSON failure
non-2xx AppError
```

as current implementation evidence.

---

# 46. FE-API-023 — Response parser preserves transport distinction

Do not treat:

```text
204 No Content
empty 200 body
invalid JSON
problem response
network failure
```

as one generic error.

Preserve enough distinction for correct product/session behavior.

---

# 47. Error normalization

Current client maps HTTP statuses into a typed `AppError` kind and preserves:

```text
status
message
details
validationErrors
correlationId
```

where available.

---

# 48. FE-API-024 — Client behavior branches on stable error semantics, not prose

Prefer:

```text
error.kind
error.code
HTTP status
typed problem fields
```

over:

```text
message.includes("not allowed")
```

Human-readable strings are not stable machine protocols by default.

---

# 49. Validation errors

Validation errors can carry field-level error collections.

Form/feature UI may map them to user fields.

---

# 50. FE-API-025 — Validation mapping does not invent server field meaning

If a backend validation field cannot be mapped to the current client form safely:

```text
show a bounded form/global validation error
record contract mismatch
```

Do not guess a different field.

---

# 51. Conflict errors

`conflict` is a first-class error kind in current client/query foundation behavior.

Product mutation logic may choose:

```text
rollback
refetch
conflict UI
```

according to product semantics.

---

# 52. FE-API-026 — Conflict is not generic retryable network failure

Do not blindly retry `409/conflict`.

The conflict may require:

```text
new version
refetch
user decision
rebase
```

according to operation contract.

---

# 53. Not-found/forbidden distinction

Backend may intentionally map unauthorized resources to not-found for information hiding.

Frontend should preserve the API contract rather than infer hidden authorization details.

---

# 54. FE-API-027 — Client does not reverse-engineer protected resource existence

If API says `not_found`, do not run secondary probes to decide whether:

```text
resource exists but forbidden
```

unless the backend contract explicitly provides that information.

---

# 55. Retry policy relationship

Query foundation currently marks several `AppError` kinds non-retryable and limits ordinary query retry.

Generic mutation retry is disabled by default.

These are current defaults.

---

# 56. FE-API-028 — Retry behavior follows operation/error class

Do not globally retry:

```text
auth
forbidden
not_found
validation
conflict
non-idempotent mutation
```

as if they were transient network failures.

---

# 57. Browser CSRF architecture

The accepted cross-boundary CSRF contract is the backend
`ADR-005 — Cross-Origin CSRF Bootstrap Protocol`
(`backend/docs/decisions/ADR-005-csrf-cross-origin-bootstrap.md`),
which supersedes the historical Double Submit Cookie transport of ADR-003:

```text
bootstrap (safe):
GET /api/v1/auth/csrf
→ response body carries the token (client keeps it in memory only)
→ Set-Cookie: csrf_token=<token> (HttpOnly, host-scoped)

unsafe browser mutation:
Cookie: csrf_token=<token>   ← attached by the browser automatically
X-CSRF-Token: <token>        ← sent by the client from memory

unsafe methods:
POST / PUT / PATCH / DELETE

failure contract:
security.csrf_validation_failed ProblemDetails
→ client clears its in-memory token and re-bootstraps once
```

The cookie is never JavaScript-read; the body is the only client source.
Enablement remains environment configuration; see §61–62.

Frontend browser transport must match this contract exactly.

---

# 58. FE-API-029 — CSRF cookie/header names are a cross-boundary contract

Browser client and backend MUST agree on:

```text
cookie name
header name
unsafe methods
credential mode
enablement/rollout
```

This is not a cosmetic naming choice.

---

# 59. CSRF source alignment (closed)

Historical drift (pre-closure): the frontend read a JavaScript-readable
`XSRF-TOKEN` cookie and sent `X-XSRF-TOKEN`, which never matched the backend
`csrf_token` / `X-CSRF-Token` contract.

This drift was repaired at the Identity & Accounts Phase 13 closure. Current
frontend source implements exactly the ADR-005 bootstrap protocol:

```text
packages/foundation/contracts/src/client/csrf.ts
→ GET auth/csrf bootstrap, in-memory token, X-CSRF-Token header

packages/foundation/contracts/src/client/api-client.ts
→ csrfAwareFetch on unsafe browser requests, single-flight bootstrap,
  security.csrf_validation_failed → clear + re-bootstrap recovery
```

The legacy spellings `XSRF-TOKEN` / `X-XSRF-TOKEN` and any cookie/meta/storage
token discovery are forbidden; client source-scan tests guard this.

---

# 60. FE-API-030 — CSRF wire contract is single-spelling, drift-guarded

Current classification:

```text
RESOLVED — aligned with backend ADR-005
```

The historical SOURCE_DEBT / CONTRACT_DRIFT classification applied to the
pre-closure frontend helper only; it is retained here as decision history.

Standing rule (unchanged by the closure):

```text
frontend browser client
→ obtain token from GET /api/v1/auth/csrf response body
→ send X-CSRF-Token on unsafe browser mutations
```

Any future deviation is a deliberate cross-boundary contract change that must
go through ADR/supersession — not silent client renaming.

---

# 61. CSRF rollout

Backend protection can be configuration-controlled.

Disabled protection does not make a mismatched client contract correct.

---

# 62. FE-API-031 — Feature flag must not hide contract incompatibility

Before enabling CSRF in an environment, verify actual browser mutation traffic sends the expected token.

A disabled backend flag is rollout state, not contract evidence.

---

# 63. CSRF and authorization

CSRF is browser request-forgery protection.

It is separate from resource authorization.

---

# 64. FE-API-032 — Valid CSRF token grants no resource permission

After CSRF passes, backend authorization still determines whether the user may perform the operation.

Frontend MUST NOT interpret successful CSRF handling as authorization.

---

# 65. Mobile CSRF distinction

Native credential transport may not use browser cookie CSRF in the same way.

Do not force browser anti-forgery implementation into product packages used by mobile.

---

# 66. FE-API-033 — Browser CSRF belongs to browser transport/runtime

Keep DOM/cookie reading out of runtime-neutral generated contract/product core.

Platform-specific credential/anti-forgery behavior stays at runtime/client boundary.

---

# 67. GET safety

Frontend should treat GET/HEAD as safe according to backend/API architecture.

Do not rely on GET endpoints for durable business mutation.

---

# 68. FE-API-034 — Client does not encode mutation semantics into safe-method workarounds

If a mutation endpoint is missing, do not call a GET with side effects or hide mutation in query semantics.

Fix the backend/public contract.

---

# 69. 202 Accepted

A backend `202 Accepted` can mean work was accepted, not completed.

Client must use operation status/event contract when completion matters.

---

# 70. FE-API-035 — Accepted is not completed

For long-running operations:

```text
request accepted
≠ final business success
```

Do not immediately render irreversible “completed” state without the completion contract.

---

# 71. Pagination

Pagination parameters/result metadata are producer contract.

Client query keys must include parameters that materially change result identity.

Detailed key rules live in state docs.

---

# 72. FE-API-036 — Pagination state participates in request/cache identity

Do not cache:

```text
page 1
page 2
different filters
```

under the same state identity.

---

# 73. Filtering and sorting

Filter/sort syntax is API contract.

Client-specific UI state can map to it.

---

# 74. FE-API-037 — UI filter model maps to API filter contract explicitly

Do not pass arbitrary UI state object as query parameters and assume backend semantics.

Create an owned mapping.

---

# 75. API versioning

Backend public API version/compatibility policy controls wire evolution.

Frontend should not depend on undocumented fields/routes.

---

# 76. FE-API-038 — Client consumes supported public contract, not backend implementation accident

Do not parse:

```text
internal server class names
unlisted JSON fields
debug headers
```

as permanent product protocol.

---

# 77. Additive backend changes

An additive optional response field can be compatible.

Frontend may adopt it after generated contract update.

Do not assume all additive enum values are harmless.

---

# 78. FE-API-039 — Open enums/unions require forward-compatible client behavior

When server can add values:

```text
client should have safe unknown/default handling
```

where the contract allows forward evolution.

Do not crash the entire screen on an unknown display category if safe fallback is possible.

---

# 79. Breaking backend changes

Removing/renaming required fields/routes is contract breaking unless migration/version strategy says otherwise.

Frontend and backend rollout must be compatible.

---

# 80. FE-API-040 — Contract migration plans for mixed-version deployment

When frontend/backend deploy independently or can be cached:

```text
old client ↔ new server
new client ↔ old server
```

compatibility must be considered.

Do not assume perfectly atomic rollout.

---

# 81. Endpoint deprecation

Deprecated endpoint can coexist during migration.

Do not leave dual clients indefinitely without removal condition.

---

# 82. FE-API-041 — One operation has one active client authority per migration phase

During endpoint migration, define:

```text
old path compatibility
new path cutover
consumer migration
removal
```

Avoid product packages randomly choosing old/new endpoints.

---

# 83. API client abstraction

A thin generic client is useful.

Do not wrap every generated type/operation in several redundant layers with no semantics.

---

# 84. FE-API-042 — Abstraction depth follows real policy

Add an adapter when it owns:

```text
mapping
retry/idempotency policy
product operation grouping
runtime difference
```

not simply to rename `get()` to `fetchData()`.

---

# 85. Direct fetch

Direct `fetch()` can be appropriate inside the approved generic transport/runtime implementation.

It is generally wrong in arbitrary feature components when canonical client exists.

---

# 86. FE-API-043 — Product components do not own raw transport by default

Prefer:

```text
component
→ hook/state
→ product API service
→ generic client
```

This keeps auth/error/idempotency/correlation behavior consistent.

---

# 87. Service grouping

Current Work Management service factory groups typed API adapters behind `WorkManagementServices`.

The host/provider injects this service group.

---

# 88. FE-API-044 — Product service group is composition convenience, not global service locator

A service group SHOULD expose only the product capability APIs it owns.

Do not append unrelated Billing/Governance services to Work Management context.

---

# 89. API client testability

Current generic client supports injectable `fetchImpl`, clock and correlation factory.

This makes transport behavior testable without global monkey patching.

---

# 90. FE-API-045 — Test seams are explicit dependencies

Prefer injecting:

```text
fetch
clock
ID generator
session-expired callback
```

through defined client config rather than patching globals per test.

---

# 91. Fake backend semantics

Unit tests may fake network responses.

They must match real public contract shapes/semantics where claiming backend compatibility.

---

# 92. FE-API-046 — Mock convenience does not redefine contract

A test fixture returning an impossible backend shape is not proof of feature compatibility.

Use generated types/builders/contract fixtures where practical.

---

# 93. Contract test categories

Relevant proof can include:

```text
codegen drift
API client unit tests
error mapping tests
auth refresh tests
CSRF transport tests
idempotency header tests
product API adapter tests
integration/E2E against backend
```

Choose based on protected property.

---

# 94. FE-API-047 — Contract-critical headers require direct tests

Headers such as:

```text
Idempotency-Key
X-Correlation-ID
CSRF header
```

SHOULD have tests proving actual emitted requests when the contract is critical.

Typecheck cannot prove header spelling.

---

# 95. OpenAPI compatibility

Generated compilation proves TypeScript reflects current producer schema.

It does not prove product mapping/UX is semantically compatible.

---

# 96. FE-API-048 — Regeneration is necessary but not sufficient

After generated contract diff:

```text
review changed operations/types
update affected adapters/state
run affected behavior tests
```

Do not auto-commit generated changes without semantic review.

---

# 97. Realtime contract relationship

Realtime generated messages are producer-owned wire events.

Detailed connection/order/gap logic belongs to `realtime.md`.

---

# 98. FE-API-049 — Realtime event shape and realtime state application are separate owners

```text
generated event type
→ contracts

event-to-product-state behavior
→ realtime/product-state owner
```

Do not put cache mutation logic in generated files.

---

# 99. File upload/download

Binary/multipart/streaming contracts may require transport handling different from JSON.

Do not force every operation through JSON assumptions.

---

# 100. FE-API-050 — Transport codec follows endpoint contract

If endpoint uses:

```text
multipart
binary
stream
text
```

the generic/product client must preserve that contract.

Do not stringify arbitrary body blindly.

---

# 101. Content-Type

Generic JSON client currently defaults `Content-Type: application/json`.

Operation-specific transport can override where contract requires.

---

# 102. FE-API-051 — Default header is not universal protocol

Do not attach JSON content type to multipart/binary operation if it breaks browser boundary generation.

Operation contract wins.

---

# 103. Credentials

Current browser API client uses `credentials: include`.

CORS/cookie/CSRF deployment must remain compatible.

---

# 104. FE-API-052 — Credential mode is runtime security architecture

Do not let product APIs choose credential mode independently per call without an explicit use case.

Centralize browser/native auth transport policy.

---

# 105. API base URL

Base URL comes from normalized host/runtime configuration.

Do not hardcode environment URLs inside feature packages.

---

# 106. FE-API-053 — Endpoint environment is host/runtime configuration

Product API wrapper owns relative operation semantics.

Host/runtime owns which backend environment it targets.

---

# 107. Correlation error display

Correlation ID can be shown/copied in safe support/error UX when useful.

Do not expose secret request data.

---

# 108. FE-API-054 — Diagnostics preserve privacy

Error logging/telemetry MUST avoid:

```text
access token
refresh token
API key
password
private payload
```

Correlation ID is safe only as a tracing identifier, not as proof of user identity.

---

# 109. Error detail

Backend problem detail may be human-readable.

Do not expose internal stack/provider messages directly.

---

# 110. FE-API-055 — User-facing error uses safe normalized message

When backend returns unsafe/unstructured detail, map to a safe client message and retain safe correlation/code for support.

---

# 111. Contract ownership change

Moving an endpoint between backend contexts/services can preserve public contract or change it.

Frontend cares about public compatibility, not internal backend project path.

---

# 112. FE-API-056 — Backend internal refactor is not frontend contract change unless public behavior changes

Do not modify frontend merely because backend handler/class moved.

React only to public contract/evidence changes.

---

# 113. API gateway/service extraction

Future backend service extraction should not force frontend product code to know internal service topology if public API/runtime abstraction remains stable.

---

# 114. FE-API-057 — Client targets public capability contract, not internal service decomposition

Avoid embedding:

```text
Identity service host
WorkManagement service host
Billing service host
```

throughout feature packages unless public routing architecture intentionally requires it.

Runtime/gateway configuration owns topology.

---

# 115. Contract-first delivery

When frontend depends on a new backend operation:

```text
define/approve producer contract
generate/update client
implement adapter/state
test compatibility
```

rather than creating fake final frontend contract first.

---

# 116. FE-API-058 — Missing producer contract is a stop condition

If frontend cannot safely know:

```text
request shape
response shape
error semantics
permission result
event semantics
```

stop and resolve the backend/system contract.

Do not permanently guess.

---

# 117. Contract drift classification

When frontend/backend disagree classify:

```text
DOC_STALE
SOURCE_DEBT
CONTRACT_CHANGE
TRANSITION
UNRESOLVED
```

according to repository governance.

---

# 118. FE-API-059 — Contract disagreement is not fixed by whichever side is easier to edit

Identify the actual public authority.

Then repair:

```text
producer
generator
client
docs
tests
```

accordingly.

---

# 119. Current CSRF debt resolution checklist

Before claiming browser CSRF compatibility:

```text
[ ] backend cookie name confirmed
[ ] backend header name confirmed
[ ] frontend reads that cookie
[ ] frontend emits that header
[ ] unsafe methods covered
[ ] credentials mode compatible
[ ] enabled-mode integration test passes
[ ] auth/session flow still passes
```

---

# 120. FE-API-060 — CSRF debt must be closed before relying on enabled protection

Do not certify CSRF browser protection while the client and server token names differ.

---

# 121. New endpoint checklist

```text
[ ] backend producer contract exists
[ ] generated types current
[ ] operation owner identified
[ ] product API adapter added if needed
[ ] auth/credential mode correct
[ ] idempotency/concurrency contract handled
[ ] errors normalized
[ ] cancellation considered
[ ] state/query owner identified
[ ] tests
```

---

# 122. Contract change checklist

```text
[ ] producer diff reviewed
[ ] backward/forward compatibility
[ ] generated output updated
[ ] handwritten adapter mapping updated
[ ] error/enum handling updated
[ ] query/mutation effects updated
[ ] mobile/web impact
[ ] realtime relation
[ ] E2E/integration evidence as required
```

---

# 123. API client change checklist

```text
[ ] generic versus product responsibility
[ ] auth refresh
[ ] credential mode
[ ] CSRF
[ ] correlation
[ ] idempotency
[ ] abort/cancellation
[ ] response parser
[ ] AppError mapping
[ ] retry bound
[ ] web/mobile compatibility
[ ] tests
```

---

# 124. Generated contract checklist

```text
[ ] producer input changed intentionally
[ ] generator deterministic
[ ] generated diff reviewed
[ ] no manual edits
[ ] exports updated if public surface changes
[ ] affected adapters compile
[ ] codegen:check passes
```

---

# 125. Stop conditions

Stop implementation if:

- the backend operation does not exist but frontend is inventing it;
- generated types are being manually patched;
- a feature creates its own auth refresh logic;
- an idempotency key is regenerated for retry of the same logical command without contract justification;
- code branches on backend prose;
- a component hardcodes backend URL/environment;
- a browser CSRF helper uses names different from the backend contract;
- a product package reads browser/native credential storage directly;
- a generated event is treated as product-state authority without reconciliation owner;
- a direct `fetch()` bypasses required auth/correlation/error policies;
- an API version breaking change has no mixed-version rollout plan;
- client code attempts to infer hidden unauthorized resource existence.

---

# 126. Executable evidence

Primary current evidence:

```text
backend/contracts/openapi/notrelix.v1.json
artifacts/contracts/realtime.v1.json
frontend/tooling/codegen/
frontend/packages/foundation/contracts/
frontend/packages/product/*/*/src/api/
frontend/packages/features/*/
frontend/turbo.json
frontend/package.json
backend CSRF source/ADR
frontend contract/client tests
```

---

# 127. Related frontend architecture

Read:

```text
frontend-overview.md
dependency-boundaries.md
hosts-composition-routing.md
state-query-mutations.md
realtime.md
testing-and-quality-gates.md
architecture-change-policy.md
```

---

# 128. Related backend/system authority

When relevant read:

```text
backend/docs/architecture/api-and-contracts.md
backend/docs/architecture/security-tenancy-authorization.md
docs/architecture/contract-boundaries.md
docs/architecture/events-realtime-and-delivery-boundary.md
docs/delivery/contract-first-delivery.md
```

Frontend cannot redefine these producer semantics.

---

# 129. Explicit non-responsibilities

This document does not define:

```text
backend endpoint internals
backend authorization policy
database transaction semantics
product cache mutation algorithms
realtime ordering/gap algorithm
visual error design
```

It defines the frontend wire/client boundary around those contracts.

---

# 130. Final API contract model

The intended frontend contract architecture is:

```text
BACKEND / SYSTEM PRODUCER
        ↓
OpenAPI / realtime contract artifacts
        ↓
codegen
        ↓
@notrelix/contracts generated wire types
        ↓
generic Notrelix API/runtime client
        ↓
product/feature API adapters
        ↓
query/mutation/realtime owners
        ↓
UI
```

with:

```text
backend authorization
→ authoritative

generated wire types
→ producer-derived

client semantic mapping
→ explicit

credentials/refresh/CSRF
→ runtime/client boundary

idempotency
→ operation-defined

errors
→ normalized stable semantics

contract drift
→ executable codegen + tests
```

The boundary is successful when frontend teams can consume backend evolution without handwritten DTO drift, duplicated auth transport, or component-local protocol invention.
