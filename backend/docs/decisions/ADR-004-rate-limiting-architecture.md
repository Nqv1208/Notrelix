---
document_id: ADR-004
document_type: architecture-decision
status: Accepted
owner: backend-architecture
applies_to:
  - backend
  - backend-api
  - backend-application
  - backend-security
  - rate-limiting
  - abuse-protection
evidence:
  - backend/docs/architecture/api-and-contracts.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/application-model.md
  - backend/src/Notrelix.API/Middleware/PreAuthenticationRateLimitMiddleware.cs
  - backend/src/Notrelix.API/Middleware/AuthenticatedRateLimitMiddleware.cs
  - backend/src/Notrelix.API/RateLimiting/
  - backend/src/Notrelix.Application/Common/RateLimiting/
  - backend/src/Notrelix.Infrastructure/RateLimiting/
  - backend/src/Notrelix.API/appsettings.json
review_on:
  - decision-superseded
  - rate-limit-layer-model-change
  - rate-limit-partition-change
  - api-key-rate-limit-identity-change
  - rate-limit-algorithm-change
  - rate-limit-failure-mode-change
  - tenant-aware-rate-limit-change
---

# ADR-004: 5-Tier Rate Limiting Architecture

## ID

`ADR-004`

## Status

Accepted

### Current alignment warning

The historical decision remains recorded as `Accepted`.

However, current `develop` source does **not** fully implement every consequence described by the historical ADR.

Current verification identifies source debt in:

```text
Tier 5 tenant-aware Application behavior
TokenBucket algorithm semantics
configured FailMode enforcement
raw API-key partition material
```

These gaps are documented under **Current Implementation Alignment and Drift** below.

This normalization does **not** silently change the historical decision to match the current code.

It also does **not** silently declare the current code to be the new architecture.

The drift must be resolved by:

```text
bringing source back into alignment
or
making a deliberate superseding architecture decision
```

depending on the intended target.

---

## Date

`2026-08-11`

Historical note:

```text
The original ADR did not contain an explicit Date section.
This date is recovered from the Git history entry that introduced/preserved
this ADR in the current documentation refoundation commit.
```

---

## Owners

Current stewardship:

- `backend-architecture`

Historical authorship/owner:

```text
Not recorded explicitly in the original ADR.
```

This normalization does not infer historical authorship from current stewardship.

---

## Context

Notrelix serves several client and execution classes with materially different abuse and throughput characteristics:

```text
anonymous/public browser requests
authentication-sensitive requests
authenticated user traffic
API integrations
Account/Workspace-scoped application operations
```

The original ADR records that one global rate-limit policy is not sufficient for all of them.

Examples from the historical context:

```text
authentication endpoints
→ require strict abuse protection

authenticated interactive application traffic
→ needs higher normal throughput and controlled bursts

API integrations
→ need a separate credential-based partition and higher throughput

Workspace/account operations
→ require tenant context not yet available at early API middleware
```

The architecture therefore needed to answer two different questions:

```text
Which partition can be identified before/at the HTTP host boundary?
```

and:

```text
Which partition requires authoritative Application tenant context?
```

The accepted decision split rate limiting across two layers.

---

## Decision

Notrelix uses a **two-layer, five-tier rate-limiting architecture**.

The historical five tiers are:

```text
API / transport layer
├── Tier 1 — Anonymous
├── Tier 2 — Sensitive
├── Tier 3 — Authenticated User
└── Tier 4 — API Key

Application layer
└── Tier 5 — Tenant-aware Account / Workspace
```

The durable decision is the **partition/ownership split by the semantic context available at each layer**.

The numeric limits recorded in the original ADR are operational defaults/evidence, not immutable architecture.

---

# 1. Tier 1 — Anonymous

Historical policy:

```text
Policy:
GeneralAnonymous

Partition:
IP

Original recorded limit:
60/min

Use:
public unauthenticated endpoints
```

Current source still defines:

```text
GeneralAnonymous
PartitionBy = Ip
PermitLimit = 60
WindowSeconds = 60
Algorithm = SlidingWindow
FailMode = Open
```

in `backend/src/Notrelix.API/appsettings.json`.

