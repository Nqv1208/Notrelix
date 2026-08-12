---
document_id: BE-MIGRATIONS-DATA-CHANGE
document_type: operations
status: active
owner: backend-data-operations
applies_to:
  - backend-persistence
  - backend-migrations
  - backend-data-change
  - backend-backfills
  - backend-rls
  - backend-indexes
  - backend-data-repair
evidence:
  - backend/src/Notrelix.Infrastructure/Data/ApplicationDbContext.cs
  - backend/src/Notrelix.Infrastructure/Data/ApplicationDbContextInitialiser.cs
  - backend/src/Notrelix.Infrastructure/Data/Configurations/
  - backend/src/Notrelix.Infrastructure/Data/Converters/
  - backend/src/Notrelix.Infrastructure/Data/Migrations/
  - backend/src/Notrelix.Infrastructure/Data/Rls/
  - backend/tests/Notrelix.Infrastructure.Tests/
  - backend/tests/Notrelix.Integration.Tests/
  - Makefile
  - docs/delivery/migration-policy.md
review_on:
  - ef-model-change
  - migration-chain-change
  - backfill-change
  - rls-policy-change
  - index-or-constraint-change
  - persisted-json-change
  - data-repair-change
  - retention-or-destructive-change
---

# Migrations and Data Change

> **A database migration changes durable production state, not just code. The target is correct only when old data, old/new readers and writers, RLS, indexes, backlog, derived state, and recovery semantics remain valid through every deployed phase.**
>
> Infrastructure owns persistence mechanics. Domain/Application/product owners define the meaning being persisted. Migration plans move real state from one valid meaning to another without inventing values or creating dual authority.

This document is the canonical backend operational owner for:

- EF Core migration handling;
- model/schema drift;
- existing-data compatibility;
- expand/backfill/cutover/contract execution;
- RLS policy changes;
- index/constraint/converter changes;
- persisted JSON/discriminator/version changes;
- backfills;
- data repair;
- destructive data changes;
- migration verification;
- rollback versus forward recovery;
- migration command/runtime separation.

Repository-wide migration governance remains in:

```text
../../../docs/delivery/migration-policy.md
```

Material transitions should instantiate:

```text
../../../docs/templates/migration-plan-template.md
```

---

# 1. Data-change ownership

Data change has at least three owners:

```text
Product/Domain/Application
→ what the data means

Infrastructure
→ how meaning is represented/migrated

Delivery/Operations
→ how the transition is executed/recovered
```

Do not let SQL convenience decide business meaning.

---

# 2. BE-OPS-DATA-001 — Meaning changes before representation

Correct flow:

```text
approved semantic/contract change
        ↓
Domain/Application update
        ↓
mapping/schema design
        ↓
migration/backfill
        ↓
compatibility rollout
```

Do not create a column/table first and invent the product semantics afterward.

---

# 3. EF model authority

The active persistence model is evidenced by:

```text
Domain/Application types
+
EF configurations/converters
+
DbContext model
+
migration chain
```

No manually authored schema document is an equal executable authority.

---

# 4. BE-OPS-DATA-002 — Migration source and model must agree

A migration is not complete if:

```text
migration files exist
but
current EF model has unrecorded changes
```

Pending model changes must be resolved intentionally.

---

# 5. Pending model changes

When EF detects pending changes:

```text
inspect intended source/model
determine whether change is intentional
create/review migration if yes
revert accidental model drift if no
```

Do not suppress the warning as normal workflow.

---

# 6. BE-OPS-DATA-003 — PendingModelChangesWarning is not silenced to make deploy green

Suppressing provider warning hides divergence and moves failure to production.

Only a narrowly understood framework/tooling false positive could justify a governed exception.

---

# 7. Migration history

Migration files form append-oriented history.

Do not rewrite already-applied production migrations casually.

A correction normally becomes a new migration.

---

# 8. BE-OPS-DATA-004 — Applied migration history is immutable evidence by default

Editing old migration after environments have applied it can create:

```text
same migration ID
different schema
```

which destroys reproducibility.

Use a new forward migration.

---

# 9. Clean database versus upgrade

Two different properties:

