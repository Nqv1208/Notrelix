---
document_id: BE-SECURITY-TENANCY-AUTHORIZATION
document_type: architecture
status: active
owner: backend-security-architecture
applies_to:
  - backend
  - authentication
  - authorization
  - tenancy
  - rls
  - privileged-operations
  - security-sensitive-cache
  - background-execution
  - realtime
  - provider-boundaries
evidence:
  - backend/src/Notrelix.Application/Common/Security/
  - backend/src/Notrelix.Application/Common/Context/
  - backend/src/Notrelix.Application/Common/Tenancy/
  - backend/src/Notrelix.Infrastructure/Data/Rls/
  - backend/src/Notrelix.Infrastructure/Auth/
  - backend/src/Notrelix.API/Security/
  - backend/src/Notrelix.API/Middleware/
  - backend/tests/Notrelix.Application.Tests/
  - backend/tests/Notrelix.Infrastructure.Tests/
  - backend/tests/Notrelix.API.Tests/
  - backend/tests/Notrelix.Integration.Tests/
  - backend/tests/Notrelix.Architecture.Tests/
  - backend/docs/decisions/ADR-002-rls-bootstrap-connection-lifecycle.md
  - backend/docs/decisions/ADR-003-csrf-protection.md
  - backend/docs/decisions/ADR-004-rate-limiting-architecture.md
review_on:
  - authentication-model-change
  - authorization-model-change
  - resource-scope-change
  - permission-cache-change
  - rls-model-change
  - tenant-context-change
  - privileged-operation-change
  - csrf-change
  - rate-limit-change
  - credential-or-secret-boundary-change
---

# Security, Tenancy, and Authorization

> **Backend security is a layered authorization and isolation system, not one middleware, one role check, or one database policy.**
>
> Authentication identifies a principal. Application authorizes a principal to perform an action on a resource. Infrastructure/RLS constrains persistence as defense-in-depth. Platform/background paths reconstruct equivalent tenant/security scope. API protects the transport boundary. Cache/realtime/search/provider mechanisms preserve those decisions rather than becoming new authorization authorities.

This document is the canonical backend architecture owner for:

- authentication versus authorization boundaries;
- Account/Workspace/resource scope;
- resource/action permission evaluation;
- tenant-context propagation;
- RLS defense-in-depth;
- permission decision/version/invalidation semantics;
- protected cache/realtime/background behavior;
- privileged/system operation constraints;
- anonymous/share/API-key/service-principal access posture;
- secret/token boundary rules;
- CSRF/rate-limit relationship to authorization;
- security audit versus product activity;
- cross-tenant negative-proof requirements.

Product-specific permissions/roles remain owned by the relevant product context and Governance.

Host-specific CSRF/rate-limit rationale remains in accepted ADRs.

Repository-wide security quality remains owned by `../../../docs/quality/security-quality-standard.md`.

---

# 1. Security model at a glance

A protected operation conceptually passes through:

```text
credential / request
        ↓
API authentication
        ↓
principal / request context
        ↓
Application tenant/resource resolution
        ↓
Application authorization
        ↓
Domain owned invariant
        ↓
Infrastructure persistence + RLS
        ↓
commit
        ↓
Platform/realtime/background propagation
```

Each layer protects a different failure mode.

No single layer is sufficient by itself.

---

# 2. BE-SEC-001 — Authentication and authorization are distinct

Authentication answers:

```text
Who/what is making the request?
Is the credential/session valid?
```

Authorization answers:

```text
May this principal perform action A on resource R now?
```

A valid identity MUST NOT imply resource access automatically.

---

# 3. Authentication boundary

API/host integrates:

```text
JWT
cookies/session
API keys
share/public tokens
provider webhook credentials
service/system credentials
```

according to the credential mode.

Application receives an authenticated/validated principal representation, not raw credential protocol.

---

# 4. BE-SEC-002 — Raw credential mechanics remain at the outer boundary

Do not pass inward:

```text
Authorization header
raw API key
cookie value
OAuth code
JWT signing key
password
webhook secret
```

as ordinary Domain/Application business data.

Translate to:

```text
principal
credential class
stable key/connection ID
scope facts
```

as appropriate.

---

# 5. Principal

A principal can represent:

```text
human user
API-key/integration identity
service/system identity
public/share capability
```

according to product/security design.

Do not assume every protected operation is a human user operation.

---

# 6. BE-SEC-003 — Principal type is explicit

Authorization should know when the actor is:

```text
user
API key
service principal
public/share capability
system operation
```

