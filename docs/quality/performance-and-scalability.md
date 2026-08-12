---
document_id: QLT-PERFORMANCE
document_type: quality-standard
status: active
owner: engineering-quality
applies_to:
  - repository
  - backend
  - frontend
  - api
  - data
  - messaging
  - realtime
  - analytics
  - integrations
evidence:
  - docs/quality/engineering-quality-standard.md
  - docs/quality/testing-strategy.md
  - docs/quality/security-quality-standard.md
  - docs/product/contexts/work-management.md
  - docs/product/contexts/documents.md
  - docs/product/contexts/collaboration.md
  - docs/product/contexts/automation.md
  - docs/product/contexts/integrations.md
  - docs/product/contexts/analytics.md
  - docs/architecture/data-ownership-and-consistency.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/platform-and-messaging.md
  - frontend/docs/architecture/state-query-mutations.md
  - frontend/docs/architecture/realtime.md
review_on:
  - scalability-model-change
  - query-model-change
  - caching-model-change
  - projection-or-index-change
  - messaging-fanout-change
  - realtime-model-change
  - frontend-bundle-or-rendering-change
  - performance-gate-change
  - operations-slo-change
---

# Performance and Scalability

> **Performance is a product, data-model, and execution-shape property. It is not a late micro-optimization phase.**
>
> Notrelix should remain bounded and predictable as Workspaces, Boards, Items, Pages, collaboration history, automation executions, integrations, and analytical data grow.

This document owns repository-wide performance and scalability quality requirements.

It does **not** invent universal latency/SLO numbers. Approved production SLOs, alert thresholds, capacity targets, and runtime budgets belong to Operations.

The purpose here is to define what engineering work must remain bounded, measurable, ownership-safe, and scalable before production SLOs can be trusted.

# 1. Performance model

Performance reasoning begins with:

```text
workload
×
cardinality
×
query/execution shape
×
concurrency
×
fan-out
×
failure/retry behavior
```

not with isolated micro-benchmark numbers.

# 2. QLT-PERF-001 — Tenant-scale work is bounded

List, search, filter, report, export, background, and UI paths MUST NOT assume tenant collections remain small.

Potentially large collections use as appropriate:

```text
pagination
cursor/windowing
bounded batch
index-backed query
projection
streaming
virtualization
```

# 3. Unbounded work

Warning signs include:

```text
ToList() over full tenant collection
load all Items then filter
deserialize every dynamic value to answer one query
broadcast invalidation to entire application
retry without attempt/backoff bound
render every row of a large dataset
```

# 4. QLT-PERF-002 — Complexity is reviewed against expected cardinality

For a changed hot path, identify relevant cardinalities:

```text
Workspaces per Account
members per Workspace
Boards per Workspace
Fields per Board
Items per Board
Blocks per Page
comments/activity per resource
Automation executions
Integration mappings
Analytics dimensions
```

A locally fast operation can still be unacceptable if its complexity grows with an unbounded collection unnecessarily.

# 5. Big-O is not sufficient

Asymptotic reasoning is useful, but production cost also depends on:

- network round trips;
- database plan;
- indexes;
- serialization;
- allocations;
- lock duration;
- cache behavior;
- fan-out;
- provider limits.

# 6. QLT-PERF-003 — Optimize only after semantic ownership is correct

Caching, denormalization, materialized projections, replicas, memoization, and batching MUST preserve:

```text
authoritative owner
tenant scope
authorization
concurrency
invalidation
rebuild/recovery
```

Performance does not justify duplicate writable business truth.

# 7. Source versus projection

A projection may be faster to query.

It remains derived unless architecture explicitly transfers ownership.

# 8. QLT-PERF-004 — Read optimization never creates a second mutation authority

Examples:

```text
Analytics projection
search index
typed Item-value materialization
unread count
reaction count
workload projection
```

remain derived from named authoritative sources.

# 9. Database query shape

