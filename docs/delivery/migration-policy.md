---
document_id: DEL-MIGRATION
document_type: delivery-policy
status: active
owner: engineering-delivery
applies_to:
  - repository
  - backend
  - database
  - persisted-contracts
  - data-backfills
  - ownership-migrations
  - projections
  - message-backlogs
evidence:
  - docs/delivery/change-classification.md
  - docs/delivery/contract-first-delivery.md
  - docs/delivery/definition-of-done.md
  - docs/delivery/release-and-rollout.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/quality/testing-strategy.md
  - docs/quality/security-quality-standard.md
  - docs/quality/performance-and-scalability.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/src/Notrelix.Infrastructure/
  - backend/tests/Notrelix.Infrastructure.Tests/
  - backend/tests/Notrelix.Integration.Tests/
review_on:
  - schema-migration-policy-change
  - data-backfill-policy-change
  - persisted-identity-change
  - data-ownership-migration
  - dual-read-write-policy-change
  - destructive-migration-policy-change
  - migration-testing-policy-change
---

# Migration Policy

> **A migration changes durable representation or authority while production continues to contain old data, old code, old messages, or old consumers.**
>
> Migration is complete only when the target state is authoritative, old and new compatibility obligations are resolved, completion is objectively proven, and obsolete paths can be removed safely.

This document owns repository-wide migration policy for durable data and persisted compatibility.

Infrastructure owns EF/PostgreSQL implementation mechanics.

Product/system/context docs own semantic meaning and data ownership.

Release policy owns when migration stages are deployed.

# 1. Migration classes

Migrations include:

```text
schema migration
data backfill/transformation
persisted enum/discriminator/key migration
index/constraint/RLS migration
data-ownership/context migration
projection/cache/search rebuild
message/event backlog compatibility migration
provider mapping migration
contract-linked persisted configuration migration
```

Not every deployment requires a migration.

# 2. DEL-MIG-001 — Migration starts from real old state

Before implementation identify:

```text
old schema/data
old persisted identities
old writers/readers
old events/messages
old clients/workers
data cardinality
invalid/legacy edge cases
tenant distribution
```

Do not design only from the clean target model.

# 3. Current versus target state

A migration plan explicitly states:

```text
CURRENT
TARGET
TRANSITION
```

Current source debt does not become target architecture automatically.

# 4. DEL-MIG-002 — Migration has one declared target authority

At every transition stage declare which representation/context is authoritative.

A temporary duplicate copy does not mean both are equally authoritative.

# 5. Preferred lifecycle

Where compatibility requires staged evolution:

```text
Expand
→ Compatible Code
→ Backfill
→ Switch
→ Verify
→ Contract
```

# 6. DEL-MIG-003 — Expand precedes destructive contraction when mixed versions exist

Add compatible representation before removing old representation unless true atomic cutover is proven.

# 7. Expand

Expand can include:

```text
new nullable column/table
new index
new event/config version
new read model
new owner endpoint
dual-readable field
```

Expansion should keep old code valid.

# 8. DEL-MIG-004 — Expansion does not change authority accidentally

Adding a new column/table/projection does not make it authoritative until the migration plan's cutover stage.

# 9. Compatible code

Deploy code that can operate while old/new representations coexist.

Strategies include:

```text
read old + new
write new while preserving old compatibility
adapter
version-aware reader
shadow comparison
```

# 10. DEL-MIG-005 — Compatibility code has bounded lifetime

Declare:

```text
why it exists
migration stage
owner
removal condition
```

Do not normalize migration adapters into permanent architecture silently.

# 11. Backfill

Backfill moves/transforms existing durable state.

# 12. DEL-MIG-006 — Backfill is idempotent or safely resumable

A failed/restarted batch must not:

- duplicate records;
- corrupt transformed data;
- skip unknown rows silently.

Use stable identity/checkpoint.

# 13. Backfill key

Choose deterministic traversal such as:

```text
primary key cursor
tenant + key cursor
created/id ordered keyset
explicit migration ledger
```

Avoid unstable offset pagination on concurrently changing large tables when it can skip/duplicate.

# 14. DEL-MIG-007 — Backfill is bounded

Define:

```text
batch size
concurrency
transaction size
retry
pause/resume
resource budget
```

Do not hold one transaction over an entire large tenant/table.

# 15. Tenant isolation