because available permissions, audit, rate limit, and revocation semantics can differ.

Do not encode all principals as a fake user ID.

---

# 7. User identity versus membership

An authenticated User exists in Identity.

Workspace membership/role belongs to Workspace/Governance semantics.

Rule:

```text
authenticated User
≠ Workspace member
≠ Workspace owner
≠ resource editor
```

Each additional fact is resolved explicitly.

---

# 8. BE-SEC-004 — User existence does not authorize tenant access

A known `UserId` MUST NOT be used as sufficient proof to:

```text
read Workspace
mutate Board
read Page
manage Billing
access provider connection
```

Application resolves the relevant membership/policy/resource facts.

---

# 9. Tenant dimensions

Notrelix can have several security scope dimensions:

```text
Account
Workspace
resource
user-private scope
provider connection
public/share capability
```

Do not reduce all tenancy to one generic `TenantId` if the product model distinguishes them.

---

# 10. BE-SEC-005 — Security scope is explicit at every protected boundary

For a protected operation identify the required:

```text
principal
Account
Workspace
resource kind
resource ID
action
```

as applicable.

Do not infer scope only from route location or current UI page.

---

# 11. Resource/action authorization

Application authorization should be based on:

```text
principal
resource
action/permission
scope
current policy facts
```

Current Application Security contracts include concepts such as:

```text
IPermissionEvaluator
IPermissionService
IWorkspacePermissionService
IResourceReferenceResolver
IResourceScopeResolver
IPermissionVersionProvider
IAuthorizationDecisionStore
PermissionContext
PermissionDecision
PermissionScope
```

These are current source evidence of centralized permission/resource evaluation.

---

# 12. BE-SEC-006 — Authorization is resource/action oriented

Prefer:

```text
can principal perform board.item.update on this Board Item?
```

over:

```text
role == Admin
```

when product policy is resource-scoped.

Role may be one input to the decision.

Role is not the whole authorization architecture.

---

# 13. Role semantics

Roles belong to Governance/Workspace product semantics.

Application may consume a role/policy decision.

Do not scatter string comparisons:

```text
"Admin"
"Owner"
"Member"
```

across handlers/endpoints as independent policy implementations.

---

# 14. BE-SEC-007 — Product role names do not become hard-coded authorization protocol by accident

Use stable permission/action contracts where the product authorization model defines them.

A plan/role rename SHOULD NOT require rewriting hundreds of unrelated handlers if semantic permissions are unchanged.

---

# 15. Permission decision

A permission decision should be stable enough to communicate:

```text
allowed/denied
scope
reason/category where safe
permission/resource version if used
```

Do not expose internal database/provider failure as a normal denied decision.

---

# 16. BE-SEC-008 — Authorization failure and authorization-system failure are distinct

Examples:

```text
principal lacks permission
→ denied

permission store unavailable
→ security dependency failure / fail-safe behavior

resource not found
→ not found / hidden according to contract
```

Do not translate security subsystem failure into `Allowed`.

---

# 17. Fail closed

For security-sensitive authorization, inability to prove access normally means:

```text
deny/degrade
```

not:

```text
allow to preserve availability
```

Any exception requires explicit accepted architecture/risk policy.

---

# 18. BE-SEC-009 — Authorization does not fail open by convenience

Do not allow protected mutation because:

```text
Redis unavailable
permission evaluator threw
Workspace lookup timed out
RLS context missing
```

Design safe unavailable/degraded behavior instead.

---

# 19. Resource reference

A `ResourceRef`/resource-kind contract identifies a resource for cross-cutting authorization/activity/collaboration.

It does not grant permission.

---

# 20. BE-SEC-010 — Resource identity is not authority

A caller knowing:

```text
BoardId
PageId
CommentId
WorkspaceId
```

does not imply permission to read/mutate it.

Do not rely on “unguessable GUID” as access control.

---

# 21. Resource scope resolution

Application may resolve:

```text
resource → Workspace → Account
```

or other ownership facts through a narrow read contract.

The resolver should retrieve enough to authorize, not expose foreign-context mutation.

---

# 22. BE-SEC-011 — Resource scope is resolved from authoritative server data

Client input can identify the requested resource.

Server data/contracts establish its owning Account/Workspace/resource relationship.

Do not trust:

```text
client-provided WorkspaceId + resourceId
```

as proof they belong together.

---

# 23. Scope mismatch

If route/body/scope facts disagree with authoritative ownership:

```text
reject
```

according to the public security/error contract.

Do not silently rebind to client-provided tenant.

