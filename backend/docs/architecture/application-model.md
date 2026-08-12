---
document_id: BE-APPLICATION-MODEL
document_type: architecture
status: active
owner: backend-architecture
applies_to:
  - backend/src/Notrelix.Application
  - backend/tests/Notrelix.Application.Tests
  - backend/tests/Notrelix.Integration.Tests
evidence:
  - backend/src/Notrelix.Application/Notrelix.Application.csproj
  - backend/src/Notrelix.Application/Common/
  - backend/src/Notrelix.Application/Features/
  - backend/docs/decisions/ADR-001-pipeline-boundary.md
  - backend/tests/Notrelix.Application.Tests/
  - backend/tests/Notrelix.Architecture.Tests/
review_on:
  - application-pipeline-change
  - request-marker-change
  - authorization-boundary-change
  - transaction-boundary-change
  - idempotency-boundary-change
  - post-commit-boundary-change
  - application-persistence-exception-change
---

# Application Model

> **Application owns the complete server-side use-case boundary: intent, orchestration, authorization, tenant/resource scope, transaction policy, external facts, concurrency/idempotency coordination, and the transition from committed local state to post-commit work.**
>
> Application does not own provider mechanics or persistence implementation. Its current EF Core package reference is an approved exception, not a precedent for new handler-local persistence.

This document is the canonical backend owner for Application architecture.

Product semantics remain owned by repository product-context documents.

Domain modeling remains owned by `domain-modeling.md`.

Persistence/provider implementation remains owned by `infrastructure-and-data.md`.

---

# 1. Application purpose

Application translates an authenticated/requested intent into a complete server-side use case.

Conceptually:

```text
request intent
        ↓
classify request contract
        ↓
validate request
        ↓
resolve tenant/resource facts
        ↓
authorize
        ↓
load aggregate/external facts
        ↓
invoke Domain behavior
        ↓
persist within transaction
        ↓
enroll durable post-commit work
        ↓
commit
        ↓
post-commit/cache/result
```

The handler is one participant in this boundary.

The **Application pipeline + handler + ports** collectively own the use case.

---

# 2. BE-APP-001 — Application owns orchestration, not business truth

Application decides:

```text
which use case is being executed
which facts must be loaded
which policy/resource must be checked
which transaction is required
which Domain operation is invoked
which result/error leaves the use case
```

Domain still owns its local business invariants.

Do not duplicate Domain rules in handlers.

---

# 3. Commands and queries

Use clear use-case semantics:

```text
Command
→ requests a state-changing business operation

Query
→ requests an authorized read result
```

Do not classify by HTTP method alone.

A POST endpoint may execute a query-like operation and a background message may execute a command-like use case.

The Application contract is semantic.

---

# 4. BE-APP-002 — Request type expresses intent

Prefer:

```text
TransferWorkspaceOwnershipCommand
MoveBoardItemCommand
GetBoardQuery
ResolveResourcePermissionQuery
```

over generic:

```text
UpdateEntityCommand
ExecuteRequest
GetDataQuery
```

when the specific use case carries policy/lifecycle meaning.

---

# 5. Current feature placement

Current target placement is module-first:

```text
Features/{BoundedContext}/{Module}/Commands/{UseCase}/
Features/{BoundedContext}/{Module}/Queries/{UseCase}/
```

Optional module-local support:

```text
DTOs/
ReadModels/
Mapping/
Permissions/
Cache/
Services/
```

This layout communicates semantic ownership and use-case intent.

---

# 6. BE-APP-003 — New use cases follow the canonical module-first placement

Do not add new use cases to legacy:

```text
Features/{Context}/Commands/{Module}/{UseCase}
Features/{Context}/Queries/{Module}/{UseCase}
```

solely because neighboring code still exists there.

Legacy placement is migration/source debt, not a target convention.

---

# 7. Folder does not create ownership

Current Application also contains supporting folders such as:

```text
Notifications
Operations
Search
```

Source placement does not automatically define a new business bounded context.

Use the repository context map to determine semantic ownership first.

---

# 8. Application Common

Current `Common/` contains cross-cutting Application contracts/mechanisms such as:

```text
Behaviors
Caching
Context
Data
Email
Entitlements
Events
Idempotency
Integrations
Messaging
Models
PostCommit
RateLimiting
Requests
Security
Storage
SystemOperations
Tenancy
Time
Tokens
```

These are Application-level mechanisms/ports.

They are not permission to move feature-specific product semantics into `Common`.

---

# 9. BE-APP-004 — Application Common contains cross-cutting use-case mechanics only

A type belongs in Common when:

```text
multiple features need the same Application contract/mechanism
the semantics are not owned by one product context
the type remains provider-independent
```

Do not move one context's service/DTO/policy into Common to shorten imports.

---

# 10. Request marker model

Current Application uses request contracts/markers to declare cross-cutting requirements.

Current `Common/Requests` includes areas for:

```text
Caching
Execution
Gates
Realtime
Scoping
Security
Transactions
```

plus command/query base contracts.

Markers are architecture declarations.

They allow the pipeline to apply policy consistently.

---

# 11. BE-APP-005 — Cross-cutting behavior is declared, not reimplemented ad hoc

If a request needs:

```text
transaction
authorization
tenant/resource scope
feature gate
idempotency
cache policy
realtime/post-commit behavior
```

use the approved request contract/marker where that mechanism is pipeline-owned.

Do not copy equivalent code into every handler.

---

# 12. Marker quality

