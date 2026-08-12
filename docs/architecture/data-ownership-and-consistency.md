---
document_id: SYS-DATA-OWNERSHIP-CONSISTENCY
document_type: architecture
status: active
owner: system-architecture
applies_to:
  - repository
  - backend
  - frontend
  - data
  - cross-context-workflows
  - projections
  - caching
  - asynchronous-processing
evidence:
  - PRODUCT.md
  - RULE.md
  - docs/architecture/system-overview.md
  - docs/architecture/bounded-context-map.md
  - backend/src/Notrelix.Domain/
  - backend/src/Notrelix.Application/
  - backend/src/Notrelix.Infrastructure/
  - backend/src/Notrelix.Platform/
  - backend/tests/
  - frontend/docs/architecture/state-query-mutations.md
  - frontend/docs/architecture/realtime.md
  - frontend/packages/
review_on:
  - authoritative-data-owner-change
  - transaction-boundary-change
  - cross-context-consistency-change
  - outbox-or-postcommit-change
  - cache-ownership-change
  - projection-model-change
  - process-manager-change
  - concurrency-model-change
  - service-extraction-change
---

# Data Ownership and Consistency

> **Data correctness starts with one authoritative owner and an explicit consistency boundary.**
>
> Notrelix does not treat physical storage, shared databases, caches, indexes, realtime payloads, or frontend state as automatic business ownership.

This document owns the system-level contract for:

- authoritative data ownership;
- local transactional consistency;
- cross-context consistency;
- projections/read models;
- caches/search indexes;
- frontend cached state;
- concurrency;
- outbox/post-commit ordering;
- idempotency/retry relationship;
- process manager/saga admission;
- provider side-effect consistency;
- recovery/rebuild expectations.

Detailed implementation remains backend/frontend-owned.

---

# 1. Data architecture thesis

Every material fact should be classified as one of:

```text
Authoritative business state
Derived projection
Cache
Transport/delivery state
Operational state
Presentation/client state
External-provider state
```

The category determines:

- who owns mutation;
- what can be rebuilt;
- what consistency is required;
- what failure means;
- how recovery works.

---

# 2. SYS-DATA-001 — One authoritative owner per business fact

A durable business fact MUST have one semantic mutation authority.

Other components may retain:

- reference;
- projection;
- cache;
- historical snapshot;
- search document;
- analytical aggregate.

They do not become co-owners.

---

# 3. Semantic owner versus storage owner

Semantic ownership answers:

> Who defines lifecycle/invariants?

Storage implementation answers:

> Where/how is it persisted?

The two are related but not identical.

One PostgreSQL database can store data for many contexts without erasing their ownership.

---

# 4. SYS-DATA-002 — Physical database sharing does not authorize cross-context mutation

Sharing one database MUST NOT justify:

- direct cross-context repository access;
- direct table mutation across owners;
- cross-context aggregate graphs;
- broad transactions by convenience.

Logical boundaries exist before physical separation.

---

# 5. Source of truth taxonomy

## 5.1 Authoritative business state

Owned by product context and protected by its business/application contracts.

Examples:

- BoardItem field values;
- Workspace membership;
- subscription entitlement;
- integration connection state.

---

## 5.2 Derived projection

Computed/materialized from authoritative facts.

Examples:

- search index;
- analytics aggregate;
- reporting view;
- denormalized read model.

Projection may be durable.

It is still derived.

---

## 5.3 Cache

Temporary/performance representation of authoritative or derived data.

Must have:

- source;
- key/scope;
- freshness;
- invalidation;
- safe miss/failure.

---

## 5.4 Transport/delivery state

Examples:

- outbox record;
- inbox/dedup record;
- delivery attempt;
- sequence tracker;
- dead-letter record.

This state owns reliable delivery mechanics.

It does not own source business semantics.

---

## 5.5 Operational state

Examples:

- health;
- checkpoint;
- job lease;
- observability metadata.

Operational state supports runtime behavior.

---

## 5.6 Presentation/client state

Examples:

- selected tab;
- open modal;
- optimistic patch;
- query cache;
- current route.

Frontend presentation state does not become durable server truth.

---

## 5.7 External-provider state

Provider systems own their external state.

