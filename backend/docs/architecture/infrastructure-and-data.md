---
document_id: BE-INFRASTRUCTURE-DATA
document_type: architecture
status: active
owner: backend-architecture
applies_to:
  - backend/src/Notrelix.Infrastructure
  - backend/tests/Notrelix.Infrastructure.Tests
  - backend/tests/Notrelix.Integration.Tests
evidence:
  - backend/src/Notrelix.Infrastructure/Notrelix.Infrastructure.csproj
  - backend/src/Notrelix.Infrastructure/Data/
  - backend/src/Notrelix.Infrastructure/Caching/
  - backend/src/Notrelix.Infrastructure/Auth/
  - backend/src/Notrelix.Infrastructure/Integrations/
  - backend/src/Notrelix.Infrastructure/Storage/
  - backend/docs/decisions/ADR-002-rls-bootstrap-connection-lifecycle.md
  - backend/tests/Notrelix.Infrastructure.Tests/
  - backend/tests/Notrelix.Integration.Tests/
review_on:
  - persistence-model-change
  - dbcontext-boundary-change
  - rls-model-change
  - migration-strategy-change
  - cache-architecture-change
  - provider-adapter-change
  - storage-search-change
  - database-provider-change
---

# Infrastructure and Data

> **Infrastructure implements persistence, database tenancy defense, cache, provider, storage, search, identity, and other technical adapters for inward Application/Domain contracts. It does not own business lifecycle or authorization semantics merely because those rules must eventually be persisted or executed through a provider.**
>
> PostgreSQL is the authoritative relational persistence class. RLS is defense-in-depth. Cache/search/projections are derived. Provider DTOs remain at the adapter boundary. Migration evidence comes from the EF model + migration chain + real existing-data proof—not a handwritten schema snapshot.

This document is the canonical backend owner for Infrastructure/data mechanics.

Application transaction/use-case semantics remain owned by `application-model.md`.

Platform delivery semantics remain owned by `platform-and-messaging.md`.

Repository migration/recovery policy remains under repository Delivery/Operations.

---

# 1. Infrastructure purpose

Infrastructure translates inward contracts into runtime/provider mechanics.

Current responsibility families include:

```text
EF Core / DbContext
PostgreSQL / Npgsql
migrations
RLS
query/read ports
Redis/cache
Identity/JWT/auth provider mechanics
email
integration providers
storage
search/read models/projections
logging/observability adapters
MassTransit/RabbitMQ adapter integration
runtime options/config binding
```

These technologies are replaceable mechanisms.

Their presence does not transfer product ownership.

---

# 2. BE-INF-001 — Infrastructure depends inward

Current project references:

```text
Infrastructure
→ Application
→ Domain
```

Infrastructure MAY know inward contracts/types to implement them.

Application/Domain MUST NOT depend on Infrastructure concrete types as ordinary architecture.

---

# 3. Current package evidence

Current `Notrelix.Infrastructure.csproj` includes packages such as:

```text
Microsoft.EntityFrameworkCore
Npgsql.EntityFrameworkCore.PostgreSQL
EFCore.NamingConventions
Microsoft.AspNetCore.Identity.EntityFrameworkCore
JWT/Bearer packages
Redis/StackExchange.Redis
Resend
Serilog
MassTransit
MassTransit.RabbitMQ
```

This is current executable evidence.

Package presence does not itself define semantic ownership.

---

# 4. Infrastructure folder evidence

Current source includes areas such as:

```text
Auth
Billing
Caching
Configuration
Data
Email
Events
Identity
Integrations/Providers
Messaging
Notifications
Observability
Operations
RateLimiting
ReadModels
Realtime
Security
Serialization
Services
Storage
```

Folders reflect implementation concerns.

Do not infer a new bounded context solely from an Infrastructure folder.

---

# 5. Persistence model

The authoritative relational path is:

```text
Domain/Application semantic model
        ↓
EF mapping/converter
        ↓
PostgreSQL schema
        ↓
migration history
```

Infrastructure adapts semantics to storage.

Do not reverse ownership:

```text
column/table convenience
→ product rule
```

---

# 6. BE-INF-002 — DbContext belongs to Infrastructure

Current:

```text
ApplicationDbContext
```

and related:

```text
DbSets
factory
initialiser
session/data scope
configurations
converters
interceptors
migrations
```

live under Infrastructure.

New persistence implementation remains here.

---

# 7. DbContext purpose

DbContext owns technical persistence composition:

```text
entity mappings
change tracking
transactions
query filters where used
converters
interceptors
database provider
migration model
```

It does not own:

```text
resource permission
business transition
feature entitlement
cross-context lifecycle
```

---

# 8. BE-INF-003 — DbContext is not a cross-context integration API

Do not let another context's Application handler use:

```text
DbContext.ForeignContextEntities
```

to mutate foreign-owned state as normal integration.

Use the target owner's Application contract/event.

---

# 9. Mapping

EF configurations map approved Domain/Application concepts to relational representation.

Review:

```text
table/schema
column type
nullability
length
conversion
owned types
keys
foreign keys
indexes
concurrency
delete behavior
```

Mapping must preserve semantic meaning.

---

# 10. BE-INF-004 — Persistence mapping does not bypass Domain API

Do not use:

```text
reflection/private-field mutation
materialization workaround
setter exposure
```

to perform ordinary business mutation that should go through Domain behavior.

Persistence rehydration support is allowed.

Business change still uses the owned API.

---

# 11. Schema organization

Schema/table naming can group technical ownership.

Do not treat a PostgreSQL schema as the only definition of bounded context.

A context boundary includes:

```text
semantics
write authority
contracts
events
```

not only table prefix.

---

# 12. BE-INF-005 — One table has one logical semantic owner

Shared database does not mean shared write authority.

Document/model foreign references carefully.

Cross-context read projections may join/copy data for queries, but writes return to the source owner.

---

# 13. Database constraints

Use database constraints to reinforce race-safe invariants such as:

```text
uniqueness
FK integrity
not-null
check conditions
```

where appropriate.

The business meaning remains Domain/Application-owned.

---

# 14. BE-INF-006 — Constraint failure maps to stable semantic outcome

Do not leak raw:

```text
constraint name
SQLSTATE
Npgsql exception
```

to public consumers.

Translate known conflicts through Infrastructure/Application result semantics.

---

# 15. Indexes

Indexes serve:

```text
authorized query patterns
tenant predicates
sort/filter
FK/uniqueness
migration/reconciliation
```

They do not redefine product semantics.

Index design should consider real cardinality and tenant distribution.

---

# 16. BE-INF-007 — Index follows query/invariant evidence

Do not add indexes mechanically to every column.

Do not omit tenant-leading/selective index design where RLS/query patterns require it.

Use query-plan/load evidence for performance-sensitive paths.

---

# 17. PostgreSQL authority

PostgreSQL is the production relational persistence class.

Tests for provider-specific semantics should use PostgreSQL-realistic evidence.

SQLite/InMemory cannot certify:

```text
RLS
Npgsql conversion
PostgreSQL locks
migration DDL
provider-specific indexes
```

---

# 18. BE-INF-008 — Provider substitution is architecture work

Changing PostgreSQL/provider is not a simple configuration swap if it changes:

```text
transactionality
RLS
locking
JSON/type mapping
index behavior
migration semantics
```

Review protected properties through architecture decision/change process.

---

# 19. EF model evidence

The current intended persistence model is represented by:

```text
Domain/Application source
+
EF configuration/model
+
migration chain/current schema
```

Do not maintain a handwritten schema snapshot as equal authority.

Generated schema output can be evidence when derived from the producer.

---

# 20. BE-INF-009 — Pending model changes are a failure to resolve, not a warning to suppress

If EF reports model drift:

```text
determine intended model
create/review migration
or correct unintended mapping change
```

Do not suppress `PendingModelChangesWarning` merely to let startup/migration proceed.

---

# 21. Migrations

Production schema change uses reviewed migrations.

Migration files are append-oriented historical persistence evidence.

Do not rewrite already-applied migration history casually.

---

# 22. BE-INF-010 — Migration changes real old state to the target meaning

For a persisted semantic change consider:

```text
existing rows
legacy invalid values
old writers
old readers
backlog/messages
RLS
indexes/constraints
rollback/forward recovery
```

Empty-database success is insufficient when old data exists.

---

# 23. Expand-contract model

When mixed versions can coexist:

```text
expand
→ compatible writer/reader
→ backfill
→ cutover
→ verify
→ contract
```

Do not combine incompatible expansion and destructive contraction in one first deployment unless true atomicity is proven.

---

# 24. BE-INF-011 — Destructive contraction waits for objective removal proof

Before dropping:

```text
column
table
old representation
index supporting old path
```

prove:

```text
old readers stopped
old writers stopped
backfill complete
rollback policy known
backlog/old clients handled
```

---

# 25. Migration initialisation

Startup/local initialization may apply migrations according to environment/runtime policy.

Production migration timing/credential is controlled by Delivery/Infrastructure runtime.

Do not make every application instance race to perform privileged DDL without an approved strategy.

---

# 26. BE-INF-012 — Migration privilege is separable from steady-state runtime

Where infrastructure supports it:

```text
migration/admin identity
≠
normal API/worker DB identity
```

Normal runtime should not require broad DDL/admin privilege solely because migrations exist.

---

# 27. Seed

Seed/bootstrap data exists for development/testing or explicit production bootstrap.

Do not make development seed a source of product truth.

Do not reset/populate production silently.

---

# 28. BE-INF-013 — Seed is environment-scoped and idempotent where repeatable

A repeatable seed should avoid duplicate/corrupt state.

Production seed must be explicit and safe.

---

# 29. RLS purpose

PostgreSQL Row-Level Security is defense-in-depth for tenant isolation.

It complements:

```text
Application authorization
```

It does not replace it.

---

# 30. BE-INF-014 — RLS and Application authorization are both required where the architecture declares them

Correct:

```text
Application denies unauthorized use case
+
RLS prevents unintended cross-tenant DB access
```

Do not intentionally weaken either because the other exists.

---

# 31. RLS context

Current architecture uses PostgreSQL session/transaction configuration for identity/scope facts such as:

```text
current user
Account
Workspace
request scope
correlation
```

exact variables/functions are current implementation evidence.

RLS context must be applied before tenant-protected SQL requiring it.

---

# 32. BE-INF-015 — RLS session context is connection-scoped state that must not leak

Connection pooling means Infrastructure must ensure:

```text
correct set
correct transaction-local/session semantics
cleanup/reset on return
no previous-request tenant leakage
```

Tests must cover reuse/lifecycle.

---

# 33. Tenant bootstrap exception path

ADR-002 defines a current bootstrap path before full Account/Workspace context is known.

Current design:

```text
authenticated user known
→ open same physical Npgsql connection
→ set minimal RLS session context
→ bootstrap query resolves scope
→ later DbRequestScope applies full transaction-local RLS context
```

This is an accepted specialized lifecycle.

---

# 34. BE-INF-016 — Bootstrap RLS context is minimal, not a global bypass

The bootstrap path sets only the facts required to safely resolve the remaining scope.

Do not use:

```text
IgnoreQueryFilters
```

as proof PostgreSQL RLS is bypassed.

EF query filters and PostgreSQL RLS are separate controls.

---

# 35. Same physical connection

ADR-002 relies on the same scoped DbContext/physical connection lifecycle between bootstrap and later full request scope.

If this lifecycle changes, the ADR/architecture/test assumptions must be re-evaluated.

---

# 36. BE-INF-017 — RLS lifecycle changes require connection-pooling/reuse proof

Test:

```text
bootstrap
full scope
transaction
pool return
next request with different tenant
background/system scope
```

as applicable.