---

# 24. BE-SEC-012 — Scope mismatch never broadens access

A malformed/mismatched scope MUST NOT fall back to:

```text
global query
first matching resource
system context
```

to “find” the object.

Fail safely.

---

# 25. Application authorization boundary

Protected read/write authorization belongs in Application pipeline/use-case architecture.

API host authentication is necessary but insufficient.

Infrastructure RLS is defense-in-depth but does not replace Application policy.

---

# 26. BE-SEC-013 — Protected Application use case has one authoritative authorization path

Avoid:

```text
some handlers use AuthorizationBehavior
some handlers check role manually
some rely on RLS only
```

for equivalent protected operations.

Use the canonical pipeline/resource contract.

---

# 27. Authorization-before-effect

Authorization must occur before:

```text
Domain mutation
DB write
outbox commit
provider action
cache write
realtime success broadcast
```

for the protected operation.

---

# 28. BE-SEC-014 — Late denial cannot be the normal security model

Do not:

```text
perform mutation
then discover user lacks permission
then attempt compensation
```

when authorization could have been established first.

---

# 29. Domain relationship

Domain may enforce owned actor/state invariants when those facts are part of its model.

Example:

```text
actor cannot approve own operation
```

if the product Domain owns that rule.

General resource authorization remains Application/Governance-owned.

---

# 30. BE-SEC-015 — Domain actor check is not general authorization

A non-empty `actorId` or Domain relationship MUST NOT replace:

```text
membership
permission
tenant
entitlement
```

evaluation.

---

# 31. RLS purpose

RLS protects persistence from cross-tenant access when Application/Infrastructure code is defective or bypassed.

It is defense-in-depth.

It does not decide complete product permission semantics.

---

# 32. BE-SEC-016 — RLS complements, never replaces, Application authorization

Required posture:

```text
Application:
may this principal perform this use case?

Infrastructure/RLS:
may this DB session access these tenant rows?
```

Both are needed where declared.

---

# 33. Current RLS implementation evidence

Current Infrastructure RLS area contains:

```text
RlsOptionsValidator
RlsPolicyApplier
RlsSessionContext
RlsSqlScripts/
```

Current accepted ADR-002 defines bootstrap/full RLS connection lifecycle.

These are implementation evidence.

The architecture contract is secure tenant session propagation and fail-closed DB isolation.

---

# 34. RLS session context

RLS session state may contain trusted runtime scope such as:

```text
authenticated user
Account
Workspace
system/operation mode
```

as current implementation defines.

It is connection/transaction-sensitive state.

---

# 35. BE-SEC-017 — RLS context cannot leak across pooled connections

A request for tenant A followed by tenant B on a reused physical connection MUST NOT observe A's session scope.

Infrastructure must set/reset state correctly.

Production-realistic tests must exercise reuse/lifecycle.

---

# 36. Bootstrap lifecycle

Some resource requests cannot know full tenant scope until a bootstrap lookup occurs.

ADR-002 defines the accepted minimal-context bootstrap on the same physical connection before full request transaction scope.

---

# 37. BE-SEC-018 — Bootstrap privilege is minimal

Bootstrap MUST expose only enough access to resolve the authoritative scope safely.

Do not use bootstrap as:

```text
global unrestricted read mode
```

or a path for ordinary business queries.

---

# 38. EF query filters versus RLS

EF query filters can provide default scope/lifecycle filtering.

PostgreSQL RLS is separate.

`IgnoreQueryFilters()` does not disable PostgreSQL RLS by itself.

---

# 39. BE-SEC-019 — Query-filter bypass does not imply tenant bypass

When an operation needs deleted/history/bootstrap rows:

```text
explicitly bypass EF filter if justified
+
preserve RLS/security context
```

Do not use filter bypass as a broad security shortcut.

---

# 40. System/privileged database context

System operations may require broader DB visibility.

They must be explicit, scoped, auditable, and least-privileged.

---

# 41. BE-SEC-020 — System context is not ordinary request fallback

Do not switch to system context because:

```text
RLS query returned nothing
scope resolver failed
test is hard to set up
```

Resolve the real authorization/scope problem.

---

# 42. Background execution

Workers/consumers/schedulers do not inherit HTTP principal/tenant automatically.

They need an explicit execution principal and tenant/resource context.

---

# 43. BE-SEC-021 — Background execution is never tenantless when touching tenant data

A tenant-scoped message/job must carry or resolve trusted:

```text
Account
Workspace
resource
principal/system operation
```

before protected DB access/effect.

