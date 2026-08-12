---
document_id: PROD-ANALYTICS
document_type: product-context
status: active
owner: analytics
applies_to:
  - analytics
  - reporting
  - dashboards
  - widgets
  - metrics
  - snapshots
  - projections
evidence:
  - PRODUCT.md
  - docs/product/product-model.md
  - docs/product/product-experience.md
  - docs/product/contexts/governance.md
  - docs/product/contexts/work-management.md
  - docs/product/contexts/documents.md
  - docs/product/contexts/collaboration.md
  - docs/product/contexts/automation.md
  - docs/product/contexts/integrations.md
  - docs/product/contexts/billing.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - backend/src/Notrelix.Domain/Analytics/
  - backend/tests/
review_on:
  - metric-definition-change
  - dashboard-model-change
  - widget-type-change
  - reporting-snapshot-change
  - projection-source-change
  - analytics-freshness-change
  - cross-tenant-analytics-change
  - analytics-authorization-change
  - analytics-retention-change
---

# Analytics / Reporting Context

> **Analytics turns authoritative product facts into read-oriented metrics, dashboards, widgets, reports, snapshots, and derived projections.**
>
> Analytics owns the meaning of analytical insight. It does not become the write-side owner of Work Management, Documents, Collaboration, Automation, Integrations, Billing, or other source facts.

This document is the canonical product owner for Analytics/Reporting semantics.

---

# 1. Mission

Analytics helps users answer questions such as:

```text
What is happening?
How much work is completed?
How has activity changed over time?
Where is capacity constrained?
Which Automation/Integration flows are failing?
What commercial/usage trend matters?
```

by deriving stable insight from source-owned facts.

---

# 2. Owns

Analytics owns product semantics for:

```text
Metric definition
Dashboard
Dashboard source/configuration
Dashboard visibility/lifecycle
Dashboard Widget
Widget type/configuration/position
Reporting Snapshot
reporting period/freshness semantics
analytics projection definitions
aggregation/window semantics
historical comparison semantics
analytics export/report semantics
```

Current source has first-class `Dashboards`, `Widgets`, and `Snapshots`.

---

# 3. Does not own

```text
Board/Item/Field truth
→ Work Management

Page/Block truth
→ Documents

Comment/Notification truth
→ Collaboration

Automation Execution truth
→ Automation

Integration Connection/Sync truth
→ Integrations

Plan/Subscription/Entitlement/Usage truth
→ Billing

resource permission
→ Governance
```

Analytics consumes approved facts/projections from these owners.

---

# 4. Ubiquitous language

**Metric** — versioned analytical business definition describing how a value is derived.

**Dimension** — grouping/filter attribute used to segment a Metric.

**Dashboard** — saved analytical presentation/configuration scoped to an owner/tenant.

**Dashboard Source** — explicit source/context/query definition available to a Dashboard.

**Widget** — one visualization/report component over a defined metric/source.

**Reporting Snapshot** — captured analytical result/state at a defined source/version/time.

**Projection** — derived data optimized for analytics/read performance.

**Freshness** — how current a projection/result is relative to source truth.

---

# 5. ANA-001 — Analytics is derived state

Dashboards, snapshots, aggregates, and projections can be rebuilt or recomputed from authoritative source facts/events according to retention constraints.

They MUST NOT become ordinary writable business source state.

---

# 6. Editing Analytics configuration

A user may edit:

```text
Dashboard title
layout
Widget type
filter
time range
grouping
visualization
```

That changes Analytics configuration.

It does not mutate source business facts unless the user explicitly invokes a source-context command.

---

# 7. ANA-002 — Analytical control is not hidden source mutation

A chart filter, grouping, or drill-down changes what is shown.

A source mutation button inside a Dashboard must route to the source owner and follow normal authorization/invariants.

---

# 8. Metric

A Metric is a business definition, not a frontend formula fragment.

A complete definition includes as applicable:

```text
stable metric key
version
name/description
source facts
scope
filters
aggregation
dimensions
time basis
time zone
null/missing behavior
unit
rounding
freshness
privacy/authorization class
```

---

# 9. ANA-003 — Metric definition is versioned business semantics

If formula meaning changes materially, decide whether:

```text
new Metric version
historical recomputation
backfill
old/new comparison window
```

is required.

Do not silently change historical meaning under the same metric identity.

---

# 10. Metric identity

Stable metric key should survive:

- display-label change;
- Dashboard move;
- frontend refactor.

Do not use Widget ID as Metric identity.

