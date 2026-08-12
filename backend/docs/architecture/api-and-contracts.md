---
document_id: BE-API-CONTRACTS
document_type: architecture
status: active
owner: backend-architecture
applies_to:
  - backend/src/Notrelix.API
  - backend/tests/Notrelix.API.Tests
  - backend/tests/Notrelix.Integration.Tests
evidence:
  - backend/src/Notrelix.API/Notrelix.API.csproj
  - backend/src/Notrelix.API/Endpoints/
  - backend/src/Notrelix.API/Contracts/
  - backend/src/Notrelix.API/OpenApi/
  - backend/src/Notrelix.API/Middleware/
  - backend/src/Notrelix.API/Idempotency/
  - backend/src/Notrelix.API/RateLimiting/
  - backend/src/Notrelix.API/Security/
  - backend/src/Notrelix.API/Versioning/
  - backend/docs/decisions/ADR-003-csrf-protection.md
  - backend/docs/decisions/ADR-004-rate-limiting-architecture.md
review_on:
  - public-http-contract-change
  - api-versioning-change
  - authentication-host-change
  - csrf-change
  - rate-limiting-change
  - idempotency-transport-change
  - openapi-generation-change
  - endpoint-composition-change
---

# API and Contracts

> **API is the public transport and composition boundary. It authenticates and binds requests, applies host-level protections, maps transport context into Application use cases, and translates stable Application results back into HTTP/OpenAPI contracts.**
>
> API does not become the business layer because it is externally visible. Resource authorization remains Application-owned; persistence remains Infrastructure-owned; committed async delivery remains Platform-owned.

This document is the canonical backend owner for:

- HTTP endpoint responsibilities;
- request/response contract placement;
- API authentication integration;
- CSRF/CORS/security middleware relationship;
- transport-level rate limiting;
- HTTP idempotency input/filtering;
- error/ProblemDetails translation;
- OpenAPI generation/export/drift;
- API versioning;
- pagination/filter/sort transport semantics;
- endpoint metadata/access classification;
- API composition and route registration;
- REST/realtime relationship.

It does **not** own product semantics, Domain invariants, RLS implementation, provider contracts, or frontend state architecture.

---

# 1. API purpose

The HTTP boundary translates:

```text
HTTP request
        ↓
host authentication/security
        ↓
binding / transport validation
        ↓
Application command/query
        ↓
Application result
        ↓
stable HTTP response / ProblemDetails
        ↓
OpenAPI contract
```

This flow should remain thin enough that the same Application use case can be invoked by another host where product architecture permits.

---

# 2. Current API project

Current `Notrelix.API.csproj` is:

```text
Microsoft.NET.Sdk.Web
```

and references production projects:

```text
Notrelix.Infrastructure
Notrelix.Application
```

Current package evidence includes:

```text
Asp.Versioning.Mvc
Microsoft.AspNetCore.OpenApi
Swashbuckle.AspNetCore
Microsoft.AspNetCore.Authentication.JwtBearer
Microsoft.EntityFrameworkCore.Design (design/private assets)
```

API composition may know outer implementation registration.

It must not move their responsibilities into endpoint code.

---

# 3. BE-API-001 — Endpoint is a transport adapter

A normal endpoint MAY:

```text
read route/query/header/body
map authenticated/request context
construct command/query
send/invoke Application
translate result
declare OpenAPI metadata
```

It MUST NOT directly own:

```text
Domain mutation
DbContext workflow
provider business call
resource permission policy
outbox delivery
```

---

# 4. Current source organization

Current API includes:

```text
Contracts
Endpoints
ErrorHandling
Extensions
Idempotency
Middleware
OpenApi
Options
RateLimiting
Security
Versioning
```

plus:

```text
Program.cs
DependencyInjection.cs
appsettings*.json
```

This is current source evidence, not a permanent folder inventory contract.

---

# 5. Endpoints

Current endpoint folders include capabilities such as:

```text
Admin
Automation
Collaboration
Documents
Governance
Health
Identity
WorkManagement
Workspaces
```

Endpoint grouping helps navigation.

It does not define bounded-context ownership by itself.

---

# 6. BE-API-002 — Endpoint grouping follows public capability, not business ownership authority

A folder can expose routes for a context/capability.

Product ownership still comes from canonical product/system architecture.

Do not infer a new bounded context from an API folder.

---

# 7. Contracts folder

Current API Contracts contains context-oriented public request/response contracts such as:

```text
Admin
Automation
Collaboration
Documents
Governance
Identity
WorkManagement
Workspaces
```

Public DTOs are transport contracts.

They should not be EF entities/provider DTOs.

---

# 8. BE-API-003 — Public DTO is explicit transport shape

A public DTO MAY differ from:

```text
Domain aggregate
Application internal read model
database row
provider payload
```

when transport compatibility/security requires it.