Backfill preserves Account/Workspace/resource isolation.

# 16. DEL-MIG-008 — Migration is tenant-safe production code

A migration/backfill MUST NOT bypass tenant/security boundaries casually because it is “internal”.

Privileged migration paths are explicit, scoped, reviewed, and observable.

# 17. Cross-tenant administration

When privileged global migration is necessary, code still partitions/logically scopes work by tenant and prevents accidental relationship crossing.

# 18. Existing invalid data

Legacy rows can violate new assumptions.

Plan policy:

```text
normalize deterministically
quarantine/report
explicit fallback
manual remediation
reject migration
```

# 19. DEL-MIG-009 — Invalid legacy data is not silently invented

Do not guess business values to satisfy a new non-null constraint unless canonical semantics define the default.

# 20. Defaults

Schema default can assist compatibility.

A database default does not automatically become product semantic default.

# 21. DEL-MIG-010 — Migration default and product default are distinguished

Document whether the value is:

```text
true semantic default
temporary migration sentinel
unknown
derived from existing facts
```

# 22. Nullability transition

Typical non-null evolution:

```text
add nullable/compatible
→ write new value
→ backfill
→ verify no null
→ enforce non-null
```

# 23. DEL-MIG-011 — Constraint enforcement follows data proof

Do not add strict constraint before existing data/writers satisfy it unless deployment is truly atomic and proven.

# 24. Rename

A persisted rename can involve:

- column;
- enum text;
- discriminator;
- event name;
- feature code;
- metric key;
- JSON property.

# 25. DEL-MIG-012 — Persisted rename is copy/compatibility migration when old readers exist

Do not assume a source-code rename migrates durable data.

# 26. Enum/status migration

Changing status values requires mapping old values to canonical new semantics.

Ambiguous merge/split needs product decision.

# 27. DEL-MIG-013 — Status migration preserves lifecycle meaning

Do not map by text similarity only.

Example:

```text
SoftDeleted
→ Deleted
```

still requires checking deletion lifecycle/events/queries, not only string replacement.

# 28. Discriminator/config JSON

Persisted polymorphic config needs explicit schema version when evolution can become incompatible.

# 29. DEL-MIG-014 — Flexible persisted JSON is versioned when compatibility requires it

Reader handles supported old versions or migration upgrades them before old reader removal.

# 30. RLS migration

RLS policy/schema changes are security-sensitive C4+C6 work.

# 31. DEL-MIG-015 — RLS migration is tested with real tenant data

Review:

```text
new table/policy
session context
foreign-tenant denial
background path
index/selectivity
migration bootstrap
```

# 32. RLS bootstrap

New tables must not enter a production window with tenant data but missing required RLS policy when architecture requires it.

# 33. DEL-MIG-016 — Security policy and tenant table become valid in the same safe stage

Migration ordering must not temporarily expose cross-tenant rows.

# 34. Index migration

Indexes can be large operational changes.

Review:

```text
build duration
locks
write amplification
storage
concurrent deployment
provider/PostgreSQL options
```

# 35. DEL-MIG-017 — Large index change has an operational plan

Do not treat it as metadata-only if it can block/slow production.

# 36. Constraint migration

Unique/FK/check constraints require existing-data validation and concurrent-writer compatibility.

# 37. DEL-MIG-018 — Constraint migration anticipates concurrent writes

A backfill can complete, then old writer can create invalid data before constraint/switch unless rollout sequence prevents it.

# 38. Dual read

Dual read can support migration:

```text
prefer new
fallback old
compare old/new
```

# 39. DEL-MIG-019 — Dual read has deterministic precedence

When values disagree, one source wins or the request fails/reports inconsistency according to migration phase.

Do not select whichever value is non-null without semantics.

# 40. Dual write

Dual write may be required temporarily for independently deployed readers.

# 41. DEL-MIG-020 — Dual write is not dual authority

Declare authoritative write source.

Secondary write is compatibility replication and must be:

- idempotent;
- observable;
- reconciliation-capable.

# 42. Dual-write failure

If primary write succeeds and compatibility write fails, plan defines:

```text
transactional atomicity
outbox/retry
reconciliation
block operation
```

according to ownership/consistency.

# 43. DEL-MIG-021 — Partial dual-write failure is modeled explicitly

Do not silently leave two representations diverged with no repair mechanism.

