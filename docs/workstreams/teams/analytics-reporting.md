---
document_id: WRK-TEAM-ANALYTICS-REPORTING
document_type: workstream-team-spec
status: active
owner: analytics-reporting-team
applies_to:
  - analytics
  - reporting
  - analytical-read-models
  - metrics
  - dashboards
  - exports
  - event-projections
  - backfill
  - analytical-authorization
evidence:
  - docs/product/analytics.md
  - docs/architecture/bounded-context-map.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/architecture/capability-extraction-strategy.md
  - docs/delivery/team-ownership.md
  - docs/workstreams/capability-map.md
  - docs/workstreams/cross-team-dependencies.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/docs/architecture/api-and-contracts.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - frontend/docs/architecture/api-and-contracts.md
  - frontend/docs/architecture/state-query-mutations.md
  - frontend/docs/generated/package-boundaries.md
review_on:
  - analytics-domain-change
  - metric-semantic-change
  - reporting-source-contract-change
  - analytical-data-ownership-change
  - projection-consistency-change
  - export-capability-change
  - analytics-authorization-change
  - analytics-storage-change
---

# Analytics & Reporting Workstream

## 1. Purpose

This workstream defines execution for the Analytics / Reporting bounded context.

Analytics owns derived analytical state and reporting semantics.

It does not own transactional source behavior.

Its purpose is to let teams and coding agents build reports, dashboards, metrics and analytical projections without inventing:

- direct private-table access to source contexts;
- transactional business logic inside reports;
- cross-tenant analytical shortcuts;
- report-specific mutations of source aggregates;
- metrics with undocumented meaning;
- event projections that cannot be rebuilt;
- realtime promises unsupported by the data pipeline;
- duplicate analytical definitions across frontend/backend.

Canonical product meaning remains in:

```text
docs/product/analytics.md
```

## 2. Foundational boundary

Transactional contexts answer:

```text
What business fact happened?
What state is currently valid?
```

Analytics answers:

```text
How should approved facts be transformed into metrics, reports and analytical read models?
```

Platform answers:

```text
How are events delivered?
How is storage/runtime/observability provided?
```

Analytics is downstream by default.

## 3. Explicit non-ownership

Analytics does NOT own:

- Account lifecycle;
- Identity;
- Workspace lifecycle;
- Governance permission semantics;
- WorkManagement transactional state;
- Documents/Collaboration transactional state;
- Automation execution semantics;
- Integration provider state;
- Billing transactional semantics;
- source-context command orchestration.

Analytics MUST NOT become a transaction coordinator.

## 4. Data ownership principle

Source bounded context owns source fact.

Analytics owns derived analytical state.

Preferred direction:

```text
Source context
→ event / explicit reporting contract
→ Analytics projection
→ analytical query/report
```

Avoid:

```text
Analytics
→ reads arbitrary private source tables
→ becomes coupled to source persistence
```

unless an explicit architecture decision authorizes a specific read strategy.

## 5. Capability decomposition

Analytics delivery is decomposed into:

```text
ANA-001 Source-event/reporting-contract inventory
ANA-002 Metric definition registry
ANA-003 Analytical projection foundation
ANA-004 Projection identity/idempotency
ANA-005 Projection ordering/late-event policy
ANA-006 Projection rebuild/backfill
ANA-007 Account/workspace metrics
ANA-008 WorkManagement analytics
ANA-009 Documents/Collaboration analytics
ANA-010 Automation/Integrations analytics
ANA-011 Billing/usage analytics
ANA-012 Cross-context report composition
ANA-013 Report/query API
ANA-014 Dashboard/report frontend
ANA-015 Filters/date/timezone semantics
ANA-016 Freshness/staleness contract
ANA-017 Export
ANA-018 Authorization/visibility
ANA-019 Retention/data lifecycle
ANA-020 Performance/storage strategy
ANA-021 Observability
ANA-022 Data quality/reconciliation
ANA-023 Migration/versioning
ANA-024 Hardening
```

These are delivery capabilities, not new bounded contexts or independent services.

## 6. Delivery waves

### Analytics Wave A — semantic and ingestion foundation

```text
ANA-001 Source inventory
ANA-002 Metric registry
ANA-003 Projection foundation
ANA-004 Idempotency
ANA-005 Ordering/late events
ANA-006 Rebuild/backfill
```

### Analytics Wave B — first domain reports