Notrelix may:

- reference;
- mirror;
- synchronize;
- map.

Provider state is not automatically authoritative for Notrelix product state.

---

# 6. SYS-DATA-003 — Ownership follows lifecycle/invariants

Use these questions:

```text
Who can create it?
Who validates mutation?
Who decides deletion/archive?
Who resolves conflicts?
Who emits the authoritative fact?
```

That owner is stronger evidence than:

- table;
- FK;
- namespace;
- cache key;
- API route.

---

# 7. Consistency classes

Notrelix uses three broad consistency patterns:

```text
1. Local transactional consistency
2. Explicit synchronous cross-owner orchestration
3. Eventual consistency
```

Process managers may coordinate longer-running workflows across the latter two.

---

# 8. Local transactional consistency

Use when one business transaction protects state owned within a coherent transaction boundary.

Expected properties:

- invariants validated;
- rejected operation does not partially commit;
- concurrency rules applied;
- related outbox enrollment atomic where required;
- transaction commit point known.

---

# 9. Aggregate consistency

An aggregate protects its own invariant boundary.

Do not create giant aggregates merely to gain transaction convenience.

Do not split one required invariant across aggregates and then assume eventual consistency will preserve it.

Aggregate design remains Domain-owned.

---

# 10. SYS-DATA-004 — Strong consistency is business-justified

Use strong atomic consistency only when failure between changes would violate a product invariant that cannot tolerate an intermediate state.

Reason must be semantic, not:

```text
same database makes it easy
```

---

# 11. Multi-root same-database transaction

A use case MAY transactionally coordinate multiple roots when:

- immediate atomicity is genuinely required;
- one Application operation owns orchestration;
- mutation ownership remains clear;
- rollback semantics are clear;
- coupling is accepted.

This is an exception to “one aggregate only”, not permission for arbitrary cross-context write graphs.

---

# 12. Cross-context strong consistency

Cross-context atomicity is high coupling.

Before using it, answer:

```text
What invariant spans contexts?
Why can temporary inconsistency not be tolerated?
Who owns transaction?
How would this work after service extraction?
What failure/rollback semantics exist?
```

If these answers are weak, prefer explicit eventual workflow.

---

# 13. SYS-DATA-005 — Choose consistency before implementation mechanism

Do not begin with:

```text
"Should I use a queue?"
```

Begin with:

```text
What must be true atomically?
What may become true later?
What does the user observe meanwhile?
```

Then choose transaction/event/process manager.

---

# 14. Eventual consistency

Use when:

- source can commit independently;
- downstream reaction may lag;
- retry is safe;
- consumer owns separate state;
- temporary divergence has acceptable semantics.

Typical model:

```text
source transaction
    state mutation
    +
    outbox fact
commit
    ↓
delivery/retry
    ↓
consumer idempotent handling
    ↓
consumer transaction/projection
```

---

# 15. SYS-DATA-006 — Source commit precedes downstream asynchronous effect

Async downstream effects must represent committed state.

Do not publish authoritative downstream fact before the source business commit is durable.

---

# 16. Outbox relationship

When source state and integration/delivery fact must stay consistent:

```text
business mutation
+
outbox enrollment
```

belong to the same durable transaction.

Exact outbox implementation is Platform/Infrastructure-owned.

---

# 17. Post-commit work

Work that requires successful commit must execute only after commit.

Examples:

- realtime publication;
- external email/provider effect;
- cache invalidation that assumes new durable state;
- integration delivery.

Do not let post-commit work observe rolled-back state as successful.

---

# 18. SYS-DATA-007 — Commit before irreversible external effect

Do not perform irreversible external provider effects inside a database transaction unless a specifically designed protocol requires it.

Default:

```text
persist intent/state/outbox
→ commit
→ deliver
→ retry/reconcile
```

This avoids holding database transaction open across uncertain external network effects.

---

# 19. External side-effect uncertainty

External calls may produce:

```text
success
known failure
unknown outcome
```

Unknown outcome is a first-class state.

A timeout may occur after provider accepted the request.

Retry without stable operation identity may duplicate side effect.

---

# 20. Provider operation identity

Retryable provider effects should have stable business/operation identity where provider semantics permit it.