Hot queries should be reviewed for:

```text
selected columns
predicate selectivity
joins
includes
N+1
sort
pagination
index usage
row width
cardinality
tenant predicate
RLS interaction
```

# 10. QLT-PERF-005 — Query shape is reviewed with data shape

A query that is efficient on 20 development records is not evidence for a Board with enterprise-scale data.

Representative data shape matters.

# 11. N+1

Repeated per-row queries on list/detail composition should be identified and removed when they scale with result size.

Avoid replacing N+1 with one enormous Cartesian product without measuring.

# 12. QLT-PERF-006 — Round trips and result amplification are both bounded

The solution to N+1 should preserve manageable:

```text
query count
row count
payload size
memory
```

# 13. Pagination

Server endpoints returning potentially large collections require explicit pagination/window semantics unless the bounded maximum is a product invariant.

# 14. QLT-PERF-007 — Pagination is part of the query contract

Define:

```text
stable order
page/cursor identity
limit/max
continuation
concurrent-change behavior
```

Do not add arbitrary pagination after the fact without considering compatibility and ordering.

# 15. Offset versus cursor

Offset pagination may be acceptable for bounded/admin cases.

High-churn/large collections may require cursor/keyset semantics for stability and performance.

The choice follows workload.

# 16. Sorting

Sort fields used at scale should have an executable query/index strategy.

Client-side sorting after downloading an entire large tenant collection is not a server scalability strategy.

# 17. Filtering

Filters should be translated to scalable server/source queries when data size warrants it.

# 18. QLT-PERF-008 — Common filter/sort paths are indexable or intentionally projected

New filterable fields require review of:

```text
type
selectivity
index/materialization
tenant key
sort compatibility
null behavior
```

# 19. Dynamic Work Management values

Flexible Board Item values are a known scalability boundary.

# 20. QLT-PERF-009 — Dynamic values do not require full JSON scans for recurring hot queries

For common filter/sort/group/report behavior, use typed/indexed/materialized strategies where required.

Do not deserialize all Item JSON in application memory.

# 21. JSON/polymorphic data

Flexible storage is allowed for extensible configuration/content when ownership is clear.

Frequently queried properties need an indexing/projection strategy.

# 22. Indexes

Indexes should follow real query predicates/order/cardinality.

More indexes are not automatically better.

# 23. QLT-PERF-010 — Index cost is part of the change

Review:

```text
read benefit
write amplification
storage
migration/build time
lock/concurrency impact
tenant prefix/selectivity
```

# 24. Composite tenant indexes

Tenant-scoped hot queries often need tenant/resource prefixes compatible with actual predicates and RLS behavior.

Do not assume a globally unique ID index solves every tenant query.

# 25. Index migration

Large-index creation/removal can be an operational migration, not a trivial schema edit.

Rollout/recovery belongs to Delivery/Operations.

# 26. Transactions

Transactions should protect required atomicity while minimizing unnecessary lock duration and external waits.

# 27. QLT-PERF-011 — External I/O does not extend source DB transaction unnecessarily

Provider/network calls should generally not occur inside the transaction that owns source state.

Use post-commit/outbox/orchestration as architecture defines.

# 28. Transaction scope

Avoid loading/updating unrelated aggregates in one transaction solely for convenience.

Strong consistency has coupling and performance cost.

# 29. Concurrency

Optimistic concurrency can preserve throughput by avoiding broad locking where product semantics fit.

High-contention resources may require explicit conflict/partitioning strategy.

# 30. QLT-PERF-012 — Contention is measured at the real coordination key

Examples:

```text
same Board ordering range
same Workspace quota
same Subscription
same Automation occurrence
same Integration mapping
```

Global locks for tenant-local invariants are suspicious.

# 31. Connection pools

Database/Redis/provider HTTP connection pools are finite shared resources.

Long-running work must not monopolize them unnecessarily.

# 32. QLT-PERF-013 — Resource usage is bounded per request/job