```text
ANA-007 Account/workspace metrics
ANA-008 WorkManagement analytics
ANA-009 Documents/Collaboration analytics
```

### Analytics Wave C — operational/commercial analytics

```text
ANA-010 Automation/Integrations
ANA-011 Billing/usage
ANA-012 Cross-context composition
```

### Analytics Wave D — delivery UX

```text
ANA-013 Report API
ANA-014 Frontend
ANA-015 Filters/timezone
ANA-016 Freshness
ANA-017 Export
ANA-018 Authorization
```

### Analytics Wave E — lifecycle/hardening

```text
ANA-019 Retention
ANA-020 Performance/storage
ANA-021 Observability
ANA-022 Data quality
ANA-023 Migration/versioning
ANA-024 Hardening
```

# Source contracts

## 7. Source inventory (ANA-001)

Before implementing a report, identify every source fact.

For each source:

```text
bounded context
semantic owner
event/reporting contract
version
account/workspace scope
resource identity
occurrence time
consumer compatibility
replay availability
```

A report MUST NOT begin from:

```text
"which tables can we join?"
```

It begins from:

```text
"which business facts define this metric?"
```

## 8. Source-context responsibilities

Source context owns:

- fact meaning;
- event/contract compatibility;
- source identity;
- source timestamps according to business semantics.

Analytics owns:

- projection;
- aggregation;
- metric;
- report.

## 9. Event vs reporting contract

Not every report must consume an event.

Possible approved sources:

- durable integration event;
- explicit reporting/read contract;
- approved read model feed;
- approved snapshot/backfill source.

Selection follows architecture.

Do not create events solely to expose private persistence details.

# Metric semantics

## 10. Metric definition registry (ANA-002)

Every metric must define:

```text
metric ID
human meaning
source facts
scope
aggregation
time basis
timezone
inclusion/exclusion
late-event behavior
correction behavior
authorization visibility
freshness class
```

## 11. Stable metric identity

Do not use:

- frontend widget name;
- SQL query filename;
- chart title;

as canonical metric identity.

Metric identity should be stable enough for API/report references.

## 12. Metric examples and boundaries

Examples may include:

- active work items;
- created/completed items per period;
- automation execution success rate;
- integration failure rate;
- document activity;
- subscription/usage metrics.

Exact metrics require product authority.

This workstream does not invent them.

## 13. Derived vs transactional meaning

A metric may be derived.

It MUST NOT redefine transactional semantics.

Example:

```text
"completed item"
```

must be based on canonical WorkManagement completion semantics, not a report-local guess such as "status label equals Done".

# Projection foundation

## 14. Projection model (ANA-003)

An analytical projection should define:

- projection identity;
- source;
- scope;
- state;
- update rule;
- rebuild strategy;
- schema/version.

Projection state is Analytics-owned derived data.

## 15. Idempotency (ANA-004)

Duplicate source delivery MUST NOT double-apply analytical state.

Projection identity/idempotency should use stable source-message identity or another approved source key.

Tests must distinguish:

- duplicate same source message;
- distinct source messages with same type;
- replay;
- correction.

## 16. Ordering (ANA-005)

Not all metrics require strict ordering.

Classify each projection:

```text
order independent
per-resource ordered
per-account ordered
window ordered
```

Do not impose global ordering unless semantics require it.

## 17. Late events

Define behavior when an event arrives after its normal window.

Possible strategies:

- apply and correct prior aggregate;
- ignore after cutoff;
- reopen period;
- store correction.

Choice belongs to analytical/product semantics.

## 18. Corrections

If source facts can be corrected/reversed, projections need explicit handling.

Avoid metrics that can only increment and never repair.

# Replay and backfill

## 19. Rebuild capability (ANA-006)

A critical analytical projection should be rebuildable or have an explicit repair strategy.

Rebuild input may come from:

- source event replay;
- approved snapshot/reporting export;
- another architecture-approved source.

## 20. Rebuild invariants

Rebuild must preserve:

- tenant isolation;
- metric semantics;
- deterministic aggregation where possible;
- idempotency;
- version handling.

## 21. Backfill

Backfill should define:

- date/resource scope;
- source;
- rate limit;
- production impact;
- checkpoint;
- resumability;
- duplicate handling;
- validation.

Do not run unbounded backfill as an ad hoc production script without operational controls.

## 22. Projection versioning