Examples:

- idempotency key;
- external operation ID;
- sync cursor/revision;
- webhook event ID.

The identity must survive retry.

---

# 21. SYS-DATA-008 — At-least-once delivery requires idempotent consumption

Retries/duplicates are normal for durable async delivery.

Consumer correctness MUST handle duplicate delivery safely.

Idempotency identity must include enough boundary identity to avoid false deduplication.

---

# 22. Consumer identity

The same message may legitimately be consumed by several consumers.

Therefore dedup scope commonly includes:

```text
message identity
+
consumer identity
```

not only event name.

Exact backend representation is Platform-owned.

---

# 23. Operation identity

Command/request idempotency is separate from message dedup.

A use case may define stable operation identity for:

- client retry;
- provider retry;
- automation retry;
- background processing.

Do not reuse one generic key without understanding scope.

---

# 24. Ordering

Ordering is a consistency contract only where business behavior requires it.

Possible scopes:

- aggregate;
- resource;
- stream;
- connection;
- provider object.

Avoid global event-type ordering unless truly required.

---

# 25. SYS-DATA-009 — Ordering guarantee is no broader than the business invariant

If BoardItem updates require per-item ordering, do not impose global Board event serialization.

Broader ordering reduces scalability and increases coupling.

---

# 26. Sequence state

Where order matters, define:

- sequence identity;
- current/expected version;
- advancement point;
- duplicate behavior;
- gap behavior;
- retry behavior.

Do not advance ordering state before successful processing/commit.

---

# 27. Concurrency

Concurrency protects against stale competing writes.

Possible mechanisms:

- aggregate/version token;
- row version;
- expected version;
- unique constraint;
- lock;
- provider revision.

Choose according to invariant.

---

# 28. SYS-DATA-010 — Concurrency conflicts fail closed

When an operation requires an expected version, missing/stale concurrency evidence must not silently degrade to last-write-wins unless product contract explicitly permits it.

Conflict is a meaningful outcome.

---

# 29. Optimistic concurrency

Typical conceptual flow:

```text
read version
→ propose mutation with expected version
→ validate/commit if current
→ conflict otherwise
```

Frontend may optimistically project state.

Server concurrency remains authoritative.

---

# 30. Database constraints

Database constraints provide race-safe durable protection for appropriate invariants.

Examples:

- unique;
- FK;
- check;
- concurrency;
- RLS;
- indexes for queryability.

Application prechecks can improve UX/error quality.

They do not replace race-safe constraints when durable invariant requires them.

---

# 31. SYS-DATA-011 — Durable invariant protection belongs at every necessary layer

Use:

```text
Domain
Application
database
```

for different aspects.

Do not duplicate business semantics mechanically.

Example:

```text
Domain
→ business rule

database unique constraint
→ race-safe enforcement

Application
→ friendly precheck/orchestration
```

---

# 32. RLS relationship

RLS is persistence-layer defense in depth.

It protects tenant row access.

It does not:

- authenticate caller;
- decide full business authorization;
- define product ownership.

RLS session/transaction context must align with request/background tenant scope.

---

# 33. Query consistency

Queries may read:

- authoritative normalized state;
- read model;
- projection;
- cache;
- analytics snapshot.

The query contract must define relevant freshness semantics.

Do not present eventually consistent projection as strongly current if product behavior depends on recency.

---

# 34. Read models

Read models may combine facts for efficient query/display.

They must identify:

- source owners;
- projection owner;
- freshness;
- rebuild/recovery;
- tenant/security scope;
- queryability.

A read model is not a new business owner.

---

# 35. Projection ownership

Projection owner is responsible for:

- projection schema;
- update/rebuild;
- consumer correctness;
- lag handling.

Source context remains responsible for source fact.

---

# 36. SYS-DATA-012 — Projections are disposable relative to source truth

Where feasible, derived projection should be rebuildable from authoritative source/events.

If it cannot be rebuilt, classify whether it is actually authoritative state and document ownership accordingly.

Do not call irreplaceable state a “cache”.

---

# 37. Projection rebuild

A rebuild plan should consider:

- source snapshot;
- historical events;
- current schema/version;
- tenant partitioning;
- ordering;
- duplicate handling;
- backfill resource cost.