---

# 44. Captured versus current authority

A background action may need one of two semantics:

```text
authority captured at source commit
or
current authority re-evaluated at execution
```

The product/use-case contract decides.

Platform does not guess.

---

# 45. BE-SEC-022 — Delayed execution states its authority time semantics

Examples:

```text
Notification about already-committed event
→ may use captured fact

Automation mutating a Board Item later
→ normally re-evaluates current target permission according to product contract
```

Do not replay old authority implicitly.

---

# 46. Cache security

Protected cache is derived data.

Cache key and validity must include enough scope/version to prevent reuse across principals/tenants/permission revisions.

---

# 47. BE-SEC-023 — Permission-sensitive cache has tenant/resource/principal or permission-version separation

Global cache key:

```text
board:{boardId}
```

may be unsafe if response varies by:

```text
Workspace
user
permission
share scope
```

Design key/invalidation from actual visibility semantics.

---

# 48. Permission versioning

Current Application includes:

```text
IPermissionVersionProvider
IResourceVersionReader
IAuthorizationDecisionStore
```

which can support invalidating cached decisions/results when policy/resource state changes.

Do not assume current concrete version strategy is the only implementation.

---

# 49. BE-SEC-024 — Revocation invalidates stale authorization decisions

Changes such as:

```text
membership removed
role changed
share revoked
resource moved
permission changed
```

must make previous cached `Allowed` decisions/results unreachable or invalid according to the contract.

---

# 50. Cache outage

If permission decision cache/store is unavailable:

```text
recompute from authority
or
deny/degrade
```

according to architecture.

Do not reuse stale allow forever.

---

# 51. BE-SEC-025 — Security cache is optimization, never sole source

Authoritative policy facts remain in source state/contracts.

Cache loss MUST NOT erase or broaden permission.

---

# 52. Realtime security

Realtime subscriptions/messages expose protected data after the original page/API load.

They need:

```text
authenticated connection
resource/tenant subscription scope
revocation handling
reconnect revalidation
```

according to realtime architecture.

---

# 53. BE-SEC-026 — Realtime connection authorization does not authorize every resource forever

A valid socket/user is not blanket permission.

Subscription/resource visibility must be scoped.

Permission changes can require:

```text
unsubscribe
invalidate
reconnect/refetch
```

---

# 54. Realtime payload scope

Tenant-scoped facts MUST NOT be broadcast on a global channel without filtering/security that is proven equivalent.

Avoid broad fan-out that relies solely on clients discarding foreign data.

---

# 55. BE-SEC-027 — Server never sends foreign tenant data expecting client-side filtering

The server determines authorized audience.

Frontend hiding is not a security boundary.

---

# 56. Search security

Search indexes/results are derived.

Search visibility must preserve resource/tenant authorization.

---

# 57. BE-SEC-028 — Search does not become a cross-tenant discovery oracle

Do not reveal:

```text
title
existence
count
snippet
resource ID
```

for unauthorized resources merely because the final detail endpoint denies access.

---

# 58. Analytics/reporting security

Analytics may aggregate cross-resource data within authorized scope.

It must preserve:

```text
tenant
role/permission
sharing/public boundaries
```

as product Analytics semantics require.

Do not use reporting DB/read model to bypass source authorization.

---

# 59. Provider connections

External provider connection/credential belongs to an Account/Workspace/user scope as product Integrations defines.

Access to the provider connection is separately authorized.

---

# 60. BE-SEC-029 — Provider credential possession is not provider-connection authorization

A credential stored by Infrastructure is not a capability any internal caller may use.

Application authorizes the operation/connection scope first.

---

# 61. Webhook boundary

External webhook is untrusted until:

```text
signature/authenticity
provider identity
timestamp/replay protection
schema validation
```

succeed.

Then map into trusted Application/provider event semantics.

---

# 62. BE-SEC-030 — Unverified webhook cannot mutate tenant state

Reject before:

```text
Application command
DB write
provider reconciliation mutation
```

unless the validated/durable acceptance mechanism itself is the approved trust boundary.

---

# 63. API keys

API keys represent a principal/credential with explicit scope/revocation.

Do not store/log the raw key after secure verification beyond what the credential mechanism requires.

---

# 64. BE-SEC-031 — API key permission is least-scoped

Key access SHOULD be constrained by:

```text
Account/Workspace
capability/action
expiry/revocation
rate limit
```

according to product design.

Do not make “valid key” equivalent to full Account admin.

---

# 65. Share/public capability tokens