```text
clean latest database
→ can new environment initialize?

upgrade old database
→ can real existing environment transition safely?
```

Both can matter.

---

# 10. BE-OPS-DATA-005 — Existing production state requires upgrade proof

If the change affects existing rows/schema meaning, empty database success is insufficient.

Test representative old state.

---

# 11. Current database commands

Current API/Makefile exposes explicit operations:

```text
--migrate
--seed
--rls-apply
```

with Makefile wrappers:

```text
make db-migrate
make db-seed
make db-init
make db-rls
```

Exact command implementation remains executable source authority.

---

# 12. BE-OPS-DATA-006 — Migration operation is explicit

Production/staging migration SHOULD be a deliberate deployment step according to runtime policy.

Do not depend on accidental first-instance startup to perform an unreviewed migration.

---

# 13. Current initialiser behavior

Current `ApplicationDbContextInitialiser.InitialiseAsync()`:

```text
checks Npgsql
→ MigrateAsync()
→ optionally applies RLS policies when enabled/configured
```

This is current implementation evidence.

Environment/runtime policy decides whether/when it is invoked.

---

# 14. BE-OPS-DATA-007 — Migration capability and startup invocation are separate

The code can support migration.

Production can still choose:

```text
explicit migration job/command
```

rather than:

```text
every API instance migrates on startup
```

for privilege/rollout safety.

---

# 15. Expand/contract

For incompatible persisted changes use staged compatibility:

```text
EXPAND
→ add new representation while old path still works

COMPATIBLE WRITER/READER
→ versions coexist

BACKFILL
→ move existing state

CUTOVER
→ new authority/read/write path

VERIFY
→ prove old path unused

CONTRACT
→ remove old representation
```

---

# 16. BE-OPS-DATA-008 — Every migration phase has one semantic authority

Do not write:

```text
old and new stay in sync
```

without declaring which one is authoritative in each phase.

Dual storage is not dual truth.

---

# 17. Expansion

Expansion should normally be backward compatible.

Examples:

```text
nullable/additive column
new table
new index
new optional JSON field/version
```

depending on old/new reader behavior.

---

# 18. BE-OPS-DATA-009 — Expansion does not require new writer immediately

A migration can deploy structural support before code writes the new shape.

This often reduces rollout coupling.

---

# 19. Compatible readers

During coexistence, readers may need to support:

```text
old only
new only
old + new
```

according to phase.

Define precedence.

Do not let each code path choose differently.

---

# 20. BE-OPS-DATA-010 — Dual-read precedence is deterministic

Example:

```text
if new value exists → use new
else → derive/read old
```

or the approved inverse.

Do not merge conflicting old/new values heuristically without policy.

---

# 21. Compatible writers

Dual write can be useful temporarily.

It creates failure modes:

```text
old succeeds/new fails
new succeeds/old fails
partial retry
order divergence
```

Use only when migration design needs it.

---

# 22. BE-OPS-DATA-011 — Dual write still has one authoritative result

Define:

```text
primary write
compatibility copy
failure handling
retry/reconciliation
```

Do not consider both stores independently authoritative.

---

# 23. Backfill

Backfill moves existing durable state.

It is a production workload.

It needs:

```text
stable traversal
bounded batch
checkpoint
idempotency
tenant safety
progress
failure handling
completion proof
```

---

# 24. BE-OPS-DATA-012 — Backfill is resumable

A crash/deploy/timeout MUST NOT require restarting blindly from the beginning unless the workload is demonstrably tiny and safe.

Use a stable checkpoint/partition strategy.

---

# 25. Stable traversal

Prefer a stable ordering key/range for large mutable tables.

Avoid unsafe OFFSET traversal when concurrent mutation can cause skip/duplicate.

---

# 26. BE-OPS-DATA-013 — Traversal key is immutable enough for the backfill contract

Common candidates:

```text
stable primary ID
creation sequence
explicit partition key
```

Choose based on actual schema.

Do not use a frequently changing business sort field.

---

# 27. Batch size

Batch size balances:

```text
transaction duration
lock pressure
memory
throughput
retry cost
replication/log volume
```

Do not hard-code one universal number in architecture docs.

---

# 28. BE-OPS-DATA-014 — Batch size is bounded and tunable