Not every projection requires event replay if authoritative query rebuild is simpler.

---

# 38. Search index

Search index is derived.

It needs:

- source identity;
- tenant/security scope;
- update path;
- lag semantics;
- deletion/tombstone handling;
- rebuild path.

Search result cannot authorize access merely because a document appears in index.

---

# 39. Analytics projections

Analytics can intentionally lag.

Metric/report semantics should state freshness expectations.

Analytics projection should not be used for source mutation invariant unless explicitly designed.

---

# 40. Dashboard projections

Dashboard may aggregate several contexts.

It does not become owner of source records.

If dashboard supports actions, route those actions to source context Application contract.

---

# 41. Cache taxonomy

Caches may hold:

```text
resource data
query result
permission result
provider metadata
computed configuration
```

Each has different invalidation/security risk.

Do not treat “Redis” as one consistency policy.

---

# 42. SYS-DATA-013 — Cache identity includes semantic scope

A cache key must include sufficient identity to avoid collisions across relevant:

- tenant/account/workspace;
- resource;
- permission/version;
- query parameters;
- contract version

as applicable.

Exact key design belongs to local owner.

---

# 43. Cache invalidation

Invalidation should follow authoritative mutation/commit.

Do not invalidate/overwrite cache as if mutation succeeded before durable commit unless the cache operation is explicitly provisional.

---

# 44. Cache miss

Cache miss must have safe fallback:

- query source;
- reconstruct;
- fail explicitly.

Cache absence must not silently alter authorization/business semantics.

---

# 45. Cache outage

Cache outage behavior is product/runtime policy.

Possible:

- degrade to source;
- disable optional optimization;
- reject if cache is security-critical and safe fallback unavailable.

Do not default to fail-open for permission/security cache.

---

# 46. Permission cache

Permission cache is derived security-sensitive state.

It needs:

- subject;
- resource/scope;
- permission/policy version;
- invalidation/freshness;
- fail-closed behavior when required.

Detailed security architecture is backend-owned.

---

# 47. Frontend server-state cache

Frontend query cache is a client-side projection of server state.

It may be:

- stale;
- optimistic;
- invalidated;
- patched by realtime.

It is not durable source of truth.

---

# 48. SYS-DATA-014 — Frontend cache must not outlive its authority scope

Workspace/account/resource transitions must prevent old-scope state from overwriting or leaking into new scope.

Client cache identity/reconciliation must preserve tenant boundaries.

---

# 49. Stale HTTP response

A request initiated in Workspace A may complete after switch to Workspace B.

Frontend must reject/prevent stale response from mutating current B state.

Detailed implementation belongs to frontend state docs.

---

# 50. Optimistic updates

Optimistic UI is provisional.

It requires:

- admission rule;
- snapshot;
- deterministic rollback or reconciliation;
- conflict behavior;
- authoritative response;
- realtime race handling.

Do not make optimistic state permanent merely because request is slow.

---

# 51. SYS-DATA-015 — Optimistic state is not business commit

UI feedback may precede durable server commit.

Product UX must distinguish provisional/failed/conflicted outcomes where material.

---

# 52. Realtime and consistency

Realtime may:

- patch;
- invalidate;
- notify.

It should include enough identity/version/scope for safe reconciliation.

If message order/gap is uncertain, recover from authoritative query state.

---

# 53. Duplicate realtime event

Duplicate handling should be:

- idempotent;
- bounded-dedup;
- version-aware.

Do not store unbounded seen-event IDs on client.

---

# 54. Out-of-order realtime

Use:

- resource version;
- sequence;
- timestamp only if contract gives it ordering meaning.

Do not infer authoritative order from arrival time.

---

# 55. Gap recovery

A sequence gap means client certainty is lost.

Safe response:

```text
mark uncertain
→ refetch authoritative state
→ resume from known point
```

Do not silently skip missing facts.

---

# 56. Data lifecycle

Ownership includes:

- create;
- update;
- archive;
- delete;
- restore where product defines it;
- retention;
- reference handling.

Deletion is a product/data-consistency event, not merely SQL row removal.

---

# 57. SYS-DATA-016 — Delete/archive policy belongs to semantic owner