Share links/tokens can grant bounded anonymous/public capability.

They are not ordinary user membership.

---

# 66. BE-SEC-032 — Share capability is non-transitive

A valid share token for resource A MUST NOT grant access to:

```text
parent Workspace broadly
sibling resources
billing/integration/admin surfaces
```

unless the product share contract explicitly grants that scope.

---

# 67. Share revocation

Revocation must affect:

```text
API access
cached authorization/results
realtime subscription
future background/use
```

as applicable.

Do not rely on token expiration alone if product supports immediate revoke.

---

# 68. BE-SEC-033 — Capability revocation has an invalidation path

If a share/API key/provider connection can be revoked, security architecture must define how stale allows are invalidated.

---

# 69. One-time tokens

Current Application Security includes token-purpose definitions and token/secret protection contracts.

One-time tokens may cover:

```text
verification
password reset
invite acceptance
sensitive operation
```

as product design defines.

---

# 70. BE-SEC-034 — One-time token is purpose-bound

A token for purpose X MUST NOT be accepted for purpose Y.

Bind:

```text
purpose
subject/resource
expiry
single-use/replay behavior
```

as applicable.

---

# 71. Token replay

A “one-time” token should have an authoritative consumed/revoked state or cryptographic/time-bound protocol that enforces the promised semantics.

Do not call a bearer token one-time if it can be reused until expiry.

---

# 72. BE-SEC-035 — Sensitive token comparison/storage is designed for disclosure resistance

Do not store raw reset/API/share secrets in broadly readable tables/logs if a hash/reference model can satisfy the contract.

Use approved protection primitives.

---

# 73. Secret encryption

Current Application has `ISecretEncryptor` as an inward contract.

Infrastructure implements secret protection/key access.

Domain/Application may hold a secret reference or encrypted blob contract where required, but not runtime master keys.

---

# 74. BE-SEC-036 — Encryption mechanism stays outer; secret lifecycle owner stays semantic

Example:

```text
Integrations owns Connection/credential lifecycle
Infrastructure owns encryption/provider/key mechanism
```

Do not make encryption service the owner of connection semantics.

---

# 75. Secret logging

Never log:

```text
JWT secret
password
API key
OAuth access/refresh token
webhook secret
provider API key
CSRF token
one-time token
```

in ordinary logs/events/Activity.

---

# 76. BE-SEC-037 — Security observability uses safe identifiers, not credential material

Use:

```text
principal ID
credential/key ID
provider connection ID
resource ID
decision category
correlation
```

where safe.

---

# 77. CSRF relationship

ADR-003 governs current CSRF architecture for browser credential modes.

CSRF is host request-origin protection.

It is not:

```text
authentication
resource authorization
tenant isolation
```

---

# 78. BE-SEC-038 — CSRF bypass is credential-mode specific

A webhook/API-key/non-browser flow may use a different anti-forgery model.

Do not disable CSRF for all cookie-authenticated state-changing routes to support one different client.

---

# 79. CORS relationship

CORS is browser-origin policy.

It does not authorize a user/resource.

A non-browser client is not constrained by browser CORS.

---

# 80. BE-SEC-039 — CORS is not a server authorization boundary

Do not say:

```text
only our frontend origin can access this
```

as the resource security model.

Use real authentication/authorization.

---

# 81. Rate limiting relationship

ADR-004 splits rate limiting by available scope.

API can protect:

```text
IP
credential
sensitive anonymous endpoint
```

Application can protect:

```text
Account
Workspace
operation
```

when those facts are known.

---

# 82. BE-SEC-040 — Rate limit does not grant or deny resource permission

A request under quota can still be unauthorized.

An authorized user can still be rate limited.

Keep these independent.

---

# 83. Brute force / sensitive endpoints

Authentication/token/reset/public-share endpoints can require stronger rate/abuse protections.

Exact policy is security/product/risk owned.

Do not apply ordinary read limits blindly.

---

# 84. BE-SEC-041 — Abuse-sensitive path has a stable partition without leaking account existence

Rate-limit/auth error behavior should avoid turning:

```text
email/username/account existence
```

into an enumeration oracle where product/security design requires concealment.

---

# 85. Audit versus Activity

Security/Governance Audit and user-visible Activity are separate concepts.

Audit can need:

```text
append-only accountability
security context
longer/different retention
privileged visibility
```

Activity is product UX/history.

---

# 86. BE-SEC-042 — Security audit is not replaced by user-visible Activity

Do not remove audit because an Activity record exists.

Do not expose confidential audit details in Activity.