---

# 11. ANA-004 — One named Metric has one semantic owner

The same metric such as:

```text
completed-items
automation-success-rate
active-collaborators
```

must not be independently recalculated with different formulas in multiple UI widgets.

---

# 12. Source facts

Metric sources can include:

```text
Work Management item/field facts
Documents facts
Collaboration facts
Automation execution facts
Integration sync facts
Billing usage/commercial facts
```

The source context keeps ownership.

---

# 13. Source event versus source query

Analytics may derive from:

- integration/product events;
- source read model;
- materialized typed projections;
- controlled backfill query.

The chosen method must preserve source semantics.

---

# 14. ANA-005 — Analytics source is explicit

Every projection/metric must be traceable to approved source facts/contracts.

A manually edited analytics table is not a source-of-truth shortcut.

---

# 15. Dashboard

Current source has `Dashboard`, `DashboardSource`, `DashboardSourceType`, `DashboardStatus`, `DashboardVisibility`, `DashboardWidget`, and `DashboardWidgetType`.

A Dashboard is durable Analytics-owned configuration/presentation.

---

# 16. ANA-006 — Dashboard owns visualization configuration, not product facts

Deleting or changing a Dashboard/Widget must not delete or alter source Boards, Items, Pages, Automations, etc.

---

# 17. Dashboard scope

A Dashboard should have explicit:

```text
Account/Workspace scope
owner
visibility
status/lifecycle
allowed sources
```

where relevant.

---

# 18. ANA-007 — Dashboard tenant scope is explicit

A Workspace Dashboard MUST NOT accidentally aggregate another tenant through shared cache/projection keys.

Cross-Account reporting requires explicit admin/system product design.

---

# 19. Dashboard lifecycle

Potential lifecycle:

```text
draft/active
archived
deleted
```

where current product supports it.

Lifecycle affects Dashboard configuration, not source facts.

---

# 20. Dashboard visibility

Current source includes DashboardVisibility.

Visibility is an Analytics/Governance input, not complete authorization by itself.

---

# 21. ANA-008 — Dashboard visibility does not bypass source authorization

A shared Dashboard cannot reveal source resources/fields the viewer is not allowed to access.

---

# 22. Dashboard Source

Dashboard Source defines which analytical source/query/model a Dashboard can use.

It should refer to stable source identity/semantic query rather than a fragile table name.

---

# 23. ANA-009 — Dashboard Source preserves source owner

A source reference to Work Management does not make Analytics owner of Work Management data.

---

# 24. Widget

A Widget is a saved visualization/report configuration placed on a Dashboard.

Current source has first-class `WidgetConfig`, `WidgetPosition`, `WidgetType`, and DashboardWidget types.

---

# 25. Widget type

Possible types may include:

```text
number/KPI
table
bar
line
pie/donut
progress
timeline
workload
text/summary
```

Exact current types remain executable evidence.

---

# 26. ANA-010 — Widget Type has a validated configuration contract

Each Widget Type defines:

```text
required source/metric
dimensions
filters
visual configuration
time range support
sorting
limit/top-N
null/empty behavior
position/layout
frontend rendering
```

Arbitrary JSON without schema is incomplete.

---

# 27. Widget Position

Position/layout is Dashboard presentation state.

It is not business ordering or source resource order.

---

# 28. ANA-011 — Analytics layout order never mutates source order

Rearranging Dashboard Widgets does not reorder Work Management Items or Documents.

---

# 29. Widget filters

Filters must use source/metric semantic fields.

Do not accept impossible field/operator combinations.

---

# 30. Widget aggregation

Aggregation should define:

```text
count
sum
average
min/max
percent/rate
distinct count
percentile
time-series bucket
```

as appropriate.

---

# 31. ANA-012 — Aggregation unit and denominator are explicit

A rate such as `success rate` must define:

```text
numerator
denominator
time window
excluded states
zero-denominator behavior
```

---

# 32. Time semantics

Analytics must distinguish:

```text
event time
business date
processing time
snapshot time
report period
```

---

# 33. ANA-013 — Time zone is part of Metric/report semantics when relevant

Daily/weekly/monthly aggregations need a defined time zone/calendar basis.

Do not let browser locale silently change the number.

---

# 34. Time ranges

Relative ranges such as “last 7 days” should define inclusive/exclusive boundaries and current-time basis.

Historical reports should remain reproducible.

---

# 35. Freshness

Analytics may be:

```text
transactionally queried
near-realtime projection
delayed stream projection
periodic snapshot
batch report
```