Database cascade may implement part of deletion.

It cannot define the product policy.

Cross-context references require explicit handling:

- retain reference;
- tombstone;
- anonymize;
- detach;
- react asynchronously;
- deny deletion.

---

# 58. Hard delete

Hard delete requires explicit product/security/retention justification.

Do not assume hard delete because a row is “child data”.

Evidence should show downstream reference/replay/audit impact.

---

# 59. Soft delete/archive

Soft delete/archive should be a product lifecycle state only when semantics require it.

Do not create universal SoftDeleted enum across contexts.

Each owner defines lifecycle.

---

# 60. Cross-context deletion

Source context should not directly traverse and delete all downstream state.

Prefer explicit policies/contracts/events.

Some dependent state may intentionally outlive source reference for:

- audit;
- history;
- legal retention.

---

# 61. Data retention

Retention can vary by context/data classification.

Product/business retention, security audit retention, and operational-log retention are different policies.

Do not infer one from another.

---

# 62. Data migration

Schema/data migration is production behavior.

It must preserve:

- compatibility;
- ownership;
- tenant scope;
- constraints;
- rollback/roll-forward strategy;
- application mixed-version requirements.

Exact EF migration process is backend-owned.

---

# 63. SYS-DATA-017 — Schema migration does not silently change semantic ownership

Moving columns/tables is not automatically a context move.

Moving authoritative business fact to a new context requires product/system migration beyond schema.

---

# 64. Expand-contract data migration

Typical safe sequence:

```text
expand schema
→ deploy compatible writers/readers
→ backfill/migrate
→ switch authority/read-write path
→ verify
→ remove old representation
```

The exact sequence depends on compatibility.

---

# 65. Backfill

Backfill must define:

- source;
- target;
- idempotency;
- batching;
- tenant scope;
- failure recovery;
- verification;
- coexistence with live writes.

Large backfills require operational planning.

---

# 66. Dual write

Dual write across stores/representations is risky.

If unavoidable during migration, define:

- authoritative side;
- write order;
- failure behavior;
- reconciliation;
- removal condition.

Do not introduce indefinite dual ownership.

---

# 67. SYS-DATA-018 — Dual write does not imply dual authority

During migration, two representations may be written.

Exactly one semantic source of truth must remain explicit at each phase.

---

# 68. Read switch

When moving reads to new projection/store:

- prove backfill completeness;
- compare results;
- handle lag;
- maintain fallback if required;
- define cutover.

Do not remove old writer/data before consumer/read migration proof.

---

# 69. Process manager / saga

Use when workflow spans multiple independent owners and needs durable workflow state.

A process manager may own:

- workflow state;
- step progress;
- correlation;
- retry/compensation policy.

It does not own participant business state.

---

# 70. Process manager admission

Use only when:

```text
multi-step
cross-owner
long-running/retryable
workflow state itself matters
```

Do not use for a simple synchronous chain of two Application calls.

---

# 71. Compensation

Compensation is not automatic rollback.

It is a new business action attempting to restore/mitigate after prior committed actions.

Define:

- when allowed;
- idempotency;
- failure;
- manual intervention.

---

# 72. Workflow state

Process-manager state is authoritative only for workflow progress.

Participant resource state remains participant-owned.

Do not store shadow copies of full participant aggregates as workflow truth.

---

# 73. Cross-context synchronous orchestration

Sometimes one Application use case synchronously calls/coordinates several owners.

Keep:

- primary use-case owner;
- target owner validation;
- transaction/failure semantics.

Avoid direct cross-context repository mutation.

---

# 74. Cross-context asynchronous orchestration

Use events/process manager when:

- independent commit is acceptable;
- retry required;
- service extraction seam valuable;
- external/provider effect involved.

Temporary inconsistency must have product semantics.

---

# 75. User-visible eventual consistency

If user can observe lag, define UX behavior.

Examples:

- “syncing”;
- “pending”;
- stale indicator;
- retry state;
- background completion;
- conflict notice.

Do not hide long eventual workflows as instantaneous success.

---

# 76. SYS-DATA-019 — Consistency promise includes user-visible semantics

If backend is eventually consistent but UI promises immediate finality, the system contract is inconsistent.