A marker should answer a stable cross-cutting question.

Good:

```text
this request is transactional
this request requires a resource permission
this request is idempotent
this request has authorized cache semantics
```

Weak:

```text
this request needs helper X
```

which simply couples the marker to implementation detail.

---

# 13. BE-APP-006 — Marker contract does not hide feature semantics

A marker may declare:

```text
permission action
resource kind/scope
```

but feature-owned business rules still remain explicit in the feature/domain.

Do not create a generic marker that silently encodes an entire feature workflow.

---

# 14. Pipeline architecture

ADR-001 defines pipeline boundary zones so behavior order is not accidental.

Conceptual zones:

```text
OUTER / pre-DB
        ↓
POST-COMMIT SCOPE BOUNDARY
        ↓
INNER / DB request + transaction
        ↓
POST-COMMIT
        ↓
CACHE / final response cache
```

The ADR describes six pipeline zones/boundaries in the current implementation.

The durable architecture is the **dependency/order semantics**, not the exact number of behavior classes forever.

---

# 15. Current behavior evidence

Current `Common/Behaviors` contains behavior types such as:

```text
ApplicationTracingBehavior
AuthorizationBehavior
AuthorizedCacheBehavior
ConcurrencyBehavior
DbRequestScopeBehavior
ExceptionMappingBehavior
FeatureGateBehavior
IdempotencyBehavior
PostCommitEnqueueBehavior
PostCommitScopeBehavior
PublicCacheBehavior
RequestContractGuardBehavior
ResourceScopeBehavior
SubscriptionGateBehavior
SystemOperationAuditBehavior
TenantBootstrapBehavior
TokenValidationBehavior
ValidationBehavior
VerifiedEmailBehavior
```

This is current source evidence.

Do not turn this list into an immutable architecture contract.

---

# 16. BE-APP-007 — Pipeline order follows dependency, not aesthetics

A behavior may run after another only when its required state is available.

Examples:

```text
resource authorization
requires resource scope

RLS full session
requires resolved tenant/resource context

cache write
requires successful commit

post-commit dispatch/enqueue
requires successful local transaction
```

Do not reorder to “group similar code” if dependency semantics change.

---

# 17. Outer zone

The pre-DB/outer portion should contain work that can be safely performed before the transactional DB scope.

Representative responsibilities:

```text
exception mapping boundary
tracing
request validation
request-contract guards
token/security preconditions
tenant bootstrap
system-operation auditing setup
resource scope resolution
```

Exact placement follows ADR/tests.

---

# 18. BE-APP-008 — Outer behavior cannot depend on inner transactional state

Do not make an outer behavior require:

```text
transaction-local RLS context
handler-produced result
post-commit action
inner cache mutation
```

If it needs such state, the zone is wrong or the design is wrong.

---

# 19. Post-commit scope boundary

Current pipeline creates a scope for deferred actions before the DB transaction.

This allows handler/domain/application code to enroll work while postponing actual post-commit execution.

---

# 20. BE-APP-009 — Post-commit scope records intent; it does not execute side effects early

Before commit:

```text
collect/enroll intended post-commit work
```

After successful commit:

```text
enqueue/execute approved post-commit mechanism
```

Do not perform irreversible provider/broker effects merely because a post-commit scope object exists.

---

# 21. Database request scope

`DbRequestScopeBehavior` is the architectural transition into the request DB connection/transaction boundary.

It participates in:

```text
connection lifecycle
full RLS session application
transaction
```

with Infrastructure implementation.

Application owns **when/why** the transactional boundary exists.

Infrastructure owns **how** EF/Npgsql implements it.

---

# 22. BE-APP-010 — Transaction policy is Application-owned; transaction technology is Infrastructure-owned

Application declares:

```text
this use case requires transaction
this local state must commit atomically
```

Infrastructure supplies:

```text
DbContext
Npgsql connection
EF transaction
RLS session implementation
```

Do not let a handler choose provider-specific transaction mechanics.

---

# 23. Inner transactional zone

Representative concerns inside the DB transaction include:

```text
authorization that requires resolved/current resource state
verified-email gate
concurrency
subscription/feature gates
idempotency
handler/domain mutation
```

Exact ordering is constrained by dependencies and architecture tests.

---

# 24. BE-APP-011 — Authorization happens before protected business effects

A protected request must not:

```text
mutate aggregate
persist row
emit committed event
call provider
```

before the approved authorization decision is established.

A later denial cannot undo already-escaped effects safely.

---

# 25. Authorization model

Application authorization should be resource/action oriented.

Conceptually:

```text
principal
+
resource reference
+
action/permission
+
tenant scope
+
current policy facts
→ decision
```

Avoid scattered:

```text
if user.Role == "Admin"
```

inside handlers.

---

# 26. BE-APP-012 — Authentication identity is not authorization

Knowing:

```text
UserId
```

does not prove:

```text
Workspace membership
resource permission
ownership
entitlement
```

Application resolves the required resource/policy facts.

---

# 27. Tenant bootstrap

Some use cases need tenant/resource resolution before full RLS context can be established.

ADR-002 documents the current bootstrap connection lifecycle.

Application owns:

```text
need for tenant/resource bootstrap
ordering relative to authorization/transaction
```

Infrastructure owns the Npgsql/RLS mechanics.

---

# 28. BE-APP-013 — Client tenant/resource identifiers are inputs, not authority

A route/body may contain:

```text
AccountId
WorkspaceId
BoardId
PageId
```

Application resolves authoritative relationships and permissions.