Different modes have different consistency.

---

# 36. ANA-014 — Freshness is explicit

Where interpretation depends on freshness, API/UI should expose or internally respect:

```text
last updated
source cutoff
projection lag
snapshot time
```

Do not claim transactionally current truth from an async projection.

---

# 37. Freshness target

Not every metric needs realtime.

Choose based on user decision need and cost.

Examples:

```text
workload planning
→ near-realtime may matter

monthly trend
→ periodic snapshot may be sufficient
```

---

# 38. Stale projection

A stale result may be:

- shown with timestamp;
- refreshed;
- temporarily unavailable;
- recomputed.

Do not silently mix data from incompatible freshness windows.

---

# 39. Reporting Snapshot

Current source has `ReportingSnapshot`, `ReportingSnapshotType`, and `ReportSnapshotPayload`.

Snapshot is first-class Analytics state.

---

# 40. ANA-015 — Reporting Snapshot is captured derived truth, not current source authority

A Snapshot records:

```text
metric/report identity
scope
source cutoff/version where possible
generated time
payload/schema version
```

It cannot be edited as current Work Management/Documents/Billing state.

---

# 41. Snapshot purpose

Snapshots may support:

- historical comparison;
- scheduled reporting;
- audit-like report reproduction;
- performance;
- export.

They remain Analytics artifacts.

---

# 42. Snapshot versioning

Payload schema/version must be explicit enough to read retained historical snapshots after Analytics evolves.

---

# 43. ANA-016 — Snapshot schema evolution is migration-aware

Do not deserialize old snapshots as if they had the new schema automatically.

---

# 44. Snapshot retention

Retention depends on:

- product need;
- privacy;
- source deletion policy;
- storage;
- compliance.

---

# 45. Source deletion

When source data is deleted, historical Analytics policy may be:

```text
recompute without deleted data
anonymize
retain approved aggregate
purge
retain snapshot under legal/product policy
```

The choice is explicit.

---

# 46. ANA-017 — Source deletion does not use accidental SQL cascade into Analytics

Derived/report history treatment follows privacy/product policy.

---

# 47. Projection

A projection is derived, query-optimized state.

It may denormalize across source contexts where allowed.

---

# 48. ANA-018 — Projection has one or more named authoritative sources

Every projection field should be traceable to source fact/metric derivation.

A projection cannot silently become writable business state.

---

# 49. Projection identity

Projection row/key should preserve:

```text
tenant scope
source resource identity
metric/version
time bucket/dimension
```

as required.

---

# 50. Projection update

Projection consumers assume duplicate/replay.

Updates must be idempotent/version-aware.

---

# 51. ANA-019 — Projection rebuild is a supported correctness path

A derived projection should have a strategy to:

```text
clear/rebuild
backfill
resume
verify
```

from authoritative source history/query within retention limits.

---

# 52. Rebuild limitation

Some historical source events may expire.

If full rebuild is impossible beyond a boundary, that retention/fidelity limitation must be documented.

---

# 53. Backfill

Schema/event changes may require backfill.

Backfill must preserve tenant scope, metric version, and source cutoff.

---

# 54. ANA-020 — Backfill cannot double-count live projection traffic

Backfill/live-consumer coordination requires idempotency or cutover strategy.

---

# 55. Enterprise-scale query strategy

Recurring high-cardinality analytics should use:

- typed/indexed projections;
- materialized values;
- pre-aggregation;
- efficient source queries.

---

# 56. ANA-021 — Analytics does not full-scan arbitrary flexible JSON per dashboard request at enterprise scale

Work Management dynamic fields need typed/index/materialization support for recurring filters/groupings/aggregations.

---

# 57. Work Management source

Analytics may consume:

- Board/Item lifecycle;
- Field values;
- Workload;
- approvals;
- status/priority;
- dates;
- derived values.

Source semantic definitions remain Work Management-owned.

---

# 58. Documents source

Possible metrics:

- Page count;
- document activity;
- content usage;
- template usage.

Do not expose document content without authorization/privacy.

---

# 59. Collaboration source

Possible metrics:

- comment volume;
- response time;
- active collaborators;
- mention/reaction trends.

Activity metrics remain distinct from Governance Audit metrics.

---

# 60. Automation source

Possible metrics:

```text
execution count
success/failure rate
duration
retry rate
Rule usage
```

Execution lifecycle remains Automation-owned.

---

# 61. Integrations source

Possible metrics:

```text
active connections
sync success/failure
provider error/rate limit
sync lag
```

Secrets/provider payload remain excluded.

---

# 62. Billing source

Analytics may report:

- subscription mix;
- usage trends;
- entitlement consumption.

Billing remains the commercial source of truth.

---

# 63. ANA-022 — Analytics cannot issue commercial charges or entitlements

Editing/reporting a Billing metric does not mutate Invoice, Subscription, Entitlement, or Usage ledger.

---

# 64. Billing Usage versus Analytics usage

Billing defines billable usage.

Analytics may independently define product usage metrics for insight.

They may share source facts but not semantic authority.

---

# 65. Governance

Analytics query/export/drilldown obey:

- tenant/resource permission;
- sensitive-field policy;
- report visibility;
- admin scope.

---

# 66. ANA-023 — Aggregation does not erase confidentiality

A grouped/aggregated result can still leak private information.

Authorization/privacy policy applies before and after aggregation as required.

---

# 67. Small-group privacy

Metrics grouped to very small populations may reveal identity indirectly.

Sensitive analytics may need:

- suppression threshold;
- anonymization;
- role restriction.

---

# 68. Cross-tenant analytics

Cross-tenant reporting is forbidden for ordinary Workspace users.

Global/admin analytics requires explicit product/security authority.

---

# 69. ANA-024 — Cross-tenant analytics is explicit privileged product capability

Shared infrastructure/cache/materialized views must never accidentally create cross-tenant results.

---

# 70. Cache

Analytics cache key may include:

```text
Account/Workspace
metric key/version
source/filter dimensions
time range
freshness version/cutoff
authorization-sensitive context
```

---

# 71. ANA-025 — Analytics cache keys preserve tenant + semantic version

A cache key missing tenant/metric version/time/filter dimensions risks correctness or leakage.

---

# 72. Authorization-sensitive cache

If two principals can see different subsets, a shared cache must store safely reusable aggregate or include authorization boundary.

---

# 73. Export

Analytics export is a protected operation.

It may expose more data than on-screen visualization.

---

# 74. ANA-026 — Export uses the same metric and authorization semantics

CSV/PDF/download must not recalculate a different formula or bypass field/resource filtering.

---

# 75. Scheduled report

If reports can be scheduled:

```text
report definition
scope
recipient
snapshot/cutoff
delivery
authorization at appropriate time
```

must be explicit.

Delivery belongs to Collaboration/Platform as appropriate.

---

# 76. User-visible numbers

A number should be interpretable.

Product UI should make available enough:

- label;
- unit;
- period;
- filter;
- freshness;
- comparison basis.

---

# 77. ANA-027 — Visually precise number requires semantically precise definition

Displaying `73.4%` is misleading if numerator, denominator, freshness, or period is undefined.

---

# 78. Comparison

Comparisons such as:

```text
+12% vs previous period
```

require explicit previous-window definition and comparable metric version.

---

# 79. Historical comparability

If Metric version changes, dashboards may need:

- break marker;
- recomputed history;
- separate series;
- migration note.

---

# 80. ANA-028 — Historical comparability is not assumed across breaking Metric versions

Do not draw one continuous trend line across incompatible metric semantics without explicit policy.

---

# 81. Null/missing data

Missing can mean:

```text
no source records
source unavailable
not applicable
not yet projected
unauthorized
```

Do not turn every case into numeric zero.

---

# 82. ANA-029 — Null, zero, unknown, and unauthorized are distinct

Metrics should preserve meaning rather than coerce every missing result to `0`.

---

# 83. Partial data

A report may be partial because one source is delayed.

If material, surface partial/incomplete state rather than pretending full accuracy.

---

# 84. ANA-030 — Multi-source report exposes meaningful completeness/freshness

When several source contexts have different cutoffs, the report should use a defined consistency/cutoff strategy.

---

# 85. Dashboard action

A Dashboard may offer an action such as:

```text
open filtered Board
open failed Automation
manage Integration
```

This is navigation/source command, not Analytics mutation authority.

---

# 86. Drill-down

Drill-down must re-evaluate source authorization.

Seeing an aggregate total does not automatically grant individual row visibility.

---

# 87. ANA-031 — Aggregate visibility does not imply detail visibility

A user may be allowed to see a team-level metric but not every underlying private record.

---

# 88. Widget source compatibility

A Widget Type can require certain source/metric shapes.

Example:

```text
time series
→ time dimension required

pie
→ categorical dimension

KPI
→ scalar aggregate
```