Do not expose internal models merely to reduce mapping code.

---

# 9. Request mapping

Request DTO maps to Application intent.

Prefer:

```text
HTTP contract
→ Command/Query
```

rather than passing the raw endpoint DTO deep into Domain.

This keeps transport concerns separate.

---

# 10. BE-API-004 — Transport parsing completes before business mutation

Malformed:

```text
JSON
route ID
header
content type
pagination input
```

fails at the API/validation boundary before protected Application effects.

---

# 11. Authentication

API/host owns authentication integration.

Current API references JWT Bearer support.

Authentication establishes:

```text
principal
credential/session validity
claims/context
```

It does not establish resource authorization by itself.

---

# 12. BE-API-005 — Authentication precedes protected Application request

Protected endpoints MUST NOT invoke protected use cases anonymously unless the use case explicitly supports anonymous/public access.

Authentication failure is a host/public result, not a Domain failure.

---

# 13. Authorization relationship

Application owns resource/action authorization.

API can classify endpoint access and require authentication/policies at host level.

Do not make API-local permission checks the only enforcement.

---

# 14. BE-API-006 — API endpoint access metadata does not replace Application authorization

Current `Security/EndpointAccessAttribute` can describe host/access intent.

For protected resource operations:

```text
Application still resolves resource/action permission
```

unless the endpoint is purely host-level and no resource authorization exists.

---

# 15. Public/anonymous endpoint

Anonymous/public access must be explicit.

Do not make a route anonymous simply because it is used by marketing/share flow.

Public share access still has its own capability/token/resource contract.

---

# 16. BE-API-007 — Anonymous is an access mode, not “no security”

Anonymous endpoints still apply:

```text
input validation
rate limiting
CSRF as applicable
token/share validation
tenant/resource scoping
abuse protections
safe errors
```

---

# 17. Request context middleware

Current API includes `HttpRequestContextMiddleware`.

Host request context may carry:

```text
correlation
authenticated principal
request metadata
```

into Application abstractions.

Do not hide target business resource solely in ambient context if it is part of explicit use-case intent.

---

# 18. BE-API-008 — HTTP context is translated, not leaked inward

Ordinary Application/Domain code MUST NOT depend on:

```text
HttpContext
IHeaderDictionary
route values
controller/minimal-API types
```

API maps those into stable inward contracts.

---

# 19. Correlation ID

Current middleware includes `CorrelationIdMiddleware`.

Correlation is operational trace context.

It is not:

```text
authorization
idempotency
business identity
```

unless an explicit contract relates them.

---

# 20. BE-API-009 — Correlation value is safe and bounded

Validate/generate according to host policy.

Do not trust arbitrarily large/unbounded client correlation values into logs/metrics.

Do not include secret/user payload in correlation IDs.

---

# 21. CSRF

ADR-003 is Accepted and currently defines Double Submit Cookie CSRF protection for cross-origin cookie-authenticated scenarios.

Current API includes:

```text
CsrfValidationMiddleware
```

with Infrastructure token/protection mechanics according to the ADR.

---

# 22. BE-API-010 — CSRF is a host boundary separate from Application authorization

CSRF answers:

```text
Did this state-changing browser request originate with the intended client context?
```

Authorization answers:

```text
May this principal perform this action on this resource?
```

Both can be required.

Do not use one as a substitute for the other.

---

# 23. CSRF state-changing methods

Current ADR applies validation to state-changing:

```text
POST
PUT
PATCH
DELETE
```

and establishes/uses a CSRF token cookie/header pattern.

Any change to the accepted mechanism requires ADR/current security architecture review.

---

# 24. BE-API-011 — CSRF bypass is explicit and narrowly justified

Do not disable CSRF globally because:

```text
one webhook
one API-key integration
one non-browser client
```

uses a different trust model.

Classify endpoints/credential modes correctly.

---

# 25. CORS

CORS is a browser host policy.

It controls which origins can make credentialed browser requests under browser enforcement.

It is not authorization.

Do not use:

```text
CORS allowed origin
```

as proof a user may access a Workspace/resource.

---

# 26. BE-API-012 — Credentialed CORS configuration is explicit and environment-safe

Do not use permissive:

```text
allow any origin + credentials
```

or development fallback in production.

Exact allowed origins are runtime configuration, not hard-coded architecture constants.

---

# 27. Security headers

Current middleware includes `SecurityHeadersMiddleware`.

Headers provide browser/client hardening.

They do not replace server-side validation/authz.

Changes belong to API/security architecture and tests.

---

# 28. Security audit middleware

Current API includes `SecurityAuditMiddleware`.

Host audit can record security-relevant transport/auth events.

It does not automatically replace Governance/business audit or Activity history.

---

# 29. BE-API-013 — Security audit records safe semantic metadata

Avoid:

```text
Authorization header
cookie content
access token
password/body secret
```

in audit/logs.

