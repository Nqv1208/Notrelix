---
document_id: OPS-RECOVERY-DATA-SAFETY
document_type: operations-standard
status: active
owner: operations
applies_to:
  - production-data
  - database
  - migrations
  - background-processing
  - messaging
  - integrations
  - object-storage
  - projections
  - recovery
evidence:
  - docs/operations/incident-readiness.md
  - docs/operations/observability.md
  - docs/delivery/migration-policy.md
  - docs/delivery/release-and-rollout.md
  - docs/quality/security-quality-standard.md
  - docs/quality/testing-strategy.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/tests/Notrelix.Infrastructure.Tests/
  - backend/tests/Notrelix.Integration.Tests/
review_on:
  - backup-strategy-change
  - restore-strategy-change
  - rpo-or-rto-change
  - persistence-topology-change
  - migration-strategy-change
  - outbox-or-idempotency-change
  - object-storage-change
  - incident-learnings-change
---

# Recovery and Data Safety

> **Recovery restores a valid product state, not merely a reachable database or a green infrastructure dashboard.**
>
> Every recovery action must preserve or deliberately reconcile tenant scope, schema meaning, aggregate invariants, outbox/message state, idempotency, provider side effects, object storage, projections, and historical evidence.

This document is the canonical repository-level owner for production data-recovery and reconciliation semantics.

It does not invent a backup vendor, backup schedule, PITR configuration, RPO, or RTO.

Concrete backup infrastructure, retention, encryption, restore tooling, and approved numerical recovery objectives belong to Infrastructure/Operations implementation and must be separately evidenced.

---

# 1. Recovery model

Canonical recovery flow:

```text
protect
→ assess
→ preserve evidence
→ stop unsafe writers
→ select recovery point/repair strategy
→ restore or repair
→ reconcile external/async state
→ verify product invariants
→ reopen workload gradually
→ record permanent loss/repair
```

---

# 2. OPS-REC-001 — Correctness precedes availability

Do not restore, replay, or reopen writes merely to recover uptime if:

- tenant isolation is uncertain;
- schema does not match running code;
- duplicate provider effects are possible;
- recovered state violates product invariants;
- outbox/dedup state is inconsistent.

---

# 3. Recovery scope

Recovery can apply to:

```text
one record/resource
one Workspace/Account
one table
one bounded-context dataset
one database
one object prefix/bucket subset
one message range
one projection/index
one provider mapping
whole environment
```

Use the narrowest recovery scope that safely restores correctness.

---

# 4. OPS-REC-002 — Recovery scope is explicit

Before acting, identify:

```text
affected capability
affected tenants/resources
time window
first known bad change
last known valid state
external side effects
dependencies
```

Avoid “restore everything” before blast radius is known.

---

# 5. Recovery evidence

Preserve as applicable:

```text
release SHA
migration history
database snapshot/PITR point
logs/traces
affected row/resource IDs
outbox rows
consumer dedup/idempotency state
dead-letter/poison state
provider operation IDs
object-storage keys
projection/index version
feature-flag/config state
```

---

# 6. OPS-REC-003 — Evidence is preserved before destructive repair

Do not:

```text
truncate
purge
overwrite
delete poison
clear idempotency
reset migration history
```

before enough evidence exists to understand/reproduce the failure.

---

# 7. Backup

A backup is a recoverable copy of durable state.

It is useful only if:

- it contains the required state;
- integrity is known;
- it can be restored;
- restore procedure is understood;
- required related stores can be reconciled.

---

# 8. OPS-REC-004 — Backup existence is not restore proof

“Backup job succeeded” is insufficient.

Operations must be able to demonstrate an appropriate restore path for critical durable state.

---

# 9. Backup inventory

For each critical durable store, document operationally:

```text
what is backed up
scope
retention
encryption
restore granularity
restore location
verification
owner
```

This file does not hardcode environment-specific values.

---

# 10. Recovery objectives

RPO/RTO or equivalent objectives may be approved by Product/Operations/business requirements.

---

# 11. OPS-REC-005 — RPO/RTO are explicit approved objectives, not invented defaults

Until approved, record:

```text
RPO: TBD
RTO: TBD
```

rather than creating enterprise-looking numbers unsupported by the deployed system.

---

# 12. Recovery-point meaning

A database recovery point can be transactionally valid while still inconsistent with:

- external provider effects;
- delivered events;
- object storage;
- search;
- caches;
- mobile/client-visible actions.

---

# 13. OPS-REC-006 — Recovery point is evaluated across side effects

Before choosing a point, identify what happened after it in:

```text
outbox/messages
providers
email/notifications
billing/payment
object storage
realtime/projections
```

---

# 14. Point-in-time recovery

PITR can be appropriate for broad DB corruption/loss if infrastructure supports it.

It is not automatically safer than targeted repair.

---

# 15. OPS-REC-007 — PITR selection considers replay/reconciliation cost

Rolling the DB backward creates a recovery interval whose external/async effects may already have escaped.

The plan must reconcile them.

---

# 16. Targeted repair

Prefer targeted repair when:

- corruption scope is known;
- correct value can be derived safely;
- broad rollback would lose valid writes;
- external effects make broad rollback more dangerous.

---

# 17. OPS-REC-008 — Targeted repair derives values from authoritative semantics

Do not “fix” rows by guessing plausible values.

Use:

- known source facts;
- deterministic migration rules;
- historical evidence;
- explicit approved correction.

---

# 18. Stop unsafe writers

When data corruption is active:

```text
pause rollout
disable specific mutation
stop consumer
set capability read-only
stop migration/backfill
```

as narrowly as possible.

---

# 19. OPS-REC-009 — Repair is not performed while known bad writer continues

Otherwise repaired rows can immediately become corrupt again.

---

# 20. Schema compatibility

Recovery must establish:

```text
database schema
migration history
running binary
stored data representation
```

are compatible.

---

# 21. OPS-REC-010 — Reachable database with wrong schema is not recovered

Validate migration chain/current schema before reopening normal writes.

---

# 22. Migration history

Applied production migrations are append-oriented evidence.

Do not rewrite migration history to conceal recovery.

---

# 23. OPS-REC-011 — Recovery correction uses new reproducible migration/repair

If production required emergency manual change:

```text
record it
reconcile source/migration
create durable corrective artifact
```

after containment.

---

# 24. EF model drift

Current backend persistence architecture treats EF model + migration chain/current generated schema as persistence evidence and considers suppressed model drift a failure mode.

---

# 25. OPS-REC-012 — Model drift is repaired, not suppressed

A startup warning about pending model changes is not a recovery obstacle to hide.

Determine the intended schema/model and migrate/correct it.

---

# 26. Tenant isolation

Recovery tooling can be privileged, but must preserve tenant relationships.

---

# 27. OPS-REC-013 — Recovery is tenant-safe

Verification includes:

```text
Account/Workspace relationships
RLS policy
foreign-tenant denial
cross-reference integrity
```

for affected data.

---

# 28. RLS after restore

A restored table/data set can exist while:

- policy is missing;
- session function/config differs;
- background consumer tenant context is wrong.

---

# 29. OPS-REC-014 — RLS is reverified after data/schema recovery

Do not assume restored data automatically has safe runtime isolation.

---

# 30. Aggregate invariants

Verify business invariants relevant to affected context.

Examples:

```text
last Workspace owner
unique membership
valid Board Field/Item values
Page hierarchy/cycle
Subscription/Entitlement state
Automation execution lifecycle
Integration mapping uniqueness
```

---

# 31. OPS-REC-015 — Row count parity is not semantic recovery proof

Equal counts can still hide:

- wrong tenant;
- wrong status;
- duplicate logical records;
- broken references;
- invalid lifecycle.

---

# 32. Concurrency/version

Restored/repaired aggregate versions must remain coherent with optimistic concurrency/event semantics.

---

# 33. OPS-REC-016 — Recovery does not reset concurrency versions casually

Resetting versions can allow stale clients/messages to overwrite newer valid state.

Any version rewrite requires explicit migration semantics.

---

# 34. Outbox

Outbox state may include:

```text
committed-but-undispatched
dispatched
claimed
retrying
```

relative to recovery point.

---

# 35. OPS-REC-017 — Source state and outbox are reconciled together

After DB rollback/repair, determine for each affected logical event:

```text
should event exist?
was it already delivered?
is outbox row present?
is replay safe?
```

---

# 36. Published event

Once external/public event was delivered, DB rollback does not unpublish it.

---

# 37. OPS-REC-018 — Delivered facts require reconciliation or compensation