Large backfill MUST NOT place the entire table in one unbounded transaction by default.

---

# 29. Idempotent backfill

Reprocessing a completed row/batch should either:

```text
no-op safely
or
produce the same target value
```

Do not create duplicate side effects.

---

# 30. BE-OPS-DATA-015 — Backfill target mapping is deterministic

Given the same authoritative old facts:

```text
same target state
```

must result.

Do not use ambient random/current time unless that value is explicitly part of approved migration semantics and recorded deterministically.

---

# 31. Unknown legacy state

Old production data can contain:

```text
null
unexpected enum
invalid historical combination
missing relation
provider mismatch
```

Migration must define policy.

---

# 32. BE-OPS-DATA-016 — Unknown legacy state is not guessed away

Allowed approaches:

```text
explicit approved normalization
quarantine/report
block migration
manual reviewed correction
explicit sentinel with product meaning
```

Do not invent a plausible value solely to satisfy NOT NULL/enum constraints.

---

# 33. Preflight

Before destructive/high-risk migration, gather:

```text
row counts
null/invalid counts
tenant distribution
old-value distribution
duplicate/conflict counts
backlog/consumer usage
```

as applicable.

Preflight validates assumptions before mutation.

---

# 34. BE-OPS-DATA-017 — Migration assumption has executable preflight where practical

If a contraction assumes:

```text
old column no longer used
all rows backfilled
no invalid values remain
```

prove it.

Do not rely on “should be done”.

---

# 35. Constraints

Add constraints after existing state is compatible.

A new strict constraint can fail deployment if old rows violate it.

---

# 36. BE-OPS-DATA-018 — Constraint rollout considers old data and mixed writers

Example sequence:

```text
add compatible field
backfill
deploy compatible writers
verify
add NOT NULL/unique/check
```

when needed.

---

# 37. Unique constraints

Uniqueness scope must match product semantics.

Review:

```text
tenant key
deleted/archived lifecycle
case normalization
provider identity
```

before adding.

---

# 38. BE-OPS-DATA-019 — Unique index is not product-rule discovery

Product/context decides what must be unique and in which scope.

Infrastructure enforces it race-safely.

---

# 39. Index changes

Indexes affect:

```text
query performance
write cost
migration duration
storage
locking
```

Design from real query/tenant access patterns.

---

# 40. BE-OPS-DATA-020 — Index change is tested against target query shape

For critical/high-cardinality path, inspect query plan/performance evidence as appropriate.

Do not add/drop index from intuition alone.

---

# 41. Online/locking impact

Some DDL/index operations can lock large tables or block writes depending on PostgreSQL operation/version.

Migration design should account for production data size and availability.

---

# 42. BE-OPS-DATA-021 — Large-table DDL has lock/availability analysis

Do not assume a fast local empty-db migration will be safe on production cardinality.

---

# 43. RLS change

RLS migration changes security behavior.

Treat it as:

```text
C4 data/schema
+
C6 security
```

as applicable.

---

# 44. BE-OPS-DATA-022 — RLS change proves allowed and denied tenants

At minimum:

```text
tenant A access
tenant B denial
```

plus bootstrap/pool/background paths affected by the policy.

---

# 45. RLS policy deployment

Policy SQL/version must align with:

```text
table/schema
session context variables
indexes
runtime identity
```

Do not update one side in isolation.

---

# 46. BE-OPS-DATA-023 — RLS policy and session-context rollout are compatible

A new policy requiring a new session variable MUST NOT deploy before all active runtimes can set it safely unless a compatibility/default plan exists.

---

# 47. Apply RLS policies

Current repository exposes explicit:

```text
--rls-apply
make db-rls
```

operation.

This allows policy application to be separated from ordinary API runtime.

---

# 48. BE-OPS-DATA-024 — RLS administration uses least privilege

Normal steady-state runtime SHOULD NOT require broad policy/DDL privilege if an explicit operational identity can perform it.

---

# 49. Converters

Persisted value converters can be migration contracts.

Changing converter may reinterpret old bytes/strings.

---

# 50. BE-OPS-DATA-025 — Converter change verifies round-trip old and new data

Test:

```text
old persisted representation
→ new reader
→ correct Domain value
```

before removing compatibility.

---

# 51. Enum/string discriminator

Persisted strings can outlive CLR names.

Rename/refactor does not automatically mean database value should change.

---

# 52. BE-OPS-DATA-026 — Persisted identity changes intentionally

For:

```text
status strings
type discriminator
event name
provider type
feature code
```

inventory old rows/readers before rename.

---

# 53. JSON persisted data

JSON columns are schema-bearing even without SQL columns for each field.

Define:

```text
version
reader compatibility
writer version
unknown fields
migration/lazy upgrade
index/query needs
```

---

# 54. BE-OPS-DATA-027 — Persisted JSON version is explicit when incompatible evolution exists

Do not assume serializer can forever deserialize every historical shape automatically.

---

# 55. Lazy migration

Some JSON/derived data can migrate on read/write.

Use only when:

```text
bounded
idempotent
observable
old values remain readable
```

and hot-path latency is acceptable.

---

# 56. BE-OPS-DATA-028 — Lazy migration does not create hidden unbounded write storm

A new deployment reading many old rows MUST NOT unexpectedly rewrite an entire dataset without capacity planning.

---

# 57. Data ownership migration

Moving a writable fact from context A to context B is more than table migration.

It requires:

```text
semantic owner transition
read/write authority
events/contracts
backfill
old/new consumer
cleanup
```

---

# 58. BE-OPS-DATA-029 — Ownership migration declares owner per phase

Do not let A and B both become permanent writers.

Use migration plan and context architecture.

---

# 59. Cross-context FK

A shared-database FK can enforce integrity.

It can also create extraction coupling.

Review whether the relation is:

```text
aggregate/internal
same-context
cross-context reference
historical snapshot
```

before adding cascade/strict dependency.

---

# 60. BE-OPS-DATA-030 — Cross-context FK does not imply cross-context write authority

A reference can exist while mutations still route to the semantic owner.

---

# 61. Delete behavior

Physical delete/cascade/restrict must follow product lifecycle.

Do not cascade away:

```text
audit
financial record
foreign-context history
provider reconciliation evidence
```

without explicit policy.

---

# 62. BE-OPS-DATA-031 — Destructive data change requires explicit semantic approval

`DROP`, delete/backfill overwrite, irreversible normalization, retention purge are not routine cleanup.

Classify and approve according to blast radius.

---

# 63. Soft deletion

A logical delete/tombstone base/filter does not answer retention/physical deletion policy.

Migration/cleanup must distinguish:

```text
product deleted
physically retained
eligible for purge
```

---

# 64. BE-OPS-DATA-032 — Product deletion and physical purge are separate transitions

Do not physically delete solely because `IsDeleted=true` unless retention/security/product policy says the purge condition is satisfied.

---

# 65. Retention

Retention can be owned by:

```text
Product
Security/privacy
Billing/legal/audit
Operations
```

depending on data.

Exact period is not invented by Infrastructure.

---

# 66. BE-OPS-DATA-033 — Retention job has an authoritative policy source

A cleanup script MUST NOT use an arbitrary age threshold without owner/reference.

---

# 67. Data repair

Repair corrects production state that violates the approved semantic model.

Repair is not ordinary product write path.

---

# 68. BE-OPS-DATA-034 — Repair is deterministic and scoped

A repair should identify:

```text
affected rows/tenants
source evidence
target value derivation
idempotency
audit/evidence
verification
rollback/forward recovery
```

---

# 69. Repair tool privilege

Repair may need privileged/system DB access.

Use explicit scoped operational identity.

Do not expose broad repair endpoint to normal users.

---

# 70. BE-OPS-DATA-035 — Repair privilege expires with the repair workflow

Do not leave permanent hidden bypass code because one incident needed it.

If reusable tooling remains, its authorization/audit are explicit.

---

# 71. Seed versus migration

Seed creates bootstrap/demo/reference data according to its purpose.

Migration changes existing schema/data meaning.

Do not use seed to compensate for missing migration.

---

# 72. BE-OPS-DATA-036 — Migration does not depend on development seed ordering

Production data must upgrade correctly without running development seed.