Invalid config must fail.

---

# 89. ANA-032 — Widget configuration is validated against source semantics

Do not let frontend save a configuration the backend cannot interpret consistently.

---

# 90. Dashboard default/shared/private

If current Dashboard visibility supports private/shared behavior, access semantics must be Governance-backed.

Private Dashboard config can still reference source resources.

---

# 91. Dashboard deletion

Deleting Dashboard/Widget removes Analytics configuration/snapshot references according to policy.

It does not delete sources.

---

# 92. ANA-033 — Dashboard deletion is non-destructive to source contexts

No cascade into Boards, Items, Pages, Executions, Billing facts, etc.

---

# 93. Snapshot deletion

Snapshot purge follows retention/privacy policy.

It does not alter historical source business state.

---

# 94. Current-source correctness

Because current Domain tree contains Dashboards/Widgets/Snapshots but no obvious first-class generic `Metrics` folder, a generic Metric registry must be treated as canonical target semantics rather than claiming every metric mechanism is already implemented.

---

# 95. ANA-034 — Product contract may be ahead of implementation, but current source gaps remain explicit

Do not invent a current generic metric engine merely to make docs symmetrical.

Implement it only when product requirements demand it, preserving this semantic contract.

---

# 96. Search versus Analytics

Search answers discovery/retrieval.

Analytics answers aggregation/insight.

A search index may contribute to a query but is not automatically an analytics metric store.

---

# 97. Operational metrics versus Analytics

Logs/traces/runtime SLO metrics belong to Operations.

Product/customer-facing analytical metrics belong to Analytics.

Do not expose infrastructure telemetry as business metric without semantic mapping.

---

# 98. ANA-035 — Operational telemetry is not product Analytics by default

CPU, HTTP latency, queue depth, and process memory are not customer business metrics unless intentionally transformed into an approved product metric.

---

# 99. Events/facts

Potential stable Analytics facts include:

```text
DashboardCreated/Changed/Deleted
WidgetAdded/Changed/Removed
ReportingSnapshotCreated
ReportGenerated
MetricDefinitionChanged
ProjectionRebuilt
```

Do not publish high-volume internal aggregation attempts as product events.

---

# 100. Realtime

Realtime can update:

- Dashboard configuration;
- near-realtime KPI;
- report generation progress;
- snapshot availability.

Durable query/projection remains authoritative.

---

# 101. ANA-036 — Realtime analytics updates remain reconcilable

Missing/duplicate/out-of-order realtime must not permanently corrupt Dashboard state or values.

---

# 102. Report generation

Large reports may be asynchronous.

Product state should distinguish:

```text
queued
running
completed
failed
expired
```

if implemented.

---

# 103. Report artifact

Generated export/report file is an artifact derived from a metric/report definition and source cutoff.

Its retention/download authorization is explicit.

---

# 104. Data residency

Analytics projections can duplicate source data.

They must respect Account/data-region/security policy.

---

# 105. ANA-037 — Derived data follows source security/data-location obligations

Moving data into a warehouse/materialized store does not exempt it from tenant/privacy requirements.

---

# 106. PII

Analytics should minimize direct PII where aggregate identity is sufficient.

Sensitive dimensions require explicit product need and authorization.

---

# 107. Retention

Projection/snapshot retention can differ from source active-state retention, but policy must be explicit.

---

# 108. Rebuild versus retained history

If source data is deleted/anonymized, rebuilding old metrics may produce different results.

Historical retention semantics must explain whether retained snapshot remains canonical historical report.

---

# 109. ANA-038 — Historical Snapshot and rebuilt current projection can legitimately differ

The product must distinguish:

```text
what was reported then
versus
what current source recomputation says now
```

where retention/privacy corrections make them different.

---

# 110. Current source alignment

Current Analytics Domain contains:

```text
Dashboards
Rules
Snapshots
Widgets
```

Current source includes:

```text
Dashboard
DashboardSource
DashboardSourceType
DashboardStatus
DashboardVisibility
DashboardWidget
DashboardWidgetType
WidgetConfig
WidgetPosition
WidgetType
ReportingSnapshot
ReportingSnapshotType
ReportSnapshotPayload
```

This supports Dashboard/configuration/snapshot semantics strongly.

---

# 111. Current ambiguity watch

Do not normalize:

```text
Dashboard Widget
→ source business record

Dashboard visibility
→ source-resource permission

ReportingSnapshot
→ current source truth

WidgetConfig JSON
→ schema-less analytics

Billing usage
→ same as analytics metric

operational telemetry
→ product metric

shared cache
→ cross-tenant authorization
```