Do not replay/re-emit blindly after rollback.

Check consumer effects.

---

# 38. Consumer dedup

Consumer idempotency state can be ahead of or behind recovered source/outbox state.

---

# 39. OPS-REC-019 — Dedup state is part of recovery

If dedup says “processed” but target state was rolled back, determine whether to:

```text
repair target
replay with controlled dedup override
rebuild projection
```

Do not simply clear all dedup rows.

---

# 40. Ordering state

Ordered consumer cursor/sequence can conflict with restored message/output state.

---

# 41. OPS-REC-020 — Ordering cursor is not rewound/skipped without proof

Reconcile:

```text
last valid sequence
processed effects
pending messages
poison state
```

before adjustment.

---

# 42. Message replay

Replay uses a stable bounded selection:

```text
event/message IDs
time range
sequence range
producer
consumer
tenant/resource scope
```

---

# 43. OPS-REC-021 — Replay is idempotent and bounded

Before replay verify:

- consumer dedup;
- target operation idempotency;
- provider effects;
- ordering;
- current authorization/tenant scope.

---

# 44. Dead letter

DLQ/poison records preserve diagnostic identity.

A repaired message may be replayed after root cause is addressed.

---

# 45. OPS-REC-022 — DLQ is evidence, not garbage

Do not purge because backlog dashboards look bad.

Retention/removal follows recovery evidence.

---

# 46. Provider effects

External effects can include:

```text
message/email sent
calendar event created
external task/issue created
payment/charge
provider resource deleted
OAuth/provider state
```

---

# 47. OPS-REC-023 — External outcome is reconciled from provider reality

For each affected operation determine:

```text
did provider commit?
provider object ID?
idempotency/correlation key?
can reverse/compensate?
should Notrelix state be repaired to match?
```

---

# 48. Unknown provider outcome

Timeout can occur after provider commit.

---

# 49. OPS-REC-024 — Unknown provider operation is reconciled before retry

Do not create duplicate external effects while recovering internal state.

---

# 50. Billing/payment

Financial records require conservative reconciliation.

Preserve:

- provider event ID;
- invoice/payment refs;
- Subscription state;
- Entitlement;
- usage ledger/idempotency.

---

# 51. OPS-REC-025 — Financial recovery preserves commercial evidence

Do not silently delete/overwrite historical invoice/payment/usage evidence to make current totals match.

Use explicit correction/adjustment semantics.

---

# 52. Object storage

Database records can reference objects.

Recovery must determine object state relative to DB state.

---

# 53. OPS-REC-026 — Database restore and object-store restore are reconciled

Possible mismatches:

```text
DB points to missing object
object exists with no DB reference
older DB points to superseded object
delete already executed externally
```

---

# 54. Object identity

Object keys are technical references, not product authority.

Repair based on source semantic record and retention/security policy.

---

# 55. Search

Search index is derived.

---

# 56. OPS-REC-027 — Rebuildable search prefers rebuild from authoritative source

After source recovery:

```text
invalidate/reindex
→ verify tenant/security
```

rather than making search index drive source repair.

---

# 57. Cache

Cache is derived/ephemeral.

---

# 58. OPS-REC-028 — Cache is invalidated/rebuilt after relevant recovery

Do not preserve stale cache just to reduce cold-start load if it can serve pre-recovery or unauthorized state.

---

# 59. Analytics/projections

Derived projections/snapshots require context-specific treatment.

Current projections can usually rebuild.

Historical snapshots may represent “what was reported then” and may be retained separately.

---

# 60. OPS-REC-029 — Current projection and historical snapshot are distinguished

Do not overwrite historical reporting evidence automatically with rebuilt current truth.

---

# 61. Realtime

Realtime is delivery/freshness, not durable authority.

After recovery clients may have stale optimistic/local state.

---

# 62. OPS-REC-030 — Recovery triggers convergence to authoritative state

Use:

```text
version/revision
invalidate/refetch
reconnect
subscription reset
```

as appropriate.

---

# 63. Frontend/mobile

Old clients may still hold/retry requests based on pre-recovery versions.

Concurrency/idempotency protects source state.

---

# 64. OPS-REC-031 — Recovery does not trust stale client retries

Server revalidates:

- version;
- authorization;
- tenant;
- current lifecycle.

---

# 65. Backup restore environment

