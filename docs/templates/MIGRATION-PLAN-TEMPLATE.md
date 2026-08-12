---
document_id: TEMPLATE-MIGRATION-PLAN
document_type: template
status: active
owner: documentation-governance
applies_to:
  - schema-migrations
  - data-migrations
  - contract-migrations
  - ownership-migrations
  - backfills
  - persisted-identities
  - async-backlogs
evidence:
  - docs/delivery/change-classification.md
  - docs/delivery/contract-first-delivery.md
  - docs/delivery/migration-policy.md
  - docs/delivery/release-and-rollout.md
  - docs/delivery/definition-of-done.md
  - docs/operations/recovery-and-data-safety.md
  - docs/quality/testing-strategy.md
review_on:
  - migration-policy-change
  - rollout-policy-change
  - schema-or-data-ownership-change
  - template-change
---

# Migration Plan Template

> **A migration plan describes how real durable old state and old consumers move to one explicit target authority through safe, observable, resumable stages.**
>
> It is an execution artifact. It is not permanent architecture authority, and it must not leave the implementation agent to invent compatibility, backfill, cutover, rollback, or destructive-removal semantics.

Use this template for migrations involving one or more:

```text
database schema
existing production data
persisted identifiers/status/discriminators
public/API/event/realtime contracts
generated consumers
data ownership
large backfill
message/event backlog
provider mappings
projection/index rebuild
feature/config persisted state
```

The canonical policy remains:

```text
docs/delivery/migration-policy.md
```

This template instantiates that policy for one concrete change.

---

# 1. When this template is required

Use when a change has material `C4` migration impact or when another change class implies durable transition work.

Common examples:

- add a required persisted field to existing rows;
- split/merge persisted lifecycle status;
- move authoritative data between bounded contexts;
- rename a persisted event identity;
- migrate v1 API/event consumers to v2;
- reindex/rebuild a large derived projection;
- change provider mapping identifiers;
- backfill tenant-scoped derived data;
- remove an old column/endpoint/event only after coexistence.

---

# 2. When it is unnecessary

Do not create a migration plan for:

```text
new table with no existing data and no mixed-version risk
private implementation refactor
generated file refresh with no compatibility change
local-only development reset
```

unless real production transition risk still exists.

Depth follows durable risk, not template symmetry.

---

# 3. Migration plan versus canonical architecture

This document records:

```text
CURRENT
TRANSITION
TARGET
```

The **TARGET** must come from canonical Product/Architecture/ADR owners.

This plan does not get to invent a new bounded context, ownership rule, permission model, or public compatibility policy.

If target architecture is unresolved:

```text
BLOCKING DECISION
```

and stop.

---

# 4. Migration plan versus release plan

Migration plan owns:

```text
data/contract phases
reader/writer compatibility
backfill
authority switch
verification
contraction
```

Release plan owns:

```text
which artifact deploys where/when
cohorts
canary
feature flags
promotion
rollback/forward-recovery execution
```

They reference each other where necessary.

---

# 5. Migration plan versus ADR

An ADR answers:

```text
Why was the target architecture chosen?
```

Migration plan answers:

```text
How do existing durable state and consumers reach it safely?
```

If a consequential target decision is not yet accepted, this plan may analyze the transition but must not authorize implementation of the unresolved architecture.

---

# 6. Required migration posture

Every plan must make explicit:

```text
old authoritative state
target authoritative state
authority per phase
old/new readers
old/new writers
existing data
backlog/replay
mixed-version deployment
backfill
verification
destructive-removal proof
rollback versus forward recovery
```

---

# 7. Copy from here