---

# 73. Migration and outbox

Schema/data changes can affect:

```text
outbox payload
consumer dedup
ordering cursor
dead-letter
replay
```

Treat old backlog as deployed data/consumer state.

---

# 74. BE-OPS-DATA-037 — Persisted delivery state migration preserves Platform identities

Do not drop/rename:

```text
message ID
consumer ID
ordering key
idempotency key
```

without Platform compatibility analysis.

---

# 75. Event backlog

A new schema/domain change can deploy while old event payloads remain queued.

New consumers must:

```text
understand old event
or
drain/migrate backlog before cutover
```

---

# 76. BE-OPS-DATA-038 — Old queued payload is migration input

Do not verify only database rows while ignoring messages that will recreate/update derived state later.

---

# 77. Cache/search/projection migration

Derived state may need:

```text
invalidate
version key
rebuild
dual index
backfill
```

when source schema/contract changes.

Derived state is not source truth, so prefer rebuild when feasible.

---

# 78. BE-OPS-DATA-039 — Derived-state migration never becomes source authority

If cache/search rebuild disagrees with source:

```text
source wins
```

unless the product explicitly classifies historical snapshot as authoritative evidence.

---

# 79. Object storage

DB metadata migration can affect object keys/providers.

Inventory actual durable objects before changing key/path/provider mapping.

---

# 80. BE-OPS-DATA-040 — DB migration and external object migration reconcile both sides

Do not update metadata to a new object location until the object exists/verified according to cutover plan.

Handle orphan/partial copy.

---

# 81. Provider state

Payment/OAuth/integration provider state can outlive local DB representation.

Migration may require:

```text
provider ID mapping
reconciliation
dual connection support
webhook compatibility
```

---

# 82. BE-OPS-DATA-041 — External provider reality is part of migration verification

Do not declare complete based only on local rows if external state is authoritative for part of the workflow.

---

# 83. Rollback

Rollback can mean separate things:

```text
binary rollback
schema rollback
data rollback
event/backlog rollback
provider rollback
```

Do not write one generic “rollback” line.

---

# 84. BE-OPS-DATA-042 — Rollback is assessed per durable side effect

Some migrations are forward-only after:

```text
new data written
provider side effect
old column dropped
event emitted
```

Plan forward recovery when rollback is unsafe.

---

# 85. Forward recovery

Forward recovery repairs/continues from the actual current state.

It is first-class.

Do not force destructive reversal when a safe compatible forward fix is better.

---

# 86. BE-OPS-DATA-043 — Recovery starts from observed reality

Before repair:

```text
what schema is applied?
which batches completed?
what old/new writers ran?
what provider effects occurred?
what backlog remains?
```

Do not assume the deployment failed atomically.

---

# 87. Migration observability

A material migration/backfill should expose:

```text
current phase
processed count
remaining estimate/count where reliable
invalid/quarantined count
failure count
oldest/stuck batch
tenant partition
```

without sensitive data.

---

# 88. BE-OPS-DATA-044 — Completion is measurable

“Job finished” is not enough.

Prove target invariants/preflight counts show no remaining old state requiring migration.

---

# 89. Migration audit/evidence

Keep enough evidence to reconstruct:

```text
migration version
release/SHA
execution time
environment
result
backfill checkpoint
repair actions
```

according to operations/compliance needs.

Do not use docs as an append-only execution log.

---

# 90. BE-OPS-DATA-045 — Execution evidence is operational artifact, not architecture authority

Permanent rule belongs in canonical docs/ADR.

One migration's progress report can be retired after completion.

---

# 91. Schema drift in CI

Tests/gates may verify migrations/model consistency.

Do not rely solely on production startup to reveal drift.

Current backend testing architecture treats migration smoke/pending-model proof as important persistence evidence.

---

# 92. BE-OPS-DATA-046 — Migration drift is detected before release when practical

A PR adding mapping changes should fail before deploy if required migration is missing.

---

# 93. Migration test environments

Use real PostgreSQL for:

```text
DDL
RLS
Npgsql conversions
indexes/constraints
```

InMemory/SQLite cannot certify these.

---

# 94. BE-OPS-DATA-047 — Persistence migration proof is provider-realistic