# 44. Shadow comparison

Read both old/new and compare to build confidence without changing user result.

# 45. DEL-MIG-022 — Shadow comparison is side-effect free

Comparison path does not double:

- provider calls;
- events;
- usage charges;
- mutations.

# 46. Authority switch

Switch is the explicit point where new representation/owner becomes authoritative.

# 47. DEL-MIG-023 — Cutover condition is objective

Examples:

```text
backfill 100% verified
new writer live
old reader compatibility proven
error/mismatch below approved threshold
consumer migration complete
```

Exact thresholds belong to migration/release plan.

# 48. Cutover mechanism

Can be:

```text
code deployment
feature flag
config switch
routing change
versioned consumer switch
```

The mechanism does not define authority; the migration plan does.

# 49. DEL-MIG-024 — Cutover is observable

Operators can determine:

- current authority;
- current phase;
- mismatch/error;
- rollback/forward options.

# 50. Contract/removal

Only after switch verification remove:

```text
old column/table
fallback read
dual write
compat adapter
old event/config reader
migration flag
```

# 51. DEL-MIG-025 — Old path removal requires non-use/completion proof

Search alone may be insufficient.

Use as applicable:

```text
DB verification
telemetry
consumer version floor
queue drain
flag cohort
old-write count
```

# 52. Schema migration implementation

Backend persistence changes belong to Infrastructure.

The EF model and migration chain/current generated schema are implementation evidence.

# 53. DEL-MIG-026 — Persistence schema changes through reviewed migrations

Do not hand-edit production schema as the normal release path.

Emergency repair, if necessary, becomes a governed reproducible repair/migration afterward.

# 54. Model drift

EF/current schema mismatch is a defect requiring migration or intentional model correction.

# 55. DEL-MIG-027 — Model drift is not suppressed to make startup pass

Do not silence pending-model-change/schema drift warnings as a migration strategy.

# 56. Migration naming/history

Migration files should explain intent through meaningful naming and remain immutable after production application except under an explicitly safe development-only policy.

# 57. DEL-MIG-028 — Applied migration history is append-oriented

Do not rewrite already-applied production migration meaning casually.

Use a new corrective migration.

# 58. Empty-database path

A clean database must still migrate/create correctly.

But this is only one proof.

# 59. DEL-MIG-029 — Existing-data upgrade is separate required proof

Test representative previous schema/data state for risky migrations.

# 60. Migration testing

Depending on risk:

```text
migration generation/build
clean apply
upgrade apply
data transformation assertion
RLS
rollback/forward behavior
Application startup
production composition
```

# 61. DEL-MIG-030 — Provider-specific persistence is tested on the real provider class

PostgreSQL/RLS migration behavior is not proven by SQLite.

# 62. CI

Current backend CI includes migration smoke/integration evidence.

Critical migration test must execute non-zero work.

# 63. Data backfill execution

Large production backfill may run as:

```text
deployment job
one-time worker
migration-specific command
controlled application task
```

Avoid embedding huge data transforms in blocking startup if duration/risk is high.

# 64. DEL-MIG-031 — Long backfill is decoupled from startup when necessary

Startup should not be held hostage by an hours-long recoverable migration workload.

Schema prerequisites can remain normal migrations; large data movement can be staged.

# 65. Backfill observability

Expose:

```text
total/estimated scope
processed
succeeded
failed/quarantined
checkpoint
rate
remaining
```

without leaking sensitive row data.

# 66. DEL-MIG-032 — Backfill completion is measurable

“Job exited successfully” is insufficient if skipped/filtered records can exist.

Use verification query/checksum/count/invariant appropriate to semantics.

# 67. Backfill retries

Retry batch/item idempotently.

Avoid retry storm against DB/provider.

# 68. Performance

Migration competes with production traffic.

Use bounded:

```text
batch
concurrency
locks
IO
CPU
transaction time
```

# 69. DEL-MIG-033 — Migration protects production workload

A theoretically correct backfill that exhausts DB capacity is not safe.

Release plan can pause/throttle.

# 70. Locking

DDL can acquire locks.

Assess table size/operation/provider behavior for high-risk schema change.

# 71. Data ownership migration

Moving authoritative data from one bounded context to another is a semantic migration.

# 72. DEL-MIG-034 — Ownership migration changes contracts before storage convenience