Do not prove only first-request success.

---

# 37. RLS policy design

Policy should be based on explicit ownership relationships.

Do not auto-generate policy solely from column naming such as:

```text
workspace_id exists
→ generate one standard policy
```

because membership/resource semantics differ.

---

# 38. BE-INF-018 — RLS policy is context-aware persistence enforcement

Before adding policy identify:

```text
table owner
Account/Workspace ownership path
user/service access pattern
system/background access
required indexes
delete/archive semantics
```

Then implement/test.

---

# 39. RLS tests

At minimum for changed tenant-protected persistence:

```text
allowed tenant
denied foreign tenant
```

plus any relevant:

```text
pooled connection leakage
background worker scope
system operation
bootstrap
```

Use PostgreSQL/Testcontainers.

---

# 40. BE-INF-019 — RLS test uses real PostgreSQL semantics

Do not use EF InMemory/SQLite to claim policy correctness.

The policy is a database feature.

---

# 41. EF global query filters

Query filters can improve default scoping/lifecycle behavior.

They are not a replacement for RLS or authorization.

A query intentionally bypassing an EF filter still remains subject to DB RLS unless privileged/system path changes it.

---

# 42. BE-INF-020 — Filter bypass is explicit and narrow

Use filter bypass only for:

```text
bootstrap
admin/system operation
restore/history
special query
```

with security reasoning.

Do not call `IgnoreQueryFilters()` as a general fix for missing results.

---

# 43. System context

Infrastructure may support privileged/system DB scope.

Such scope must be:

```text
explicit
audited
least privilege
bounded to operation
```

Do not make system context ambient/global.

---

# 44. BE-INF-021 — System DB context is not a universal tenant bypass

A system process still identifies:

```text
operation purpose
resource scope
audit/correlation
```

where applicable.

Do not let ordinary handlers opt into system context to avoid RLS problems.

---

# 45. Persistence concurrency

Infrastructure maps aggregate/resource version to database concurrency mechanism.

It should detect stale update and translate technical conflict to Application semantics.

---

# 46. BE-INF-022 — Persistence concurrency cannot silently overwrite newer state

Do not disable concurrency checks because retries are difficult.

Do not automatically reload + update unless the Application/product operation defines merge/retry.

---

# 47. Transactions

Infrastructure implements the transaction declared by Application.

It must preserve:

```text
atomic local state
RLS transaction scope
outbox/durable enrollment
rollback on failure
```

according to architecture.

---

# 48. BE-INF-023 — Infrastructure does not extend transaction around arbitrary external provider calls

Provider call inside the same method does not make provider state transactional.

Avoid long locks and unknown outcomes.

Use Application/Platform orchestration.

---

# 49. Interceptors

EF interceptors may implement technical cross-cutting persistence mechanisms.

Examples:

```text
auditing
outbox enrollment
tenant/session behavior
```

where architecture defines them.

Do not hide product business transitions in interceptors.

---

# 50. BE-INF-024 — Interceptor cannot invent Domain event/business state

An interceptor may persist/transform technical metadata from an already-decided operation.

It must not decide:

```text
subscription should cancel
workspace role should change
item should move
```

based on database state alone.

---

# 51. Read ports

Infrastructure can implement optimized Application read ports.

Read models may use:

```text
projection
SQL/EF query
joins
denormalized columns
```

as long as source ownership/security remain correct.

---

# 52. BE-INF-025 — Read model may denormalize, but source writes remain owned

Do not expose:

```text
read projection repository.Save()
```

as a mutation path to source contexts.

Rebuild derived data from authoritative sources.

---

# 53. Cross-context joins

Within the modular monolith/shared DB, a read projection may join context-owned tables for an explicitly owned read use case.

This is not permission for cross-context write coupling.

Future extraction may replace the join with projection/event replication.

---

# 54. BE-INF-026 — Cross-context read coupling is visible and replaceable

For high-value cross-context query:

```text
identify source owners
identify freshness need
identify extraction alternative
```

Do not bury it in generic repository code.

---

# 55. Cache role

Redis/cache is:

```text
derived/scoped acceleration
```

unless a separately accepted architecture explicitly assigns another role.

Cache loss must not lose authoritative product state.

---

# 56. BE-INF-027 — Cache is never the sole authorization or product authority

Do not store the only copy of:

```text
permission
membership
entitlement
business mutation
```

in Redis.

Cache miss/outage should fall back safely or degrade.

---

# 57. Cache keys

Include scope needed to prevent collision/leakage.

Potential dimensions:

```text
Account
Workspace
resource
user/principal
permission version
query/version
```

depending on data.

Do not use one global key for tenant-sensitive result.

---

# 58. BE-INF-028 — Authorization-sensitive cache is scope/version aware

On:

```text
membership revoke
permission change
share revoke
resource move
```

stale allow/result must be invalidated or made unreachable through versioned key semantics.

---

# 59. Cache serialization

Cache payload is a derived contract.

Version/serialization changes should either:

```text
remain backward readable
version key
invalidate old cache
```

Do not let deserialization failure take down authoritative writes.

---

# 60. Cache outage

If authoritative DB path is safe/capable:

```text
cache miss/outage
→ safe origin read
```

Otherwise degrade according to operations policy.

Do not serve unsafe stale permission data to preserve hit rate.

---

# 61. BE-INF-029 — Cache fallback is capacity-aware and fail-safe

A global cache outage can stampede PostgreSQL.

Use bounded fallback/monitoring.

Do not turn cache bypass into another outage.

---

# 62. Provider adapter

Infrastructure adapter maps between:

```text
Application capability contract
↔
provider protocol/SDK
```

Provider terms remain at the edge.

---

# 63. BE-INF-030 — Provider DTO does not leak into Domain/Application public semantics

Convert:

```text
provider status/code/payload
```

to stable Application result/fact.

Keep raw provider details for safe diagnostics where needed.

---

# 64. Provider timeout