A test passing on a different provider is not proof for PostgreSQL DDL/RLS behavior.

---

# 95. Representative old data

Migration fixtures should include cases relevant to blast radius:

```text
normal row
null/edge row
invalid historical row
multiple tenants
large/batch boundary
already-migrated row
```

as applicable.

---

# 96. BE-OPS-DATA-048 — Fixture scope follows risk

Do not create enormous fixtures by ceremony.

Do include the edge class that can corrupt/lose production data.

---

# 97. Multi-tenant backfill

A backfill must preserve tenant ownership.

It may process per tenant/partition for safety/fairness.

Do not move values between tenants through joins missing tenant predicates.

---

# 98. BE-OPS-DATA-049 — Backfill verifies tenant invariants

For cross-tenant sensitive change:

```text
source tenant == target tenant
foreign reference absent/denied
```

must be proven.

---

# 99. Concurrency with live writers

Online backfill can race with writes.

Use one of:

```text
dual write
version/checkpoint
compare-and-set
quiesce specific path
repeat reconciliation
```

according to contract.

---

# 100. BE-OPS-DATA-050 — Backfill/live-write race has an explicit resolution

Do not assume:

```text
backfill is fast enough
```

on production data.

---

# 101. Read cutover

Before switching reads to new representation:

```text
new path complete enough
new reader compatible
metrics/error known
fallback/recovery known
```

Optionally shadow/compare if appropriate.

---

# 102. BE-OPS-DATA-051 — Read cutover does not silently mix conflicting sources

If fallback exists, precedence and mismatch behavior are deterministic and observable.

---

# 103. Write-authority cutover

Write cutover is the moment semantic authority changes storage/path.

It must be explicit.

Do not leave indefinite dual primary writers.

---

# 104. BE-OPS-DATA-052 — Write cutover has one authoritative writer

Compatibility writer/copy may remain temporarily.

Only one path defines accepted state.

---

# 105. Contract/removal

After cutover/stabilization, remove old representation only when:

```text
old reads zero
old writes zero
backfill complete
old binaries below support floor
old backlog handled
rollback policy updated
```

---

# 106. BE-OPS-DATA-053 — Cleanup is part of migration completion

A migration is not globally complete while mandatory old-path compatibility remains indefinitely.

Track removal condition.

---

# 107. Destructive approval

High-risk deletion/repurpose/financial data change may require explicit approval beyond normal code review.

Use repository change classification/DoD.

---

# 108. BE-OPS-DATA-054 — Destructive operation is never hidden inside innocuous migration name

The review artifact should state:

```text
what can be lost
why safe
counts
recovery
approval
```

clearly.

---

# 109. Data type change

Changing:

```text
string → enum
int → bigint
timestamp semantics
decimal precision
JSON → normalized table
```

can be destructive/compatibility-sensitive.

Review conversion and old values.

---

# 110. BE-OPS-DATA-055 — Type conversion proves every supported old value

Do not rely on cast success for a small dev dataset.

Preflight production-like value distribution.

---

# 111. Nullability change

Making nullable → required needs:

```text
backfill
writer compatibility
constraint timing
unknown-value policy
```

Do not add arbitrary default.

---

# 112. BE-OPS-DATA-056 — Missing value receives product-approved meaning or blocks transition

Database default is not automatically business meaning.

---

# 113. Column rename

A direct rename can break old binary.

During mixed-version rollout consider:

```text
new column + compatibility
view/alias
staged reader/writer
```

depending on deployment model.

---

# 114. BE-OPS-DATA-057 — Rename is compatibility work, not cosmetic refactor

Persisted names can be consumed by:

```text
old binary
SQL/report
migration
projection
```

Inventory before change.

---

# 115. Table split/merge

Table normalization changes can cross aggregate/context ownership.

Define:

```text
new owner
identity mapping
read/write precedence
FK
backfill
cutover
```

Do not infer semantics from normalized schema alone.

---

# 116. BE-OPS-DATA-058 — Table topology follows semantic owner after migration

The target should make ownership clearer, not create shared writable tables.

---

# 117. Environment sequencing

Migration execution order relative to:

```text
API
workers
frontend/mobile compatibility
broker consumers
```

matters.

Repository release/migration policy owns deployment sequencing.

---

# 118. BE-OPS-DATA-059 — Every deployed stage is compatible

Do not require impossible simultaneous restart of:

```text
API
workers
mobile clients
queued messages
provider webhooks
```

unless the environment truly guarantees it.

---

# 119. Failure during migration

A migration can fail:

```text
before DDL
mid-DDL
after schema
mid-backfill
after cutover
during cleanup
```

Recovery differs by phase.

---

# 120. BE-OPS-DATA-060 — Recovery procedure is phase-aware

The operator must know:

```text
which phase is current
which authority is active
what may be retried
what must be reconciled
```

before acting.

---

# 121. Database backup

Backup can reduce recovery risk for destructive changes.

It is not the whole migration plan.

Restore may reintroduce old schema/data while provider/events have advanced.

---

# 122. BE-OPS-DATA-061 — Backup is evidence/mechanism, not automatic rollback

A restore decision must reconcile:

```text
outbox
dedup/order
provider
object storage
cache/search
```

with the restored DB.

---

# 123. Migration performance

Measure large migrations/backfills on representative data.

Review:

```text
locks
transaction log/WAL
CPU
IO
DB connections
replication
runtime workload
```

as applicable.

---

# 124. BE-OPS-DATA-062 — Production migration workload has capacity bounds

Throttle/batch if needed.

Do not allow migration to starve customer traffic.

---

# 125. Tenant fairness

Large tenant backfill can monopolize resources.

Partition/fair scheduling may be appropriate.

Exact mechanism depends on workload.

---

# 126. BE-OPS-DATA-063 — One tenant does not block all migration progress unnecessarily

Preserve safety while allowing bounded independent progress where data model permits.

---

# 127. Data validation after migration

Verify semantic invariants, not only row count.

Examples:

```text
all target values valid
tenant links correct
unique constraints satisfied
old/new value equivalence
RLS still denies foreign tenant
```

---

# 128. BE-OPS-DATA-064 — Post-migration verification checks business meaning

A 100% row-count match can still contain wrong mapped values.

Validate the invariant that motivated the migration.

---

# 129. Application compatibility verification

After schema/backfill, exercise:

```text
new read
new write
old supported read/write if still active
```

through Application/Integration contracts.

---

# 130. BE-OPS-DATA-065 — DB-only verification is insufficient for behavior-changing migration

The application must interpret the new persisted state correctly.

---

# 131. Migration completion evidence

Material migration completion should capture:

```text
phase complete
rows/tenants processed
invalid/quarantined rows
verification queries/tests
old path usage
release/version
cleanup status
```

as an operational artifact.

---

# 132. BE-OPS-DATA-066 — Completion proof precedes destructive cleanup

Do not drop old representation because the backfill process exited successfully.

Prove target state and consumer cutover.

---

# 133. Current CI/testing relationship

Backend testing architecture currently includes migration smoke and production composition/integration proof.

Migration changes should route to those gates plus focused Infrastructure tests.

Exact class/job names may evolve.

---

# 134. BE-OPS-DATA-067 — Required migration gate executes non-zero

A critical migration/RLS filter that finds zero tests is failure, not proof.

---

# 135. Change classification

Common classes:

```text
additive schema/index
→ C4

backfill/type conversion
→ C4

RLS/policy
→ C4 + C6

persisted public/event identity
→ C3 + C4

cross-context ownership
→ C4 + C5

destructive/retention/financial
→ C8 (+ other classes)
```

Modifiers:

```text
DATA_BACKFILL
CROSS_TENANT
ASYNC_BACKLOG
ROLLBACK_UNSAFE
PROVIDER_EXTERNAL
```

---

# 136. Migration-plan trigger

Instantiate the migration template when the change has material:

```text
mixed-version compatibility
backfill
dual read/write
ownership transfer
destructive cleanup
external/provider state
rollback-unsafe effects
long-running data work
```

A simple additive migration may not need a separate full plan if existing policy fully determines execution.

---

# 137. Migration review checklist