---

# 87. Audit subject

Security audit SHOULD include safe facts such as:

```text
actor/principal
action
resource
scope
decision/result
time
correlation
```

as policy requires.

Avoid raw secret/body capture.

---

# 88. Privileged operation audit

System/admin/repair/migration operations can require stronger audit.

Do not hide privileged DB/system action behind the same generic user Activity.

---

# 89. BE-SEC-043 — Privileged bypass is explicit, scoped, and auditable

If an operation legitimately bypasses normal tenant/resource policy:

```text
who/what principal
why
scope
duration
operation
evidence
```

must be known.

No permanent hidden “super admin” code path by accident.

---

# 90. System principal

A system principal is a first-class security actor.

It should not be a fake user membership inserted solely to reuse user authorization.

Model explicit allowed system capabilities.

---

# 91. BE-SEC-044 — Service/system authority is least privilege

Background/admin capability receives only the actions/scopes it needs.

Do not run all workers with unrestricted Account/Workspace permissions.

---

# 92. Cross-context authorization

A context can ask Governance/another owner for permission facts.

It must not copy the full foreign authorization model and let it drift.

---

# 93. BE-SEC-045 — Permission semantic owner is singular

If Governance owns a permission definition, Work Management/Documents/etc. consume that decision/contract.

Do not redefine the same permission differently per handler.

---

# 94. Entitlement versus authorization

Billing entitlement answers:

```text
is this feature/limit commercially available?
```

Authorization answers:

```text
may this principal perform this resource action?
```

Both may gate one operation.

---

# 95. BE-SEC-046 — Entitlement never grants resource permission by itself

Paid plan:

```text
≠ Workspace admin
≠ Board editor
≠ Billing manager
```

Likewise, resource admin does not necessarily imply paid entitlement.

---

# 96. Ownership semantics

Product “Owner” role can grant policy according to Governance.

Do not overload database row creator/`CreatedBy` as owner authorization unless product semantics explicitly do so.

---

# 97. BE-SEC-047 — Audit creator field is not permission ownership by default

`CreatedBy` records history.

Authorization uses product ownership/membership/policy.

---

# 98. Resource move/re-parenting

Moving a resource between scopes can change authorization.

Treat re-parenting as a security/tenant-sensitive mutation.

---

# 99. BE-SEC-048 — Scope-changing mutation revalidates destination authority

Before moving resource to another Workspace/parent:

```text
authorize source action
authorize destination capability
validate same Account/tenant constraints
update cache/realtime/permission versions
```

as product contract requires.

---

# 100. Tenant transfer

Account/Workspace transfer between tenants is consequential and may be forbidden.

Do not implement by changing one FK if it affects:

```text
RLS
provider connections
billing
audit
search/cache
messages
```

without explicit architecture/migration.

---

# 101. BE-SEC-049 — Tenant identity is not mutable by generic update

If transfer is supported, model a dedicated operation with migration/security proof.

---

# 102. Deletion/revocation

Deletion/archive can affect access.

A deleted/revoked resource should not remain visible because:

```text
cache
search
realtime subscription
old share token
```

still knows it.

---

# 103. BE-SEC-050 — Lifecycle access invalidation propagates to derived security surfaces

On revoke/delete/archive where access changes, update/invalidate:

```text
permission cache
read cache
search visibility
realtime subscriptions
share/public capability
```

according to contract.

---

# 104. Error disclosure

Authorization failures can reveal resource existence if mapped carelessly.

The public API contract decides when to return:

```text
403
404
```

or equivalent concealed error.

Do not invent concealment per endpoint.

---

# 105. BE-SEC-051 — Existence disclosure policy is consistent for a resource class

Equivalent protected resources SHOULD have consistent denied/not-found semantics.

Do not create an enumeration side channel by endpoint inconsistency.

---

# 106. Security logging

Log enough to investigate:

```text
failed auth class
authorization deny category
tenant-scope mismatch
RLS/security failure
rate-limit event
webhook validation failure
```

without logging secrets/private payloads.

---

# 107. BE-SEC-052 — Security log cannot become authorization source

Logs/audit are evidence.

They do not decide future permission.

---

# 108. Observability cardinality

Do not use raw:

```text
user ID
resource ID
API key
token
```

as unbounded metric labels.

Use logs/traces for high-cardinality identifiers and bounded categories for metrics.

---

# 109. Security configuration

Critical configuration includes:

```text
JWT/signing
CORS
CSRF
RLS
provider secrets
trusted proxies
rate-limit failure modes
encryption/key material
```