Review:

```text
DB connections
HTTP connections
threads/tasks
memory
file handles
message prefetch
provider concurrency
```

# 33. Memory

Avoid materializing large payloads/collections when streaming/batching is sufficient.

Large binary content should not traverse Domain/event payloads.

# 34. QLT-PERF-014 — Payload size is a first-class performance property

Review large:

```text
HTTP response
message/event
realtime payload
snapshot
export
provider payload
frontend bundle
```

# 35. API payloads

Select only consumer-needed fields for large collections.

Do not serialize full aggregate graphs by convenience.

# 36. QLT-PERF-015 — List contract is intentionally narrower than detail contract

A list/search endpoint should not automatically return the full detail object for every row.

# 37. Compression

Compression can reduce network cost but consumes CPU and does not excuse over-broad payload design.

# 38. Caching

Cache is useful for:

- expensive repeat reads;
- safe derived state;
- reference/config data.

It is not required for every query.

# 39. QLT-PERF-016 — Cache has measurable reason to exist

Before adding cache, identify:

```text
expensive operation
reuse pattern
scope
TTL/invalidation
staleness tolerance
memory/cardinality
fallback
```

# 40. Cache stampede

High-demand cache misses may need request coalescing/bounded regeneration where relevant.

# 41. QLT-PERF-017 — Cache failure does not trigger unbounded origin amplification

A cache outage/mass expiry must not cause an uncontrolled fan-in to database/provider if the workload warrants protection.

# 42. Cache key cardinality

Per-user/per-resource caches can explode in cardinality.

Measure expected key count and retention.

# 43. QLT-PERF-018 — Cache key is semantically scoped and cardinality-aware

Correctness keys such as tenant/principal/version are required even if they reduce hit rate.

Never remove security/semantic dimensions for cache efficiency.

# 44. Invalidation

Broad invalidation such as “invalidate everything in app/workspace” can become a scaling problem.

Prefer resource/query-family scope when product semantics allow.

# 45. QLT-PERF-019 — Invalidation fan-out is bounded

One small Item change should not force every unrelated query/widget/page to refetch.

# 46. Background work

Expensive/non-interactive work may move to background execution when product semantics allow.

Async is not automatically faster; it changes latency/failure shape.

# 47. QLT-PERF-020 — Async work has backpressure

Queues/workers define as applicable:

```text
bounded concurrency
prefetch
retry/backoff
dead-letter/poison handling
rate limit
tenant fairness
```

# 48. Queue backlog

A growing backlog is a capacity/failure signal.

Consumers should expose enough metrics for Operations to detect lag.

# 49. QLT-PERF-021 — Retry load is part of capacity planning

A dependency outage can multiply traffic through retries.

Use bounded exponential/provider-aware backoff and idempotency.

# 50. Retry storms

Do not synchronize retry timing across thousands of jobs when jitter is appropriate.

# 51. Tenant fairness

One large/noisy tenant should not monopolize shared workers/provider quota if a fairer bounded design is required.

# 52. QLT-PERF-022 — Shared capacity has a noisy-neighbor strategy

Possible mechanisms include:

```text
tenant-aware partitioning
per-tenant concurrency
rate limit
weighted queue
quota
backpressure
```

Choose only where workload justifies it.

# 53. Messaging fan-out

An event may trigger many consumers.

Each consumer should need the fact and have bounded work.

# 54. QLT-PERF-023 — Event fan-out is intentional

Do not publish broad “everything changed” events causing many consumers to refetch entire aggregates/tenants.

# 55. Event payload

Prefer stable changed facts/IDs over huge full snapshots unless snapshot semantics are explicitly required.

# 56. Ordering

Ordering guarantees can reduce parallelism.

Apply ordering only to the business scope that requires it.

# 57. QLT-PERF-024 — Ordering scope is no broader than invariant scope

If only one aggregate/resource stream requires order, do not globally serialize all tenant events.