```markdown
---
document_id: <MIGRATION-ID>
document_type: migration-plan
status: draft
owner: <logical-owner>
applies_to:
  - <capability/context>
evidence:
  - <canonical target architecture/product doc>
  - <source schema/contract>
  - <tests/gates>
review_on:
  - phase-complete
  - target-changed
  - migration-complete
---

# <Migration title>

## 1. Executive summary

### Current state

<What durable state/contract exists today?>

### Target state

<What must become true?>

### Reason

<Why is migration necessary?>

### Semantic owner

- current:
- target:
- canonical target doc:

### Change classes

- `C4`
- `<other classes>`

### Risk modifiers

- `<DATA_BACKFILL>`
- `<MOBILE_LAG>`
- `<ASYNC_BACKLOG>`
- `<HIGH_CARDINALITY>`
- `<CROSS_TENANT>`
- `<ROLLBACK_UNSAFE>`
- ...

### Overall completion condition

The migration is complete when:

1. ...
2. ...
3. old path can be removed because ...
4. exact evidence proves ...

---

## 2. Authorities and prerequisites

### Canonical target authority

- product/context:
- architecture:
- ADR:
- persistence/contract owner:

### Required accepted decisions

- `<None or ADR IDs>`

### Blocking unresolved decisions

If none:

`None.`

Otherwise:

| Decision | Owner | Why blocking | Required artifact |
|---|---|---|---|
| ... | ... | ... | ADR/product decision |

The implementing agent MUST NOT invent these choices.

### Prerequisites

- schema:
- code:
- tooling:
- backup/recovery:
- environment:
- observability:
- consumer inventory:

---

## 3. Current-state inventory

### Current authoritative representation

- table/store/contract:
- owner:
- schema/version:
- lifecycle:
- identifiers:

### Existing readers

| Reader | Version/deployment unit | Reads | Can lag rollout? |
|---|---|---|---:|
| ... | ... | ... | yes/no |

### Existing writers

| Writer | Version/deployment unit | Writes | Authority |
|---|---|---|---|
| ... | ... | ... | ... |

### Existing data

- estimated cardinality:
- tenant distribution:
- oldest/newest relevant data:
- null/legacy values:
- invalid/unknown values:
- high-volume tenants:
- sensitive data classification:

### Existing persisted identities

Review:

```text
column names
enum/status strings
discriminators
JSON versions
event names
feature codes
metric keys
provider mapping IDs
cache/config keys
```

### Existing async/backlog state

- queue/outbox:
- retained/replay events:
- DLQ:
- oldest supported format:
- consumer dedup state:

### Existing external/provider state

- provider mappings:
- external IDs:
- provider operation identity:
- object storage:
- payment/calendar/etc.:

### Existing source debt

| Evidence | Debt classification | Migration consequence |
|---|---|---|
| ... | SOURCE_DEBT / TRANSITION | ... |

---

## 4. Target-state definition

### Target authoritative representation

- owner:
- table/store/contract:
- stable identity:
- lifecycle:
- schema/version:

### Target readers

- ...

### Target writers

- ...

### Derived/compatibility representations

- ...

### State explicitly retired

- ...

### Invariants

1. ...
2. ...
3. ...

### Tenant/security requirements

- Account/Workspace scope:
- RLS:
- authorization:
- privileged migration scope:
- audit:

---

## 5. Compatibility matrix

### Binary/schema matrix

Fill if schema/data migration affects rolling binaries.

| Runtime | Old schema/data | Expanded schema/data | Target schema/data | Contracted schema |
|---|---:|---:|---:|---:|
| old runtime | yes/no | yes/no | yes/no | yes/no |
| migration-compatible runtime | yes/no | yes/no | yes/no | yes/no |
| target runtime | yes/no | yes/no | yes/no | yes/no |

For every `no`, state why that combination cannot occur in production.

### Producer/consumer matrix

Fill for API/event/realtime/generated contract migration.

| Producer | Consumer | Must coexist? | Expected behavior |
|---|---|---:|---|
| old | old | yes | ... |
| new | old | yes/no | ... |
| old | new | yes/no | ... |
| new | new | yes | ... |

### Mobile compatibility

- oldest supported mobile version:
- migration-compatible server window:
- removal floor:

### Browser/web compatibility

- old loaded bundles:
- cache/static asset concerns:

### Worker compatibility

- old workers:
- old queued jobs/events:
- deployment order:

### Provider compatibility

- provider schema/version:
- webhook replay:
- external objects created under old semantics:

---

## 6. Migration phases

Use only phases that apply.

Every deployed phase must be independently valid.

### Phase 0 — Preparation

Goal:

<What is established before any durable transition?>

Changes:

- tests:
- observability:
- backup/recovery proof:
- schema/contract producer:
- tooling:
- feature/config flags:

Authority:

```text
read authority:
write authority:
compatibility representation:
```

Entry criteria:

- ...

Exit criteria:

- ...

Rollback/forward recovery:

- ...

---

### Phase 1 — Expand

Goal:

Add compatible target representation without destroying old readers/writers.

Changes:

- new column/table/index:
- new event/API version:
- new provider mapping:
- new read model:
- compatibility adapter:

Authority:

```text
read authority:
write authority:
secondary representation:
```

Old runtime compatibility:

- ...

New runtime compatibility:

- ...

Verification:

- ...

Exit criteria:

- ...

---

### Phase 2 — Compatible writer / dual representation

Goal:

Ensure new durable writes contain enough information for old/new consumers.

Write path:

- authoritative write:
- compatibility write:
- transactional/outbox relationship:
- partial failure handling:

Read path:

- old reader:
- new reader:

Authority:

```text
ONE authoritative semantic write source:
<...>
```

If dual write exists:

```text
secondary write = compatibility replication
NOT second authority
```

Verification:

- mismatch:
- repair:
- telemetry:

Exit criteria:

- ...

---

### Phase 3 — Backfill / migration

Goal:

Move existing state into target representation.

Selection:

- source table/contract:
- predicates:
- excluded rows:
- tenant scope:

Stable traversal key:

- ...

Batching:

- batch size:
- transaction size:
- concurrency:
- cancellation:
- throttling:

Idempotency:

- ...

Checkpoint:

- ...

Retry:

- ...

Invalid/legacy data policy:

| Case | Meaning | Treatment | Owner decision |
|---|---|---|---|
| ... | ... | normalize/quarantine/block | ... |

Progress metrics:

- selected:
- processed:
- succeeded:
- skipped:
- failed/quarantined:
- remaining:
- oldest checkpoint:

Completion verification:

- ...

Exit criteria:

- ...

---

### Phase 4 — Shadow compare / validation

Use if target reader/calculation must be proven against current authority before cutover.

Primary result:

- old/new:

Shadow path:

- side-effect free?:
- mismatch handling:
- sample/full comparison:
- acceptable mismatch definition:
- blocker threshold owner:

Never let shadow mode duplicate provider effects, billing usage, or source writes.

Exit criteria:

- ...

---

### Phase 5 — Read cutover

Goal:

Target representation becomes read authority.

Authority:

```text
read authority:
write authority:
fallback read:
```

Cutover mechanism:

- deployment:
- feature flag:
- config:
- routing:

Required evidence:

- backfill:
- mismatch:
- performance:
- tenant/RLS:
- old reader compatibility:

Rollback safety:

- ...

Exit criteria:

- ...

---

### Phase 6 — Write-authority cutover

Goal:

Target owner/representation becomes the canonical write authority if not already.

Authority:

```text
read authority:
write authority:
old compatibility write:
```

Cross-context contract:

- ...

Authorization:

- ...

Outbox/events:

- ...

Provider/object mapping:

- ...

Exit criteria:

- ...

---

### Phase 7 — Observe / stabilize

Goal:

Prove the target path remains correct under real workload.

Observe:

```text
errors
mismatch
latency
DB load/locks
backlog
provider failures
tenant/security
old-path usage
```

Required observation window:

`<Operations/release owner decides; do not invent universal duration>`

Exit criteria:

- ...

---

### Phase 8 — Contract / remove old path

Prerequisite proof:

```text
[ ] old writers = 0
[ ] old readers = 0 or unsupported
[ ] backfill complete
[ ] old messages/backlog handled
[ ] old mobile floor retired where applicable
[ ] old provider mapping no longer required
[ ] rollback semantics understood
[ ] telemetry shows target authority stable
```

Remove:

- old column/table:
- old endpoint/event:
- fallback reader:
- compatibility writer:
- feature flag:
- migration-only code:
- old indexes/constraints:
- old generated contract:

Final verification:

- ...

---

## 7. Authority-by-phase table

This table is mandatory for ownership/data migrations.

| Phase | Read authority | Write authority | Compatibility copy | Can disagree? | Resolution |
|---|---|---|---|---:|---|
| current | ... | ... | none | no | ... |
| expand | ... | ... | ... | yes/no | ... |
| backfill | ... | ... | ... | yes/no | ... |
| cutover | ... | ... | ... | yes/no | ... |
| target | ... | ... | none | no | ... |

There must never be an unowned ambiguity where both old and new are treated as equal semantic truth.

---

## 8. Backfill design

### Source

- ...

### Destination

- ...

### Transformation

Describe deterministic mapping.

### Determinism

For each destination field:

| Target field | Source fact | Transform | Default/sentinel policy |
|---|---|---|---|
| ... | ... | ... | ... |

### Unknown value handling

Do not guess business meaning.

- ...

### Tenant partitioning

- ...

### Resource limits

- DB connections:
- CPU:
- locks:
- provider calls:
- queue:
- tenant fairness:

### Resume behavior

After process crash/redeploy:

- checkpoint:
- idempotency:
- already-processed detection:

### Dry run

- supported?:
- output:
- safety:

### Backfill code lifecycle

- location:
- owner:
- removal/retirement condition:

---

## 9. Schema safety

### DDL changes

- ...

### Lock/rewrite risk

- ...

### Table size

- ...

### Index build

- ...

### Constraint enforcement

- ...

### Nullability

- ...

### Default semantics

Distinguish:

```text
product default
migration sentinel
unknown
derived value
```

### RLS

- policy created/changed:
- ordering with table creation:
- foreign-tenant negative test:

### Migration history

- new migration:
- applied history remains append-oriented:

Do not rewrite already-applied production migrations casually.

---

## 10. Contract safety

### Public/API

- ...

### Event identity/version

- ...

### Realtime

- ...

### Generated clients

- ...

### Error/timing semantics

- ...

### Idempotency

- ...

### Old contract retirement

- ...

---

## 11. Async/backlog safety

### Outbox

- old/new event representations:
- source-commit relationship:

### Broker queue

- formats:
- oldest backlog:
- drain/version-route strategy:

### DLQ/replay

- retained old format:
- retention horizon:

### Consumer dedup

- key/schema:
- migration/reconciliation:

### Ordering

- cursor/sequence:
- compatibility:

### Replay

- bounded range:
- idempotency:
- external side effects:

---

## 12. Provider / external-state safety

### Provider identity

- ...

### Existing external objects

- ...

### Mapping migration

- ...

### Unknown outcome

- ...

### Retry

- ...

### Webhook old/new payload

- ...

### Compensation/reconciliation

- ...

Never assume DB rollback undoes provider effects.

---

## 13. Object/search/cache/projection safety

### Object storage

- DB/object references:
- orphan/missing handling:

### Search

- rebuild versus migrate:
- tenant/security:
- cutover:

### Cache

- invalidate/version:
- warmup:
- security-sensitive keys:

### Analytics/projection

- rebuild:
- historical snapshot treatment:
- freshness:

Derived systems MUST NOT become the source used to repair authoritative data unless architecture explicitly says so.

---

## 14. Security and tenant safety

### Migration runtime identity

- ...

### Privilege

- ...

### Tenant scope

- ...

### Cross-tenant/global iteration

- partition strategy:
- audit:
- safeguards:

### RLS

- ...

### Secrets

- ...

### Negative tests

- wrong tenant:
- malformed legacy data:
- unauthorized operation:
- provider replay:

---

## 15. Performance / production safety

### Expected size

- rows:
- bytes:
- tenants:
- providers:

### Duration estimate

Only provide an estimate when backed by representative evidence.

### Load test / dry run

- ...

### Lock risk

- ...

### Backpressure

- ...

### Pause/resume control

- ...

### Noisy tenant

- ...

### Peak/off-peak requirement

If required, identify Operations owner.

Do not invent a universal “run migrations at night” rule.

---

## 16. Observability

### Migration identity

- plan/version:
- execution/job ID:

### Metrics

- selected:
- processed:
- succeeded:
- failed:
- mismatch:
- remaining:
- throughput:
- checkpoint age:

### Logs

- safe record identity:
- tenant/resource:
- error class:

### Alerts

- stuck:
- high failure:
- DB pressure:
- security mismatch:

### Dashboard / query

- ...

---

## 17. Verification strategy

### Clean-state proof

- migration can create/apply on clean environment:

### Upgrade proof

- representative previous schema/data:
- old rows:
- legacy states:

### Semantic invariants

- ...

### Counts

Counts are supporting evidence, not enough alone.

### Checksums / comparisons

- ...

### Sample reads

- ...

### Representative writes

- ...

### RLS

- ...

### Application startup

- ...

### Async

- ...

### Provider/object

- ...

### Frontend/consumer

- ...

### Exact tests/gates

| Property | Test/gate | Non-zero requirement |
|---|---|---|
| migration apply | ... | ... |
| RLS | ... | ... |
| contract | ... | ... |
| backfill | ... | ... |
| consumer | ... | ... |
| integration | ... | ... |

---

## 18. Completion proof

State objective queries/signals.

### Data completion

- ...

### Reader migration

- ...

### Writer migration

- ...

### Backlog completion

- ...

### Consumer floor

- ...

### Provider completion

- ...

### Old-path non-use

- ...

### Target invariant verification

- ...

Do not use:

```text
script exited 0
```

as the sole completion criterion.

---

## 19. Rollback and forward recovery

### Before migration

- backup/restore evidence:
- last compatible artifact:

### Phase-specific rollback

| Phase | Binary rollback | Schema rollback | Data rollback | Forward recovery |
|---|---|---|---|---|
| expand | ... | ... | ... | ... |
| backfill | ... | ... | ... | ... |
| cutover | ... | ... | ... | ... |
| contraction | ... | ... | ... | ... |

### Irreversible effects

- events already published:
- provider effects:
- deleted data:
- ID changes:
- mobile/client state:

### Forward repair

- ...

Never write a generic:

```text
rollback if migration fails
```

without explaining which surfaces can actually roll back.

---

## 20. Failure scenarios

### F-MIG-01 — process crashes mid-batch

Expected recovery:

- ...

### F-MIG-02 — backfill encounters invalid legacy row

- ...

### F-MIG-03 — old writer continues after backfill

- ...

### F-MIG-04 — new/old representations disagree

- ...

### F-MIG-05 — DB capacity becomes unsafe

- ...

### F-MIG-06 — provider outcome unknown

- ...

### F-MIG-07 — old mobile/worker remains

- ...

### F-MIG-08 — rollback binary cannot read new state

- ...

Add scenarios specific to the migration.

---

## 21. Execution commands / runbook

Only include exact commands that are:

- repository-supported;
- environment-safe;
- reviewed.

For each destructive/production command state:

```text
environment
identity
scope
dry-run
expected output
stop condition
```

Do not paste secret-bearing command lines.

---

## 22. Ownership

| Responsibility | Logical owner |
|---|---|
| target semantics | ... |
| schema/migration | ... |
| backfill execution | ... |
| provider reconciliation | ... |
| release/cutover | ... |
| observability | ... |
| verification | ... |
| cleanup | ... |

Do not use temporary team staffing as architecture authority.

---

## 23. Documentation updates

### Canonical product/architecture

- ...

### Backend/frontend docs

- ...

### Generated contracts

- ...

### Operations/runbooks

- ...

### ADR

- ...

### Temporary migration artifacts to retire

- ...

---

## 24. Cleanup

After completion remove as applicable:

```text
dual reader
dual writer
fallback
migration feature flag
temporary column/table
old event/API
old generated type
old index
migration-only endpoint/job
temporary observability
```

Keep durable:

```text
applied migration history
ADR
current architecture
incident/audit evidence
required recovery evidence
```

---

## 25. Stop conditions

Stop execution if:

- target authority is unresolved;
- a required ADR is unaccepted;
- old/new runtime compatibility is unknown;
- backfill has no stable key/checkpoint/idempotency;
- legacy invalid data has no approved treatment;
- privileged migration can cross tenants without safeguards;
- dual read has no precedence;
- dual write has no single authority;
- constraint/drop occurs before old writers are gone;
- backlog/DLQ/replay old format is ignored;
- provider unknown outcomes are retried blindly;
- production load/lock risk becomes unsafe;
- completion cannot be objectively measured;
- recovery requires deleting evidence;
- destructive contraction is being executed before proof.

---

## 26. Phase status

Use this only while the migration is active.

| Phase | Status | Evidence | Blocker |
|---|---|---|---|
| preparation | not-started/in-progress/done | ... | ... |
| expand | ... | ... | ... |
| backfill | ... | ... | ... |
| cutover | ... | ... | ... |
| stabilization | ... | ... | ... |
| contraction | ... | ... | ... |

This table is temporary execution state, not permanent architecture authority.

---

## 27. Final evidence report

### Migration outcome

- ...

### Final authority

- read:
- write:

### Data verification

- ...

### Backlog/provider reconciliation

- ...

### Old-path removal proof

- ...

### Exact deployed revision(s)

- ...

### Exact CI/gates

- ...

### Permanent loss/manual repair

- none / describe with evidence:

### Remaining transition

- none / explicit owner + completion condition:

### Canonical knowledge rehomed

- ...

### Plan retirement

When all durable knowledge is rehomed and no active migration work remains:

```text
status → completed/retired according to artifact workflow
```

Do not keep this plan as the permanent architecture handbook.
```