Do not accept:

```text
client says this resource belongs to Workspace X
```

as proof.

---

# 29. Resource scope

Resource scope behavior may resolve facts such as:

```text
resource kind
resource ID
owning Account/Workspace
policy target
```

before authorization.

Scope resolution should be minimal and explicit.

Do not load an entire aggregate graph merely to determine a policy target if a narrow read port suffices.

---

# 30. BE-APP-014 — Resource resolution port is a read contract, not foreign mutation capability

A resource-scope resolver may read ownership facts.

It must not become a generic API for mutating foreign context tables.

---

# 31. Validation

Application validation owns request/use-case shape and preconditions that do not require Domain mutation.

Examples:

```text
required field
format/range
mutually exclusive options
request-level consistency
```

Domain still validates owned business invariants.

---

# 32. BE-APP-015 — Application validation does not replace Domain invariant

Validation can fail early.

It must not become the only enforcement of a Domain-owned rule if another use-case path can call the Domain behavior.

---

# 33. External facts

Application is responsible for obtaining facts Domain needs but does not own.

Examples:

```text
actor
current time
parent path
current count
entitlement
provider status
cross-aggregate existence
generated secret/token material
```

Then Application supplies immutable facts to Domain.

---

# 34. BE-APP-016 — Application external-fact query is explicit in the use case

Do not hide external fact discovery in:

```text
Domain service with repository
static global service
lazy navigation
ambient singleton
```

The handler/orchestration should make the dependency visible.

---

# 35. Time

Current Application Common contains time abstraction.

Application supplies timestamps to Domain/events as required.

Do not call ambient clock in Domain.

Use the current time abstraction to make use cases testable/deterministic.

---

# 36. BE-APP-017 — Time is sampled deliberately

If one operation needs one logical business timestamp, sample it once and reuse it where consistency requires.

Do not generate subtly different timestamps across Domain mutation/event/audit solely because several calls to the clock were easy.

---

# 37. Commands

A command handler should generally:

```text
load required facts/state
invoke business behavior
persist through port/unit of work
return semantic result
```

Cross-cutting concerns should remain in the pipeline when architecture owns them.

---

# 38. BE-APP-018 — Handler remains orchestration-focused

Stop if a handler becomes a large block containing:

```text
authorization
SQL/provider calls
business-state mutation by setter
cache protocol
broker protocol
HTTP concerns
```

Refactor to the correct layer/boundary.

---

# 39. Queries

Queries may use focused authorized read models.

Not every query needs to materialize aggregates.

For read-only use cases:

```text
Application query
→ authorized/scoped read port
→ Infrastructure optimized query/projection
→ result DTO/read model
```

is acceptable.

---

# 40. BE-APP-019 — Read optimization does not bypass authorization or source ownership

A direct projection query still enforces:

```text
tenant
resource visibility
permission
```

according to the use case.

Read-model convenience is not a security bypass.

---

# 41. Application ports

Application defines interfaces/contracts for outer mechanisms it needs.

Good port shape:

```text
capability needed by the use case
```

Examples conceptually:

```text
read resource scope
load/save aggregate
send email
store file
resolve entitlement
enqueue durable integration work
```

Do not mirror provider SDKs.

---

# 42. BE-APP-020 — Port is use-case language, not provider language

Prefer:

```text
IEmailSender.SendAsync(...)
```

over:

```text
IResendClientProxy.SendResendRequest(...)
```

unless provider-specific behavior itself is a deliberate integration contract owned at that boundary.

---

# 43. Persistence ports

Application may define persistence abstractions.

They should generally align with:

```text
aggregate/use-case reads
transaction
resource version
read projection
```

not one generic repository for every database entity.

---

# 44. BE-APP-021 — Repository/port exists because the use case needs a persistence capability

Do not create:

```text
IRepository<T>
```

for every entity as default architecture.

Aggregate roots and query/read models have different persistence needs.

---

# 45. Current EF Core exception

Current Application project directly references:

```text
Microsoft.EntityFrameworkCore
```

under:

```text
EX-BE-APP-EF-001
```

This exception exists because current compatibility/abstractions still require EF types.

It does not transfer:

```text
DbContext ownership
mapping ownership
migration ownership
SQL ownership
```

into Application.

---

# 46. BE-APP-022 — New handler-local DbContext usage is forbidden without a new governed exception/decision

Do not copy legacy/direct EF usage into new use cases simply because the package is already present.

Correct direction:

```text
Application port
        ▲
        │ implementation
Infrastructure
```

---

# 47. Removing the EF exception

When current Application contracts no longer require EF types:

```text
remove EF type dependency
remove package reference
remove exception
update architecture tests/docs
```

Do not preserve the package “just in case”.

Exception removal is architectural cleanup.

---

# 48. Transaction boundary

A transactional command should cover:

```text
all local authoritative writes
concurrency checks
outbox/post-commit enrollment required for reliable delivery
idempotency state required for the operation
```

according to architecture.

---

# 49. BE-APP-023 — Required local state and durable enrollment commit atomically

Do not commit:

```text
aggregate state
```

then best-effort create:

```text
outbox/idempotency/durable action record
```

if loss of the latter makes the source commit semantically incomplete.

---

# 50. External provider effects

Provider effects generally occur outside the local DB transaction.

Application decides the semantic state:

```text
pending
committed local request
unknown external outcome
completed
failed
```

as product design requires.

Infrastructure/provider adapter executes protocol.

---