If projection schema or metric semantics change:

- version;
- migrate/rebuild;
- dual-read/dual-write if required;
- API compatibility;
- frontend compatibility;
- cutover.

Do not silently reinterpret old analytical rows under new metric meaning.

# Domain analytical capabilities

## 23. Account/workspace metrics (ANA-007)

Potential scope:

- account activity;
- workspace activity;
- workspace counts;
- membership-derived reporting where authorized.

Identity/Workspace contexts remain semantic owners of source facts.

Analytics does not become a membership store.

## 24. WorkManagement analytics (ANA-008)

Possible reports:

- item creation/completion trends;
- field/status distribution;
- board throughput;
- aging;
- workload;
- cycle metrics where product-defined.

### Boundary with WorkManagement Dashboard

A WorkManagement Dashboard may remain WorkManagement-owned if it is a direct operational projection of WorkManagement state.

Cross-context/historical analytical reporting belongs to Analytics.

Ownership must be explicit per capability.

## 25. WorkManagement source semantics

Analytics MUST NOT guess:

```text
completed
overdue
active
archived
```

from UI labels or raw DB flags.

Use canonical WorkManagement semantics/contracts.

## 26. Documents/Collaboration analytics (ANA-009)

Possible facts:

- Page activity;
- Block activity;
- comment activity;
- collaboration volume.

Do not expose document/comment content in analytics unless product/security policy explicitly permits it.

Prefer metadata/events over raw rich content.

## 27. Automation/Integrations analytics (ANA-010)

Possible facts:

- execution volume;
- success/failure;
- latency;
- provider failure categories;
- connector health trend.

Automation owns execution semantics.

Integrations owns provider state semantics.

Analytics derives reporting only.

## 28. Billing/usage analytics (ANA-011)

Analytics may report:

- subscription trends;
- usage trends;
- entitlement adoption;
- payment/invoice metrics where authorized.

Billing owns financial semantics.

Analytics should not recalculate authoritative entitlements or billing totals independently.

# Cross-context composition

## 29. Cross-context reports (ANA-012)

A cross-context report combines approved derived facts.

It MUST NOT create a shared transactional model.

Example direction:

```text
WorkManagement projection
+
Automation projection
+
Billing projection
→ cross-context analytical view
```

## 30. Join identity

Cross-context joins should use stable shared identifiers/contracts such as:

- Account ID;
- Workspace ID;
- Resource IDs where explicitly compatible.

Do not join by:

- display name;
- email unless semantic contract says so;
- internal DB PKs from private tables;
- CLR type names.

## 31. Temporal consistency

Cross-context reports may combine eventually-consistent projections.

Define whether the report promises:

- point-in-time consistency;
- approximate/current-enough;
- independent source freshness.

Do not imply atomic cross-context consistency if the pipeline does not provide it.

# Report/query API

## 32. Report API (ANA-013)

Report API should define:

- report/metric ID;
- Account/workspace scope;
- filters;
- date range;
- timezone;
- grouping;
- pagination;
- sort;
- freshness metadata where useful;
- authorization.

## 33. Query limits

Protect expensive analytical queries with:

- date-range limits;
- pagination;
- row limits;
- timeout;
- asynchronous export/job path for large workloads where product-defined.

Do not let a single report request scan unbounded tenant history.

## 34. Error contract

Distinguish:

- invalid filter;
- invalid metric/report;
- unauthorized;
- forbidden;
- unavailable/stale projection;
- query too large;
- internal failure.

# Filters / date / timezone

## 35. Filter semantics (ANA-015)

Filters should use stable field/metric semantics.

Avoid frontend-only interpretation that differs from backend.

If filter definitions are persisted/shared, they become a versioned contract.

## 36. Time basis

Every time-based metric must define which timestamp matters:

```text
event occurred at
entity created at
entity completed at
provider settled at
projection received at
```

Do not default to projection ingestion time unless metric semantics require it.

## 37. Timezone

Define:

- storage basis;
- report timezone;
- date bucket boundary;
- daylight-saving behavior;
- Account/user timezone precedence where product-defined.

The chart library MUST NOT become the timezone authority.

# Freshness and consistency

## 38. Freshness classes (ANA-016)

Each report/metric should declare one:

```text
transactional-near-real-time
eventually-consistent
scheduled/batch
```

or another explicit class.

## 39. Freshness metadata