```text
[ ] semantic owner
[ ] old state
[ ] target state
[ ] EF mapping
[ ] migration
[ ] model drift
[ ] reader inventory
[ ] writer inventory
[ ] expand/contract
[ ] authority per phase
[ ] backfill
[ ] tenant/RLS
[ ] indexes/constraints
[ ] backlog/events
[ ] cache/search/provider/object state
[ ] rollback/forward recovery
[ ] verification
[ ] cleanup
```

---

# 138. Backfill checklist

```text
[ ] stable traversal key
[ ] bounded batch
[ ] tenant partition
[ ] idempotent mapping
[ ] durable checkpoint
[ ] live-writer race
[ ] invalid old rows
[ ] cancellation/restart
[ ] progress metrics
[ ] completion proof
```

---

# 139. RLS migration checklist

```text
[ ] table owner
[ ] policy
[ ] session variables
[ ] old/new runtime compatibility
[ ] bootstrap
[ ] indexes
[ ] tenant A allowed
[ ] tenant B denied
[ ] pool reuse
[ ] worker/system path
```

---

# 140. Destructive checklist

```text
[ ] exact data removed/repurposed
[ ] preflight count
[ ] business approval
[ ] backup/recovery role
[ ] old readers/writers zero
[ ] old backlog handled
[ ] forward recovery
[ ] audit/evidence
```

---

# 141. Stop conditions

Stop migration/data-change implementation if:

- business meaning of target state is unresolved;
- EF model drift is being suppressed;
- already-applied migration history is being rewritten casually;
- existing production data is ignored;
- unknown legacy values are assigned guessed defaults;
- dual read/write has no declared authority;
- backfill has no stable checkpoint/resume strategy;
- RLS change lacks cross-tenant proof;
- large-table DDL has no lock/capacity analysis;
- persisted discriminator/event identity is renamed as “just refactor”;
- old queued messages/provider/object state are ignored;
- rollback is assumed safe without durable-effect analysis;
- destructive cleanup has no objective completion proof.

---

# 142. Executable evidence

Primary source:

```text
backend/src/Notrelix.Infrastructure/Data/ApplicationDbContext*
backend/src/Notrelix.Infrastructure/Data/Configurations
backend/src/Notrelix.Infrastructure/Data/Converters
backend/src/Notrelix.Infrastructure/Data/Migrations
backend/src/Notrelix.Infrastructure/Data/Rls
```

Primary tests:

```text
backend/tests/Notrelix.Infrastructure.Tests
backend/tests/Notrelix.Integration.Tests
```

Current local operations are routed through:

```text
make db-migrate
make db-seed
make db-init
make db-rls
```

---

# 143. Related canonical owners

Backend:

```text
../architecture/domain-modeling.md
../architecture/application-model.md
../architecture/infrastructure-and-data.md
../architecture/platform-and-messaging.md
../architecture/security-tenancy-authorization.md
../architecture/testing-and-quality-gates.md
configuration-and-runtime.md
```

Repository:

```text
../../../docs/delivery/migration-policy.md
../../../docs/delivery/release-and-rollout.md
../../../docs/operations/recovery-and-data-safety.md
../../../docs/operations/incident-readiness.md
../../../docs/templates/migration-plan-template.md
```

---

# 144. Non-responsibilities

This document does not decide:

```text
product lifecycle semantics
public API compatibility by itself
deployment-provider implementation
retention period values
financial/legal policy
frontend local-state migration
```

Those belong to their owners.

---

# 145. Final data-change rule

A healthy durable migration can be stated as:

```text
approved old meaning
        ↓
compatible expansion
        ↓
old/new readers and writers remain valid
        ↓
bounded deterministic tenant-safe backfill
        ↓
explicit authority cutover
        ↓
semantic + PostgreSQL/RLS verification
        ↓
objective old-path removal proof
        ↓
destructive contraction
```

with:

```text
no hidden model drift
no guessed legacy value
no permanent dual truth
no empty-DB-only proof
no cross-tenant corruption
no ignored backlog/provider state
no fake rollback
no cleanup-before-verification
```

The objective is not merely to get the latest EF model onto the database.

The objective is to move **real durable production state** from one valid semantic contract to another while every deployed phase remains secure, recoverable, and understandable.