Validate/fail safe.

---

# 110. BE-SEC-053 — Production security config has no silent permissive fallback

Do not use development defaults in production for:

```text
JWT secret
allow-all origin
disabled CSRF
disabled RLS
unrestricted system context
```

unless an explicitly approved environment mode requires it safely.

---

# 111. Forwarded headers / proxy trust

Client IP/security decisions behind proxy require trusted forwarding configuration.

Do not trust arbitrary forwarded headers from the public internet.

This matters to:

```text
rate limiting
audit
secure scheme/origin
```

---

# 112. BE-SEC-054 — Network-derived identity is trusted only through configured proxy boundary

Do not use raw `X-Forwarded-For` as authenticated source IP without trusted proxy handling.

---

# 113. File/storage security

Protected object access requires Application authorization.

Object key/signed URL is transport capability, not generic public access.

Signed URL must be scoped/time-bounded as design requires.

---

# 114. BE-SEC-055 — Storage key/path is not tenant authorization

Knowing object storage path MUST NOT bypass resource authorization.

---

# 115. Search/cache/realtime derived security

All derived systems must have an invalidation/reconciliation story when source authorization changes.

They must not become stale security authorities.

---

# 116. BE-SEC-056 — Authorization authority remains server-source state

Derived systems can accelerate decisions only while their version/freshness contract is valid.

---

# 117. Multi-tenant tests

Tenant-sensitive tests should use at least:

```text
tenant A
tenant B
```

where practical.

Prove:

```text
A accesses A
A denied B
```

One-tenant fixture cannot prove isolation.

---

# 118. BE-SEC-057 — Security proof includes a negative path

For material protected behavior, test at least one:

```text
unauthenticated
unauthorized
wrong tenant
revoked permission
invalid share/API key
RLS denial
```

as appropriate.

Happy path alone is insufficient.

---

# 119. Application security tests

Application tests prove:

```text
resource/action permission
scope resolution
gate ordering
revocation/version decisions
result semantics
```

Mock outer mechanisms only when the authorization semantics remain real.

---

# 120. Infrastructure security tests

Infrastructure/PostgreSQL tests prove:

```text
RLS
session lifecycle
pool reuse
secret/protection adapter
provider authentication mapping
cache scoping
```

Use real PostgreSQL where RLS/provider-specific behavior is the property.

---

# 121. API security tests

API tests prove:

```text
authentication host behavior
CSRF
CORS metadata/config behavior
rate limiting
security headers
webhook verification boundary
ProblemDetails safety
```

Do not claim resource authorization solely from API middleware tests.

---

# 122. Platform/background security tests

Platform/Integration tests prove:

```text
tenant context propagation
consumer scope
dedup/security interaction
delayed/replay authority behavior
realtime dispatch scope
```

as applicable.

---

# 123. Architecture security tests

Machine gates SHOULD enforce structurally automatable rules such as:

```text
protected Application request requires authz contract
forbidden handler bypass
Domain purity
tenant-scoped event contract
forbidden outer dependency
endpoint access declaration
```

where reliable.

---

# 124. BE-SEC-058 — Structural security invariant becomes executable when practical

A written `MUST` that can be reliably detected should not depend solely on reviewer memory.

Do not write brittle false-positive-heavy gates for semantic properties that need behavioral tests.

---

# 125. Current executable evidence

Current backend Integration tests include named suites for:

```text
TenantIsolationTests
CrossTenantIsolationTests
RlsRuntimeEnforcementTests
```

and current CI verifies those critical tests executed, alongside production composition and other reliability suites.

This is strong evidence that tenant/RLS proof is a required foundation property.

---

# 126. BE-SEC-059 — Security critical gate must do non-zero work

A security filter/suite that selects zero intended tests is not green proof.

CI/test tooling must detect missing critical execution.

---

# 127. Security change classification

Typical:

```text
authentication/session
→ C6

resource authorization
→ C6

RLS/schema
→ C6 + C4

API key/share token
→ C6 (+ C3 if public contract)

provider webhook trust
→ C6/C7

security config
→ C6/C7

privileged destructive operation
→ C6/C8
```

Modifiers can include:

```text
CROSS_TENANT
PROVIDER_EXTERNAL
MOBILE_LAG
ROLLBACK_UNSAFE
```

---

# 128. Security ADR trigger

ADR is appropriate for durable foundation decisions such as:

```text
RLS/session model
authentication transport
CSRF architecture
rate limiting architecture
permission model foundation
service-principal model
secret protection architecture
```