Use principal/resource/event identifiers and safe classifications.

---

# 30. Rate limiting

ADR-004 is Accepted and defines a two-layer architecture with multiple tiers:

```text
API middleware
→ pre-auth IP/sensitive
→ authenticated user/API-key partitions

Application behavior
→ Account/Workspace-aware rate limits
```

The durable principle is split by available semantic scope.

---

# 31. Current rate-limit evidence

Current API includes:

```text
PreAuthenticationRateLimitMiddleware
AuthenticatedRateLimitMiddleware
RateLimitPolicyAttribute
IRateLimitPolicyProvider
RateLimitPolicyProvider
PartitionKey
```

Application owns tenant-aware behavior where Account/Workspace context exists.

---

# 32. BE-API-014 — API rate limit uses transport-visible partition only

API middleware may partition by facts reliably available at that stage, such as:

```text
IP
authenticated principal
API key identity
```

Do not fake Workspace rate limiting before Workspace context is resolved.

---

# 33. API-key handling

If API keys are supported, endpoint authentication/access contract must identify:

```text
key principal
scope
revocation
rate-limit partition
tenant/resource authorization
```

The raw key value must not be logged or used as metric label.

---

# 34. BE-API-015 — Rate-limit key is not exposed secret identity

Use a safe stable key identifier/hash/internal ID for observation/partition where appropriate.

Do not emit raw API key in responses/logs.

---

# 35. Rate-limit failure mode

Current ADR permits configurable infrastructure failure mode:

```text
open
or
closed
```

The choice must follow endpoint/security risk.

Do not choose fail-open universally.

---

# 36. BE-API-016 — Rate-limit failure mode is risk-classified

Sensitive authentication/abuse paths may require different behavior from ordinary reads.

The policy owner decides; middleware executes.

---

# 37. Rate-limit response

A rate-limited response is a stable public error category.

Expose safe retry/rate metadata only as approved.

Do not leak internal Redis/provider keys.

---

# 38. Idempotency transport

Current API contains:

```text
HttpIdempotencyEndpointFilter
IdempotencyOperationFilter
```

API accepts/validates/forwards idempotency input for Application operations that actually support idempotency.

---

# 39. BE-API-017 — Idempotency header does not create idempotent semantics by itself

An endpoint MUST NOT advertise/accept an idempotency contract unless Application has a stable operation identity/state model.

API is transport input only.

---

# 40. Idempotency key validation

Transport validates basic key constraints:

```text
presence when required
length/format
safe character policy
```

Application owns semantic scope/conflict.

Do not hash arbitrary entire request body in API and call that the sole operation contract unless architecture explicitly defines it.

---

# 41. BE-API-018 — Retry of one logical HTTP operation preserves the same idempotency key

Client/server guidance and OpenAPI should make this clear.

A generated fresh key per automatic retry defeats idempotency.

---

# 42. HTTP status for idempotency conflict

Same key with conflicting semantic request should map to a stable conflict/problem response as Application contract defines.

Do not silently return the first success for a materially different request.

---

# 43. Error translation

Current API has `ErrorHandling/` and API tests include `ProblemDetails`.

The public error boundary should translate stable Application result categories into consistent HTTP ProblemDetails/error contracts.

---

# 44. BE-API-019 — Same semantic failure maps consistently across endpoints

Examples:

```text
validation
not found
authorization
conflict
not entitled
rate limited
idempotency conflict
dependency unavailable
```

should not produce arbitrary different shapes/statuses per endpoint unless contract differs intentionally.

---

# 45. ProblemDetails

ProblemDetails may include:

```text
stable type/code
title
status
safe detail
correlation
field errors
```

as contract defines.

Do not expose stack trace, SQL/provider error, secret, internal path.

---

# 46. BE-API-020 — Public error code is semantic and stable

Clients should not parse:

```text
exception class
English message
SQLSTATE
constraint name
```

as the contract.

Map to stable codes/categories.

---

# 47. Unknown exception

Unknown programming/runtime failures map to safe 5xx response while retaining correlation for diagnostics.

Do not downgrade unknown exceptions into validation/200 responses.

Observability retains internal detail securely.

---

# 48. OpenAPI

Current API includes:

```text
OpenApiExportCommand
IdempotencyOperationFilter
SecurityRequirementsOperationFilter
```

and package support from ASP.NET OpenAPI + Swashbuckle.

OpenAPI is generated public contract evidence.

---

# 49. BE-API-021 — OpenAPI is generated from the API producer

Do not maintain a handwritten endpoint/schema catalog as an equal authority.

Change endpoint/contracts/metadata, regenerate/export OpenAPI, review semantic diff.

---

# 50. OpenAPI drift

CI/tests should detect unintended drift.

A drift check proves generated output matches producer.

It does not prove the changed contract is product-compatible.

Review both:

```text
drift
+
semantic compatibility
```