Define:

```text
new owner
old owner
new write contract
read consumers
events
deletion/lifecycle
authorization
```

before moving tables/classes.

# 73. Ownership transition phases

Possible:

```text
new owner reads legacy data through adapter
backfill to new owner store
new owner becomes write authority
old owner reads through contract
old store retired
```

# 74. DEL-MIG-035 — Foreign context never mutates new owner storage directly as transitional shortcut

Use explicit migration/compatibility port/tooling.

Do not preserve old coupling as permanent access.

# 75. Cross-context references

When identity remains stable, migrate references carefully.

Do not regenerate IDs unnecessarily if existing contracts/events/links depend on them.

# 76. DEL-MIG-036 — Stable logical identity is preserved unless migration explicitly changes identity

If identity changes, provide mapping and consumer migration.

# 77. Projection migration

Derived projection can often be rebuilt.

# 78. DEL-MIG-037 — Rebuildable projection prefers rebuild over complex dual-authority migration

If source history/current query can reconstruct it safely:

```text
create new projection
→ backfill/rebuild
→ compare
→ switch reader
→ remove old
```

# 79. Search migration

Search index version can use parallel index + reindex + alias/switch when infrastructure supports it.

Search remains derived.

# 80. Cache migration

Cache normally should be versioned/invalidated rather than data-migrated unless warmup is operationally required.

# 81. DEL-MIG-038 — Cache compatibility never dictates business schema authority

A cache can be dropped/rebuilt when safe.

Do not contort source data model to preserve stale cache entries.

# 82. Analytics migration

Metric/version/projection changes may require:

- new metric version;
- backfill;
- snapshot compatibility;
- historical break marker.

# 83. Message/event backlog migration

Old messages can survive producer deployment.

# 84. DEL-MIG-039 — Backlog compatibility is a migration obligation

Before removing old message/event reader:

```text
drain
version-route
transform/replay
or prove no old backlog remains
```

# 85. Dead-letter/replay

DLQ/replay archives can contain old format beyond normal queue age.

Retention/replay policy determines compatibility horizon.

# 86. Provider mapping migration

Provider IDs/config/cursors may require migration when adapter/provider API changes.

# 87. DEL-MIG-040 — Provider mapping migration preserves external and Notrelix identities

Do not silently relink a Connection/resource to a different provider account/object.

# 88. Secret migration

Secret rotation/move changes references while preserving logical Connection where appropriate.

Raw secret data is handled only by approved secret infrastructure.

# 89. Destructive migration

Examples:

```text
drop column/table
purge data
shorten retention
rewrite IDs without mapping
provider-side deletion
```

# 90. DEL-MIG-041 — Destructive migration requires explicit irreversibility analysis

State:

```text
what is destroyed
backup/export availability
rollback impossibility
forward recovery
authorization/approval
retention/legal impact
```

# 91. Backup

Backup is not a generic substitute for safe migration.

Restore time/data-loss window can make it unsuitable as primary rollback mechanism.

# 92. DEL-MIG-042 — Backup claim includes restore feasibility

If backup is cited as recovery, know:

```text
what is backed up
restore scope
RPO/RTO or equivalent operational expectation
how to avoid overwriting newer valid data
```

Operations owns concrete targets/runbook.

# 93. Partial failure

Every migration stage answers:

```text
what can fail?
what committed already?
can rerun?
what checkpoint?
what user-visible impact?
what repair path?
```

# 94. DEL-MIG-043 — Migration resumes from durable checkpoint

Do not depend on process memory to know which production rows were migrated.

# 95. Validation queries

Post-migration validation should assert semantic invariants, not only row count.

Examples:

```text
no unmapped old status
all new required IDs valid
no cross-tenant references
old/new calculated values match
no duplicate mappings
```

# 96. DEL-MIG-044 — Completion proof matches the migrated meaning

A matching total row count may still hide incorrect field mappings/tenant relationships.

# 97. Mismatch handling

During dual/shadow periods, mismatch can:

- stop cutover;
- quarantine;
- trigger repair;
- surface metric.

Do not auto-resolve ambiguous semantic mismatch.

# 98. User-visible transition

If migration can temporarily produce:

- read-only;
- pending;
- unavailable capability;

the product experience communicates it honestly.

# 99. DEL-MIG-045 — Migration state does not masquerade as final success