Every network/provider operation has bounded timeout/cancellation appropriate to the operation.

For external writes, timeout may mean unknown outcome.

---

# 65. BE-INF-031 — Adapter distinguishes transient failure, terminal rejection, and unknown outcome

Examples:

```text
429/rate limit
→ transient/backoff

invalid credential
→ terminal/re-auth required

timeout after write request
→ outcome unknown/reconcile
```

Do not classify every exception as retryable.

---

# 66. Provider retry

Adapter-level retry may be appropriate for transient safe operations.

It must preserve:

```text
idempotency
rate limit
cancellation
provider capacity
```

Do not retry irreversible write without stable provider operation identity/reconciliation.

---

# 67. BE-INF-032 — Provider retry never creates duplicate business side effect silently

Use provider idempotency key/correlation or reconcile current provider reality.

Application owns pending/unknown semantic result where needed.

---

# 68. Email

Email provider adapter implements notification/email capability.

It does not own:

```text
who should receive
what business event occurred
what permission user has
```

Application/context decides recipient/content intent; Infrastructure sends through provider.

---

# 69. Identity/auth provider mechanics

Infrastructure can own:

```text
ASP.NET Identity persistence
password hashing
JWT signing/validation implementation
OAuth provider adapters
token persistence
```

Product Identity semantics remain Identity context/Application.

---

# 70. BE-INF-033 — Authentication provider mechanism does not define Workspace authorization

A valid JWT/user account does not imply resource permission.

Keep resource authorization in Application/Governance.

---

# 71. Password hashing

Password hashes are sensitive technical authentication data.

Use established secure hashing implementation/config.

Do not expose hashes to Domain events/logs/API.

Hash algorithm/cost changes are security/data migration concerns.

---

# 72. JWT

JWT claims are authentication/context inputs.

Do not treat client-presented claim as final resource authorization if current server policy must resolve resource membership/permission.

Signing keys are secret runtime configuration.

---

# 73. OAuth provider mapping

External subject/provider IDs map to Identity-owned user/account semantics.

Do not replace internal stable user identity with provider subject as the global Domain ID automatically.

Provider unlink/relink/rotation must preserve internal ownership.

---

# 74. Storage

Storage adapter owns binary/object mechanics.

Domain/Application typically own:

```text
metadata
ownership
lifecycle
authorization intent
```

Infrastructure owns:

```text
upload/download/delete protocol
object key
signed access mechanism
provider error
```

---

# 75. BE-INF-034 — Object URL/key is not authorization

A storage key identifies an object.

Access is checked by the owning use case and/or time-bounded signed mechanism.

Do not expose a permanent public URL for protected data merely for convenience.

---

# 76. File upload

For multi-step:

```text
object upload
+
DB metadata
```

define pending/final/orphan semantics.

Do not report final success if the durable object or metadata did not complete according to the contract.

---

# 77. Search

Search adapter/index is derived.

Search can optimize:

```text
full text
ranking
cross-resource discovery
```

It does not own source business data.

---

# 78. BE-INF-035 — Search index is rebuildable/derived unless explicitly classified otherwise

Write source context first.

Update index/projection asynchronously/synchronously as architecture defines.

If index is lost, rebuild from authoritative source.

---

# 79. Search authorization

Index/search result must preserve tenant/resource visibility.

Do not return search result then rely solely on frontend to hide unauthorized entries.

Use:

```text
scoped index
permission projection
post-filter with authoritative check
```

as architecture requires.

---

# 80. Projections/read models

Infrastructure can materialize projections for:

```text
governance
analytics
work-management read models
search
```

Current source contains projection/read-model folders.

These are derived representations.

---

# 81. BE-INF-036 — Projection has explicit source facts and freshness

Know:

```text
producer
source owner
update trigger
lag/freshness
rebuild path
tenant scope
```

Do not let projection drift silently become business truth.

---

# 82. Analytics storage

Analytics snapshots/history may intentionally preserve historical reporting state.

Distinguish:

```text
rebuildable current projection
historical immutable snapshot
```

Do not rebuild/overwrite historical report evidence automatically unless product Analytics semantics say so.

---

# 83. Messaging adapter boundary

Infrastructure currently references MassTransit/RabbitMQ and contains messaging integration.

Platform owns reusable delivery semantics.

Infrastructure can implement broker-specific adapter/registration/serialization.

---

# 84. BE-INF-037 — Broker provider implementation does not own delivery semantics

Infrastructure may configure:

```text
RabbitMQ endpoint
MassTransit adapter
transport serializer
connection
```

Platform owns:

```text
logical message/consumer identity
retry/poison/order rules
```

Product context owns event meaning.

---

# 85. Event persistence

Infrastructure may persist:

```text
outbox
consumer dedup
ordering cursor
poison/dead-letter metadata
```

as Platform requires.

The schema supports the mechanism.

It does not redefine the Platform semantics.

---

# 86. BE-INF-038 — Delivery schema changes preserve Platform identity/invariant

Before changing message tables/indexes:

```text
inspect logical key
retention
ordering
retry
replay
migration/backlog
```

Do not rename/drop technical columns without delivery-contract review.

---

# 87. Serialization

Infrastructure serialization can map:

```text
Domain/Application/event contract
→ transport/storage representation
```

Versioning must preserve compatibility where old data/messages exist.

Do not serialize private CLR type name as long-term public discriminator by default.

---

# 88. BE-INF-039 — Persisted discriminator/event name is a compatibility contract

Changing:

```text
enum string
JSON type discriminator
event name
provider mapping key
```

can be a C3/C4 migration even if C# refactor is small.

Inventory old data/consumers before change.

---

# 89. JSON columns

JSON can be useful for flexible/versioned data.

It does not eliminate schema design.

Define:

```text
owner
version
validation
query/index needs
migration
unknown fields
```

Do not dump arbitrary provider payload into authoritative Domain storage without ownership.

---

# 90. BE-INF-040 — JSON schema/version evolves deliberately