---

# 51. BE-API-022 — Generated OpenAPI diff is reviewed, not blindly accepted

Inspect:

```text
operation
route
method
requiredness
type
enum
nullable
security
idempotency
error
version
```

Do not update the artifact only to turn CI green.

---

# 52. Generated frontend clients

Frontend generated contracts should derive from backend/OpenAPI producer where architecture says so.

Do not hand-edit generated frontend DTO to compensate for backend contract error.

Contract flow:

```text
backend producer
→ OpenAPI
→ generator
→ frontend
```

---

# 53. BE-API-023 — Producer and generated consumer change atomically at contract level

For additive/breaking changes ensure:

```text
producer
generated artifact
consumer compatibility
```

are synchronized through contract-first delivery.

Repository owner:

```text
../../../docs/delivery/contract-first-delivery.md
```

---

# 54. API versioning

Current API uses `Asp.Versioning.Mvc` and has `Versioning/ApiVersionConstants.cs`.

Versioning is explicit public compatibility mechanism.

Do not create a new version for every additive change.

Do not force incompatible semantics into an existing version to avoid migration.

---

# 55. BE-API-024 — Version changes when supported consumer compatibility requires it

Assess:

```text
old web bundle
mobile
external integrations
background callers
generated clients
```

A breaking change may require:

```text
new version
compatibility adapter
staged migration
```

---

# 56. Version identity

API version is distinct from:

```text
aggregate Version
event contract Version
application release version
```

Do not conflate them.

---

# 57. Version coexistence

During migration:

```text
v1
v2
```

may coexist.

Both must route to valid Application semantics and preserve security/tenant behavior.

Do not implement v1 by directly accessing old persistence if Application can own a compatibility adapter.

---

# 58. BE-API-025 — Old API version remains a supported consumer contract until retirement criteria are met

Do not delete v1 because current web switched to v2 if mobile/external clients remain supported.

Use migration/removal proof.

---

# 59. Route design

Routes expose resource/use-case concepts.

Do not encode internal:

```text
database table
repository
provider
implementation class
```

in public route design unless it is genuine public product vocabulary.

---

# 60. BE-API-026 — Route does not define aggregate boundary

An endpoint may address a child resource.

Application/Domain may still load/operate through the owning aggregate.

Transport addressability does not make every resource an aggregate root.

---

# 61. HTTP method

Use HTTP method according to operation semantics/idempotency/cache expectations.

Do not treat:

```text
GET
```

as permission to mutate hidden state.

Side-effecting operations should not masquerade as safe reads.

---

# 62. BE-API-027 — GET/HEAD remain safe from business mutation

Incidental:

```text
metrics/logging
CSRF cookie establishment under accepted host design
```

may occur.

Do not commit business state from GET.

---

# 63. Request body size

Bound request/upload sizes by capability and infrastructure policy.

Do not allow unbounded JSON/list/file input.

Large file upload should use storage-specific flow as architecture defines rather than buffering arbitrarily in API memory.

---

# 64. BE-API-028 — Unbounded input is rejected before expensive protected work

Limits can apply to:

```text
body
collection count
query length
upload
filter complexity
```

based on risk/workload.

Do not invent universal numbers here; use validated configuration/product contract.

---

# 65. Pagination

Unbounded collection endpoints require pagination/limits.

Public contract defines:

```text
cursor/page
limit
sort
filter
continuation
```

Application defines authorized read semantics.

Infrastructure implements efficient query.

---

# 66. BE-API-029 — Pagination preserves stable order and tenant authorization

A cursor must not:

```text
leak tenant/resource facts
allow page crossing permission boundary
change meaning unpredictably with a code refactor
```

when it is a public contract.

---

# 67. Cursor opacity

Opaque cursor can protect implementation flexibility.

Do not expose raw SQL offset/tenant secret/internal provider token unless explicitly part of contract and safe.

---

# 68. Filter/sort

Allowed filter/sort fields are an API contract.

Validate:

```text
known field
type
operation
bounded complexity
authorization implications
```

Do not translate arbitrary client strings to SQL/member reflection without a safe allowlist/model.

---

# 69. BE-API-030 — Filter/sort cannot expand visible scope

Filtering changes subset/order.

It must not bypass:

```text
tenant
resource permission
visibility
```

of the base query.

---

# 70. Bulk endpoints

Bulk transport must represent Application bulk semantics:

```text
all-or-nothing
per-item
accepted async
```

Do not choose partial success based only on ease of HTTP response construction.

---

# 71. BE-API-031 — Bulk response reports semantic per-operation outcome honestly

If async accepted:

```text
202/pending identity
```

is not final success.

If partial:

```text
per-item result
```

is explicit.

---

# 72. Long-running operation

Operations that cannot complete safely within normal request lifetime may return an operation resource/status.

Application/product owns operation semantics.

API exposes transport representation.