Where feasible, validate restore in an isolated environment before production cutover.

---

# 66. OPS-REC-032 — Restore validation does not write into live production accidentally

Recovery tooling requires explicit target/environment safeguards.

---

# 67. Restore test

An effective restore exercise can verify:

```text
backup integrity
restore commands/tooling
schema/migration
application startup
RLS
representative reads/writes
```

according to scope.

---

# 68. OPS-REC-033 — Recovery tooling is exercised periodically according to risk

Exact cadence belongs to Operations.

An untested backup/restore assumption remains operational risk.

---

# 69. Partial restore

Tenant/table-level restore can reduce blast radius but may introduce reference/transaction inconsistencies.

Use only with explicit dependency/invariant analysis.

---

# 70. OPS-REC-034 — Partial restore includes dependency graph

Identify:

```text
foreign/context references
outbox/events
object storage
provider mappings
derived projections
```

before merging recovered subset.

---

# 71. Merge recovery

Restoring old rows into a live newer DB is a data migration/repair, not a simple restore.

It needs conflict semantics.

---

# 72. OPS-REC-035 — Recovery merge never uses blind last-write-wins

Determine authoritative value by product semantics/version/history.

---

# 73. Lost interval

If recovery cannot preserve all writes/events in an interval, document:

```text
time window
affected tenants/resources
known lost facts
external effects
customer impact
repair status
```

---

# 74. OPS-REC-036 — Permanent loss is explicit

Do not silently normalize counts or hide missing interval.

Incident/audit evidence records it according to policy.

---

# 75. Manual repair

Manual repair should be scripted/versioned when repeatability or scale warrants it.

---

# 76. OPS-REC-037 — Manual repair is reviewed production code

Requirements:

```text
scope
dry-run where useful
idempotency
tenant safety
logging
verification
rollback/forward repair
```

---

# 77. Dry run

A repair/backfill can support:

```text
selected rows
proposed changes
mismatch counts
```

without writing.

Do not expose sensitive content unnecessarily.

---

# 78. Repair authorization

Only approved operational/admin principals perform production repair.

Product users cannot access recovery endpoints implicitly.

---

# 79. OPS-REC-038 — Recovery capability is not ordinary product API

Privileged repair paths are isolated, audited, and removed/disabled when temporary.

---

# 80. Recovery order

A typical broad recovery can follow:

```text
1. stop unsafe writers
2. preserve evidence
3. restore/repair authoritative data
4. verify schema/RLS/invariants
5. reconcile outbox/messages
6. reconcile providers/object storage
7. rebuild caches/search/projections
8. force/rely on client convergence
9. reopen gradually
10. monitor
```

Actual order depends on failure.

---

# 81. OPS-REC-039 — Derived systems reopen after source correctness

Do not rebuild/search/broadcast from data that has not passed source invariant checks.

---

# 82. Write reopening

Reopen write workload gradually if broad recovery changed capacity/cache/projections.

---

# 83. OPS-REC-040 — Reopening requires write-path verification

Test representative:

```text
authorized read
authorized write
cross-tenant denial
transaction/outbox
background processing
```

before full normal operation for affected capability.

---

# 84. Post-recovery monitoring

Observe:

```text
error/latency
DB locks/pool
outbox/backlog
dedup/retry
realtime convergence
provider reconciliation
cache/search freshness
```

until stable.

---

# 85. Recovery and incident closure

Incident closes only after the original defect and downstream consistency are verified.

---

# 86. OPS-REC-041 — Restore success does not automatically close incident

Recovery verification is application-level.

---

# 87. Recovery documentation

A recovery runbook can contain vendor-specific commands.

This canonical file retains vendor-neutral correctness rules.

---

# 88. OPS-REC-042 — Vendor tooling does not define recovery semantics

Whether using managed PostgreSQL snapshots, PITR, object versioning, etc., the same product reconciliation obligations remain.

---

# 89. Backup encryption/access

Backup data has the same or stronger sensitivity as source data.

Access is restricted and audited according to policy.

---

# 90. OPS-REC-043 — Backup is not a lower-security copy

Do not expose production data through broad backup access.

---

# 91. Backup retention

Retention balances:

- recovery need;
- privacy/legal;
- storage/cost;
- deletion obligations.

Concrete periods require approved policy.

---

# 92. OPS-REC-044 — Deleted data in backups follows explicit retention policy