Product/design/frontend must represent material pending/uncertain states truthfully.

---

# 77. Cross-context read consistency

A composed read may combine states from different times.

If exact atomic snapshot is required, justify and implement it.

Otherwise define acceptable freshness.

Do not accidentally promise snapshot consistency for dashboards/reports.

---

# 78. Snapshot/report consistency

Reports may need:

- “as of” timestamp;
- snapshot version;
- data freshness indicator.

This is product Analytics semantics.

---

# 79. Event-sourced versus state-sourced

Notrelix does not require event sourcing as a general architecture.

Events may support:

- integration;
- audit;
- projections;
- workflow.

Authoritative current state can remain relational aggregate state.

Do not treat event presence as event-sourcing architecture.

---

# 80. Event history

If historical events are not a complete replayable source of truth, do not claim projections can always rebuild from event log.

Choose actual rebuild source.

---

# 81. Outbox is not event store

Outbox exists for reliable delivery.

It is not automatically:

- permanent event history;
- audit log;
- event-sourcing store.

Retention/replay semantics are Platform/operations concerns.

---

# 82. Audit is not source business state

Audit records evidence actions.

They should not become the only representation of current resource state unless a specific architecture explicitly says so.

---

# 83. Activity feed is not audit

User-facing Collaboration activity can be incomplete/curated differently from security audit.

Do not use one as a substitute for the other.

---

# 84. Idempotency state lifecycle

Idempotency/dedup records are operational correctness state.

They need:

- identity;
- scope;
- result/state lifecycle;
- retention;
- conflict behavior.

Exact schema is backend-owned.

---

# 85. Poison/dead-letter state

Dead-letter/poison records preserve enough identity/context for:

- diagnosis;
- replay;
- consumer ownership;
- tenant safety.

They do not change source business ownership.

---

# 86. Retry policy

Retry must distinguish:

```text
transient
terminal
unknown/uncertain
poison/deterministic invalid
```

Retry everything forever is not reliability.

---

# 87. SYS-DATA-020 — Retry is bounded by semantics, not generic transport policy alone

Business/provider operation may have side-effect constraints that determine whether retry is safe.

Platform provides mechanism.

Product/Application/Integration semantics determine admissibility.

---

# 88. Manual replay

Replay must define:

- identity reuse;
- dedup handling;
- current contract compatibility;
- tenant scope;
- side-effect safety;
- observability.

Do not replay a message by manually cloning it with a new identity unless intended semantics justify a new operation.

---

# 89. Recovery

For each derived/transport state, know whether recovery is:

- rebuild;
- refetch;
- replay;
- reconcile;
- manual repair.

Irrecoverable state should be classified as authoritative, not casually treated as derived.

---

# 90. Disaster recovery

Backup/restore may temporarily reintroduce older state.

Recovery design should consider:

- message queues/outbox;
- provider side effects;
- cache/projections;
- idempotency records;
- client state.

Detailed operational recovery belongs to Operations.

---

# 91. Data ownership after service extraction

When a context is extracted:

- semantic owner remains same;
- physical database may move;
- synchronous calls may become network contracts;
- transaction boundaries narrow;
- projections/events may become more important.

If semantic ownership must be reinvented during extraction, pre-extraction boundaries were insufficient.

---

# 92. Distributed transaction avoidance

Notrelix does not adopt distributed transactions as default cross-service strategy.

Prefer:

- local commit;
- durable event/outbox;
- idempotent consumer;
- process manager/compensation.

A distributed transaction requires explicit architecture decision.

---

# 93. Shared database during modular monolith

Shared DB can support:

- efficient local transactions;
- consistent backup;
- operational simplicity.

But it must not encourage:

- foreign-context writes;
- ownership-blind joins in mutation paths;
- schema coupling that prevents extraction.

---

# 94. Cross-context joins

Read-only joins may be acceptable for optimized reporting/read models if:

- ownership understood;
- security scope enforced;
- dependency documented;
- extraction impact accepted.

Do not use such joins as hidden mutation/business coupling.

---

# 95. Queryability

Flexible product state must remain queryable at enterprise scale.

Example Work Management dynamic values may use:

- flexible canonical representation;
- typed/query-optimized projections/indexes.

Do not force every dynamic field into fixed relational columns.

Do not parse unlimited arbitrary JSON for every large filter/report workload.

Exact model is Infrastructure/Product-owned.

---

# 96. JSON / polymorphic data

JSON is appropriate for genuinely flexible configuration/content.

It is not an excuse to avoid schema/version/query design.

Persisted polymorphism should define:

- discriminator;
- schema version;
- compatibility;
- migration.

---

# 97. Referential integrity

FKs can protect valid references inside one physical DB.

They do not imply aggregate/context ownership.

Service extraction may replace physical FK with contract validation/reference lifecycle.

Semantic reference policy must survive.

---

# 98. Uniqueness

Uniqueness must define scope.

Examples:

```text
global
Account
Workspace
parent/resource
provider connection
```

Application precheck plus database constraint may be appropriate.

Do not infer scope from one index accidentally.

---

# 99. Tenant partitioning

Tenant scope should be included in:

- relevant uniqueness;
- cache;
- projection;
- background processing;
- query filters/RLS.

Do not use global keys for tenant-local identity unless identity is truly global.

---

# 100. Data and authorization

Data existence does not imply access.

Queries/projections/caches must preserve authorization semantics.

Search/analytics/realtime must not bypass source security because they are “read only”.

---

# 101. Export

Export is a data boundary.

It requires:

- authorization;
- tenant scope;
- sensitive-data rules;
- consistency/freshness semantics;
- potentially large-query behavior.

Exported snapshot becomes external data outside normal server control.

---

# 102. Import

Import mutates authoritative contexts through their contracts.

Do not bulk-write tables bypassing:

- invariants;
- authorization;
- identity mapping;
- idempotency;
- validation

unless a specifically governed migration/import path exists.

---

# 103. Sync

Provider sync is bidirectional consistency between independently authoritative systems.

Define per field/resource:

- Notrelix authoritative;
- provider authoritative;
- merge/conflict policy;
- revision identity;
- retry;
- deletion semantics.

“Two-way sync” without per-fact ownership is ambiguous.

---

# 104. Conflict resolution

Conflict policy belongs to semantic owner.

Possible:

- reject stale write;
- last-write-wins where product permits;
- field-level merge;
- user resolution;
- provider-authoritative overwrite.

Do not make generic Infrastructure choose product conflict policy.

---

# 105. Clock/time consistency

Wall-clock timestamps are useful metadata.

They are not automatically safe ordering/concurrency tokens across distributed systems.

Use explicit version/sequence when correctness depends on order.

---

# 106. Time-based cache freshness

TTL is a performance freshness mechanism.

It is not sufficient for every correctness/security cache.

Use event/version invalidation where stale data would be unsafe.

---

# 107. Data visibility during transaction

External systems/clients must not observe uncommitted business success.

Avoid:

- publish before commit;
- send final success realtime before commit;
- irreversible provider call before commit without designed protocol.

---

# 108. Failure after commit

After commit, downstream effect may fail.

The system must preserve:

- source success;
- pending/retry state;
- operational evidence;
- truthful client/product status where material.

Do not roll back an already committed local transaction conceptually by pretending it never happened.

---

# 109. Partial cross-context success

In eventual workflows, partial completion is expected.

Design:

- workflow state;
- retry;
- compensation;
- user visibility;
- manual recovery.

Do not hide partial state inside generic exception logging.

---

# 110. Consistency decision matrix

| Need | Preferred model |
|---|---|
| One aggregate invariant | local transaction |
| Several roots, truly atomic same use case | explicit Application transaction |
| Cross-context reaction tolerates lag | outbox + event + idempotent consumer |
| Long-running multi-step workflow | process manager/saga |
| External irreversible provider effect | commit intent then post-commit delivery |
| Read optimization | projection/read model |
| Performance-only duplicate | cache |
| Search | secured derived index |
| Analytics/reporting | derived projection with freshness semantics |
| Client state | server-authoritative query cache + reconciliation |

This is guidance.

Business invariant decides final choice.

---

# 111. Consistency change workflow

For a change in consistency model:

```text
1 identify authoritative owner
2 identify invariant
3 define commit boundary
4 classify sync/eventual
5 define identity/concurrency
6 define outbox/postcommit if needed
7 define retry/idempotency
8 define projection/cache effects
9 define client-visible state
10 define failure/recovery
11 update source/tests/contracts
12 ADR if consequential
```

---

# 112. Data ownership checklist

```text
[ ] authoritative owner
[ ] lifecycle owner
[ ] physical store identified
[ ] tenant/account/workspace scope
[ ] authorization boundary
[ ] mutation contract
[ ] concurrency rule
[ ] derived copies inventoried
[ ] deletion/retention policy
[ ] migration/extraction impact
```

---

# 113. Consistency checklist

```text
[ ] invariant requiring consistency stated
[ ] commit point stated
[ ] transaction owner stated
[ ] temporary inconsistency acceptable/not acceptable explained
[ ] downstream effects classified
[ ] stable operation/message identity
[ ] retry safety
[ ] ordering requirement
[ ] duplicate behavior
[ ] projection/cache freshness
[ ] user-visible pending/conflict state
[ ] recovery/replay strategy
```

---

# 114. Projection checklist

```text
[ ] source owner
[ ] projection owner
[ ] scope/security
[ ] freshness expectation
[ ] update mechanism
[ ] duplicate/order/gap handling
[ ] rebuild source
[ ] deletion/tombstone behavior
[ ] failure/degradation behavior
```

---

# 115. Cache checklist

```text
[ ] authoritative source
[ ] semantic key scope
[ ] security sensitivity
[ ] freshness
[ ] invalidation
[ ] miss fallback
[ ] outage behavior
[ ] write timing relative to commit
[ ] cross-workspace/account isolation
```

---

# 116. Async workflow checklist

```text
[ ] source fact committed
[ ] outbox/enrollment atomic if required
[ ] message identity
[ ] consumer identity
[ ] tenant scope
[ ] idempotency
[ ] retry classification
[ ] ordering scope
[ ] poison/dead-letter
[ ] provider uncertainty
[ ] manual replay safety
[ ] observability
```

---

# 117. Process-manager checklist

```text
[ ] multi-step workflow justified
[ ] participant owners explicit
[ ] workflow state owner
[ ] correlation identity
[ ] step idempotency
[ ] retries
[ ] compensation
[ ] terminal state
[ ] manual intervention
[ ] removal/completion semantics
```

---

# 118. Stop conditions

Stop rather than guess if:

- one fact appears authoritative in two contexts;
- “cache” cannot be rebuilt and ownership is unclear;
- transaction spans contexts only because DB is shared;
- provider timeout has unknown outcome but retry identity absent;
- async consumer identity/dedup scope unclear;
- ordering is required but stream/sequence undefined;
- user-facing success is shown before durable commit without provisional semantics;
- deletion policy depends on raw DB cascade only;
- dual-write migration has no authoritative side;
- search/analytics projection can bypass authorization.

---

# 119. Related canonical owners

```text
docs/architecture/system-overview.md
docs/architecture/bounded-context-map.md
docs/architecture/contract-boundaries.md
docs/architecture/events-realtime-and-delivery-boundary.md

backend/docs/architecture/domain-modeling.md
backend/docs/architecture/application-model.md
backend/docs/architecture/infrastructure-and-data.md
backend/docs/architecture/platform-and-messaging.md
backend/docs/architecture/security-tenancy-authorization.md
backend/docs/operations/migrations-and-data-change.md

frontend/docs/architecture/state-query-mutations.md
frontend/docs/architecture/realtime.md

docs/delivery/change-impact-and-migration.md
docs/operations/recovery-and-data-safety.md
```

---

# 120. Final data rule

For every material piece of state, Notrelix must be able to answer:

```text
Who owns the fact?
Where is authoritative state?
Which copies are derived?
What must be atomic?
What may become consistent later?
What identity protects retries/concurrency?
What happens on duplicate/order/failure?
How do clients learn/recover?
How is the state deleted/retained?
How can projections/caches be rebuilt?
How would ownership survive service extraction?
```

The architecture is healthy when:

> **ownership remains singular, consistency is intentional, derived state is recoverable, and failure does not create a second hidden source of truth.**