# 58. Automation

Automation can amplify one event into many Actions and recursive events.

# 59. QLT-PERF-025 — Automation has runaway-work bounds

Review:

```text
recursion depth
rule count matched
actions per execution
retry count
scheduled catch-up
workspace quota
provider concurrency
```

# 60. Scheduled work

Missed-fire catch-up must be bounded.

Do not replay years of missed hourly jobs in one uncontrolled burst.

# 61. Integrations

External providers impose:

- rate limits;
- latency;
- pagination;
- burst constraints;
- webhook retry;
- quota.

# 62. QLT-PERF-026 — Provider throughput respects provider capability

Use provider-specific batching, incremental cursors, backoff, and bounded parallelism.

Do not treat provider API as local database.

# 63. Integration sync

Initial/full sync uses checkpointing and bounded pages.

Incremental sync should avoid full provider rescans when provider supports cursors/revisions.

# 64. Webhooks

Webhook ingress should authenticate and acknowledge/process according to provider contract without doing expensive synchronous product work unnecessarily.

# 65. QLT-PERF-027 — Webhook request path is bounded

Signature verification and durable intake may be synchronous.

Large downstream sync/mutation fan-out should move to durable async processing when appropriate.

# 66. Documents

Large Pages/Block trees require deliberate:

- loading granularity;
- history/snapshot strategy;
- editor rendering;
- search indexing.

# 67. QLT-PERF-028 — Document editing does not require full-history/full-workspace reload per edit

The current-edit path should operate on the bounded content/version context needed for correctness.

# 68. Document snapshots

Snapshots can improve recovery/read efficiency but consume storage and serialization cost.

Snapshot cadence is workload-driven.

# 69. Collaboration

Comments/activity/notifications grow indefinitely unless retention/pagination is explicit.

# 70. QLT-PERF-029 — Collaboration histories are paged/windowed

Do not load all comments/activity for a long-lived resource to render initial view.

# 71. Presence

Presence is ephemeral/high-frequency.

It should not create heavy durable writes for every cursor/heartbeat unless product explicitly requires historical persistence.

# 72. QLT-PERF-030 — Ephemeral signals stay lightweight

Presence/typing/cursor paths prioritize bounded payload, fan-out, expiry, and reconnect behavior.

# 73. Analytics

Analytics is derived and often high-cardinality.

Use pre-aggregation/materialized/indexed projections where recurring workload requires it.

# 74. QLT-PERF-031 — Dashboard requests do not compute arbitrary tenant-wide scans repeatedly

Recurring metrics should have scalable source query/projection strategies.

# 75. Reporting snapshots

Large reports may be asynchronous/artifact-based.

Do not hold interactive HTTP connection for unbounded export generation.

# 76. Search

Search/index is a derived capability intended for discovery-scale queries.

Search results still preserve tenant/authz filters.

# 77. QLT-PERF-032 — Performance optimization cannot weaken authorization filtering

A faster global index/query must still enforce tenant/resource/field visibility.

# 78. Frontend rendering

Large tables/boards/document trees should avoid rendering all off-screen nodes when virtualization/windowing is appropriate.

# 79. QLT-PERF-033 — Large frontend collections are windowed where measured need exists

Virtualization must preserve:

- accessibility;
- focus;
- selection;
- ordering;
- scroll behavior.

Do not add virtualization to tiny lists merely for fashion.

# 80. Frontend render cost

Measure before adding pervasive memoization.

Unnecessary memoization can increase complexity/memory and still fail to solve root state invalidation.

# 81. QLT-PERF-034 — Memoization follows measured render cost

Prefer correct component/state boundaries first.

# 82. Frontend query/cache

Query keys should be scoped and invalidation targeted.

Do not refetch all Workspace data after every mutation.

# 83. Realtime frontend

Realtime handlers should patch/invalidate the smallest safe resource/query family.