---

# 8. Migration depth rules

A small additive nullable field may need only:

```text
expand
writer
backfill
verify
constraint
```

A context-ownership or public-event migration may need every section.

Do not delete risk sections just to shorten the plan.

Do not fill irrelevant sections with fake ceremony.

---

# 9. Authority-by-phase is mandatory for dual-state migrations

A plan that says:

```text
we will keep old and new in sync for a while
```

is incomplete.

It must say:

```text
during phase 2:
old X remains semantic authority
new Y is compatibility copy

during phase 5:
new Y becomes read authority

during phase 6:
new owner becomes write authority
```

If both are allowed to disagree without a winner, the design has created dual truth.

---

# 10. Backfill quality

The legacy template correctly required:

```text
selection/range
batch/cancellation
idempotency
resumability
tenant scope
progress metrics
failure retry
```

A complete plan additionally defines:

```text
stable traversal key
invalid legacy data policy
checkpoint
resource budget
completion proof
```

The goal is production-safe execution, not merely a script that works on a local database.

---

# 11. Compatibility quality

Migration analysis must include real independent deployment units.

Do not model only:

```text
new backend + new frontend
```

when production can contain:

```text
new backend + old mobile
new worker + old queued messages
old web bundle + new backend
```

---

# 12. Removal quality