If persisted JSON shape changes:

```text
reader compatibility
writer version
backfill/lazy migration
old rows
```

must be understood.

Do not assume serializer default handles all historical shapes forever.

---

# 91. EF converters

Converters bridge Domain value types and provider representation.

They should be deterministic/reversible as required.

Do not hide lossy conversion of meaningful precision/identity.

---

# 92. BE-INF-041 — Converter preserves Domain equality/meaning

Round-trip:

```text
Domain value
→ DB
→ Domain value
```

should preserve semantic equality.

Test provider-specific conversions with real PostgreSQL where relevant.

---

# 93. Date/time

Persist timestamps with explicit UTC/offset semantics consistent with Domain/Application time contracts.

Do not mix local server timezone implicitly.

Time-zone presentation belongs outside persistence.

---

# 94. Money/decimal

Persist exact money/decimal semantics with appropriate precision/scale.

Do not silently convert to floating point.

Billing product semantics define currency/rounding; Infrastructure preserves them.

---

# 95. IDs

Current Domain base uses Guid/Guid v7.

PostgreSQL representation should preserve full identity semantics.

Provider external IDs remain separate columns/value mappings where needed.

Do not overload internal ID column with provider ID.

---

# 96. BE-INF-042 — Persistence identity mapping is stable across provider/internal IDs

Store explicit:

```text
internal stable ID
provider external ID
```

when both exist.

Do not make provider reconnection alter source entity identity.

---

# 97. Delete behavior

Database cascade/restrict/set-null behavior must match product aggregate/reference lifecycle.

Do not enable cascade merely to make FK cleanup easy.

A DB cascade can erase foreign context/history unexpectedly.

---

# 98. BE-INF-043 — Delete behavior is reviewed with semantic ownership

Ask:

```text
is child aggregate-owned?
is reference cross-context?
is historical record retained?
is restore supported?
is physical deletion allowed?
```

Then choose FK/delete behavior.

---

# 99. Soft-delete/query filters

If a context uses logical deletion/tombstone mechanism, EF filters can hide rows in ordinary queries.

Do not equate query filtering with lifecycle authorization.

Restore/history/system paths may need explicit access.

---

# 100. BE-INF-044 — Query filter is persistence convenience, not lifecycle rule

Domain/Application still decide:

```text
what deleted means
who may restore
which operations are forbidden
```

Do not implement product deletion solely with an EF filter.

---

# 101. Auditing

Infrastructure can persist shared audit fields and Governance audit records through approved contracts.

Do not turn EF SaveChanges interception into the only product Activity/Audit definition.

Application/product owner decides which facts need which history.

---

# 102. BE-INF-045 — Infrastructure audit mechanism consumes decided semantic facts

It may record:

```text
actor
time
entity/resource
operation
```

after use-case decision.

It must not infer high-level product event solely from changed columns if semantics are ambiguous.

---

# 103. Observability

Infrastructure logs/metrics should expose:

```text
provider/database dependency health
query/migration/backlog correlation
safe resource/tenant identifiers
retry/outcome class
```

without secrets/private payload dumping.

---

# 104. BE-INF-046 — SQL/provider logging is privacy and cardinality aware

Do not enable full sensitive-data logging in production as ordinary diagnostics.

Do not label metrics by arbitrary resource IDs with unbounded cardinality.

---

# 105. Connection pooling

Npgsql connection pools are shared runtime resources.

Session-level settings, temporary state, transaction status, and failures must be safely reset/reused.

ADR-002 specifically depends on pool cleanup semantics.

---

# 106. BE-INF-047 — Connection state does not leak between requests

Test/reason about:

```text
tenant A
→ pool return
→ tenant B
```

especially for RLS/session config.

A successful first request proves nothing about pool isolation.

---

# 107. Database retry

Transient database retry must respect transaction/idempotency.

Do not retry an operation whose commit outcome is unknown without knowing whether duplicate mutation can occur.

Provider/EF execution strategies require careful transaction semantics.

---

# 108. BE-INF-048 — Retry classification is mechanism-aware

Classify:

```text
connection transient
deadlock/serialization conflict
constraint violation
cancellation
timeout with unknown commit
```

differently.

Do not wrap every DB exception in an infinite retry policy.

---

# 109. Deadlocks/locks

For hot paths monitor/review:

```text
lock ordering
transaction duration
batch size
index
query plan
```

Do not solve deadlock by disabling transaction correctness.

Application/Domain invariant still defines what must remain atomic.

---

# 110. Backfills

Infrastructure may implement migration/backfill tooling.

It must be:

```text
bounded
resumable
idempotent
tenant-safe
observable
cancellable
```

for large production work.

Use repository migration template for material cases.

---

# 111. BE-INF-049 — Backfill has stable traversal and checkpoint

Avoid:

```text
OFFSET page through changing table
```

for very large mutable datasets where it can skip/duplicate.

Use a stable key/range strategy appropriate to the schema.

---

# 112. Backfill invalid data

If legacy rows cannot map deterministically:

```text
quarantine/report
approved normalization
block migration
explicit sentinel
```

Do not guess.

Product/context owner decides meaning.

---

# 113. Database backup/recovery boundary

Infrastructure provides database backup/restore mechanism.

Operations owns recovery decision/reconciliation.

Do not claim:

```text
snapshot exists
→ recovery solved
```

Recovery must verify application/RLS/outbox/provider state.

---

# 114. BE-INF-050 — Restore does not skip reconciliation

After DB restore/repair inspect:

```text
schema/migration
RLS
outbox
dedup/order
provider effects
object storage
cache/search/projections
```

according to recovery policy.

---

# 115. Configuration/options

Infrastructure can bind provider/runtime options.

Typed options should validate required values.

Do not use permissive production fallback for:

```text
DB
JWT
provider secret
CORS/security
```

Exact environment ownership belongs to repository Infrastructure docs.

---

# 116. BE-INF-051 — Secret value never becomes Application/Domain config data