# 84. QLT-PERF-035 — Realtime event does not trigger application-wide fan-out by default

When the event cannot be safely patched, invalidate/refetch the affected authoritative query rather than the entire app.

# 85. Workspace switch

Dispose old subscriptions and avoid simultaneous unbounded loading of old/new tenant data.

# 86. Bundles

Web/mobile bundles should be reviewed when adding heavy dependencies to widely loaded entry paths.

# 87. QLT-PERF-036 — Dependency cost is evaluated at host boundary

A library acceptable in a lazy-loaded editor may be unacceptable in:

- app bootstrap;
- mobile bundle;
- marketing critical path.

# 88. Code splitting

Split by meaningful route/capability when it improves startup/use pattern.

Do not create excessive tiny chunks without evidence.

# 89. Mobile

Mobile has tighter:

- CPU;
- memory;
- network;
- battery;
- lifecycle

constraints.

# 90. QLT-PERF-037 — Mobile performance is not assumed from web performance

Native-safe dependency and runtime behavior require separate evidence where changed.

# 91. Network efficiency

Batch/debounce/coalesce user interactions only when semantics permit.

Do not hide lost updates.

# 92. Performance budgets

A surface may adopt explicit budgets for:

```text
latency
query count
payload size
bundle size
memory
throughput
projection lag
```

when an approved product/operations requirement exists.

# 93. QLT-PERF-038 — No universal performance number is invented in this document

Service/user SLOs and production thresholds belong to Operations and must be based on actual workload/product requirement.

# 94. Evidence

Performance-sensitive changes should provide representative evidence proportional to risk:

```text
query plan
benchmark
load test
profiling
allocation trace
bundle analysis
render profile
queue throughput
provider rate-limit simulation
```

# 95. QLT-PERF-039 — Performance evidence states workload assumptions

A benchmark without:

```text
data size
concurrency
environment
operation
warm/cold state
measurement method
```

is not reusable evidence.

# 96. Micro-benchmark

Useful for pure algorithm/hot routine.

It cannot prove end-to-end query/network/system scalability alone.

# 97. Query plan

For hot DB query, inspect plan under representative row/index distribution where feasible.

# 98. Load test

Load testing should exercise:

- realistic mix;
- concurrency;
- tenant distribution;
- failure/retry if relevant.

Avoid vanity single-endpoint throughput.

# 99. QLT-PERF-040 — Load test protects a stated capacity question

Example:

```text
Can a Workspace with N Items filter/sort under expected concurrent editors?
Can workers drain provider backlog after outage?
Can realtime fan-out remain bounded?
```

The test is tied to a decision, not a scoreboard.

# 100. Regression

When a performance regression caused real risk, preserve:

- benchmark;
- query plan guard;
- scenario load test;
- bundle budget

if deterministic and valuable.

# 101. Performance in CI

Not every benchmark belongs in required PR CI because shared runners can be noisy.

Deterministic structural guards can run in PR CI; heavier benchmarks/load tests may run in controlled environments/workflows when approved.

# 102. QLT-PERF-041 — Noisy benchmark is not a false precision gate

Do not fail PR on tiny timing differences from uncontrolled shared runners.

Use statistically/operationally meaningful thresholds and stable environments when timing becomes a gate.

# 103. Capacity changes

A change that increases expected:

- records;
- fan-out;
- payload;
- execution frequency;
- retention

should review capacity even if individual operations are unchanged.

# 104. QLT-PERF-042 — Retention is a scalability input

Infinite retention of:

- activity;
- messages;
- execution history;
- snapshots;
- audit;
- provider payloads

has storage/query/backup/recovery cost.

Retention follows product/legal/operations contracts.

# 105. Cost efficiency

Cloud/provider cost can reveal poor scaling:

- repeated full scans;
- excessive egress;
- unbounded provider calls;
- oversized snapshots;
- over-indexing.

Cost is not the only objective, but architecture should avoid obviously superlinear waste.