A pending/backfilling external effect/resource state remains visible as such.

# 100. Migration artifact

A material migration should have an execution plan/runbook/task record with:

```text
owner
current/target
classification/modifiers
stages
commands/jobs
verification
rollback/forward recovery
completion
cleanup
```

This temporary plan is not permanent architecture authority.

# 101. DEL-MIG-046 — Migration plan is delivery evidence, not new canonical architecture

Once completed, durable knowledge moves to canonical owners/ADR/runbooks; temporary tracker can be retired.

# 102. Data migration checklist

```text
[ ] current old data sampled/understood
[ ] target semantics
[ ] authoritative owner
[ ] expand step
[ ] old/new reader/writer compatibility
[ ] backfill key/batch/idempotency
[ ] tenant/RLS
[ ] invalid legacy data policy
[ ] performance/locking
[ ] verification
[ ] cutover
[ ] contraction
[ ] recovery
```

# 103. Schema migration checklist

```text
[ ] EF/current model intended
[ ] reviewed migration
[ ] clean apply
[ ] upgrade apply
[ ] old code compatibility
[ ] constraints/indexes
[ ] RLS
[ ] lock/size risk
[ ] startup/composition
[ ] contraction criteria
```

# 104. Ownership migration checklist

```text
[ ] old/new semantic owner
[ ] target contracts
[ ] stable identity/mapping
[ ] authority per stage
[ ] dual read/write precedence
[ ] events/consumers
[ ] auth/tenant
[ ] backfill
[ ] cutover
[ ] old persistence/access removal
```

# 105. Backlog migration checklist

```text
[ ] old message/event versions
[ ] queue/backlog age
[ ] DLQ/replay retention
[ ] old/new consumers
[ ] version compatibility
[ ] drain/transform strategy
[ ] removal proof
```

# 106. Completion checklist

```text
[ ] target authority active
[ ] all required data migrated
[ ] invariants verified
[ ] mismatch resolved
[ ] old writers stopped
[ ] old readers/consumers migrated
[ ] backlog handled
[ ] telemetry healthy
[ ] rollback/forward state understood
[ ] old path eligible for removal
```

# 107. Current backend alignment

Current backend persistence documentation states:

```text
EF Core/PostgreSQL migrations belong to Infrastructure
persistence evidence is EF model + migration chain/current generated schema
PostgreSQL schema changes through reviewed migrations
RLS complements Application authorization
migration changes require review/integration proof
model drift should not be suppressed
```

Current CI includes Infrastructure/Integration tests and migration smoke evidence.

# 108. Stop conditions

Stop migration if:

- target authority is not declared;
- design assumes empty production database;
- a backfill has no stable checkpoint/idempotency;
- migration globally bypasses tenant safety without governed privileged design;
- ambiguous old data is guessed into new business meaning;
- non-null/unique/FK constraint is enforced before data/writers are ready;
- dual read has no precedence;
- dual write has no single authority or repair path;
- model drift warning is suppressed rather than migrated;
- applied production migration is rewritten to hide correction;
- old message/backlog compatibility is ignored;
- destructive migration cites “we have backups” without restore feasibility;
- completion proof is only “script exited 0”;
- old path is dropped before readers/writers/backlog are proven migrated.

# 109. Related canonical owners

```text
docs/delivery/change-classification.md
docs/delivery/contract-first-delivery.md
docs/delivery/definition-of-done.md
docs/delivery/release-and-rollout.md
docs/architecture/data-ownership-and-consistency.md
docs/quality/testing-strategy.md
docs/quality/security-quality-standard.md
docs/quality/performance-and-scalability.md
backend/docs/architecture/infrastructure-and-data.md
```

# 110. Final migration rule

For every migration, answer:

```text
What durable old state exists today?
What target state/owner is canonical?
Which stage is authoritative now?
Can old/new readers and writers coexist?
How is existing data migrated in bounded idempotent batches?
How are tenant/RLS/security invariants preserved?
How are invalid legacy rows handled?
What proves backfill/cutover completion?
What old messages/clients/providers still depend on old format?
What can be rolled back and what needs forward recovery?
What objective proof permits destructive contraction?
```

The target is:

> **migration as a resumable, tenant-safe, objectively verifiable transfer from one durable representation or authority to another—without dual truth, silent data invention, or premature destruction of the old path.**