Where user decisions depend on freshness, API/UI may expose:

- last updated;
- data-through timestamp;
- delayed/degraded state.

Do not promise "live" when projection delay exists.

## 40. Degraded analytics

If an analytical pipeline is delayed:

- transactional product operations should normally continue;
- report UI should surface stale/degraded state;
- retries/backlog should be observable.

Analytics failure should not become a global transaction blocker unless an explicit business invariant requires it.

# Authorization and tenancy

## 41. Authorization (ANA-018)

Analytics must preserve:

- Account isolation;
- Workspace visibility;
- resource visibility;
- Billing sensitivity;
- Identity sensitivity.

Governance owns permission semantics.

Analytics owns report resource/action meaning.

Platform/Application owns enforcement.

## 42. Security filtering

Do not retrieve cross-tenant data and filter it only in frontend.

Authorization must constrain backend query/projection access.

## 43. Aggregated privacy

Aggregation does not automatically remove sensitivity.

A report with low-cardinality groups may reveal protected user data.

Define masking/minimum group sizes only if product/security policy requires them.

## 44. Sensitive domains

Billing and Identity analytical data may require stricter permissions than ordinary WorkManagement metrics.

Do not assume "Analytics admin" automatically grants all sensitive reports without Governance/product authority.

# Frontend reports and dashboards

## 45. Frontend ownership (ANA-014)

Analytics frontend owns:

- report selection;
- dashboard/report layout;
- filters;
- date range;
- visualization;
- export initiation;
- loading/empty/error/stale states.

Generic visualization primitives may live in UI/Foundation only when truly product-agnostic.

## 46. Server-state identity

Report query keys must distinguish:

- Account;
- Workspace;
- report/metric;
- date range;
- filters;
- timezone;
- grouping.

Account/workspace transition must not reuse previous tenant report data.

## 47. Visualization correctness

Charts/tables must preserve metric semantics.

Do not:

- truncate axes misleadingly where product quality forbids it;
- convert units inconsistently;
- aggregate client-side differently from backend metric definition;
- hide missing data as zero unless defined.

## 48. Empty vs zero vs unavailable

Distinguish:

```text
zero
no data
not yet projected
forbidden
unavailable
stale
```

These states have different meanings.

# Export

## 49. Export (ANA-017)

If export is product-defined, specify:

- report/metric;
- scope;
- date range;
- format;
- row/size limit;
- synchronous vs asynchronous;
- authorization;
- sensitive fields;
- retention;
- expiration;
- download security.

## 50. Async export

Large exports may require job orchestration.

Define:

- job identity;
- status;
- completion;
- failure;
- expiration;
- access control;
- storage cleanup.

Do not expose export files through permanent unauthenticated URLs unless explicitly designed.

## 51. Export consistency

Export should declare whether it is:

- snapshot at request time;
- snapshot at processing time;
- rolling data.

Avoid surprising mismatch between UI totals and downloaded data without freshness metadata.

# Retention and lifecycle

## 52. Analytical retention (ANA-019)

Derived analytical data may have different retention from transactional data.

Define:

- retention period;
- tenant deletion behavior;
- legal/compliance requirements;
- rebuild source availability;
- export/history expectations.

## 53. Account deletion

Account deletion may require analytical cleanup or anonymization.

Do not rely on source table cascade.

Analytics owns cleanup of Analytics-owned derived data according to system policy.

## 54. User deletion/anonymization

Historical metrics may need to remain while personally identifying details are removed.

Define whether reports retain:

- anonymous actor;
- tombstoned identity;
- aggregate-only data.

# Storage and performance

## 55. Storage strategy (ANA-020)

Current implementation should use the simplest architecture consistent with load and ownership.

Possible strategies may include:

- relational analytical tables;
- materialized projections;
- dedicated analytical store later.

Do not introduce a separate data platform solely because "analytics scales differently" without evidence.

## 56. Storage ownership

Regardless of technology, Analytics owns its derived store.

Source contexts should not write Analytics tables directly unless via an explicitly defined ingestion mechanism.

## 57. Partitioning

High-volume analytical stores may need partitioning by:

- Account;
- time;
- metric;
- another measured access pattern.

Partition design must preserve tenant isolation and query efficiency.

## 58. Indexing

Measure report query patterns before adding broad indexes.

Track:

- common filters;
- date ranges;
- group by;
- Account/workspace predicates.