Deletion from primary storage does not imply instant disappearance from immutable backups unless policy/infrastructure guarantees it.

---

# 93. Recovery drills

Critical scenarios can be rehearsed:

```text
DB restore
tenant-scoped repair
message replay
provider reconciliation
object-loss recovery
```

using safe non-production data.

---

# 94. OPS-REC-045 — Drill validates decision points, not only commands

A useful exercise proves responders know:

```text
which recovery scope
what not to replay
how to verify
when to reopen
```

---

# 95. Data-corruption checklist

```text
[ ] stop bad writer
[ ] affected tenants/resources/time
[ ] preserve evidence
[ ] schema/release/config state
[ ] targeted repair vs restore
[ ] tenant/RLS
[ ] aggregate invariants
[ ] outbox/dedup/order
[ ] provider/object effects
[ ] projections/cache/search
[ ] verification
[ ] permanent loss/repair evidence
```

---

# 96. Restore checklist

```text
[ ] backup/source identified
[ ] restore target isolated/safe
[ ] schema/migration compatible
[ ] RLS verified
[ ] application starts
[ ] data invariants sampled/proven
[ ] outbox/message reconciliation
[ ] provider/object reconciliation
[ ] derived systems rebuilt
[ ] representative read/write
[ ] reopen/monitor
```

---

# 97. Replay checklist

```text
[ ] bounded logical range
[ ] producer/consumer identity
[ ] tenant/resource scope
[ ] dedup/idempotency
[ ] ordering
[ ] external side effects
[ ] current authorization
[ ] stop condition
[ ] post-replay verification
```

---

# 98. Recovery-objective checklist

```text
[ ] capability/data class
[ ] approved RPO or TBD
[ ] approved RTO or TBD
[ ] backup mechanism evidence
[ ] restore evidence
[ ] reconciliation scope
[ ] owner
[ ] review cadence
```

---

# 99. Current backend alignment

Current backend persistence architecture states:

```text
EF Core/PostgreSQL mappings/migrations belong to Infrastructure
persistence evidence = EF model + migration chain/current schema
RLS complements Application authorization
cache/provider/search are adapters
destructive changes need rollback/roll-forward planning
model drift suppression is a failure mode
```

Current Platform architecture requires stable message/consumer identity, idempotency, ordering, poison handling, explicit tenant context, and retry/dead-letter observability.

These are recovery dependencies, not recovery substitutes.

---

# 100. Stop conditions

Stop recovery and reassess if:

- a restore point is chosen without considering external/provider/event effects;
- evidence is being deleted to simplify the incident;
- RLS/tenant relationships are not verified;
- aggregate correctness is inferred from row count only;
- dedup/idempotency state is cleared blindly;
- ordering cursor is rewound/skipped with no effect analysis;
- provider timeout is retried before reconciliation;
- search/cache/projection is used to repair authoritative source;
- model drift is suppressed;
- old migration history is rewritten;
- backup is cited with no restore feasibility;
- recovery tool can target production accidentally;
- permanent lost interval is hidden rather than recorded.

---

# 101. Related canonical owners

```text
docs/operations/observability.md
docs/operations/incident-readiness.md
docs/operations/service-degradation.md
docs/delivery/migration-policy.md
docs/delivery/release-and-rollout.md
docs/quality/security-quality-standard.md
docs/architecture/data-ownership-and-consistency.md
docs/architecture/events-realtime-and-delivery-boundary.md
backend/docs/architecture/infrastructure-and-data.md
backend/docs/architecture/platform-and-messaging.md
```

---

# 102. Final recovery rule

For every data-recovery action, answer:

```text
What authoritative state is wrong or unavailable?
Which tenants/resources/time interval are affected?
What evidence must be preserved?
Is targeted repair safer than broad restore?
Which schema/RLS/product invariants define a valid result?
Which events/messages/dedup/order state already escaped?
Which provider/object-storage effects already happened?
Which derived systems can simply be rebuilt?
What can be replayed safely and with what identity?
What objective verification permits writes to reopen?
What loss/repair must remain recorded permanently?
```

The target is:

> **recovery that reconstructs a coherent product and distributed state, rather than merely restoring infrastructure, while preserving tenant safety, historical evidence, idempotency, and the real-world side effects that cannot be rolled back by database restore.**