Do not hold HTTP connection indefinitely as the only recovery model.

---

# 73. BE-API-032 — Accepted means accepted, not completed

Use correct public state/status for queued/background work.

Do not return a final success representation before authoritative completion.

---

# 74. Concurrency transport

API carries expected-version/ETag/header/body semantics as the public contract.

Application owns conflict decision.

Infrastructure owns persistence concurrency.

---

# 75. BE-API-033 — Concurrency conflict is stable public outcome

Do not map stale-write DB conflict to:

```text
500
or
silent overwrite
```

Translate according to public contract.

---

# 76. Conditional requests

If ETag/If-Match is used, it should map cleanly to Application expected version.

Do not create a second unrelated version value at API.

---

# 77. Content negotiation

Use supported media types/JSON conventions deliberately.

Do not expose internal serializer options as public feature knobs.

Breaking serialization convention changes require compatibility review.

---

# 78. BE-API-034 — Serializer configuration is part of public compatibility

Changes to:

```text
enum representation
case naming
null handling
date format
polymorphism
```

can be contract changes.

Review old clients.

---

# 79. Dates/time

Public date/time representation should be unambiguous, typically offset/UTC-aware according to contract.

Do not serialize server local time implicitly.

Business timezone semantics remain product-owned.

---

# 80. IDs

Public IDs should use stable contract formats.

Do not expose provider external IDs as internal resource identity unless product contract deliberately does so.

Validate empty/malformed IDs at transport/use-case boundary.

---

# 81. BE-API-035 — Public identity format remains stable across persistence refactor

Changing EF key mapping should not silently break public resource IDs.

Identity migration is a contract/data change.

---

# 82. Authentication cookies/tokens

API host config owns how credentials are received/set.

Security architecture defines cookie attributes/token handling.

Do not include auth token in response body/log unless explicitly required and safely designed.

---

# 83. BE-API-036 — Credential transport has one intentional trust model

Cookie, Bearer JWT, API key and public/share token have different:

```text
CSRF
CORS
storage
replay
revocation
rate-limit
```

requirements.

Do not apply one mechanism blindly to all.

---

# 84. API key versus user JWT

API key authentication can identify an integration/service principal.

Resource authorization still evaluates the key's allowed tenant/resource capability.

Do not treat high rate limit as broader permission.

---

# 85. Share/public token

A share token may grant bounded resource capability without ordinary membership.

Treat token as capability contract:

```text
scope
expiry
revocation
permissions
resource
```

not a magic bypass flag.

---

# 86. BE-API-037 — Capability token is scoped and revocable according to Product contract

Do not convert a valid share token into global authenticated-user authority.

---

# 87. Health endpoints

Current API includes `Endpoints/Health`.

Health is operational endpoint, not product API.

Distinguish:

```text
liveness
readiness
dependency/capability health
```

according to operations/runtime architecture.

---

# 88. BE-API-038 — Health endpoint is bounded and side-effect free

Do not:

```text
perform business mutation
scan all tenants
run expensive migration
```

as a normal health check.

Do not expose secret dependency details publicly.

---

# 89. Admin endpoints

Admin/system endpoints require explicit privileged security model.

Do not assume URL prefix `/admin` is authorization.

Application/system-operation policy remains authoritative.

---

# 90. BE-API-039 — Admin endpoint is protected by explicit server policy

Test:

```text
unauthenticated
ordinary user
wrong scope
authorized system/admin principal
```

as applicable.

No route-name-only authorization.

---

# 91. Internal endpoint

An endpoint exposed only on internal network is not automatically trusted.

Network is one control.

Authentication/authorization still applies as required.

Do not put broad data repair endpoint behind “internal only” and skip governance.

---

# 92. BE-API-040 — Internal network is not identity

Service-to-service/API calls need explicit principal/credential/scope where protected behavior exists.

---

# 93. Webhooks

Provider webhooks can enter through API.

API/Infrastructure must authenticate/verify the provider boundary before product mutation.

Application receives normalized trusted event/command.

---

# 94. BE-API-041 — Webhook authenticity/replay validation precedes Application effect

Do not parse provider payload then mutate product before signature/replay verification.

Provider event ID can support dedup where contract defines it.

---

# 95. Webhook response timing

Provider protocols may require quick acknowledgement.

If product processing is durable async:

```text
validate
durably enroll
ack provider
process later
```

according to integration architecture.

Do not return success before durable acceptance if loss is unacceptable.

---

# 96. Realtime relationship

REST and realtime can expose consequences of the same committed business facts.

Realtime is not a second mutation/source truth.

Repository event/realtime architecture owns convergence.

---

# 97. BE-API-042 — REST read remains authoritative reconciliation path unless another owner explicitly replaces it

Client gap/reconnect should be able to recover authoritative state.

Do not make the only copy of a state change a transient realtime frame.

---

# 98. OpenAPI security metadata