## 59. Pre-aggregation

Pre-aggregate when measured query cost justifies it.

Every pre-aggregate needs:

- source metric semantics;
- update rule;
- rebuild;
- correction handling.

Do not maintain multiple inconsistent aggregates for the same metric.

## 60. Caching

Report cache must include all semantic dimensions:

- Account;
- Workspace;
- metric/report;
- filters;
- time range;
- timezone;
- authorization-sensitive scope.

Cross-tenant cache leakage is a critical security failure.

# Data quality

## 61. Data quality controls (ANA-022)

Analytics should detect:

- missing source events;
- duplicate application;
- projection lag;
- impossible counts;
- source/projection divergence;
- orphaned identifiers;
- version mismatch.

## 62. Reconciliation

For critical metrics, define reconciliation against an approved source snapshot or invariant.

Example:

```text
current active item projection count
vs
approved WorkManagement reporting snapshot
```

Do not query private source tables casually; reconciliation source must be architecture-approved.

## 63. Quality thresholds

Where business-critical, define thresholds for:

- maximum lag;
- mismatch tolerance;
- missing event rate;
- rebuild time.

Thresholds should be evidence-based.

# Observability

## 64. Pipeline signals (ANA-021)

Track:

- ingestion rate;
- projection lag;
- duplicate rate;
- failed projection count;
- poison messages;
- backfill progress;
- report query latency;
- export job latency;
- cache hit rate.

## 65. Correlation

A projection update should be traceable to:

```text
source event/message
→ consumer
→ projection
→ report query
```

without exposing sensitive payload contents unnecessarily.

## 66. Degraded-state visibility

Operations should know when:

- a projection stopped;
- lag exceeds threshold;
- a source version is unsupported;
- a backfill failed;
- an export backlog grows.

# Migration/versioning

## 67. Projection schema migration (ANA-023)

Changing analytical schema requires:

- old/new schema;
- migration or rebuild;
- compatibility;
- cutover;
- rollback/forward-fix;
- report/API version impact.

## 68. Metric semantic change

Changing the meaning of a metric is more serious than renaming a column.

Classify:

```text
same metric, implementation fix
new metric version
breaking semantic change
```

Historical comparison may become invalid if semantics change.

## 69. Source event version change

When source event changes:

- producer owns compatibility;
- Analytics updates consumer;
- replay/backfill considered;
- old event support window defined.

## 70. Rebuild cutover

For large projections, consider:

```text
build v2
validate
switch reads
retire v1
```

instead of destructive in-place migration when risk/scale requires it.

# Testing

## 71. Projection unit tests

Cover:

- first apply;
- duplicate;
- out-of-order where relevant;
- late event;
- correction;
- invalid version;
- replay.

## 72. Metric semantic tests

Use source facts to prove expected metric output.

Avoid tests coupled only to SQL shape.

The test should express business metric meaning.

## 73. Application/query tests

Cover:

- filters;
- date range;
- timezone;
- grouping;
- authorization;
- stale/unavailable state;
- query limits.

## 74. Infrastructure tests

Cover:

- projection persistence;
- indexes/constraints;
- tenant partitioning;
- migration/rebuild;
- event consumer integration;
- cache partitioning.

## 75. API tests

Cover:

- report/metric contract;
- unauthorized/forbidden;
- invalid filter/date;
- pagination;
- freshness metadata;
- export request.

## 76. Frontend tests

Cover:

- filters;
- date/timezone;
- loading;
- zero/no-data distinction;
- stale/degraded state;
- authorization;
- Account/workspace switching;
- chart/table semantics;
- export.

## 77. Critical E2E — projection

```text
source business action
→ source event
→ Analytics consumes once effectively
→ projection updates
→ report reflects expected metric
```

## 78. Critical E2E — duplicate/replay

```text
same source event delivered twice
→ metric changes once

rebuild projection
→ same expected analytical result
```

## 79. Critical E2E — tenant isolation

```text
Account A report
→ contains only A data

switch to Account B
→ no A analytical state/cache leaks
```

## 80. Critical E2E — authorization

```text
actor without report permission
→ backend denies report
→ frontend does not expose protected data
```

# Dependency readiness

## 81. Readiness matrix