Infrastructure resolves secrets at the adapter/composition boundary.

Pass only the semantic/provider result needed inward.

Do not store secret in Domain event/result/log.

---

# 117. Dependency injection

Infrastructure registers implementations for inward contracts.

Composition should make provider choice replaceable.

Avoid service locator/static global access from Domain/Application.

---

# 118. BE-INF-052 — DI registration does not erase ownership

A service registered as `Singleton`/`Scoped` does not decide its semantic layer.

The interface responsibility and implementation dependency determine ownership.

---

# 119. Lifetime

Choose lifetime based on:

```text
thread safety
request/transaction scope
connection/provider client guidance
cache/state
```

not habit.

DbContext remains scoped to the request/unit of work as architecture requires.

---

# 120. Background services

Infrastructure may host provider/integration background mechanics where appropriate, while Platform owns generic consumer/delivery mechanics.

Every background path still reconstructs:

```text
tenant
correlation
idempotency
scope
```

as required.

---

# 121. BE-INF-053 — Background Infrastructure does not use unrestricted DB scope by default

Use explicit worker/system context only when the operation genuinely requires it.

Tenant-specific work should remain tenant-scoped.

---

# 122. Rate limiting implementation

Infrastructure can implement stores/counters/provider mechanics for rate limiting.

API/Application owns policy placement/operation identity as accepted architecture defines.

Do not embed product entitlement semantics into Redis counter code.

---

# 123. Realtime implementation

Infrastructure may implement SignalR/realtime adapters if current topology places them here.

The adapter delivers Application/Platform-approved messages.

It does not own source state or permission semantics.

---

# 124. BE-INF-054 — Realtime adapter revalidates/uses authoritative scope contract

Do not trust a stale client subscription forever after membership/share revocation.

Security architecture determines subscription authorization/reconciliation.

---

# 125. Notifications implementation

Provider delivery mechanics belong here.

Notification product record/recipient semantics belong to owning context/Application.

Email/push provider failure becomes stable delivery outcome, not product event ownership.

---

# 126. Billing provider

Payment provider adapter handles:

```text
API/webhook protocol
provider IDs
idempotency
signature
unknown outcome
```

Billing context owns:

```text
Subscription
Entitlement
Invoice/Payment business meaning
Usage
```

Do not map provider state one-to-one into Billing Domain without deliberate translation.

---

# 127. BE-INF-055 — Financial provider reconciliation preserves external reality

For timeout/replay/webhook:

```text
identify provider operation/event
deduplicate
reconcile current provider state
```

Do not duplicate charge or delete financial evidence to “fix” state.

---

# 128. Webhooks

Infrastructure validates provider-level:

```text
signature
timestamp/replay token
schema
provider identity
```

before passing trusted normalized event to Application/context.

Application still decides product effect.

---

# 129. BE-INF-056 — Unverified webhook never mutates product state

Signature/replay validation happens before use-case effect.

Do not let Application receive an untrusted provider payload and hope business validation will compensate.

---

# 130. Provider webhooks and idempotency

Provider event ID is often a stable dedup fact.

Do not treat repeated webhook delivery as a new business occurrence automatically.

Persist/coordinate dedup according to context/provider contract.

---

# 131. Search/storage provider change

Switching provider can require:

```text
data migration
index rebuild
object migration
URL/key mapping
dual read
cutover
reconciliation
```

Do not change adapter registration and assume old durable external data follows automatically.

---

# 132. BE-INF-057 — Durable external-provider migration has an explicit data/state plan

Object storage/search/payment/provider resources may outlive one binary.

Inventory existing external state before provider replacement.

---

# 133. Infrastructure tests

Primary:

```text
backend/tests/Notrelix.Infrastructure.Tests
```

Use for:

```text
mapping
converters
RLS
migrations
cache adapters
provider mapping
persistence
```

Use Integration for production graph/cross-layer behavior.

---

# 134. BE-INF-058 — Test fidelity matches the Infrastructure property

Use real:

```text
PostgreSQL
```

for PostgreSQL/RLS semantics.

Use mocks/fakes only for adapter orchestration where protocol fidelity is not the protected property.

Do not overclaim.

---

# 135. Migration tests

For schema changes:

```text
clean database
+
representative previous state
+
migration
+
semantic verification
+
RLS
+
Application read/write where relevant
```

Do not only assert migration command exited successfully.

---

# 136. Cache tests

Prove:

```text
scope
serialization
expiry/invalidation
fallback
permission-sensitive behavior
```

Do not assert only `SetAsync` was called.

---

# 137. Provider tests

At the adapter seam, test:

```text
request mapping
response mapping
error classification
timeout
rate limit
cancellation
idempotency/correlation
unknown outcome
```

Use sandbox/contract integration when real provider semantics are required and safe.

---

# 138. RLS architecture tests

Where possible, gates can ensure:

```text
tenant-owned tables are registered
policy coverage rules
forbidden privileged bypass usage
```

but do not auto-generate policies from naming assumptions.

Real policy behavior still needs PostgreSQL tests.

---

# 139. BE-INF-059 — Architecture guard cannot replace runtime RLS proof

Static/source checks can detect missing declarations.

They cannot prove PostgreSQL policy execution for real session context.

Use both where useful.

---

# 140. Data ownership tests

For cross-context persistence:

```text
ensure foreign context cannot directly mutate
ensure projection writes are isolated
ensure source context remains authoritative
```

through architecture/integration tests where possible.

---

# 141. Current EF exception interaction

Application's EF package exception does not move:

```text
ApplicationDbContext
migrations
configuration
RLS
```

out of Infrastructure.

Any new direct persistence use in Application must be separately governed.

---

# 142. BE-INF-060 — Infrastructure remains the persistence implementation owner while EF exception exists

The exception narrows dependency purity.

It does not create dual persistence ownership.

---

# 143. Schema naming/versioning

Persisted strings can become compatibility contracts.

Examples:

```text
status values
discriminators
event names
feature codes
provider type
JSON version
```

Change them with migration/consumer inventory.

---

# 144. BE-INF-061 — CLR rename does not automatically rename persisted identity

A refactor can preserve:

```text
column
discriminator
event name
```

until migration is complete.

Do not let convention-based mapping silently rename production storage/public identities.

---

# 145. Naming conventions

EF naming-convention package can translate CLR naming to SQL naming.

Convention is a mechanism.

Explicit mapping should override it where stable schema compatibility requires.

Do not make a code-style rename a destructive DB migration accidentally.

---

# 146. Query filters and soft-delete uniqueness

Logical deletion can interact with unique constraints/indexes.

Decide whether deleted values can be reused according to product lifecycle.

Do not add filtered unique index solely because physical rows remain.

---

# 147. BE-INF-062 — Persistence uniqueness follows current product lifecycle

Examples:

```text
slug reusable after archive?
email reusable after account deletion?
provider connection unique while revoked?
```

Product owner decides; schema enforces.

---

# 148. Retention

Infrastructure implements retention/deletion jobs according to Product/Security/Operations policy.

Do not invent retention periods in code/config without approved owner.

Retention jobs must be tenant-safe, bounded, auditable where required.

---

# 149. BE-INF-063 — Retention mechanism does not erase required history blindly

Before deletion consider:

```text
audit
financial
legal/privacy
replay
recovery
provider state
```

Route policy to the correct owner.

---

# 150. Data repair

Production repair tooling should be:

```text
scoped
reviewed
idempotent
observable
tenant-safe
reproducible
```

Do not expose repair endpoints as ordinary product APIs.

---

# 151. BE-INF-064 — Repair derives target value from authoritative semantics

Do not guess plausible values.

Use:

```text
known source facts
deterministic migration rule
approved correction
historical evidence
```

and record the repair.

---

# 152. Infrastructure performance

Performance-sensitive persistence/provider work should consider:

```text
cardinality
query plan
indexes
connection pool
transaction duration
cache stampede
provider limits
batch size
```

Do not optimize by violating tenant or ownership boundaries.

---

# 153. BE-INF-065 — Measured performance problem does not transfer semantic ownership

A denormalized read projection/cache may solve performance.

Source writes still route to the semantic owner.

---

# 154. Connection pool capacity

Pool size is runtime configuration.

Do not hardcode one canonical number in architecture docs.

Monitor:

```text
pool exhaustion
wait time
DB max connections
worker/API concurrency
```

and tune by environment/workload.

---

# 155. Provider client lifetime

HTTP/provider client lifetime follows provider/runtime guidance and DI safety.

Do not create new raw client per request if it causes connection exhaustion.

Do not share mutable tenant credentials across scopes unsafely.

---

# 156. BE-INF-066 — Provider client separates shared transport from tenant credential/state

A reusable HTTP transport can be shared.

Tenant/provider connection authorization/tokens remain scoped and secret-safe.

---

# 157. Secrets

Infrastructure may receive:

```text
JWT key
DB secret
Redis password
provider API key
OAuth client secret
webhook secret
```

through typed runtime config/secret delivery.

Never commit/log/expose them.

---

# 158. BE-INF-067 — Secret is not persisted into ordinary business table/event accidentally

Store secret reference/encrypted credential according to approved design.

Do not copy secret into:

```text
Domain event
Activity
Audit message
cache payload
public DTO
```

---

# 159. Encryption/protection

Sensitive stored credentials may require encryption/key protection.

Infrastructure implements crypto mechanism.

Product/security owner defines which data requires protection and lifecycle.

Do not create custom cryptography without approved design.

---

# 160. Serialization privacy

Provider/raw payload logging should be minimized.

Store raw payload only when product/audit/replay requirements justify it and retention/security are defined.

Do not keep everything “for debugging”.

---

# 161. BE-INF-068 — Raw provider payload is not default durable storage

Prefer normalized owned facts.

If raw evidence is required:

```text
scope
encryption
retention
access
redaction
```

must be explicit.

---

# 162. Database timestamps

Database-generated timestamps can differ from Application-supplied business time.

Use the correct owner:

```text
business occurrence time
→ Application/Domain supplied

technical DB insertion time
→ database may supply
```

Do not substitute one silently.

---

# 163. BE-INF-069 — Persistence-generated technical metadata does not overwrite business timestamp semantics

If an event/aggregate says the operation occurred at T, mapping should preserve T.

DB `now()` may be a separate technical field.

---

# 164. Read consistency

A query may require:

```text
same transaction
current authoritative DB
cache-tolerant stale
projection eventual
```

Application defines the use-case expectation.

Infrastructure implements an appropriate query mechanism.

---

# 165. BE-INF-070 — Read port documents freshness/authority where material

Do not swap:

```text
DB read
→ stale projection/cache
```

without preserving the query contract.

Performance optimization can change user-visible semantics.

---

# 166. Replica use

If read replicas are introduced later, replication lag becomes part of query consistency.

Do not route authorization/critical read-after-write to replica without explicit tolerance.

This would be a runtime/data architecture change.

---

# 167. Multi-region

Current docs do not assume a multi-region database architecture.

Do not invent active-active conflict semantics.

If introduced, it requires system/data architecture decision.

---

# 168. Partitioning/sharding

Do not shard by tenant merely because SaaS systems sometimes do.

First prove:

```text
cardinality/load
hot tenant
storage
operational need
```

and preserve RLS/data ownership/migration semantics.

---

# 169. BE-INF-071 — Scaling topology follows evidence

Partitioning/sharding/read replica/provider change is consequential architecture.

Use ADR/change plan.

Do not hide it inside a repository implementation refactor.

---

# 170. Database schema extraction

Future service extraction may move owned tables/data.

Prepare by maintaining clear logical table ownership now.

Do not force one schema/database per context prematurely solely for extraction optics.

---

# 171. BE-INF-072 — Extraction changes data topology, not semantic owner