# 106. QLT-PERF-043 — Cost optimization cannot transfer semantic ownership

Cheaper storage/query strategy must preserve authoritative source and security.

# 107. Change impact — query/data shape

Review:

```text
indexes
pagination
selectivity
RLS
payload
cache
projection
migration
```

# 108. Change impact — event/background

Review:

```text
fan-out
ordering
retry
backpressure
tenant fairness
queue lag
idempotency
```

# 109. Change impact — frontend dependency/state

Review:

```text
bundle
render
query invalidation
realtime fan-out
mobile
accessibility
```

# 110. Change impact — retention

Review:

```text
storage growth
query indexes
backup/restore
privacy
snapshot/export
purge
```

# 111. Query checklist

```text
[ ] tenant scope
[ ] expected cardinality
[ ] bounded result
[ ] stable order
[ ] selected columns
[ ] N+1/result amplification
[ ] filter/sort indexes
[ ] JSON/materialization strategy
[ ] RLS/index interaction
[ ] representative plan/evidence if hot
```

# 112. Background-work checklist

```text
[ ] logical work identity
[ ] bounded batch
[ ] concurrency
[ ] backpressure
[ ] retry/backoff
[ ] poison/dead-letter
[ ] tenant fairness
[ ] provider limit
[ ] observability
[ ] recovery after backlog
```

# 113. Frontend checklist

```text
[ ] initial payload/bundle impact
[ ] large-list rendering
[ ] query-key scope
[ ] invalidation fan-out
[ ] realtime fan-out
[ ] expensive render measured
[ ] mobile impact
[ ] accessibility preserved
```

# 114. Performance evidence checklist

```text
[ ] performance question stated
[ ] workload/data size stated
[ ] environment stated
[ ] baseline stated where comparative
[ ] measurement method
[ ] result
[ ] semantic/security correctness preserved
[ ] regression guard chosen if valuable
```

# 115. Stop conditions

Stop rather than merge if:

- tenant-scale path loads an entire unbounded collection without product invariant;
- recurring Board filter/report requires full arbitrary JSON scan;
- one mutation invalidates/refetches entire app/Workspace without need;
- provider/retry path has unbounded concurrency/backoff;
- source DB transaction waits on external provider;
- cache optimization removes tenant/permission/version dimensions;
- event ordering serializes a broader scope than required with no justification;
- large export/report holds interactive request unboundedly;
- mobile startup receives a heavy web-only dependency without review;
- performance claim is based only on tiny development data or an isolated micro-benchmark;
- optimization creates duplicate writable truth.

# 116. Related canonical owners

```text
docs/quality/engineering-quality-standard.md
docs/quality/testing-strategy.md
docs/quality/security-quality-standard.md
docs/quality/accessibility-standard.md
docs/delivery/change-classification.md
docs/architecture/data-ownership-and-consistency.md
docs/product/contexts/work-management.md
docs/product/contexts/documents.md
docs/product/contexts/automation.md
docs/product/contexts/integrations.md
docs/product/contexts/analytics.md
backend/docs/architecture/infrastructure-and-data.md
backend/docs/architecture/platform-and-messaging.md
frontend/docs/architecture/state-query-mutations.md
frontend/docs/architecture/realtime.md
```

# 117. Final performance rule

For every performance-sensitive change, answer:

```text
What workload/cardinality grows?
What work is bounded per request/job/event/render?
Which source/index/projection answers the query?
What concurrency/fan-out/retry amplification exists?
What cache exists and how is it invalidated safely?
What happens under one noisy tenant or provider outage?
What frontend/mobile payload/render cost changes?
What representative evidence proves the claim?
Which production SLO/capacity requirement, if any, owns the actual threshold?
```

The target is:

> **performance that scales through bounded work, correct query/data shape, deliberate projections and backpressure, without sacrificing tenant isolation, authoritative ownership, accessibility, or failure correctness.**