---

# 112. Change impact — Metric

Review:

```text
source facts
version/history
backfill
Dashboards/Widgets
time zone
freshness
frontend formatting
exports
tests
```

---

# 113. Change impact — Dashboard/Widget

Review:

```text
config schema
visibility/authorization
source compatibility
frontend
snapshots
migration
```

---

# 114. Change impact — Projection

Review:

```text
source events/schema
idempotency
rebuild/backfill
tenant isolation
freshness
large-query performance
retention
```

---

# 115. Change impact — Snapshot

Review:

```text
payload schema/version
source cutoff
retention
privacy/deletion
historical comparability
download/export
```

---

# 116. Change impact — Authorization

Review:

```text
Dashboard visibility
source-resource permission
aggregate/detail distinction
cross-tenant admin scope
export
cache
```

---

# 117. Metric checklist

```text
[ ] stable metric key
[ ] version
[ ] source owner/facts
[ ] scope
[ ] aggregation
[ ] dimensions
[ ] time basis/time zone
[ ] null/zero behavior
[ ] unit/rounding
[ ] freshness
[ ] authorization/privacy
[ ] historical comparability
```

---

# 118. Dashboard checklist

```text
[ ] tenant scope
[ ] owner
[ ] visibility
[ ] lifecycle
[ ] source definitions
[ ] Widget config validation
[ ] layout only affects Analytics
[ ] source authorization
[ ] freshness UX
[ ] deletion non-destructive
```

---

# 119. Projection checklist

```text
[ ] authoritative source
[ ] tenant key
[ ] metric/version
[ ] event/query input
[ ] idempotency
[ ] rebuild
[ ] backfill/live coordination
[ ] freshness
[ ] authorization
[ ] retention
```

---

# 120. Snapshot checklist

```text
[ ] report/metric identity
[ ] tenant scope
[ ] source cutoff/version
[ ] generated time
[ ] payload schema/version
[ ] retention
[ ] privacy/deletion
[ ] immutable historical meaning
```

---

# 121. Testing/evidence

Critical evidence should cover:

```text
Metric edge cases
time zone/time-window
null/zero
aggregation/denominator
Dashboard lifecycle/visibility
Widget config validation
tenant isolation
source authorization
projection duplicate/replay
projection rebuild/backfill
freshness
snapshot schema/history
source deletion/anonymization
cache isolation
large-query strategy
export authorization
historical metric version changes
```

---

# 122. Stop conditions

Stop rather than guess if:

- Analytics table is becoming writable business source;
- one named Metric is calculated differently across widgets;
- Dashboard visibility is treated as source permission;
- cache/projection lacks tenant scope;
- lagging projection is presented as transactionally current;
- dashboard request full-scans arbitrary Item JSON at scale;
- cross-tenant aggregation lacks explicit privileged policy;
- Snapshot is edited as current business state;
- Billing invoice/usage is mutated from Analytics;
- null/unknown is silently coerced to zero;
- metric formula changes without version/history impact review;
- operational telemetry is exposed as product metric without semantic mapping;
- current source gap is hidden by claiming an implemented generic metric engine that does not exist.

---

# 123. Related canonical owners

```text
PRODUCT.md
docs/product/product-model.md
docs/product/product-experience.md
docs/product/contexts/governance.md
docs/product/contexts/work-management.md
docs/product/contexts/documents.md
docs/product/contexts/collaboration.md
docs/product/contexts/automation.md
docs/product/contexts/integrations.md
docs/product/contexts/billing.md
docs/architecture/data-ownership-and-consistency.md
docs/architecture/events-realtime-and-delivery-boundary.md
docs/quality/performance-and-scalability.md
```

---

# 124. Final Analytics rule

For every Analytics/Reporting capability, answer:

```text
What metric/report question is being answered?
Which source context owns each fact?
What stable Metric version defines the calculation?
Which Account/Workspace scope applies?
What aggregation, dimensions, unit, time basis, and null behavior apply?
How fresh is the result?
What projection/snapshot derives it?
Can it be rebuilt/backfilled?
What source authorization/privacy must be preserved?
Does aggregate visibility differ from detail visibility?
What happens when source data or Metric definition changes?
```

The target is:

> **a trustworthy read-oriented insight layer whose metrics are versioned and explainable, whose projections are rebuildable and tenant-safe, and whose dashboards never become a second writable source of product truth.**