| Capability | Required upstream readiness |
|---|---|
| Source inventory | source contract D4+ |
| Projection foundation | Platform messaging D5 |
| Metric registry | product metric semantics D5 |
| WorkManagement analytics | WM source facts D4+ |
| Documents analytics | Documents source facts D4+ |
| Automation analytics | Automation source facts D4+ |
| Billing analytics | Billing source facts D4+ |
| Cross-context reports | each source projection D4+ |
| Report API | projection/query D4+ |
| Frontend | API + Account isolation D4+ |
| Export | report authorization/storage D4+ |
| Backfill | replay/source snapshot D4+ |

# Parallelization

## 82. Safe parallel work

After source/metric/projection contracts stabilize:

- independent domain projections;
- frontend report shells;
- export pipeline;
- data-quality checks;
- backfill tooling

may proceed in parallel.

## 83. Unsafe parallelization

Do not let teams independently invent:

- same metric under different definitions;
- different timezones/bucket logic;
- private source-table queries;
- duplicate projection identities;
- incompatible freshness semantics;
- separate authorization models.

# Cross-team handoff templates

## 84. Source-to-Analytics handoff

```text
Source context:
Business fact:
Event/reporting contract:
Semantic owner:
Account/workspace scope:
Timestamp semantics:
Version:
Replay:
Ordering:
Current readiness:
Required readiness:
Tests:
```

## 85. Metric handoff

```text
Metric ID:
Product meaning:
Source facts:
Aggregation:
Time basis:
Timezone:
Filters:
Authorization:
Freshness:
Correction behavior:
Tests:
```

## 86. Report handoff

```text
Report ID:
Metrics:
Scope:
Filters:
Date range:
Freshness:
Authorization:
Frontend owner:
Export:
Performance limit:
Tests:
```

# Decision authority

## 87. Team-local decisions

May decide locally:

- internal projection data structures;
- private query helpers;
- report component composition;
- index/query optimization;
- cache implementation preserving semantics;
- test fixtures;
- backfill implementation preserving contract.

## 88. Decisions requiring escalation

Escalate:

- direct private source-table access;
- new source event semantics;
- new cross-tenant analytical model;
- Analytics mutation of source state;
- new data platform/service;
- metric semantic changes affecting product commitments;
- new global export storage/security model;
- new analytical retention policy with legal/security impact;
- service extraction.

## 89. Stop conditions

Stop and escalate when:

- source fact owner is unclear;
- metric meaning cannot be defined without guessing;
- required source data exists only in private persistence;
- projection cannot be rebuilt/repaired;
- tenant authorization cannot be applied cleanly;
- metric semantic change would invalidate historical comparisons without versioning;
- cross-context report implies atomic consistency not provided by architecture;
- export exposes sensitive data without policy;
- a separate data platform is proposed without measured need;
- Analytics becomes required for a transactional invariant.

# Completion criteria

## 90. Capability Definition of Done

An Analytics slice is `DONE` only when:

- source owner is explicit;
- metric semantics are documented;
- projection is idempotent;
- late/replay/correction behavior is defined;
- tenant/authz boundaries are enforced;
- freshness is explicit;
- report API/frontend agree;
- rebuild/backfill strategy exists where required;
- migration/versioning is handled;
- performance is within expected bounds;
- data quality/observability exists;
- tests prove source→projection→report behavior;
- architecture gates remain green.

## 91. Analytics foundation exit criteria

Analytics supports broad parallel reporting when:

- source inventory is stable;
- projection mechanism is D5;
- metric registry is canonical;
- idempotency/replay are D5;
- Account/workspace isolation is D5;
- report authorization is D5;
- freshness classes are established;
- WorkManagement/Documents/Automation/Billing source contracts are explicit;
- backfill/rebuild is proven;
- frontend analytical state is tenant-safe;
- reports do not depend on private source persistence.

## 92. Service extraction readiness

Analytics is a plausible future extraction candidate because:

- read load may scale differently;
- data may be independently derived;
- storage may eventually differ from transactional storage;
- rebuild/replay can support independent lifecycle.

That still does not justify extraction today.

Before extraction prove:

- stable source contracts;
- independent Analytics-owned data;
- replay/backfill strategy;
- tenant/security boundary;
- query/API contract;
- operational observability;
- failure isolation benefit;
- no transactional context depends synchronously on Analytics;
- separate storage/deployment provides measured value;
- migration/cutover is understood.

Analytics should expose the boundary cleanly inside the modular monolith before deployment separation.