The exact numbers may be tuned operationally without superseding this ADR if the architectural tier remains the same.

---

# 2. Tier 2 — Sensitive

Historical policy:

```text
Policy:
AuthStrictByIp

Partition:
IP

Original recorded limit:
5 / 60 seconds

Use:
login
registration
password reset
other abuse-sensitive anonymous/authentication paths
```

Current source still defines:

```text
AuthStrictByIp
PartitionBy = Ip
PermitLimit = 5
WindowSeconds = 60
Algorithm = SlidingWindow
FailMode = Closed
```

The stricter failure posture is operational/security policy associated with a sensitive path.

---

# 3. Tier 3 — Authenticated user

Historical policy:

```text
Policy:
GeneralAuthenticated

Partition:
UserId

Original recorded limit:
300/min

Original algorithm:
TokenBucket

Use:
normal authenticated application traffic
```

The historical rationale for Token Bucket was to allow bounded bursts such as an initial application page load while preserving a sustained average rate.

Current configuration still declares:

```text
Algorithm = TokenBucket
PartitionBy = UserId
```

but current Infrastructure implementation is not fully aligned with this semantic algorithm declaration.

That drift is documented below.

---

# 4. Tier 4 — API key

Historical policy:

```text
Policy:
ApiKeyAuthenticated

Partition:
API key

Original recorded limit:
500/min

Original algorithm:
TokenBucket

Use:
API integrations
```

The accepted architectural intent is:

```text
integration credential identity
→ independent partition from human user traffic
```

The historical ADR describes the partition as the `X-API-Key` header value.

Current canonical security architecture now requires a safe, non-secret partition identity rather than exposing raw credential material to infrastructure keys/diagnostics.

That current conflict is documented below and requires resolution rather than historical rewriting.

---

# 5. Tier 5 — Account / Workspace

Historical policy:

```text
Layer:
Application

Partition:
AccountId and/or WorkspaceId

Policy:
configurable

Use:
tenant-scoped operations
```

The historical ADR assigns this tier to:

```text
TenantAwareRateLimitBehavior
```

because Account/Workspace context is authoritative only after Application tenant/resource resolution.

This is the central reason rate limiting was not implemented as API middleware only.

---

## Why two layers?

The original ADR explicitly records three reasons.

### API can reject early

For partitions visible before the full Application pipeline:

```text
IP
UserId
API-key principal
```

the API can reject before executing more expensive business/persistence work.

### Application has tenant scope

Account/Workspace scope is resolved within the Application boundary.

The early API middleware cannot safely invent or trust tenant scope merely from arbitrary request input.

### Ownership remains clear

Conceptually:

```text
API
→ transport-visible abuse partitions

Application
→ authoritative tenant-aware operation partition
```

Rate limiting remains separate from business authorization.

---

## API middleware decision

The current implementation uses:

```text
PreAuthenticationRateLimitMiddleware
AuthenticatedRateLimitMiddleware
RateLimitPolicyAttribute
IRateLimitPolicyProvider
```

### Pre-authentication middleware

The current `PreAuthenticationRateLimitMiddleware`:

```text
reads endpoint RateLimitPolicyAttribute
loads the named policy
acts only when PartitionBy == Ip
uses HttpContext.Connection.RemoteIpAddress
calls IRateLimitService
writes TooManyRequests when denied
```

This preserves the early IP-based tier.

### Authenticated middleware

The current `AuthenticatedRateLimitMiddleware`:

```text
reads endpoint RateLimitPolicyAttribute
loads the named policy
runs after authentication
supports UserId
supports ApiKey
rejects AccountId/WorkspaceId configuration at API middleware
```

Its current guard explicitly says Account/Workspace limiting belongs in:

```text
TenantAwareRateLimitBehavior in the Application pipeline
```

This current source still preserves the **architectural ownership intent** of Tier 5 even though the behavior itself is currently absent from the Application behavior inventory.

---

## Partition decision

The original ADR records these partition sources:

```text
Ip
→ connection remote IP

UserId
→ authenticated JWT subject

AccountId
→ current Application tenant context

WorkspaceId
→ current Application tenant context

ApiKey
→ X-API-Key header value
```