Old path removal is its own proven phase.

Required proof can include:

```text
old writes = 0
old reads = 0
backfill verified
consumer floor reached
queue/DLQ horizon handled
provider mapping migrated
telemetry shows no fallback
```

Do not delete the old representation in the same first deployment that introduces its replacement unless true atomicity is demonstrated.

---

# 13. Recovery quality

Migration safety must reason beyond the DB:

```text
database
outbox
consumer dedup
events
provider effects
object storage
cache/search/projections
clients
```

A DB restore can create a state that is earlier than already-emitted external effects.

The plan must decide reconciliation versus compensation.

---

# 14. Plan-retirement quality

After migration:

```text
durable rule → canonical product/architecture docs
decision → ADR
recovery procedure → Operations
schema history → migrations
generated fact → generator
```

The migration plan can then retire.

This preserves knowledge without creating a second architecture authority.

---

# 15. Migration-plan quality test

A coding/operator agent is ready to execute when it can answer:

```text
What exact old state exists?
What exact target authority must exist?
Which old/new versions can coexist?
Who is read/write authority in every phase?
How does existing data move safely?
How does the job resume after failure?
How are tenants/RLS protected?
How are invalid old rows handled?
What happens to queued events and provider effects?
What objective evidence proves each phase?
When can destructive cleanup happen?
Which rollback surfaces are actually reversible?
```

If those answers require invention, the plan is not ready.