Current `SecurityRequirementsOperationFilter` helps generated contract reflect auth requirements.

OpenAPI security metadata must match actual endpoint host policy.

Do not document an endpoint authenticated while runtime allows anonymous, or vice versa.

---

# 99. BE-API-043 — Security metadata drift is a contract/security defect

Test/generate from runtime metadata where practical.

Do not hand-maintain a contradictory security catalog.

---

# 100. OpenAPI idempotency metadata

Current `IdempotencyOperationFilter` can expose the idempotency contract in API description.

Only annotate operations that actually support it.

Do not globally advertise `Idempotency-Key` without Application semantics.

---

# 101. API docs/version deprecation

When retiring an API version/endpoint, public contract can mark deprecation before removal.

Removal requires supported-consumer evidence.

Do not keep dead compatibility forever without an owned retirement plan.

---

# 102. BE-API-044 — Deprecation has a replacement/removal policy

A deprecated endpoint should identify:

```text
replacement
compatibility window/consumer floor
removal owner/condition
```

where externally consumed.

---

# 103. Endpoint registration

Route registration should be deterministic and testable.

Do not maintain manual prose route inventory as authority.

Source/OpenAPI/tests are executable inventory.

---

# 104. BE-API-045 — Every production endpoint is discoverable through composition/OpenAPI/access gate as appropriate

Avoid hidden endpoints registered outside standard composition that bypass:

```text
auth metadata
versioning
rate limiting
OpenAPI
architecture tests
```

unless deliberately excluded for a documented reason such as health/ops.

---

# 105. Composition root

API is allowed to reference Infrastructure because it composes outer implementations.

Composition wires:

```text
Application ports
Infrastructure implementations
Platform/runtime services
auth
middleware
OpenAPI
host options
```

It should not execute those mechanisms during registration beyond safe startup validation.

---

# 106. BE-API-046 — Composition dependency does not transfer business ownership to API

API can know which Infrastructure implementation to register.

Endpoint code still calls Application.

Do not use direct Infrastructure service because API can reference its assembly.

---

# 107. Startup validation

Critical host configuration should validate before serving traffic.

Examples:

```text
JWT/security
DB/provider registration
CORS
CSRF configuration
rate-limit config
OpenAPI/version config
```

according to runtime policy.

Do not fall back to insecure development values in production.

---

# 108. BE-API-047 — Misconfigured critical security host fails safe

Do not silently disable:

```text
auth
CSRF
security headers
trusted origins
```

because config is missing, unless an explicit environment/rollout policy allows the feature to be intentionally disabled safely.

---

# 109. Middleware ordering

Middleware order is architecture because later middleware may depend on:

```text
correlation
authentication
rate-limit partition
request context
CSRF
security audit
error handling
```

Current middleware includes pre-auth and authenticated rate-limit phases.

---

# 110. BE-API-048 — Middleware order follows trust/context prerequisites

Example:

```text
pre-auth rate limit
→ before authentication

authenticated rate limit
→ after authenticated principal exists
```

Do not reorder solely for readability.

---

# 111. Exception handling middleware

A global boundary should catch unhandled transport/runtime exceptions and produce safe response/correlation.

It must not swallow cancellation/client disconnect incorrectly or misclassify known Application result.

---

# 112. BE-API-049 — One public error mapping path avoids endpoint-specific drift

Endpoint should not manually invent ProblemDetails for common result categories if a canonical mapper exists.

Feature-specific error can extend the stable contract deliberately.

---

# 113. Response caching

HTTP/public cache headers are transport cache semantics.

Application/Infrastructure authorized caching is separate.

Do not mark tenant/private data publicly cacheable because server-side cache exists.

---

# 114. BE-API-050 — HTTP cacheability is reviewed independently from server cache

Consider:

```text
public/private
Vary
authorization
cookie
tenant
staleness
invalidations
```

before exposing shared intermediary caching.

---

# 115. Compression

Response compression is host transport optimization.

It must not expose secrets through unsafe cross-origin/compression contexts without security review.

Do not change semantic contract.

---

# 116. File download

Protected file endpoint should:

```text
authorize resource
resolve storage reference
stream/signed access
set safe content metadata
```

Do not expose raw internal storage path/key without need.

---

# 117. BE-API-051 — Content-Disposition/content-type are validated public transport metadata

Prevent:

```text
header injection
unsafe inline execution
wrong MIME
```

for untrusted uploaded filenames/content.

---

# 118. File upload

Validate transport concerns:

```text
size
content type
filename metadata
stream
```

while product ownership/lifecycle belongs to Application/context.

Virus/content scanning, if required, is an outer security/provider mechanism with explicit pending state.

---

# 119. BE-API-052 — Upload acceptance state is honest

If scan/storage/provider processing remains pending:

```text
accepted/pending
```

not final product success where unsafe content could still be rejected.