The durable architecture requirement is that the partition comes from a **trusted identity/scope available at the layer that owns that tier**.

---

## Partition trust rules

### IP

IP partitioning is a transport/network identity.

Behind proxies, the source IP must respect the trusted forwarded-header/proxy configuration.

Do not trust arbitrary forwarded headers.

### UserId

Authenticated user partitioning uses a verified authenticated principal identity.

Do not use a client-provided body/query user ID.

### AccountId / WorkspaceId

Tenant partitioning must use authoritative server-resolved Application tenant context.

Do not use:

```text
route WorkspaceId
request body AccountId
```

as the sole proof of tenant scope.

### API key

The historical ADR used the raw API-key header value as the partition.

Current security architecture requires the runtime to partition using a safe credential identity/reference/hash rather than persisting/exposing raw credential material as an infrastructure key.

This is a current decision/source alignment issue.

---

## Algorithm decision

The original ADR records:

```text
anonymous/sensitive
→ Sliding Window

authenticated/API-key
→ Token Bucket
```

### Why Sliding Window for anonymous/sensitive?

The original decision uses stricter window behavior for abuse-sensitive/public paths.

### Why Token Bucket for authenticated traffic?

The original ADR explicitly states:

```text
allow bounded bursts
while preserving an average rate
```

for authenticated traffic such as a client loading several resources at once.

The exact algorithm implementation is therefore not merely a configuration label when the burst semantics matter.

---

## Failure-mode decision

The original ADR records configurable failure behavior:

```text
Open
→ allow request when the rate-limit infrastructure cannot decide

Closed
→ reject request when the rate-limit infrastructure cannot decide
```

The choice is risk-sensitive.

Sensitive authentication/abuse paths can require fail-closed behavior.

Ordinary availability-oriented paths can be configured differently.

Failure-mode policy is **not** the same as the normal limit decision.

---

## Decision invariants

### Tier ownership follows available trusted scope

Do not calculate Workspace/Account partition in early API middleware when authoritative tenant context is unavailable.

### Rate limiting is not authorization

A request under its quota can still be unauthorized.

A fully authorized request can still be rate limited.

### Partition identity is stable

Retries/requests from the same logical partition must map consistently according to the policy.

### Credential secrets are not observability identities

A secret API-key value must not become a public/log/metric/debug identifier merely because it identifies a rate-limit partition.

### Algorithm name reflects actual semantics

If a policy is configured as `TokenBucket`, the implementation must actually provide the accepted Token Bucket behavior or the architecture/configuration must be changed deliberately.

### Failure mode is enforced

A configured `Open`/`Closed` policy is meaningful only if infrastructure errors are translated according to that mode.

### Numeric limits remain configurable

Changing the current permit/window numbers does not automatically change this ADR.

### Endpoint policy is explicit

Rate-limited endpoints declare/select the intended policy through the approved host metadata/config model.

---

## Alternatives Considered

### Alternative A — One global rate-limit policy

This alternative is recoverable from the original ADR context as the rejected baseline.

Concept:

```text
all requests
→ one policy
→ one partition/limit model
```

#### Potential benefit

- simpler configuration;
- fewer middleware/pipeline concepts.

#### Why not chosen

The historical context explicitly states that different client/trust classes require different treatment.

One policy cannot simultaneously model:

```text
strict login abuse protection
normal authenticated SPA burst behavior
API integration throughput
tenant-wide Account/Workspace pressure
```

without either over-throttling valid traffic or under-protecting sensitive paths.

### Alternative B — API middleware only

This alternative is recoverable from the original decision's explicit “Why Two Layers?” rationale.

Concept:

```text
all rate limiting
→ HTTP middleware
```

#### Potential benefit

- early rejection;
- one host implementation location.

#### Why not chosen

Account/Workspace context is not safely available at the early API middleware boundary.

Implementing tenant tiers there would require:

```text
trusting client tenant identifiers
duplicating Application scope resolution
or
performing business/persistence work at the wrong host layer
```

The original ADR therefore assigns tenant-aware limiting to Application.

### Other alternatives

```text
Not recorded in the original ADR.
```

This normalization does not invent historical evaluation of third-party gateways, ASP.NET built-in limiters, fixed-window-only architecture, or provider-managed rate limiting.