The migration plan moves:

```text
owned tables/state
contracts
backlog
provider mappings
```

while preserving source context semantics.

---

# 172. Current data tree is evidence

Current `Infrastructure/Data` contains:

```text
Configurations
Converters
Interceptors
Migrations
Rls
ReadPorts
Projections
Events
Authz
Audit
Ops
...
ApplicationDbContext*
EfRequestDataSession
SystemContextScope
```

This is current implementation evidence.

Do not treat every directory as a permanent architecture module.

---

# 173. Infrastructure change classes

Typical:

```text
mapping/index/cache adapter
→ C2/C7 depending impact

schema/backfill
→ C4

RLS/auth provider
→ C6 (+ C4/C7)

new dependency/provider
→ C7/C5

persisted identity rename
→ C3/C4

destructive retention
→ C8
```

Obligations are cumulative.

---

# 174. ADR trigger

A new ADR may be required for:

```text
database/provider strategy
RLS/session architecture
new cache authority model
provider abstraction foundation
cross-context persistence model
migration foundation
new durable search/storage technology
```

Routine EF mapping change does not need an ADR if canonical rules determine it.

---

# 175. Persistence review checklist

```text
[ ] semantic owner
[ ] table/schema mapping
[ ] keys/FKs
[ ] nullability
[ ] converter
[ ] concurrency
[ ] index
[ ] tenant ownership
[ ] RLS
[ ] filter
[ ] existing-data migration
[ ] old/new compatibility
[ ] rollback/forward recovery
[ ] PostgreSQL tests
```

---

# 176. RLS review checklist

```text
[ ] table owner
[ ] Account/Workspace path
[ ] user/service/system access
[ ] bootstrap/full context
[ ] connection lifecycle
[ ] allowed tenant
[ ] denied tenant
[ ] pool reuse
[ ] indexes
[ ] background scope
```

---

# 177. Cache review checklist

```text
[ ] derived only
[ ] key scope
[ ] permission sensitivity
[ ] invalidation
[ ] version
[ ] serialization
[ ] outage fallback
[ ] stampede/capacity
[ ] tenant leak negative test
```

---

# 178. Provider review checklist

```text
[ ] inward Application contract
[ ] provider mapping
[ ] credentials
[ ] timeout
[ ] cancellation
[ ] retry class
[ ] rate limit
[ ] idempotency
[ ] unknown outcome
[ ] webhook/signature
[ ] reconciliation
[ ] logs/privacy
```

---

# 179. Migration review checklist

```text
[ ] current real data
[ ] target meaning
[ ] expand/contract
[ ] old/new binaries
[ ] backfill
[ ] invalid legacy rows
[ ] RLS/index/constraint
[ ] backlog/provider state
[ ] completion proof
[ ] destructive removal proof
[ ] recovery
```

---

# 180. Stop conditions

Stop Infrastructure implementation if:

- the owning product/context meaning is unresolved;
- mapping change is being used to redefine Domain behavior;
- a foreign context is being given direct write access;
- RLS is being disabled to make a query work;
- `IgnoreQueryFilters()` is proposed as a broad tenant bypass;
- migration only works on empty DB for an existing-data change;
- EF model drift is being suppressed;
- provider timeout is treated as definite failure without outcome analysis;
- cache becomes permission/source authority;
- public/provider DTO leaks inward;
- broker schema change ignores Platform identity/backlog;
- object/search/provider migration ignores existing durable external state;
- connection/session state can leak across pooled requests;
- a privileged system context is becoming the default path.

---

# 181. Executable evidence

Primary source:

```text
backend/src/Notrelix.Infrastructure
backend/src/Notrelix.Infrastructure/Data
backend/src/Notrelix.Infrastructure/Caching
backend/src/Notrelix.Infrastructure/Auth
backend/src/Notrelix.Infrastructure/Integrations
backend/src/Notrelix.Infrastructure/Storage
```

Primary tests:

```text
backend/tests/Notrelix.Infrastructure.Tests
backend/tests/Notrelix.Integration.Tests
backend/tests/Notrelix.Architecture.Tests
```

Focused:

```bash
cd backend
dotnet test tests/Notrelix.Infrastructure.Tests/Notrelix.Infrastructure.Tests.csproj
```

RLS/migration/production graph changes require PostgreSQL-realistic integration proof.

---

# 182. Related architecture

```text
backend-overview.md
domain-modeling.md
application-model.md
platform-and-messaging.md
api-and-contracts.md
security-tenancy-authorization.md
testing-and-quality-gates.md
```

Related ADR:

```text
../decisions/ADR-002-rls-bootstrap-connection-lifecycle.md
```

Repository:

```text
../../../docs/delivery/migration-policy.md
../../../docs/operations/recovery-and-data-safety.md
../../../docs/infrastructure/environment-model.md
```

---

# 183. Non-responsibilities

Infrastructure does not own:

```text
aggregate boundary
product lifecycle
resource authorization semantics
Billing entitlement meaning
Automation rule meaning
HTTP route/public API version
frontend caching behavior
release cohort
RPO/RTO/SLO values
```

It implements the mechanisms those owners require.

---

# 184. Final Infrastructure rule

A healthy Infrastructure implementation can be explained as:

```text
inward Application/Domain contract
        ↓
provider-neutral capability boundary
        ↓
EF/PostgreSQL/Redis/provider/storage/search implementation
        ↓
tenant-safe mapping/session/RLS
        ↓
migration/retry/reconciliation mechanics
        ↓
stable semantic result back inward
```

with:

```text
no business rule invented from schema
no direct cross-context write shortcut
no RLS bypass for convenience
no EF model drift suppression
no cache/source confusion
no provider DTO leakage
no blind retry of unknown external outcome
no migration that ignores real old data
```

The objective is a replaceable outer mechanism layer that makes PostgreSQL/provider behavior reliable and secure without making those technologies the owners of Notrelix business meaning.