# 51. BE-APP-024 — Distributed transaction is not simulated by holding DB transaction around provider call

Do not hold DB locks while waiting on an external provider merely to pretend:

```text
DB + provider = atomic
```

Use durable orchestration/idempotency/reconciliation.

---

# 52. Concurrency

Expected-version semantics belong to the use case.

The pipeline/handler coordinates:

```text
expected version
current version
conflict result
```

Infrastructure maps database concurrency.

Domain owns state/version mutation semantics.

---

# 53. BE-APP-025 — Conflict is returned before committed stale overwrite

A stale request must not silently:

```text
reload latest
apply stale mutation
overwrite
```

unless the product operation explicitly defines merge/retry semantics.

Return a stable conflict result.

---

# 54. Concurrency result

Conflict is distinct from:

```text
validation
authorization
not found
provider failure
```

Do not map every persistence concurrency exception to generic internal error.

Infrastructure translates technical exception; Application owns semantic conflict result.

---

# 55. Idempotency

Idempotency coordinates retryable logical operations.

Current Application contains idempotency contracts/behavior.

The logical key should identify:

```text
one operation
for one intended semantic request
within one scope
```

---

# 56. BE-APP-026 — Idempotency identity is stable across retry

A retry must not generate a new semantic operation key.

Prove:

```text
same key + same request
same key + conflicting request
concurrent duplicate
retry after transient failure
```

as required.

---

# 57. Idempotency and transaction

Where the operation's idempotency record is part of correctness, its state transition must align with transaction/post-commit boundaries.

Do not mark:

```text
Succeeded
```

before the source transaction commits.

---

# 58. BE-APP-027 — Idempotency success follows authoritative success

A request may be:

```text
started
committed
post-commit pending
completed
```

depending on architecture.

Do not collapse these states if retry semantics depend on them.

---

# 59. Feature/subscription gates

Application may enforce:

```text
entitlement
subscription state
feature availability
quota
```

through declared gates.

Remember:

```text
entitlement
≠ authorization
```

A user may be entitled but unauthorized on a resource.

A user may be authorized but the plan does not include the feature.

---

# 60. BE-APP-028 — Commercial gate and resource permission are separate decisions

Do not implement:

```text
paid plan → allow resource mutation
```

without the normal resource authorization.

Do not implement:

```text
Workspace Admin → bypass Billing entitlement
```

unless product Billing semantics explicitly permit it.

---

# 61. Token/verified-email gates

Security/account-state gates can apply before selected use cases.

They should be declared/centralized when cross-cutting.

Do not scatter:

```text
if !emailVerified
```

across handlers if the same policy is pipeline-owned.

---

# 62. BE-APP-029 — Gate failure occurs before protected mutation

Feature, token, verified-email, subscription, authorization, or rate-limit gate should reject before the protected state/provider effect.

Do not compensate for easily preventable precondition failure after mutation.

---

# 63. System operations

Privileged/system operations may use explicit request markers/contracts.

They do not get implicit global trust.

A system operation must state:

```text
who/what principal
scope
audit
allowed bypass
data access
```

according to security architecture.

---

# 64. BE-APP-030 — System operation is explicit capability, not `if IsSystem then bypass all`

Privileged mechanisms must remain bounded and auditable.

Do not use a system flag as a universal architecture escape hatch.

---

# 65. Rate limiting

Application may participate in semantic rate-limit markers/gates.

Provider/gateway implementation may live elsewhere.

Rate limiting must not redefine business authorization.

Related accepted architecture history:

```text
ADR-004
```

---

# 66. Caching model

Application owns **when a use-case result can be cached** and the semantic scope/invalidation contract.

Infrastructure owns Redis/cache implementation.

Current pipeline distinguishes public and authorized cache behavior.

---

# 67. BE-APP-031 — Cacheability is a use-case contract

Before caching, define:

```text
scope
tenant/resource/user dimension
permission sensitivity
freshness
invalidating mutations
failure fallback
```

Do not cache because the query is expensive without a correctness contract.

---

# 68. Public cache

Public cache applies only to data that is genuinely safe to share under its cache scope.

Do not classify a response public merely because it does not contain obvious user IDs.

Authorization/tenant/private state may still be embedded.

---

# 69. Authorized cache

Permission-sensitive cache must include enough identity/version/scope to prevent stale cross-principal access.

On permission/membership/resource visibility changes, invalidate or version accordingly.

---

# 70. BE-APP-032 — Cache read/write cannot run ahead of authorization/transaction guarantees

Do not:

```text
serve authorized cache before determining its authorization scope
write successful response cache before source transaction commits
```

Pipeline zones protect these boundaries.

---

# 71. Post-commit work

Post-commit actions may include:

```text
integration-event enrollment/delivery
realtime notification
cache invalidation
notification dispatch
activity/audit propagation
idempotency completion side effects
```

depending on ownership.

Not every effect must be synchronous.

---

# 72. BE-APP-033 — Post-commit work is classified by durability need

For each effect decide:

```text
must survive process crash?
can retry?
can duplicate?
ordering?
provider side effect?
tenant scope?
```

Then use the approved Platform/Infrastructure mechanism.

Do not put all after-commit callbacks into one best-effort in-memory list.

---

# 73. Event mapping

Current Application contains:

```text
EventMappers/
Events/
```

Application can map owned committed Domain facts into outward integration contracts.

The mapper should not mutate source state.

---

# 74. BE-APP-034 — Integration event mapping preserves semantic owner and stable identity

Do not expose:

```text
Domain CLR class full name
private property layout
provider DTO
```

as public event identity by accident.

Use the repository event/contract architecture.

---

# 75. Cross-context commands

One context may request another context perform a use case.

Prefer explicit Application contract/orchestration.

Do not let a handler directly save another context's aggregate/repository as an internal shortcut.

---

# 76. BE-APP-035 — Cross-context write routes to the target owner

If Automation wants to update a Board Item:

```text
Automation
→ requests Work Management operation
→ Work Management authorizes/enforces invariant
```

Automation must not mutate Work Management persistence directly.

---

# 77. Cross-context reads

A feature may need foreign facts synchronously.

Use:

```text
narrow read port
owner query contract
projection
```

based on freshness/performance.

Do not expose a foreign DbContext/DbSet.

---

# 78. BE-APP-036 — Cross-context read contract is minimal

Request only the fact needed:

```text
Workspace owner count
resource tenant scope
entitlement decision
provider connection status
```

rather than returning the whole foreign aggregate.

This reduces coupling and extraction cost.

---

# 79. Result model

Application results should communicate stable use-case outcomes.

Common categories can include:

```text
success
validation
not found
authorization denied
conflict
not entitled
rate limited
dependency/provider failure
```

Exact result abstractions belong to current source.

---

# 80. BE-APP-037 — Application result does not expose Infrastructure exception

Do not return:

```text
DbUpdateException
NpgsqlException
RedisConnectionException
provider SDK exception
```

as the semantic result.

Infrastructure/outer mapping translates to stable categories.

---

# 81. Error mapping

Current pipeline includes exception mapping behavior.

Its job is to convert known Application/Domain/technical failures into stable Application results at the correct boundary.

It must not hide unknown programming defects as user-correctable business errors.

---

# 82. BE-APP-038 — Exception mapping is explicit and loss-aware

Preserve:

```text
semantic category
correlation
safe diagnostic detail
```

without leaking secrets/provider internals.

Do not catch every exception and return generic success/failure that destroys observability.

---

# 83. Cancellation

Application operations should propagate cancellation to:

```text
queries
DB
provider calls
background waits
```

where supported.

Do not treat client cancellation as authorization to roll back already-committed external facts.

Outcome semantics still matter.

---

# 84. BE-APP-039 — Cancellation respects transaction/external-effect state

Before commit:

```text
cancellation may abort
```

After local commit/provider unknown outcome:

```text
cancellation cannot pretend nothing happened
```

Use reconciliation/post-commit semantics.

---

# 85. Background execution

An Application use case may run from HTTP, worker, scheduler, or system operation.

It must not rely on HTTP-specific ambient state.

Required context must be supplied through Application contracts.

---

# 86. BE-APP-040 — Application use case is host-independent

Do not read:

```text
HttpContext
controller
route data
```

inside ordinary Application feature code.

API maps host context into the Application request/context.

---

# 87. Current-user/context abstractions

Application Common contains request/current context abstractions.

Use them only for cross-cutting orchestration facts intended by architecture.

Do not hide all feature inputs in an ambient current context.

A command should still express the resource/intent it operates on.

---

# 88. BE-APP-041 — Ambient request context supplements, not replaces, explicit use-case input

Good:

```text
command contains target ItemId
current context provides authenticated principal/correlation
```

Bad:

```text
handler guesses target Workspace from whichever ambient context happens to be set
```

when the operation requires an explicit target.

---

# 89. Audit/activity

Application may coordinate:

```text
security/system audit
user-visible activity
business event/history
```

through separate contracts.

Do not merge them because all involve “something happened”.

Each has different owner/retention/audience.

---

# 90. BE-APP-042 — Audit, Activity, Notification, Domain Event are distinct outputs

One use case may produce several.

Do not use one generic event stream/table to satisfy all semantics unless a deliberate architecture says so.

---

# 91. Realtime

Application may declare realtime consequences after committed state.

Realtime does not become the source transaction.

The client must reconcile authoritative state on gaps/reconnect as repository architecture defines.

---

# 92. BE-APP-043 — Realtime notification follows authoritative state

Do not broadcast:

```text
ItemUpdated
```

before the authoritative mutation commits.

If a post-commit broadcast fails, source state remains truth and recovery/invalidation handles convergence.

---

# 93. Application tests

Primary proof:

```text
backend/tests/Notrelix.Application.Tests
```

Use Application tests for:

```text
handler orchestration
markers/pipeline
authorization decisions
validation
result semantics
concurrency/idempotency orchestration
cache/post-commit contract
```

Use Integration tests where real DB/RLS/host semantics matter.

---

# 94. BE-APP-044 — Mock only the outer mechanism when testing Application semantics

Do not mock:

```text
the business outcome itself
```

then assert the handler forwarded it.

A test should exercise the actual orchestration/property.

---

# 95. Pipeline tests

Critical pipeline order should be machine-enforced.

ADR-001 references runtime/architecture order tests.

When adding a behavior:

```text
declare correct zone
update composition
update architecture/runtime order proof
```

Do not rely on registration order being obvious to reviewers.

---

# 96. BE-APP-045 — New pipeline behavior states its prerequisite and produced state

Before adding, answer:

```text
what must already be true?
what does this behavior establish?
which later behaviors depend on it?
what happens on failure?
```

If no cross-cutting use case exists, a handler/service may be better than another global behavior.

---

# 97. Pipeline growth control

A pipeline with many behaviors can become invisible complexity.

Do not add a new behavior for one feature-specific condition.