---

## Consequences

### Positive

The historical decision provides:

- separate abuse policies for anonymous, sensitive, authenticated, API-key, and tenant traffic;
- early rejection for transport-visible partitions;
- authoritative Account/Workspace partitioning in Application;
- explicit per-endpoint policy metadata;
- configurable rate-limit policy definitions;
- distinct algorithms for strict versus burst-tolerant traffic;
- configurable infrastructure-failure posture.

### Operational consequence

Numeric permit/window values can be tuned in configuration.

Operators must understand:

```text
partition
algorithm
limit
failure mode
```

rather than treating a single request-per-minute number as the entire policy.

### Security consequence

Sensitive endpoint configuration can use:

```text
strict partition
stricter limit
fail-closed behavior
```

while less sensitive paths can choose availability-oriented policy.

### Complexity consequence

The architecture intentionally has multiple tiers and two enforcement layers.

This increases implementation/testing obligations.

The complexity is accepted because the client/trust contexts are materially different.

---

## Compatibility / Migration

No database schema migration is inherently required by the original decision.

Changes can still be compatibility/security relevant.

### Numeric policy change

Changing:

```text
PermitLimit
WindowSeconds
```

is normally runtime policy tuning if:

```text
tier
partition
algorithm semantics
failure mode
```

remain compatible.

### Partition change

Changing:

```text
IP → UserId
UserId → AccountId
raw credential → stable key identity/hash
```

changes how existing traffic is grouped and can produce a sudden reset/merge/split of rate-limit state.

Review rollout and Redis key compatibility.

### Algorithm change

Changing:

```text
SlidingWindow ↔ TokenBucket
```

changes burst semantics and requires deliberate review.

### Fail-mode change

Changing:

```text
Open ↔ Closed
```

changes behavior during Redis/rate-limit infrastructure failure and is security/availability relevant.

### Tenant-tier change

Adding/removing/moving the Account/Workspace tier changes the two-layer architecture and can require a superseding ADR.

---

# Current Implementation Alignment and Drift

This section records **current evidence** without rewriting the historical decision.

The classifications below are current-state observations against the accepted ADR and the current canonical architecture.

---

## Drift 1 — Tier 5 Application behavior is absent

### Accepted decision

ADR-004 specifies:

```text
TenantAwareRateLimitBehavior
→ Application pipeline
→ AccountId / WorkspaceId tier
```

### Current evidence

Current:

```text
backend/src/Notrelix.Application/Common/Behaviors/
```

does not contain:

```text
TenantAwareRateLimitBehavior.cs
```

Current `backend/src/Notrelix.Application/DependencyInjection.cs` does not register a tenant-aware rate-limit pipeline behavior.

Current `AuthenticatedRateLimitMiddleware` still contains an explicit guard stating that:

```text
PartitionKey.AccountId / WorkspaceId
must be configured via TenantAwareRateLimitBehavior
in the Application pipeline
```

### Classification

```text
SOURCE_DEBT
```

against the accepted Tier 5 decision, unless a deliberate superseding decision removes or relocates the tier.

### Required resolution

Choose one governed target:

```text
A. restore/implement the accepted Application tenant-aware tier
or
B. supersede ADR-004 with a different tenant-rate-limit architecture
```

Do not delete the middleware guard merely to hide the missing tier.

---

## Drift 2 — `TokenBucket` configuration does not currently produce Token Bucket semantics

### Accepted decision

Authenticated/API-key tiers are recorded as:

```text
TokenBucket
```

to support burst capacity with a sustained average rate.

### Current evidence

Current API middleware maps `"TokenBucket"` to:

```text
RateLimitAlgorithm.TokenBucket
```

and passes that enum to `IRateLimitService`.

Current `RedisRateLimitService.CheckAsync(...)` has a special branch only for:

```text
FixedWindow
```

and sends every other algorithm value through the same sorted-set sliding-window path.

Therefore current:

```text
SlidingWindow
TokenBucket
```

do not have distinct Redis algorithm implementations.

### Classification

```text
SOURCE_DEBT
```

against the accepted algorithm semantics.

### Required resolution

Choose deliberately:

```text
A. implement real Token Bucket semantics
or
B. change the accepted policy/ADR/configuration to Sliding Window
```

Do not keep the `TokenBucket` label if it has no Token Bucket behavior.

---

## Drift 3 — Configured `FailMode` is not enforced in the visible API/service path

### Accepted decision

Policies support:

```text
FailMode = Open | Closed
```

for infrastructure failure.

### Current evidence

Current `RateLimitPolicy` binds:

```text
FailMode
```

from configuration.

Current `PreAuthenticationRateLimitMiddleware` and `AuthenticatedRateLimitMiddleware`:

```text
read PermitLimit
read WindowSeconds
read Algorithm
read PartitionBy
```

but do not branch on `FailMode`.

Current `RedisRateLimitService` does not receive `FailMode`.

No failure-mode handling is present in these visible rate-limit execution paths.

### Classification

```text
SOURCE_DEBT
```

unless another current outer mechanism not represented by these paths deliberately owns the failure-mode behavior.

### Required resolution

Make failure-mode ownership executable and testable:

```text
policy
→ infrastructure error
→ Open/Closed result
```

or remove/change the accepted consequence through a new decision.

---

## Drift 4 — API-key partition uses raw credential material

### Historical decision

The original ADR states:

```text
ApiKey
→ X-API-Key header value
```

### Current evidence

Current `AuthenticatedRateLimitMiddleware` takes:

```text
context.Request.Headers["X-API-Key"]
```

as the partition key.

Current `RedisRateLimitService.BuildKey(...)` builds:

```text
Notrelix_ratelimit:{policy}:{partition.ToLowerInvariant()}
```

using the supplied partition value.

### Current canonical conflict

Current security/API architecture requires a safe credential partition identity such as:

```text
stable API-key ID
or
approved one-way partition hash
```

rather than using the raw secret as infrastructure key/diagnostic identity.

The current implementation also lowercases the supplied partition material as part of Redis key generation.

### Classification

```text
SOURCE_DEBT
+
SECURITY_REVIEW_REQUIRED
```

### Required resolution

Preserve the **Tier 4 logical partition** while changing its representation to a safe credential identity.

Because this changes a security-sensitive part of the accepted decision's concrete partition representation, resolve through the decision/governance process rather than silently editing history.

A superseding ADR may be appropriate if the API-key identity model changes materially.

---

## Current aligned areas

Despite the drifts above, current source still aligns with major parts of the historical architecture:

```text
API has pre-auth IP middleware
API has authenticated UserId/API-key middleware
AccountId/WorkspaceId are explicitly rejected at API middleware
endpoint metadata selects named policies
policy config remains externalized
Redis is the current rate-limit service mechanism
current anonymous/sensitive numeric defaults match the historical ADR
current authenticated/API-key numeric defaults match the historical ADR
```

The decision is therefore not treated as wholly abandoned.

It is treated as an Accepted architecture with unresolved implementation/security drift that must be reconciled.

---

## Evidence

### Canonical current architecture

- `backend/docs/architecture/api-and-contracts.md`
- `backend/docs/architecture/security-tenancy-authorization.md`
- `backend/docs/architecture/application-model.md`
- `backend/docs/architecture/testing-and-quality-gates.md`
- `backend/docs/operations/configuration-and-runtime.md`

### Historical decision record

- this ADR;
- `backend/docs/decisions/README.md`.

### Current API source

- `backend/src/Notrelix.API/Middleware/PreAuthenticationRateLimitMiddleware.cs`
- `backend/src/Notrelix.API/Middleware/AuthenticatedRateLimitMiddleware.cs`
- `backend/src/Notrelix.API/RateLimiting/RateLimitPolicy.cs` or current equivalent declaration;
- `backend/src/Notrelix.API/RateLimiting/RateLimitPolicyProvider.cs`
- `backend/src/Notrelix.API/RateLimiting/RateLimitPolicyAttribute.cs`
- `backend/src/Notrelix.API/RateLimiting/PartitionKey.cs`
- `backend/src/Notrelix.API/Options/RateLimitingOptions.cs`
- `backend/src/Notrelix.API/appsettings.json`

### Current Application source