---

# 120. Request cancellation

Propagate `CancellationToken` into Application.

Do not interpret disconnected client as proof transaction/provider effect did not complete.

Application/Infrastructure manage committed/unknown outcome semantics.

---

# 121. BE-API-053 — Client disconnect does not rewrite authoritative outcome

If source committed after disconnect, it remains committed.

Retry/idempotency lets client recover safely.

---

# 122. Timeout

Host timeout is a resource-protection mechanism.

A timed-out response can coexist with committed backend/provider outcome.

Do not automatically retry non-idempotent operations without stable operation identity.

---

# 123. API observability

Record safely:

```text
operation/route
status/result category
latency
principal class
tenant/resource scope where safe
correlation
release
rate-limit/idempotency outcome
```

Avoid raw secret/body logging.

---

# 124. BE-API-054 — Metrics use bounded route/operation identity

Use route template/operation name, not raw URL with resource IDs, as high-cardinality metric label.

---

# 125. Sensitive request logging

Never log ordinary:

```text
Authorization
Cookie
password
OAuth code
API key
CSRF token
file body
provider secret
```

Redact before structured logging.

---

# 126. API test scope

Primary:

```text
backend/tests/Notrelix.API.Tests
```

Current test folders include:

```text
Admin
Assertions
Contracts
Idempotency
Identity
Middleware
ProblemDetails
WorkManagement
Workspaces
```

Use tests for transport/host contract.

---

# 127. BE-API-055 — API tests prove transport/public behavior, not substitute for Domain/Application tests

Use API test for:

```text
binding
auth middleware
CSRF
rate limit
ProblemDetails
OpenAPI
version/idempotency transport
```

Use Domain/Application tests for owned business rules.

---

# 128. Integration proof

Use Integration when the API contract depends on:

```text
real PostgreSQL/RLS
transaction
production DI graph
provider/messaging composition
```

Do not claim RLS from an API test using EF InMemory.

---

# 129. Contract tests

For public changes test:

```text
request shape
response shape
required fields
status/error
security metadata
idempotency
version
OpenAPI
```

and old/new compatibility as applicable.

---

# 130. BE-API-056 — Contract test asserts semantics, not only snapshot bytes

Snapshot is useful when full contract diff matters.

Still understand why:

```text
requiredness
enum
security
error
```

changed.

---

# 131. Middleware tests

Test ordering/behavior at host seam when the middleware's placement matters.

Unit-testing a helper alone is insufficient for:

```text
pre-auth versus post-auth rate limit
CSRF method filtering
security headers
correlation propagation
```

---

# 132. Security negative tests

Applicable:

```text
anonymous protected endpoint
wrong principal/scope
missing/invalid CSRF
forged share/API key
rate-limit partition
webhook invalid signature
```

Prove no protected business mutation if the security boundary rejects.

---

# 133. BE-API-057 — Rejected request produces no protected effect

Where meaningful verify:

```text
Application handler not invoked
DB unchanged
provider not called
```

at the appropriate seam.

---

# 134. OpenAPI gate

API CI should produce/check OpenAPI deterministically.

A public contract change should fail drift until generated evidence is updated intentionally.

Do not bypass the gate by excluding the endpoint from OpenAPI unless exclusion is deliberate architecture.

---

# 135. BE-API-058 — OpenAPI exclusion is explicit

Health/internal technical endpoints may be excluded by policy.

Business/public endpoints should not be hidden merely to avoid contract review.

---

# 136. Idempotency test matrix

Applicable API cases:

```text
required/missing header
malformed key
first request
same key/same request
same key/conflicting request
retry after timeout
OpenAPI metadata
```

Application/Integration proves persistence semantics.

---

# 137. Versioning test matrix

Applicable:

```text
default/current version
explicit old version
unsupported version
deprecated version
same semantic auth/error behavior
OpenAPI per version
```

---

# 138. Pagination test matrix

Applicable:

```text
default limit
max/invalid limit
stable cursor
tenant isolation
sort/filter validation
next-page correctness
```

Use representative data > one page.

---

# 139. Rate-limit test matrix

Applicable:

```text
anonymous partition
sensitive endpoint
authenticated principal partition
API-key partition
tenant-aware Application tier
failure mode
headers/error
```

Do not require all tiers in every endpoint test.

---

# 140. CSRF test matrix

According to accepted ADR:

```text
safe GET behavior/token setup
state-changing missing token
mismatch
valid cookie+header
disabled/configured rollout state
non-browser/credential mode exemptions if explicitly designed
```

---

# 141. BE-API-059 — Security mechanism change requires accepted ADR alignment

If CSRF/rate-limit architecture changes materially:

```text
update/supersede ADR
update canonical security/API docs
update host tests
```

Do not silently rewrite behavior while ADR remains Accepted.

---

# 142. Change classification

Examples:

```text
additive endpoint/field
→ C1

semantic behavior
→ C2

breaking endpoint/schema/error/version
→ C3

persistence-backed contract migration
→ C4

host architecture/middleware model
→ C5/C7

auth/CSRF/rate-limit/public-share
→ C6

runtime configuration
→ C7

destructive admin endpoint
→ C8
```

Modifiers:

```text
MOBILE_LAG
PROVIDER_EXTERNAL
CROSS_TENANT
ROLLBACK_UNSAFE
```

often apply.

---

# 143. API ADR trigger

ADR may be required for:

```text
authentication transport strategy
CSRF foundation
rate-limit architecture
versioning strategy
public error contract foundation
API-key trust model
major endpoint/composition architecture
```

Routine endpoint addition following current architecture does not need an ADR.

---

# 144. Endpoint review checklist

```text
[ ] product/use-case owner
[ ] route/method
[ ] request mapping
[ ] authentication mode
[ ] Application authorization
[ ] tenant/resource scope
[ ] idempotency
[ ] concurrency
[ ] rate limit
[ ] CSRF/CORS as applicable
[ ] response
[ ] ProblemDetails
[ ] OpenAPI
[ ] version compatibility
[ ] tests
```

---

# 145. Public contract review checklist

```text
[ ] semantic owner
[ ] stable operation
[ ] request requiredness
[ ] response meaning
[ ] error categories
[ ] auth/security metadata
[ ] idempotency/concurrency
[ ] pagination/filter/sort
[ ] old clients
[ ] mobile/browser bundle
[ ] generated frontend consumer
[ ] OpenAPI diff
```

---

# 146. Security-host review checklist

```text
[ ] auth principal
[ ] anonymous/public classification
[ ] CSRF
[ ] CORS
[ ] security headers
[ ] rate limit partition
[ ] audit
[ ] secret-safe logging
[ ] middleware order
[ ] negative tests
```

---

# 147. Version retirement checklist

```text
[ ] replacement exists
[ ] old client inventory
[ ] mobile floor
[ ] external consumer
[ ] old browser bundle risk
[ ] telemetry old-version use
[ ] migration window
[ ] docs/deprecation
[ ] removal evidence
```

---

# 148. Stop conditions

Stop API implementation if:

- endpoint needs direct DbContext/provider workflow to complete business behavior;
- API-local role check would be the only authorization;
- public DTO exposes EF/provider model;
- an authenticated principal is being treated as resource authorization;
- CSRF is being disabled globally for one incompatible client;
- rate-limit partition requires tenant facts unavailable at API stage;
- idempotency is advertised without Application operation identity;
- public error requires leaking provider/SQL exception;
- breaking contract has unknown mobile/external consumers;
- OpenAPI is being bypassed to avoid drift review;
- internal-network endpoint is being treated as trusted with no security model;
- webhook mutation occurs before authenticity/replay validation.

---

# 149. Executable evidence

Current source:

```text
backend/src/Notrelix.API
```

Current tests:

```text
backend/tests/Notrelix.API.Tests
backend/tests/Notrelix.Integration.Tests
```

Focused:

```bash
cd backend
dotnet test tests/Notrelix.API.Tests/Notrelix.API.Tests.csproj
```

Public/security changes also require OpenAPI/integration/architecture gates according to classification.

---

# 150. Related canonical owners

Backend:

```text
application-model.md
infrastructure-and-data.md
platform-and-messaging.md
security-tenancy-authorization.md
testing-and-quality-gates.md
```

ADRs:

```text
../decisions/ADR-003-csrf-protection.md
../decisions/ADR-004-rate-limiting-architecture.md
```

Repository:

```text
../../../docs/architecture/contract-boundaries.md
../../../docs/delivery/contract-first-delivery.md
../../../docs/quality/security-quality-standard.md
```

---

# 151. Non-responsibilities

API does not own:

```text
Domain aggregate invariant
Application resource authorization semantics
PostgreSQL mapping/RLS
Platform retry/order/poison
provider business meaning
frontend server-state/query architecture
product entitlement/lifecycle
```

It exposes and protects the transport boundary around those owners.

---

# 152. Final API rule

A healthy endpoint can be stated as:

```text
public route/contract
        ↓
host trust controls
(authentication / CSRF / rate limit / headers)
        ↓
validated transport input
        ↓
Application command/query
        ↓
Application authorization + use-case execution
        ↓
stable result
        ↓
consistent ProblemDetails/response
        ↓
generated OpenAPI contract
```

with:

```text
explicit versioning when incompatible
idempotency only for supported operations
bounded pagination/input
safe public errors
deterministic route/OpenAPI inventory
```

and without:

```text
DbContext in endpoint
provider workflow in endpoint
API-only permission check
EF/provider DTO leakage
hidden endpoint contract
global CSRF bypass
tenant rate limit before tenant context
hand-edited generated consumer
```