Use pipeline only when the rule is genuinely cross-cutting and declarative.

---

# 98. BE-APP-046 — Cross-cutting frequency alone is not enough; ordering semantics must be clear

A behavior that reads/mutates state should have a stable zone.

Avoid hidden dependencies through ambient mutable context.

---

# 99. Application architecture tests

Good executable checks can enforce:

```text
no Infrastructure/API reference
handler placement
pipeline order/zones
authorization markers for protected commands
request contract invariants
forbidden direct persistence patterns where detectable
```

Do not encode product-specific rule logic into generic architecture tests.

---

# 100. Current project dependency

Current Application project references only:

```text
Notrelix.Domain
```

as a production project.

Preserve this compile-time closure.

Do not add direct reference to:

```text
Notrelix.Infrastructure
Notrelix.Platform
Notrelix.API
```

for convenience.

---

# 101. BE-APP-047 — Outer implementation is injected through inward contract

If Application needs a capability implemented by Infrastructure/Platform, define/use an inward contract.

Composition in API/outer DI binds the implementation.

Do not reverse the project dependency.

---

# 102. Infrastructure versus Platform port

Use Infrastructure for mechanisms such as:

```text
persistence
provider client
storage
cache adapter
```

Use Platform for reusable delivery/runtime mechanisms such as:

```text
post-commit dispatcher
consumer mechanics
ordering
retry/poison
```

Application can declare the capability contract without knowing the concrete project.

---

# 103. Application does not own OpenAPI

Application request/result may influence public contract.

API owns HTTP/OpenAPI shape/versioning.

Do not annotate Application command DTO purely to satisfy a transport serialization concern when a transport mapping is more appropriate.

---

# 104. BE-APP-048 — One Application use case may have multiple transports

The same command/query semantics may be invoked from:

```text
HTTP
worker
automation
internal orchestration
```

where product architecture permits.

Keep transport-specific concerns outside the use case.

---

# 105. Application does not own EF schema

Even when current Application contracts refer to EF compatibility types under exception, Application does not decide:

```text
table
column
migration
index
RLS SQL
query filter
converter
```

Infrastructure owns those mechanisms.

---

# 106. Application does not own provider schema

Integration Application contracts express the needed business capability/facts.

Infrastructure adapter maps:

```text
provider-specific request/response/error
```

to the Application contract.

Do not leak SDK DTOs into feature commands/results.

---

# 107. BE-APP-049 — Provider unknown outcome is a semantic state when required

For external writes:

```text
timeout
```

may mean:

```text
unknown whether provider committed
```

Application owns the use-case state/result that represents this uncertainty.

Infrastructure performs reconciliation mechanics.

---

# 108. Security-sensitive cache

Authorization-sensitive read caching requires permission/version invalidation.

Do not persist “allowed” indefinitely.

Application owns when a cached result can be trusted for the current principal/resource scope.

---

# 109. Data consistency classification

For each use case classify:

```text
strong local transaction
optimistic concurrency
eventual cross-context propagation
external pending/unknown outcome
derived read model
```

Do not mix these accidentally.

---

# 110. BE-APP-050 — Consistency model is explicit

If a consumer can observe delayed state, define:

```text
what is authoritative now?
what is pending?
what retries?
what reconciles?
```

Do not make eventual consistency an accidental side effect of background code.

---

# 111. Application read/write separation

CQRS is a useful separation of intent.

It does not require separate databases or microservices.

Queries may use optimized read models.

Commands preserve use-case/business transaction semantics.

---

# 112. BE-APP-051 — Query model does not become write model by convenience

Do not mutate a projection/read DTO to perform business changes.

Route writes through the owning command/use case.

---

# 113. Module-local services

A module may use narrow Application services when orchestration is reused and does not belong in Domain.

Avoid broad:

```text
BoardService
WorkspaceService
CommonService
```

that becomes a catch-all.

Name the capability.

---

# 114. BE-APP-052 — Application service remains use-case focused

If a service accumulates:

```text
persistence
authorization
provider
cache
business rules
```

across unrelated operations, split by responsibility/owner.

Do not reconstruct a classic service layer that bypasses vertical slices.

---

# 115. DTOs/read models

Application DTOs may represent:

```text
use-case result
read projection
cross-layer contract
```

They should not mirror every EF entity.

Use semantic fields required by the consumer/use case.

---

# 116. Mapping

AutoMapper exists in current Application dependencies.

Use mapping only when it improves repetitive shape translation.

Do not hide business transformations or permission logic inside mapping profiles.

---

# 117. BE-APP-053 — Mapping is structural, not business authorization/invariant execution

A mapper can transform:

```text
source fields → result fields
```

It should not decide:

```text
who may see the field
whether operation is allowed
whether lifecycle transition succeeds
```

unless the mapping is explicitly fed an already-decided semantic fact and remains transparent.

---

# 118. Application event mappers

Event mapping can translate Domain fact to integration contract.

Keep:

```text
stable public logical name/version
scope
minimal payload
```

outside Domain transport concerns.

Do not make mapper query mutable state after commit unless the contract explicitly requires a snapshot/query and race semantics are understood.

---

# 119. BE-APP-054 — Event contract captures the intended committed fact deterministically

Prefer mapping from:

```text
Domain event + stable committed facts
```

rather than reloading a changing aggregate later and publishing a different state accidentally.

---

# 120. Request contract guard

Current pipeline includes request contract guarding.

Its purpose is architecture correctness:

```text
required markers/contracts are coherent
forbidden marker combinations fail
```

Do not silently ignore an invalid contract declaration.

---

# 121. BE-APP-055 — Invalid request contract fails before handler execution

A command marked inconsistently should fail development/test/runtime guard rather than degrade to partial policy.

Examples conceptually:

```text
permission marker without resource scope
idempotent marker without operation identity
cache marker without safe scope
```

depending on actual marker contracts.

---

# 122. System-operation audit

Privileged system operations should be observable/auditable.

The Application behavior records the operation semantics/actor/context needed by the audit mechanism.

Do not log secret payloads merely for audit completeness.

---

# 123. Application tracing

Tracing should use semantic use-case/resource identifiers where safe.

Do not create unbounded-cardinality metric labels from arbitrary user input.

Tracing is operational evidence, not product truth.

---

# 124. BE-APP-056 — Observability cannot change use-case outcome

Telemetry failure should not ordinarily turn a valid critical business transaction into failure unless the telemetry/audit record is itself a required durable compliance invariant.

If audit durability is mandatory, model it explicitly rather than relying on best-effort logging.

---

# 125. Feature gates

Release flags and Billing entitlements are distinct.

A feature gate may be:

```text
release exposure
product entitlement
workspace configuration
```

Do not use one generic boolean with ambiguous ownership.

---

# 126. BE-APP-057 — Gate owner is explicit

For every gate know whether authority is:

```text
Delivery/release flag
Billing entitlement
product/context setting
security policy
```

Application orchestrates the check but does not invent the source meaning.

---

# 127. Rate-limit gate

Rate limit can reject before expensive/protected work.

Do not put rate-limit counters in Domain.

Provider/host implementation is outer-layer.

Application may declare semantic operation identity/scope.

---

# 128. Offline/background execution markers

Execution markers can distinguish request constraints.

Do not use an execution marker to bypass authorization/tenant.

Background work still needs equivalent server authority and explicit scope.

---

# 129. BE-APP-058 — Execution mode changes mechanism, not product permission

HTTP/background/offline invocation can have different delivery semantics.

It does not automatically change:

```text
who may act
what invariant applies
what tenant owns the resource
```

---

# 130. Application deletion operations

Application coordinates product lifecycle operations such as:

```text
archive
delete/tombstone
restore
revoke
cancel
```

based on the owning context.

Do not expose generic persistence soft-delete as the use-case contract by default.

---

# 131. BE-APP-059 — Application names lifecycle by product intent

Prefer:

```text
ArchivePageCommand
RevokeShareLinkCommand
CancelSubscriptionCommand
```

over:

```text
SoftDeleteEntityCommand
```

when semantics differ.

---

# 132. Bulk commands

A bulk use case must define:

```text
per-item authorization
transaction scope
partial success
idempotency
limits
background processing
```

Do not wrap thousands of unrelated aggregate mutations in one DB transaction automatically.

---

# 133. BE-APP-060 — Bulk semantics are explicit

Choose one:

```text
all-or-nothing local set
per-item independent result
accepted async batch
```

according to product need.

Do not let loop implementation accidentally decide product semantics.

---

# 134. Pagination

Application queries for unbounded collections define:

```text
cursor/page semantics
filter/sort
tenant scope
stable ordering
limits
```

Infrastructure implements efficient query.

Do not return unbounded collections because the current test dataset is small.

---

# 135. BE-APP-061 — Pagination contract is semantic before query optimization

Stable cursor/sort behavior matters to clients.

Do not change cursor meaning merely to fit a new index without contract review.

---

# 136. Search

Search may be a supporting capability/read projection.

Application queries still enforce scope/authorization.

Search result is derived data.

Writes route to source contexts.

---

# 137. Notifications

Notification use cases consume source facts and user preferences/delivery contracts.

Notification storage/delivery does not become source business truth.

Application keeps source event/resource identity explicit.

---

# 138. Entitlements

Application can ask Billing entitlement contracts.

It should avoid hard-coded plan-name branching:

```text
if Plan == "Pro"
```

when Billing owns feature/limit semantics.

Use stable capability/entitlement contract.

---

# 139. BE-APP-062 — Plan display name is not capability policy

Product/Billing may rename plans or combine offers.

Application should depend on the semantic entitlement/limit, not marketing plan strings.

---

# 140. Application configuration

Application can define typed option-independent contracts if needed.

Runtime option binding/secret/provider configuration belongs to Infrastructure/API composition.

Do not read raw environment variables in feature handlers.

---

# 141. BE-APP-063 — Runtime configuration enters through typed outer contract

If a use case needs a configurable technical policy:

```text
timeout
limit
feature technical threshold
```

use an explicit configuration/port with safe validation.

Do not scatter `Environment.GetEnvironmentVariable`.

---

# 142. Failure atomicity at Application layer

If a command returns:

```text
validation/denied/conflict/failure
```

the local transaction must not commit partial authoritative changes unless the product explicitly models partial success.

---

# 143. BE-APP-064 — Rejection leaves no committed protected partial effect

Test where applicable:

```text
DB unchanged
outbox not enrolled
cache not updated
provider not called
cursor not advanced
```

at the appropriate seam.

---

# 144. Partial success

Some bulk/provider workflows legitimately support partial success.

This must be explicit in the command/result contract.

Do not infer it from catching exceptions per loop iteration.

---

# 145. Application performance

Avoid N+1 cross-context/DB queries inside handlers for high-cardinality operations.

Use focused read ports/projections/batching while preserving semantic owner.