- `backend/src/Notrelix.Application/Common/RateLimiting/IRateLimitService.cs`
- `backend/src/Notrelix.Application/Common/RateLimiting/RateLimitRequest.cs`
- `backend/src/Notrelix.Application/Common/RateLimiting/RateLimitDecision.cs`
- `backend/src/Notrelix.Application/DependencyInjection.cs`
- `backend/src/Notrelix.Application/Common/Behaviors/`

Current evidence does **not** show the historical `TenantAwareRateLimitBehavior`.

### Current Infrastructure source

- `backend/src/Notrelix.Infrastructure/RateLimiting/RedisRateLimitService.cs`
- `backend/src/Notrelix.Infrastructure/RateLimiting/RedisRateLimitOptions.cs`

### Tests / gates

Required proof for the architecture should include, as applicable:

```text
anonymous IP partition
sensitive IP partition
authenticated UserId partition
API-key safe identity partition
Account tenant partition
Workspace tenant partition
different tenants progress independently
limit exceeded → stable 429
configured algorithm semantics
Open infrastructure failure behavior
Closed infrastructure failure behavior
trusted proxy/IP behavior
credential secrecy
```

Primary test projects:

- `backend/tests/Notrelix.API.Tests/`
- `backend/tests/Notrelix.Application.Tests/`
- `backend/tests/Notrelix.Infrastructure.Tests/`
- `backend/tests/Notrelix.Integration.Tests/`

Current tree evidence alone is not sufficient to claim all historical tiers are fully proven because of the drifts recorded above.

---

## Supersedes

`None`

The original ADR does not record a prior backend ADR superseded by this decision.

---

## Superseded By

`None`

Current registry status remains:

```text
Accepted
```

No newer backend ADR currently supersedes ADR-004.

The unresolved current drift does **not** authorize silently marking this ADR Superseded.

A new decision must exist before changing the status for that reason.

---

## Historical normalization note

This file has been normalized to the current ADR schema while preserving the historical decision.

The normalization adds:

```text
metadata
ID
recoverable date
current stewardship
structured alternatives
compatibility/migration
current architecture/source evidence
explicit current-drift classification
supersession fields
```

It deliberately does **not**:

```text
change five tiers to match current missing Tier 5 source
rename TokenBucket to SlidingWindow to hide implementation drift
remove FailMode because current service does not enforce it
rewrite raw API-key partitioning as if the historical ADR had already chosen a safe hash/key ID
```

Those are current architecture/source decisions that require deliberate resolution.

---

## Decision-change trigger

A superseding ADR should be considered if Notrelix materially changes:

```text
the two-layer API/Application rate-limit split
the five-tier partition architecture
the trusted partition identity model
the authenticated burst algorithm foundation
the Redis/infrastructure-failure policy architecture
the Account/Workspace tenant-rate-limit ownership
```

Routine changes such as:

```text
tuning PermitLimit
tuning WindowSeconds
adding a new named endpoint policy within an existing tier
refactoring middleware/service class names
```

do not automatically require a new ADR if the architecture remains unchanged.

---

## Resolution checklist for current drift

Before claiming ADR-004 is fully implemented again:

```text
[ ] Tier 5 target explicitly decided
[ ] Account/Workspace behavior implemented or ADR superseded
[ ] TokenBucket label and implementation semantics agree
[ ] FailMode is executed and tested
[ ] raw API-key secret is not used as the Redis partition identity
[ ] API-key partition representation preserves stable credential identity
[ ] API/Application/Infrastructure tests prove each supported tier
[ ] infrastructure-failure Open/Closed tests exist
[ ] tenant partitions use authoritative Application scope
[ ] registry/current architecture updated if decision changes
```

---

## Final decision statement

The historical architectural intent remains:

```text
transport-visible abuse controls
        ↓
API tiers by IP / authenticated identity / integration identity
        +
authoritative tenant-aware control
        ↓
Application tier by Account / Workspace
```

with policy-defined:

```text
partition
limit
window
algorithm
failure mode
```

The current source must not be treated as architecture merely because parts of this design have drifted.

The required next action is to reconcile the implementation with this accepted decision **or** supersede the decision explicitly where the intended architecture has changed.