Routine permission addition following the existing model normally does not need a new ADR.

---

# 129. Security review checklist

```text
[ ] principal class
[ ] Account/Workspace/resource scope
[ ] action/permission
[ ] authoritative scope resolution
[ ] Application authorization
[ ] Domain invariant
[ ] RLS
[ ] cache invalidation/version
[ ] realtime/background scope
[ ] share/API-key/provider trust
[ ] secret handling
[ ] audit
[ ] negative tests
[ ] critical CI gate
```

---

# 130. Tenant review checklist

```text
[ ] source tenant owner
[ ] Account/Workspace relationship
[ ] route/body scope not trusted
[ ] DB/RLS context
[ ] pooled connection isolation
[ ] cache key
[ ] message scope
[ ] background worker
[ ] search/realtime
[ ] cross-tenant negative proof
```

---

# 131. Permission review checklist

```text
[ ] stable action/resource semantics
[ ] role only as input
[ ] current principal
[ ] resource scope
[ ] permission owner
[ ] decision failure mode
[ ] revocation
[ ] version/cache invalidation
[ ] denied/no-effect proof
```

---

# 132. Credential review checklist

```text
[ ] credential type
[ ] verification
[ ] storage/protection
[ ] scope
[ ] expiry
[ ] revocation
[ ] replay
[ ] rate limit
[ ] log redaction
[ ] provider/secret boundary
```

---

# 133. Stop conditions

Stop implementation if:

- authenticated identity is being treated as resource permission;
- client tenant/resource relationship is being trusted;
- role strings are becoming the only authorization logic;
- a protected handler bypasses the canonical Application authorization path;
- RLS is being disabled to make a query work;
- pooled connection tenant context can leak;
- system context is proposed as ordinary fallback;
- cache/search/realtime can reveal foreign tenant data;
- delayed/background work has no explicit authority semantics;
- share/API-key capability scope is broad/undefined;
- webhook mutation occurs before authenticity/replay verification;
- secret/token would enter logs/events/client contract;
- security failure is being made fail-open for availability without approved policy.

---

# 134. Executable evidence

Primary source areas:

```text
backend/src/Notrelix.Application/Common/Security
backend/src/Notrelix.Application/Common/Context
backend/src/Notrelix.Application/Common/Tenancy
backend/src/Notrelix.Infrastructure/Data/Rls
backend/src/Notrelix.Infrastructure/Auth
backend/src/Notrelix.API/Security
backend/src/Notrelix.API/Middleware
```

Primary tests:

```text
backend/tests/Notrelix.Application.Tests
backend/tests/Notrelix.Infrastructure.Tests
backend/tests/Notrelix.API.Tests
backend/tests/Notrelix.Integration.Tests
backend/tests/Notrelix.Architecture.Tests
```

---

# 135. Related ADRs

```text
../decisions/ADR-002-rls-bootstrap-connection-lifecycle.md
../decisions/ADR-003-csrf-protection.md
../decisions/ADR-004-rate-limiting-architecture.md
```

Accepted ADR history must be superseded—not silently rewritten—if these foundations change.

---

# 136. Related canonical owners

Backend:

```text
application-model.md
infrastructure-and-data.md
platform-and-messaging.md
api-and-contracts.md
testing-and-quality-gates.md
```

Repository:

```text
../../../docs/product/contexts/governance.md
../../../docs/product/contexts/identity.md
../../../docs/product/contexts/workspaces.md
../../../docs/quality/security-quality-standard.md
../../../docs/architecture/data-ownership-and-consistency.md
```

---

# 137. Non-responsibilities

This document does not define:

```text
product-specific permission catalog
UI button visibility/copy
exact production secret provider
numeric rate-limit values
organization incident escalation tree
legal retention schedule
frontend component implementation
```

Those belong to narrower owners.

---

# 138. Final security rule

A protected backend operation is correct when:

```text
credential
→ authenticated principal
→ authoritative tenant/resource scope
→ Application resource/action decision
→ Domain invariant
→ tenant-safe persistence/RLS
→ committed effect
→ tenant-safe cache/realtime/background propagation
```

and when revocation/failure preserves:

```text
no cross-tenant observation
no unauthorized mutation
no stale cached allow
no tenantless consumer
no secret leakage
no fail-open security shortcut
```

The goal is not to stack security features mechanically.

The goal is to ensure that **every path to protected data or effects has one explicit principal, one explicit resource scope, one authoritative authorization decision, and independent persistence/runtime defenses capable of catching mistakes elsewhere**.