Do not solve performance by moving authorization/business logic into SQL/provider layer exclusively.

---

# 146. BE-APP-065 — Performance optimization preserves use-case contract

Before optimizing, keep:

```text
authorization
tenant scope
result semantics
consistency
failure behavior
```

identical unless a product/architecture change is approved.

---

# 147. Application change classes

Examples:

```text
new handler following existing pattern
→ C1/C2 depending behavior

pipeline marker/order
→ C5 (+ security class if applicable)

authorization semantics
→ C6

idempotency/concurrency semantics
→ C2/C6/C7 depending scope

public result contract
→ C1/C3

data migration contract
→ C4
```

Classify by effect, not layer name.

---

# 148. ADR trigger

A new ADR may be required for:

```text
pipeline zone architecture
new cross-cutting request-marker foundation
transaction model
idempotency foundation
cross-context orchestration model
Application/Infrastructure dependency boundary
```

Routine feature commands do not need ADRs.

---

# 149. Application review checklist

```text
[ ] owning context/module
[ ] clear command/query intent
[ ] request markers correct
[ ] validation
[ ] resource/tenant scope
[ ] authorization
[ ] external facts
[ ] Domain operation
[ ] transaction
[ ] expected version
[ ] idempotency
[ ] cache
[ ] post-commit
[ ] result/error
[ ] cross-context contract
[ ] no provider/DbContext leakage
[ ] tests/gates
```

---

# 150. Pipeline behavior admission checklist

```text
[ ] genuinely cross-cutting
[ ] declared prerequisite
[ ] produced state
[ ] zone
[ ] failure semantics
[ ] order relative to neighbors
[ ] request marker/trigger
[ ] no feature-specific business policy
[ ] architecture/runtime order tests
```

---

# 151. Port review checklist

```text
[ ] use-case capability language
[ ] provider-independent
[ ] narrow
[ ] tenant/resource scope explicit
[ ] failure semantics
[ ] cancellation
[ ] idempotency where needed
[ ] implementation belongs outside Application
```

---

# 152. Query review checklist

```text
[ ] authorized
[ ] scoped
[ ] bounded/paginated
[ ] stable sort/cursor
[ ] minimal read model
[ ] no aggregate loading without behavior need
[ ] cache semantics
[ ] derived/source authority clear
```

---

# 153. Command review checklist

```text
[ ] intent
[ ] preconditions
[ ] authorization
[ ] external facts
[ ] concurrency
[ ] idempotency
[ ] transaction
[ ] Domain mutation
[ ] durable enrollment
[ ] provider side effects outside local transaction
[ ] result
[ ] no partial commit
```

---

# 154. Stop conditions

Stop Application implementation if:

- semantic owner is unresolved;
- request needs foreign-table mutation;
- handler needs new direct `DbContext` usage without approved exception;
- authorization is being implemented as role-string checks inside handler;
- pipeline order prerequisite is unclear;
- a global behavior is being added for one feature-only rule;
- idempotency has no logical operation identity;
- transaction excludes required durable outbox/enrollment;
- provider call is being placed inside DB transaction to simulate atomicity;
- cache key/invalidation lacks tenant/permission scope;
- background execution assumes HTTP ambient state;
- result contract exposes Infrastructure/provider exception;
- cross-context read/write contract is broader than needed;
- public compatibility consumers are unknown.

---

# 155. Executable evidence

Primary source:

```text
backend/src/Notrelix.Application
backend/src/Notrelix.Application/Common/Behaviors
backend/src/Notrelix.Application/Common/Requests
backend/src/Notrelix.Application/Features
```

Primary tests:

```text
backend/tests/Notrelix.Application.Tests
backend/tests/Notrelix.Integration.Tests
backend/tests/Notrelix.Architecture.Tests
```

Focused:

```bash
cd backend
dotnet test tests/Notrelix.Application.Tests/Notrelix.Application.Tests.csproj
```

Pipeline/authorization changes also require architecture/integration proof as classified.

---

# 156. Related architecture

```text
backend-overview.md
domain-modeling.md
infrastructure-and-data.md
platform-and-messaging.md
api-and-contracts.md
security-tenancy-authorization.md
testing-and-quality-gates.md
```

Related ADRs:

```text
../decisions/ADR-001-pipeline-boundary.md
../decisions/ADR-002-rls-bootstrap-connection-lifecycle.md
```

---

# 157. Non-responsibilities

Application does not own:

```text
specific Product semantics
EF mapping/schema/migrations
RLS SQL/provider connection mechanics
Redis implementation
provider SDK
broker implementation
HTTP route/OpenAPI representation
frontend state
deployment/runtime topology
```

It owns the use-case contract that those layers serve.

---

# 158. Final Application rule

A healthy Application use case can be explained as:

```text
explicit intent
+
authenticated principal
+
resolved tenant/resource
+
authorized action
+
required external facts
        ↓
pipeline-owned cross-cutting policy
        ↓
focused handler
        ↓
Domain behavior
        ↓
local transaction + durable enrollment
        ↓
commit
        ↓
post-commit/cache/result
```

with:

```text
no handler-local provider protocol
no new direct DbContext persistence
no duplicated Domain rule
no ad-hoc role authorization
no hidden tenant scope
no unbounded retry/idempotency ambiguity
no side effect before authoritative commit
```

The objective is a use-case layer that is explicit enough to be secure and reliable, while remaining independent enough that persistence, messaging, providers, and hosts can evolve around it.
