---
document_id: WRK-SPEC-ANALYTICS-REPORTING
document_type: workstream-specification
status: active
owner: analytics-reporting-team
applies_to:
  - backend
  - frontend
  - analytics
  - reporting
  - metrics
  - projections
  - dashboards
  - widgets
  - snapshots
  - exports
  - cross-context-reporting
  - privacy
  - migrations
  - certification
evidence:
  - docs/product/analytics.md
  - docs/workstreams/teams/analytics-reporting.md
  - docs/workstreams/cross-team-dependencies.md
  - docs/workstreams/backend-roadmap.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/infrastructure-and-data.md
  - docs/workstreams/executions/backend-team-architecture-closure/
review_on:
  - accepted-candidate-sha-change
  - metric-semantic-change
  - source-contract-change
  - projection-identity-or-ordering-change
  - dashboard-widget-contract-change
  - analytics-authorization-change
  - freshness-or-retention-change
  - export-contract-change
  - migration-or-rls-change
  - architecture-runtime-change
baseline:
  ref: pull/160-head
  sha: 902bc9c5b39a003df3dce6673edc124984d2b398
---

# SPEC — P6 Analytics & Reporting

## 1. Purpose

P6 turns approved source-owned business facts into **derived analytical state**:

```text
source owner
→ stable event/reporting contract
→ Analytics projection / approved direct analytical query
→ versioned Metric
→ report / Dashboard / Widget / Snapshot / Export
```

Analytics owns analytical interpretation and presentation. It does **not** acquire transactional ownership of source facts.

This is a brownfield execution specification. It distinguishes business semantics, production-reachable implementation, reference mechanisms, Domain/persistence-only code, legacy architecture debt, missing product surface and final exact-candidate evidence.

## 2. Requirement namespace

`ANA-*` cannot be reused as the P6 execution-requirement namespace because it already has two distinct authorities:

```text
docs/product/analytics.md
→ ANA-001 .. ANA-038 = product rules

docs/workstreams/teams/analytics-reporting.md
→ ANA-001 .. ANA-024 = delivery capability labels
```

This execution package uses:

```text
ARREQ001 .. ARREQ128
ARAC001  .. ARAC018
AR-GAP-01 .. AR-GAP-18
```

## 3. Authority precedence

```text
docs/product/analytics.md
        ↓
docs/workstreams/teams/analytics-reporting.md
        ↓
backend/frontend/Platform/Governance/Data architecture owners
        ↓
this SPEC
        ↓
PLAN → TESTS → CERTIFICATION
```

Historical audits and source-status prose are evidence, not higher semantic authority.

## 4. Brownfield posture

P6 is **not greenfield**, but it is also **not a released Analytics product yet**.

The strongest current mechanism is the WorkManagement placement reference:

```text
BoardItemMoved / Created / Archived V2
→ Analytics placement consumers
→ WorkspaceWorkItemPlacementService
→ reporting.workspace_work_item_placements
→ GetWorkspacePlacementsQuery

rebuild:
RebuildWorkspacePlacementsCommand
→ Analytics-owned source port
→ Infrastructure delegate adapter
→ WorkManagement Public IWorkItemProjectionSource
→ reconcile local Analytics projection
```

Dashboard/Widget/Snapshot types exist but do not have equivalent Application/API/frontend reachability.

## 5. Source posture matrix

| Capability | Exact-source posture | Evidence | P6 target |
|---|---|---|---|
| Work placement projection/live consumers | IMPLEMENTED_REFERENCE / EXACT_CANDIDATE_RERUN | WorkspaceWorkItemPlacementProjection + three Work event consumers + revision/rebuild/crash tests | Retain as canonical reference mechanism; do not generalize semantics blindly. |
| Placement local query | IMPLEMENTED_INTERNAL | GetWorkspacePlacementsQuery reads local Analytics projection; explicitly not HTTP-exposed | Retain internal reference; expose only through a product Metric/report contract if admitted. |
| Placement rebuild source | IMPLEMENTED_REFERENCE | Analytics-owned port → delegate adapter → WorkManagement public source | Retain; rerun producer-substitution/rebuild/concurrency proofs. |
| Dashboard aggregate/widgets/sources | DOMAIN_PERSISTENCE_ONLY | Domain aggregates + EF/RLS exist; no Application/API | Implement P6 Application/API; add explicit owner and safe visibility. |
| Dashboard Public visibility | DOMAIN_ONLY_UNRELEASED | Enum contains Public but no public/share security contract | Keep non-reachable in P6. |
| Widget config | PARTIAL_DOMAIN_VALIDATION | Basic JSON kind validation exists | Migrate to typed/versioned Metric/source-aware config. |
| Metric registry | ABSENT_TARGET_CONTRACT | Product semantics only | Implement canonical registry/definition boundary before report APIs. |
| ReportingSnapshot | LEGACY_GAP | Domain model+persistence/tests; Architecture allowlist marks LegacyGap | Relocate to reporting artifact/projection boundary and add lineage/lifecycle. |
| WorkspaceUsageDaily / FeatureUsageDaily | INFRASTRUCTURE_ONLY / NO_GROWTH | Persistence types exist; no proven production population/Metric contract | Do not certify or expand until a Metric/source handoff admits them. |
| Cross-context reports | ABSENT | No report-composition runtime found | Implement only from certified projections/reporting contracts. |
| Report/Dashboard API | ABSENT | No API endpoint tree found | Implement after Metric/Dashboard contracts stabilize. |
| Analytics frontend | ABSENT | No analytics/report feature/package; Workspace dashboard is separate | Create dedicated P6 consumer boundary if released. |
| Export/report jobs | ABSENT | No export/report artifact runtime found | Implement independently; no readiness inference from Dashboard. |
| Realtime Analytics | NOT_REQUIRED_FOR_INITIAL_P6 / ABSENT | No producer/consumer recovery contract found | Query/refetch is initial authority; any later realtime must be recoverable. |
| RLS reporting scope | IMPLEMENTED_MECHANISM | RLS policies cover dashboards/widgets/sources/snapshots/placement | Retain and rerun exact-candidate tenant/runtime proof. |
| Observability/data quality | PARTIAL | Reference projection has targeted logs/tests; no broad P6 quality/lag framework | Add capability-specific metrics/reconciliation/health evidence. |

## 6. Frozen P6 delivery decisions

```text
Reference projection:
  retain WorkspaceWorkItemPlacementProjection
  retain producer revision as ordering authority
  retain local-read + producer-owned rebuild boundaries

Metric layer:
  implement typed/versioned Metric definition registry
  do not fabricate product Metrics without a Product/Source handoff

Dashboard:
  implement Application/API lifecycle
  persist explicit private owner identity
  release Private + Workspace visibility
  keep Public visibility non-reachable in P6

Dashboard source:
  bind released sources/widgets to approved Metric/report/source-contract IDs
  Search/External remain non-reachable until separately admitted

Widget:
  DashboardWidgetType is canonical P6 widget-kind vocabulary
  retire/migrate/guard duplicate Widgets.WidgetType
  JSON persistence only behind typed/versioned per-kind contracts

ReportingSnapshot:
  derived reporting artifact/projection, not source authority
  relocate out of Domain LegacyGap
  add Metric/report version + source cutoff lineage + retention lifecycle

WorkspaceUsageDaily / FeatureUsageDaily:
  no-growth until Metric/source semantics and production population path exist

Frontend:
  Workspace dashboard remains Workspace operational UI, not P6 evidence
  released P6 UI uses a dedicated Analytics contract/query boundary

Realtime:
  not required for initial P6 certification
  query/refetch is authoritative
  any later realtime path must be gap-recoverable

Export:
  certify independently from Dashboard/report query readiness

Service/data-platform extraction:
  not part of P6
```

## 6.1. Frozen initial P6 released slice

The initial P6 product slice is concrete. Implementation agents MUST NOT choose an arbitrary first Metric/report.

### Metric — `work-item-count:v1`

```text
Metric ID: work-item-count
Semantic version: v1
Product meaning: count of current Work item placement rows represented by WorkspaceWorkItemPlacementProjection
Source: WorkManagement placement V2 facts → WorkspaceWorkItemPlacementProjection
Scope: one Workspace
Aggregation: COUNT(current placement rows)
Filter: archiveState = active | archived | all
Optional grouping: boardId, groupId
Time basis: current derived placement state; no historical date-range semantics in v1
Timezone: not applicable to the metric value
Freshness: eventually-consistent; expose generatedAt + projection freshness/degraded metadata
Correction: newer producer revision / producer-owned rebuild reconciliation
Zero: only when projection/source lane is initialized and healthy; degraded/unavailable is not zero
Authorization: Workspace report action + source-resource visibility for board/group drill-down
```

### Report — `work-placement-overview:v1`

```text
Report ID: work-placement-overview
Version: v1
Metric: work-item-count:v1
Views: active count; archived count; active count by Board; active count by Group
Scope: one Workspace
Filters: boardId, groupId, archiveState
Date range: unsupported in v1; supplying one is a validation error
Freshness: same reference-projection contract as work-item-count:v1
Frontend owner: dedicated Analytics feature/package
Dashboard: Private + Workspace Dashboards may reference this Metric/report
Export: none in initial P6
Scheduled delivery: none in initial P6
Realtime: none required; query/refetch is authoritative
```

### Dashboard source target

P6 MUST add stable `Metric` / `Report` Dashboard source identity plus semantic version. Existing `Board` / `BoardView` rows are compatibility inputs only until explicitly mapped to an approved analytical contract. `Search` and `External` remain non-reachable in initial P6.

The released slice therefore requires `MetricCatalog → work-item-count:v1 → work-placement-overview:v1 → typed API → dedicated Analytics frontend`, plus Dashboard lifecycle. No other Metric/report/source becomes released because a Domain type or event exists.

## 7. Confirmed preparation gaps

| Gap | Description | Status | Source evidence | Requirements |
|---|---|---|---|---|
| `AR-GAP-01` | No generic Metric registry/runtime | `CONFIRMED` | Product explicitly says no obvious first-class generic Metrics source; current Application has no Metric registry/query engine. | ARREQ017–ARREQ024 |
| `AR-GAP-02` | Dashboard/Widget is Domain+persistence only | `CONFIRMED` | Dashboard/Widget/Source aggregates/configuration exist, but no Analytics Application handlers or API endpoints were found. | ARREQ041–ARREQ048, ARREQ090 |
| `AR-GAP-03` | Dashboard ownership/visibility contract incomplete | `CONFIRMED` | Dashboard has Private/Workspace/Public but no explicit OwnerUserId and no released public/share authorization implementation. | ARREQ042–ARREQ043, ARREQ049–ARREQ056 |
| `AR-GAP-04` | Dashboard Source/Widget config is only partially semantic | `CONFIRMED` | DashboardSource uses Board/BoardView/Search/External + JsonValue filter; Widget validation checks basic JSON fields but not canonical Metric/source semantics. | ARREQ045–ARREQ047, ARREQ116 |
| `AR-GAP-05` | Duplicate widget vocabularies | `CONFIRMED` | `DashboardWidgetType` and `Widgets.WidgetType` coexist with different values and no single released contract. | ARREQ046, ARREQ116 |
| `AR-GAP-06` | ReportingSnapshot is an architecture LegacyGap | `CONFIRMED` | DomainHardeningArchitectureTests classifies ReportingSnapshot as LegacyGap; current model lacks full Metric/source-cutoff lineage. | ARREQ065–ARREQ072 |
| `AR-GAP-07` | Infrastructure daily usage models have no proven P6 producer | `CONFIRMED` | WorkspaceUsageDaily/FeatureUsageDaily persistence models exist, but source search found no production population/Metric registry path. | ARREQ079 |
| `AR-GAP-08` | Only Work placement projection is production-hardened reference | `CONFIRMED` | Work item placement has consumers/query/rebuild/recovery tests; Documents/Collaboration/Automation/Integrations/Billing analytical projections are not equivalently implemented. | ARREQ033–ARREQ040, ARREQ073–ARREQ080 |
| `AR-GAP-09` | No released report/Metric/Dashboard API | `CONFIRMED` | No Notrelix.API Analytics/Reporting/Dashboard endpoint tree was found at the preparation SHA. | ARREQ089–ARREQ091 |
| `AR-GAP-10` | No dedicated Analytics frontend surface | `CONFIRMED` | No frontend Analytics/report package exists; current `/workspaces/$workspaceId/dashboard` is Workspace-owned and directly queries Boards/Pages. | ARREQ092–ARREQ096 |
| `AR-GAP-11` | No export/scheduled-report delivery pipeline | `CONFIRMED / DEFERRED_INITIAL_P6` | No Analytics export/report artifact or scheduled-delivery Application/API/runtime path was found; initial P6 explicitly releases neither capability. | ARREQ097–ARREQ104 |
| `AR-GAP-12` | No cross-context report composition runtime | `CONFIRMED` | Product/team define cross-context analytics, but exact source shows no P6 report composer joining certified projections. | ARREQ081–ARREQ088 |
| `AR-GAP-13` | Freshness/completeness contract not exposed end to end | `PARTIAL_GAP` | Placement rows have SourceRevision/LastOccurredAt, but no general Metric/report freshness/cutoff/degraded response contract exists. | ARREQ057–ARREQ060 |
| `AR-GAP-14` | No Analytics realtime producer/consumer contract | `CONFIRMED` | Product allows realtime but no Analytics public/realtime producer or dedicated frontend recovery path was found; P6 initial release does not require realtime. | ARREQ062–ARREQ064 |
| `AR-GAP-15` | Retention/deletion/privacy lifecycle not executable for reporting artifacts | `PARTIAL_GAP` | Product policy is detailed, but no P6 Application lifecycle for snapshot/export retention/anonymization/purge exists. | ARREQ071–ARREQ072, ARREQ055 |
| `AR-GAP-16` | Analytics-specific data-quality/ops signals incomplete | `PARTIAL_GAP` | Reference projection has logs/tests, but broad lag/divergence/reconciliation/report/export telemetry is not implemented as P6 capability. | ARREQ109–ARREQ112 |
| `AR-GAP-17` | Historical TAC status is mechanism evidence, not product readiness | `DOC_STALE_RISK` | AR-FLOW/TAC evidence proves the Work reference mechanism but can be misread as certifying product Metrics/Dashboard/reporting. | ARREQ040, ARREQ123–ARREQ128 |
| `AR-GAP-18` | Original P6 package lacked a concrete released Metric/report slice | `DOC_GAP_CLOSED_BY_EXECUTION_SPEC` | This SPEC freezes `work-item-count:v1` + `work-placement-overview:v1`; other sources/reports remain independently blocked/deferred until admitted. | ARREQ003–ARREQ008, ARREQ017–ARREQ024, ARREQ089–ARREQ096, ARREQ128 |

# Normative requirements

## 8. Global ownership and execution governance

### ARREQ001 — Analytics remains derived state

Analytics-owned projections, metrics, reports, dashboards, snapshots and exports MUST remain derived/read-oriented state. They MUST NOT become the transactional source of truth for Work Management, Documents, Collaboration, Automation, Integrations, Billing, Identity, Workspace or Governance.

### ARREQ002 — Analytics never hides source mutation

Dashboard controls, filters, drill-downs and report interactions MUST NOT mutate source business state unless they invoke an explicit source-context command through that context's public contract and normal authorization/invariants.

### ARREQ003 — Authority precedence is explicit

Product Analytics semantics come from `docs/product/analytics.md`; team delivery boundaries come from `docs/workstreams/teams/analytics-reporting.md`; backend/frontend architecture owners define mechanics; this execution SPEC refines but MUST NOT override higher authority.

### ARREQ004 — Brownfield source posture precedes implementation

Every P6 capability MUST be classified from exact source as production-reachable, implemented reference, domain-only, infrastructure-only, legacy-gap, contract-drift or absent before implementation. Source existence alone is not readiness.

### ARREQ005 — Product-source gaps remain explicit

Documentation MUST NOT invent a generic Metric engine, report API, dashboard API, export pipeline, realtime producer or frontend Analytics surface when exact source does not contain them. Missing implementation remains named debt until implemented and proven.

### ARREQ006 — Exact-candidate evidence is mandatory

Final readiness MUST bind to one accepted candidate SHA and non-zero execution evidence. Historical TAC/CI evidence is preparation context only unless rerun on the accepted candidate or explicitly reused under a still-valid immutable contract.

### ARREQ007 — Consumer-specific readiness replaces one global score

Metrics, projections, reports, exports and source contexts are certified independently. One stable WorkManagement projection MUST NOT certify Documents, Billing, Automation, Integrations or cross-context analytics.

### ARREQ008 — No broad cleanup PR

P6 changes MUST be capability-directed. Unrelated refactors, data-platform extraction, provider-neutral abstractions or repository-wide changes require their own authority and MUST NOT be smuggled into Analytics implementation.

## 9. Source contracts and source inventory

### ARREQ009 — Every analytical source has an approved semantic owner

Each projection or Metric MUST name the bounded context that owns the source fact and the approved event/reporting/query contract by which Analytics consumes it.

### ARREQ010 — Source handoff records stable identity and scope

Each admitted source MUST record logical contract identity/version, Account/Workspace/resource scope, source occurrence/business time, producer revision/sequence where applicable, replay/backfill availability and compatibility window.

### ARREQ011 — Analytics does not create private-table source contracts

A source schema/table name, EF entity, CLR type or ad-hoc cross-context join MUST NOT become the semantic source contract for Analytics.

### ARREQ012 — Source queries are producer-owned public contracts

When an event lacks sufficient information or rebuild needs current source state, Analytics MUST call a producer-owned public reporting/projection-source contract through an Analytics-owned port/ACL; Analytics MUST NOT read the producer DbContext directly.

### ARREQ013 — Source correction and deletion semantics are admitted before projection

Each source handoff MUST define correction/reversal/archive/delete behavior and whether historical derived state is repaired, retained, anonymized or purged.

### ARREQ014 — Source security classification propagates into Analytics

Sensitive fields, resource visibility, tenant/data-region obligations and authorization-relevant source facts MUST be carried into the analytical authorization/privacy design; aggregation does not erase those obligations.

### ARREQ015 — Source-event compatibility includes backlog and replay

A source event version change MUST review deployed Analytics consumers, retained/backlogged messages, rebuild source, producer revisions and old/new projection compatibility before removing old support.

### ARREQ016 — Reference projection does not define all source semantics

`WorkspaceWorkItemPlacementProjection` is the current production reference mechanism for Work placement only. Its identity/order/rebuild choices MUST NOT be copied blindly into metrics or source contexts with different semantics.

## 10. Metric semantics

### ARREQ017 — Metric identity is stable and versioned

Every released Metric MUST have a stable semantic key plus explicit semantic version. Widget ID, chart title, SQL query filename, endpoint name or frontend label MUST NOT be used as Metric identity.

### ARREQ018 — Metric definition has one canonical owner

A released Metric MUST have one canonical definition containing source facts, scope, filters, aggregation, dimensions, time basis, unit, rounding, freshness and privacy/authorization class. Backend, export and frontend MUST consume that definition rather than reimplement formulas.

### ARREQ019 — Displayed precision and comparison basis require defined meaning

Every user-visible number MUST have a defined unit, reporting period, inclusion/exclusion rule, freshness state and comparison basis. If the UI shows a comparison such as “vs previous period”, the current window and comparison window MUST define exact start/end boundaries, inclusivity, timezone/calendar basis and Metric semantic-version compatibility. The UI MUST NOT display apparently precise numbers whose denominator, source cutoff or comparison window is undefined.

### ARREQ020 — Rate and percentage metrics define numerator and denominator

Percent/rate Metrics MUST explicitly define numerator, denominator, excluded states, zero-denominator behavior, unit/rounding and applicable time window.

### ARREQ021 — Time-based Metrics define calendar and timezone semantics

Daily/weekly/monthly and relative-window Metrics MUST define event/business timestamp, report timezone, boundary inclusivity, DST behavior and Account/user timezone precedence where product-defined. Browser locale or chart-library defaults are not authority.

### ARREQ022 — Metric corrections are explicit

If source facts can be corrected/reversed/archived, the Metric definition MUST specify whether and how historical buckets/aggregates are repaired, reopened, compensated or frozen.

### ARREQ023 — Zero, no data, unknown, unavailable and unauthorized are distinct

Metric/query contracts MUST preserve these states and MUST NOT coerce all missing/forbidden/stale inputs to numeric zero.

### ARREQ024 — Breaking Metric meaning requires historical-comparability policy

A material semantic change MUST be classified as implementation fix, new Metric version or breaking replacement and MUST define backfill/recompute, old/new history behavior, comparison/trend break and API/frontend migration. Previous-period or year-over-year comparison is valid only when both windows use semantically compatible Metric versions; otherwise the product MUST show a break marker, separate series or explicitly recomputed comparable history.

## 11. Projection foundation

### ARREQ025 — Projection names its authoritative source and derived identity

Each projection MUST record source contract(s), projection key, tenant scope, semantic version, update rule and rebuilding authority.

### ARREQ026 — Projection update is idempotent

Duplicate delivery of the same logical source occurrence MUST NOT double-apply derived state. Distinct occurrences with the same event type MUST remain distinct.

### ARREQ027 — Ordering is the smallest semantics-required scope

Each projection MUST declare whether it is order-independent, per-resource ordered, per-workspace/account ordered or window-ordered. Global ordering MUST NOT be introduced when source revision or a narrower stream is sufficient.

### ARREQ028 — Late and out-of-order facts cannot regress projection state

When ordering/version proves an incoming fact is stale, it MUST NOT overwrite newer derived state. When ordering cannot be proven, the projection MUST use a safe reconciliation/invalidation policy instead of guessing.

### ARREQ029 — Projection failure commits no false checkpoint

Projection state, dedup/consumer settlement and any progress/checkpoint MUST advance only after the required derived effect durably succeeds; crash-before-commit MUST remain safely retryable under Platform delivery semantics.

### ARREQ030 — Projection normal reads are local

Normal report/query reads MUST use Analytics-owned derived state or explicit transactional source query when that is the declared design. A local projection query MUST NOT synchronously re-read foreign source persistence on every request.

### ARREQ031 — Projection rebuild and backfill are operationally bounded

Critical derived state MUST have a deterministic rebuild/reconciliation path using approved producer snapshots, replayable facts or another approved source. Every rebuild/backfill contract MUST define Account/Workspace/resource/date scope, source, rate limit, production-impact guard, durable checkpoint, resumability, duplicate handling, validation/reconciliation evidence and source-version handling. Unbounded ad-hoc production scripts are not an accepted rebuild mechanism.

### ARREQ032 — Backfill and live traffic converge without double counting

Backfill/rebuild MUST coordinate with concurrent live consumers through source revision, idempotency, cutover or another explicit mechanism so live facts cannot be applied twice or overwritten by an older snapshot. Resuming from a durable checkpoint MUST preserve the same rule, and final validation MUST compare the rebuilt result with an approved producer invariant/snapshot before the backfill is declared complete.

## 12. Work placement reference projection

### ARREQ033 — Work placement reference keeps producer revision authority

The existing Work placement projection MUST continue to use WorkManagement producer revision as semantic ordering authority. Wall-clock timestamps MUST NOT replace producer revision as ordering truth.

### ARREQ034 — Work placement live consumers use event facts when sufficient

Moved/archived consumers MUST project from the committed event payload when it contains all required placement facts; they MUST NOT synchronously fetch Work source state merely for convenience.

### ARREQ035 — Work placement incomplete events use the producer public source

When a committed event such as item creation lacks required placement fields, the consumer MAY use the producer-owned `IWorkItemProjectionSource` path; no WorkManagement private persistence dependency is permitted.

### ARREQ036 — Work placement local query remains Analytics-owned

`GetWorkspacePlacementsQuery` or its replacement MUST read only Analytics reporting state under canonical authorization/tenant scope and MUST NOT become a hidden live WorkManagement query.

### ARREQ037 — Work placement rebuild preserves newer live facts

Rebuild snapshot application MUST never overwrite a strictly newer producer revision. Rows absent from an old snapshot require producer revalidation before deletion.

### ARREQ038 — Work placement crash recovery remains atomic

A projection persistence failure before commit MUST roll back the derived mutation and consumer claim/settlement so Platform redelivery can converge without mutating Work source state.

### ARREQ039 — Reference projection is internally consumable but not falsely exposed as a product report

The placement query may remain an internal/read-model capability until a product Metric/report consumes it. Its existence MUST NOT be presented as a complete Analytics dashboard/report API.

### ARREQ040 — Reference projection exact-candidate proof is reused correctly

TAC `AR-FLOW-01..04` evidence may be reused as preparation anchors, but P6 final certification MUST rerun the affected production graph when source event, revision, rebuild, RLS, broker/dedup or projection persistence behavior changes.

## 13. Dashboard and Widget configuration

### ARREQ041 — Dashboard is user-managed Analytics configuration

Dashboard identity, name, lifecycle, ownership, visibility, sources, widgets and layout are Analytics-owned configuration. Dashboard mutation MUST NOT change source Boards, Items, Pages, Automations, Integration state or Billing facts.

### ARREQ042 — Private Dashboard has explicit owner

A Private Dashboard MUST persist an explicit owner/principal identity and define transfer/deletion behavior; `CreatedBy` audit metadata alone MUST NOT silently serve as mutable authorization ownership.

### ARREQ043 — Released Dashboard visibility is Governance-backed

P6 releases Private and Workspace visibility only. `Public` visibility remains non-reachable until a separate public/share authorization contract exists. Workspace visibility MUST still respect source/resource authorization.

### ARREQ044 — Dashboard archive/delete is non-destructive

Archiving/deleting Dashboard/Widget/Source removes Analytics configuration according to policy and MUST NOT cascade into source bounded-context records.

### ARREQ045 — Dashboard Source references stable analytical/source contracts

Dashboard Source MUST reference an approved Metric/report/source-contract identity and version rather than private table semantics. Existing `Search`/`External` enum values are not released until an explicit source contract is admitted.

### ARREQ046 — DashboardWidgetType is the canonical P6 widget-kind vocabulary

For P6, `DashboardWidgetType` is the persisted/public Dashboard widget-kind authority. The standalone `Widgets.WidgetType` duplicate MUST be retired, migrated or kept non-reachable so two independent widget vocabularies cannot diverge.

### ARREQ047 — Widget configuration is typed and versioned by widget kind

Persistence MAY remain JSON, but every released widget kind MUST have a typed/versioned config contract validated against the selected Metric/source semantics. Basic JSON-shape validation alone is insufficient.

### ARREQ048 — Widget layout never changes source ordering

Move/resize/reorder operations affect only Dashboard layout. Widget order/position MUST NOT update Work item order, document order or any source-context ordering field.

## 14. Authorization, tenancy and privacy

### ARREQ049 — Analytics request scope is explicit Account and Workspace

Every workspace analytical request, query, projection operation, export and background rebuild MUST carry/restore the correct Account/Workspace scope and use canonical tenant/RLS enforcement.

### ARREQ050 — Dashboard visibility does not grant source permission

Viewer access to a Dashboard determines access to the Dashboard configuration, not automatic access to all rows/resources underlying its Widgets.

### ARREQ051 — Aggregate visibility does not imply drill-down visibility

A principal allowed to see an aggregate MAY still be forbidden from individual resources. Drill-down MUST re-evaluate the source/report authorization contract.

### ARREQ052 — Aggregation does not erase confidentiality

Sensitive/low-cardinality groups MUST be masked, suppressed or restricted where product/security policy requires it. Analytics MUST NOT expose protected PII merely because values are grouped.

### ARREQ053 — Cross-tenant analytics is privileged and explicit

Ordinary Workspace users MUST NOT query cross-tenant projections or caches. Any global/admin analytics requires a separately authorized product capability and MUST NOT arise accidentally from shared storage.

### ARREQ054 — Authorization-sensitive caching is partitioned

Cache identity MUST include tenant plus Metric/report version, filters/time range/timezone and any authorization boundary needed to prevent principals with different visibility from sharing unsafe results.

### ARREQ055 — Derived data inherits security and data-location obligations

Replicating source data into reporting tables, caches, exports or a future warehouse does not remove Account region, privacy, retention or access restrictions.

### ARREQ056 — RLS is defense in depth, not the sole report authorization policy

Reporting tables remain protected by RLS, while Application/Governance still determines report actions, Dashboard visibility, sensitive dimensions and drill-down permissions.

## 15. Freshness, completeness and realtime

### ARREQ057 — Every released Metric/report has a freshness class

Declare transactional/current, near-realtime/eventually-consistent, scheduled/batch or another explicit class. The API/UI MUST NOT call an asynchronous projection 'live' without meeting that contract.

### ARREQ058 — Freshness metadata is queryable

Where interpretation depends on freshness, report responses MUST include meaningful `dataThrough`/source cutoff, generated/last-updated time, projection lag/degraded state or equivalent metadata.

### ARREQ059 — Stale, unavailable and not-yet-projected are distinct

A stale projection MAY be shown with explicit metadata, refreshed or temporarily unavailable; it MUST NOT be silently presented as exact current truth.

### ARREQ060 — Multi-source reports declare completeness and cutoff strategy

A report combining sources with different freshness MUST expose partial/degraded state or use a defined common cutoff. It MUST NOT silently mix incompatible windows.

### ARREQ061 — Analytics failure does not block unrelated transactions

Projection/report outages are downstream failures by default. WorkManagement/Documents/Automation/Billing transactional commands MUST continue unless an explicit source invariant says otherwise.

### ARREQ062 — Realtime is freshness, not Analytics authority

Durable query/projection state remains authoritative. Realtime messages MUST NOT become the only record of a Metric, Dashboard configuration, report job or snapshot.

### ARREQ063 — Initial P6 certification does not require Analytics realtime

P6 MAY ship query/refetch-driven Analytics without a realtime producer. No realtime surface may be advertised as released unless its producer contract and recovery path are implemented and certified.

### ARREQ064 — Any Analytics realtime path is gap-recoverable

If realtime is released, duplicate/out-of-order/gap/reconnect handling MUST converge by authoritative refetch/reconciliation and tenant-scoped subscription. Missing realtime MUST never permanently corrupt Dashboard or Metric state.

## 16. Snapshots, history and retention

### ARREQ065 — Reporting Snapshot is a derived report artifact, not transactional Domain authority

P6 MUST treat `ReportingSnapshot` semantics as an Analytics reporting artifact/projection. It MUST NOT be promoted into source business authority or used as the reference projection for unrelated Metrics.

### ARREQ066 — Reporting Snapshot leaves the Domain LegacyGap classification

The current Domain `ReportingSnapshot` LegacyGap MUST be closed by relocating snapshot persistence/modeling to the Analytics Application/Infrastructure reporting artifact boundary while retaining product semantics and migration compatibility.

### ARREQ067 — Snapshot identity contains reproducibility lineage

A released Snapshot MUST record report/Metric identity+version, Account/Workspace scope, schema version, source cutoff/watermark, captured/generated time and payload. `ReportType + SchemaVersion + Data + CapturedAt` alone is insufficient for certification.

### ARREQ068 — Snapshot schema readers are version-aware

Retained historical snapshot payloads MUST be read with their stored schema/Metric version. New code MUST NOT deserialize/reinterpret old data under a new schema silently.

### ARREQ069 — Snapshot and current rebuilt projection may legitimately differ

UI/API MUST distinguish 'what was reported then' from 'what current source recomputation says now' when retention, deletion, anonymization or Metric-version changes make them differ.

### ARREQ070 — Historical comparisons preserve Metric compatibility

A Snapshot or historical series MUST NOT be compared as one continuous semantic series across incompatible Metric versions unless explicit recomputation/mapping policy permits it.

### ARREQ071 — Source deletion follows explicit derived-data policy

Analytics-owned projection/snapshot rows are purged, anonymized, retained aggregate-only or recomputed according to product/privacy policy; deletion MUST NOT depend on accidental foreign SQL cascade.

### ARREQ072 — Snapshot/export retention and access are explicit

Retention, expiration, deletion, legal/privacy handling and download/read authorization for retained reporting artifacts MUST be governed independently from source active-state lifetime.

## 17. Source-context onboarding

### ARREQ073 — WorkManagement analytics consumes canonical Work facts

Completion, overdue, active, archived, workload, field/status or cycle semantics MUST come from approved WorkManagement contracts. Report-local guesses such as label='Done' are forbidden.

### ARREQ074 — Documents analytics minimizes content exposure

Documents metrics SHOULD consume metadata/events/reporting contracts; rich content or sensitive block data requires explicit product/security approval and source authorization.

### ARREQ075 — Collaboration analytics remains distinct from Activity/Audit

Comment/reaction/collaboration Metrics MAY derive from collaboration facts, but user-facing Activity and governed Audit remain separate contracts and MUST NOT be reconstructed from Analytics counts.

### ARREQ076 — Automation analytics preserves Automation execution semantics

Execution count, success/failure, duration and retry Metrics MUST consume Automation-owned execution facts and classifications; Analytics MUST NOT redefine execution success or provider outcome.

### ARREQ077 — Integrations analytics preserves provider/sync semantics

Connection health, sync failure, rate-limit or lag Metrics MUST consume Integrations-owned facts without provider credentials/raw sensitive payloads and without redefining provider state.

### ARREQ078 — Billing analytics never becomes commercial authority

Subscription/usage/entitlement trends MAY be reported, but invoice totals, billable usage, entitlement decisions and commercial ledger semantics remain Billing-owned.

### ARREQ079 — Account/workspace metrics use canonical Workspace/Identity facts and legacy usage models remain no-growth

Account/workspace activity, workspace count and membership-derived Metrics MUST consume approved Account/Workspace/Identity public facts such as AccountCreated, WorkspaceCreated and WorkspaceMemberAdded/Removed (or a producer-owned reporting contract) and MUST NOT turn Analytics into a membership store. `WorkspaceUsageDaily` and `FeatureUsageDaily` remain infrastructure-only/no-growth until a named Metric owner, Source→Analytics handoff and production population/rebuild path are explicit.

### ARREQ080 — Each source context is certified independently

WorkManagement, Workspace/Account/Identity, Documents, Collaboration, Automation, Integrations and Billing source onboarding each require their own contract/readiness/security/rebuild evidence. A green Work reference projection cannot substitute for Workspace/Identity or any other source.

## 18. Cross-context reporting

### ARREQ081 — Cross-context report composes approved derived/source contracts

Cross-context Analytics MUST combine certified projections/Metric outputs or approved reporting contracts; it MUST NOT establish a permanent semantic dependency on foreign private tables.

### ARREQ082 — Cross-context joins use stable shared identity

Join keys MUST be explicitly compatible Account/Workspace/resource identities or another approved contract. Display name, email, CLR type or unrelated private database PK is not an analytical join contract.

### ARREQ083 — Cross-context consistency is not overstated

The report MUST declare point-in-time/common-cutoff, bounded-staleness, or independent-source freshness semantics. P6 MUST NOT imply atomic multi-context consistency that the architecture does not provide.

### ARREQ084 — Authorization is the intersection of participating sources

A cross-context report MUST NOT broaden access. The viewer must satisfy the report action plus the participating source/privacy contracts required for the returned aggregate/detail.

### ARREQ085 — Cross-context correction propagates deterministically

Source correction/version/deletion in any participating projection MUST update or invalidate the composed report according to declared semantics without retaining impossible combinations.

### ARREQ086 — Partial multi-source results are explicit

If one source is unavailable or behind its accepted freshness target, the report MUST return explicit partial/degraded metadata or fail according to its contract; it MUST NOT manufacture zero/default values.

### ARREQ087 — Cross-context cache identity includes every semantic dimension

Cache keys MUST include all source/Metric versions, tenant scope, report filters/timezone/cutoff and authorization-sensitive boundary required for safe reuse.

### ARREQ088 — No synchronous transactional dependency on Analytics

Source contexts MUST NOT call P6 reporting projections synchronously to authorize or decide their own transactional invariants. Analytics remains replaceable/downstream.

## 19. Report API and frontend

### ARREQ089 — Report/Metric API is typed and bounded

Released query APIs MUST expose report/Metric identity, Account/Workspace scope, filters, date range, timezone, grouping, sorting/pagination/limits and freshness/completeness metadata with deterministic validation/error semantics.

### ARREQ090 — Dashboard Application/API lifecycle is implemented before release

P6 MUST provide canonical Application/API commands/queries for create/list/detail/rename, visibility, source management, widget add/update/move/remove and archive/delete behavior before claiming Dashboard production readiness.

### ARREQ091 — No direct HTTP source-table analytics

API handlers MUST call Analytics Application queries/use cases over Analytics-owned projections/Metric services. They MUST NOT perform ad-hoc joins across other bounded-context DbSets.

### ARREQ092 — Frontend uses server-owned Metric semantics

Frontend formats and visualizes returned Metric/report contracts but MUST NOT independently recalculate a semantically different numerator, denominator, timezone bucket or source filter.

### ARREQ093 — Workspace operational dashboard is not Analytics product evidence

`frontend/apps/web/src/routes/workspaces/$workspaceId/dashboard.tsx` remains a Workspace-owned operational surface that queries Boards/Pages directly. It MUST NOT be counted as P6 Analytics frontend readiness.

### ARREQ094 — P6 frontend has a dedicated Analytics contract boundary

If Dashboard/report UI is released, create/route a dedicated Analytics frontend package/feature using generated public API contracts and canonical tenant-aware query keys rather than extending the Workspace dashboard with handwritten report semantics.

### ARREQ095 — Frontend query keys partition tenant and report semantics

Account, Workspace, report/Metric ID+version, filters, time range, timezone, grouping and authorization-sensitive identity MUST participate in server-state identity as required.

### ARREQ096 — Frontend state distinguishes zero/no-data/stale/partial/forbidden/unavailable

Loading/empty/error/degraded UX MUST preserve backend analytical meaning; Account/Workspace switch MUST clear/isolate previous tenant analytical data and pending work.

## 20. Export and generated report artifacts

### ARREQ097 — Export is a protected Analytics operation

Every released export MUST name report/Metric version, tenant scope, filters/date/timezone, selected columns/dimensions, authorization policy, size/row limits and artifact retention.

### ARREQ098 — Export reuses the same query semantics as interactive reporting

Export MUST use the certified Metric/report definition and source authorization. It MUST NOT recalculate formulas or bypass field/resource visibility.

### ARREQ099 — Large export uses asynchronous report job semantics

When synchronous limits are exceeded, P6 MUST use a durable report/export job with stable job identity and explicit queued/running/completed/failed/expired states; the request path MUST NOT hold an unbounded query/download open.

### ARREQ100 — Export artifact captures a source cutoff

Generated files MUST record/report the data cutoff or snapshot time used so UI totals and downloaded content can be explained when freshness differs.

### ARREQ101 — Export download remains authorized

Artifact storage/download MUST use expiring or authenticated access and revalidate appropriate tenant/report authorization; permanent public URLs are forbidden unless separately designed.

### ARREQ102 — Export artifact contains no hidden sensitive dimensions

Hidden source fields, private resources and restricted PII MUST remain excluded under the same report policy even if the file format could technically include them.

### ARREQ103 — Export job retries cannot duplicate artifacts/effects unsafely

Retry uses stable job/artifact identity and MUST NOT produce ambiguous multiple final files or advance completed state before durable artifact creation.

### ARREQ104 — Export and scheduled-report delivery are independently certifiable

Initial P6 releases neither export nor scheduled-report delivery for `work-placement-overview:v1`; both MUST be non-reachable in API/frontend/background-worker capability discovery. If a later released slice admits export, ARREQ097–ARREQ103 apply in full. If a later slice admits scheduled reports, its handoff MUST define report definition/version, tenant scope, recipient identity, source cutoff/snapshot, delivery owner/channel, authorization at request time and delivery time, retry/idempotency, retention/expiration and failure visibility. Delivery belongs to Collaboration/Platform or another approved owner rather than becoming an ad-hoc Analytics mailer. Dashboard/Metric readiness MUST NOT imply export or scheduled-delivery readiness.

## 21. Performance, storage, cache and data quality

### ARREQ105 — Enterprise dashboard requests do not full-scan arbitrary JSON

Recurring high-cardinality filters/grouping/aggregation MUST use typed/indexed/materialized projection fields or another measured strategy rather than scanning arbitrary flexible JSON on every request.

### ARREQ106 — Storage technology follows measured need

P6 stays in the modular-monolith/reporting store until measured load and isolation justify another data platform. A warehouse/service is not introduced solely because Analytics might scale differently.

### ARREQ107 — Index/partition/pre-aggregation is metric-driven

Indexes, partitions and pre-aggregates MUST map to measured query patterns and canonical Metric semantics and must retain tenant scope, rebuild and correction behavior.

### ARREQ108 — Analytics cache keys are semantically complete

Cache keys MUST include tenant, Metric/report version, filters, time range, timezone, grouping/cutoff/freshness identity and authorization scope needed for correctness.

### ARREQ109 — Data-quality checks detect projection divergence

Critical projections/metrics MUST detect missing/duplicate application, impossible counts, source/projection divergence, orphaned identities, unsupported source versions and lag beyond declared targets.

### ARREQ110 — Critical Metrics have an approved reconciliation source

Reconciliation MUST compare against an architecture-approved producer snapshot/invariant, not casually query private source tables. Mismatch policy and repair path are explicit.

### ARREQ111 — Operational observability exposes lag/backfill/report health

Track consumer/projection failures, lag/oldest age where relevant, duplicate/stale apply, backfill progress, report latency, export backlog and recovery results without exposing secrets or unnecessary PII.

### ARREQ112 — Operational telemetry is not product Analytics

Queue depth, HTTP latency, CPU/memory and tracing metrics remain Operations signals unless intentionally transformed through an approved product Metric definition.

## 22. Migration and compatibility

### ARREQ113 — Reporting schema migration has clean and supported-upgrade proof

Changes to reporting tables, RLS, JSON config, projection identity, snapshot artifact schema or Metric registry MUST pass clean database, supported upgrade and pending-model-drift checks.

### ARREQ114 — Projection migration chooses migrate or rebuild explicitly

For each schema/semantic change, state whether rows migrate in place, rebuild into v2, dual-read/cutover or are discarded/recomputed; destructive reinterpretation is forbidden.

### ARREQ115 — Metric semantic changes version public contracts

API/cache/snapshot/export/frontend contracts that identify a Metric MUST preserve old versions or provide an explicit compatible rollout/backfill and retirement window.

### ARREQ116 — Persisted Dashboard/Widget configuration is migration-aware

Widget/source discriminator/config schema changes MUST include readers/migration for stored dashboards before removing old shape support.

### ARREQ117 — Source contract version changes preserve consumer/backfill compatibility

P6 MUST support the agreed old/new source event/reporting versions across backlog/replay and rebuild windows and MUST NOT silently abandon retained analytical inputs.

### ARREQ118 — RLS policy deploy order is safe

Reporting tables/policies/indexes/migrations MUST deploy in an order that never creates an interval of broader tenant access or background processing without required session context.

### ARREQ119 — Historical artifacts remain readable after code evolution

Supported retained snapshots/exports/report definitions MUST either remain readable by compatibility readers or be migrated under an explicit retention policy.

### ARREQ120 — Rollback/forward-fix preserves analytical meaning

Rollback MUST NOT restore code that interprets new persisted Metric/config/snapshot data under incompatible old semantics. When unsafe, use forward-fix/cutover instead.

## 23. Architecture, evidence and certification

### ARREQ121 — Architecture gates forbid foreign persistence ownership and new Application EF coupling

Architecture tests MUST prevent Analytics Application/Infrastructure consumers from depending on foreign bounded-context DbContexts/private repository types outside approved producer-owned public adapters. New P6 Application contracts MUST NOT expose `DbSet<T>`/EF Core persistence primitives. The existing `IReportingDbContext` is migration debt: P6 MUST replace its Analytics use cases with feature-owned repository/query-store contracts implemented in Infrastructure, without introducing a generic repository or a second transaction boundary.

### ARREQ122 — Production runtime owner is proved

Projection/rebuild/report/background claims MUST be proven through actual production DI/message/data-session composition. Isolated Domain/unit tests alone cannot certify runtime reachability.

### ARREQ123 — TAC AR-FLOW reference is reused without becoming product authority

`AR-FLOW-01` live projection, `AR-FLOW-02` local read, `AR-FLOW-03` producer-backed rebuild and `AR-FLOW-04` failure/recovery remain canonical mechanism evidence for the Work placement reference; P6 product Metrics/Dashboard semantics remain owned here.

### ARREQ124 — Required failure paths are executable evidence

Duplicate, stale/out-of-order, crash-before-commit, source unavailable, rebuild retry, cross-tenant, forbidden drill-down, stale/partial, migration and large-query limit paths MUST have executable proof where applicable.

### ARREQ125 — CI reports non-zero execution and exact evidence

Required backend/frontend/architecture/migration/runtime suites MUST report discovered/executed/passed/failed/skipped counts and exact workflow/job/artifact locators for the accepted candidate.

### ARREQ126 — Readiness targets are capability-specific

P6 certification MUST distinguish source readiness, Metric semantics, projection correctness, Dashboard/API/frontend, snapshot/export, privacy/security, freshness and operations. Deferred/unsupported slices stay explicit.

### ARREQ127 — Cross-team handoffs preserve team authority

P6 MUST use the source-to-Analytics, Metric and Report handoff schemas from the team authority and append candidate/evidence/invalidation metadata without replacing their required semantic fields.

### ARREQ128 — P6 completion is an exact released-slice decision

P6 is complete only for explicitly listed Metrics, source projections, reports, Dashboards, exports and frontend surfaces whose requirements/tests/certification reach target readiness on the accepted candidate; domain classes or green unrelated CI do not imply completion.

# Acceptance criteria

| ID | Criterion | Release assertion |
|---|---|---|
| `ARAC001` | Derived-state ownership | No released P6 path mutates or becomes source business authority. |
| `ARAC002` | Source-contract inventory | Every released source projection has an approved semantic owner/contract/version/scope/rebuild source. |
| `ARAC003` | Metric authority | Every released Metric has one stable identity/version and one canonical semantic definition. |
| `ARAC004` | Projection correctness | Duplicate, stale/out-of-order, crash and rebuild/live overlap converge without double count/regression. |
| `ARAC005` | Work placement reference | AR-FLOW-01..04 remain valid and exact-candidate rerunnable. |
| `ARAC006` | Dashboard lifecycle | Released Dashboard configuration has Application/API lifecycle, explicit owner, safe visibility and non-destructive deletion. |
| `ARAC007` | Widget/source contract | One canonical widget vocabulary and typed/versioned config validated against approved Metric/source semantics. |
| `ARAC008` | Tenant/security/privacy | RLS + Application/Governance authorization + privacy controls prevent cross-tenant and unauthorized aggregate/detail leakage. |
| `ARAC009` | Freshness/completeness | Stale/partial/unavailable/unknown states and source cutoffs are truthful. |
| `ARAC010` | Snapshot semantics | Reporting snapshots are derived artifacts with lineage/version/retention and no Domain-authority LegacyGap. |
| `ARAC011` | Source onboarding | Each Work/Documents/Collaboration/Automation/Integrations/Billing projection is certified independently. |
| `ARAC012` | Cross-context composition | Reports combine approved contracts with explicit identity, cutoff and authorization intersection. |
| `ARAC013` | API/frontend parity | Typed report/Dashboard contracts and frontend state share server-owned Metric semantics and tenant-safe identity. |
| `ARAC014` | Export parity | Released export uses the same Metric/authorization/cutoff semantics and safe artifact access. |
| `ARAC015` | Performance/data quality | Query/load strategy avoids arbitrary JSON full scans and exposes divergence/lag/recovery signals. |
| `ARAC016` | Migration/compatibility | Reporting schema, persisted configs, Metric versions and source-version windows migrate/rebuild without silent reinterpretation. |
| `ARAC017` | Architecture/runtime proof | No foreign private persistence dependency; actual production runtime owner and failure paths are proven. |
| `ARAC018` | Exact-candidate release | All released-slice records bind to the accepted SHA with non-zero evidence and explicit invalidation/handoff. |

# Product-rule crosswalk

`ANA-*` below refers to product rules in `docs/product/analytics.md`.

| Product rule | P6 requirement coverage |
|---|---|
| `ANA-001` | ARREQ001, ARREQ025, ARREQ065 |
| `ANA-002` | ARREQ002, ARREQ048, ARREQ091 |
| `ANA-003` | ARREQ017, ARREQ024, ARREQ115 |
| `ANA-004` | ARREQ018 |
| `ANA-005` | ARREQ009–ARREQ016 |
| `ANA-006` | ARREQ041, ARREQ044, ARREQ048 |
| `ANA-007` | ARREQ041–ARREQ043, ARREQ049 |
| `ANA-008` | ARREQ043, ARREQ050–ARREQ052 |
| `ANA-009` | ARREQ011–ARREQ012, ARREQ045 |
| `ANA-010` | ARREQ046–ARREQ047 |
| `ANA-011` | ARREQ048 |
| `ANA-012` | ARREQ020 |
| `ANA-013` | ARREQ021 |
| `ANA-014` | ARREQ057–ARREQ060 |
| `ANA-015` | ARREQ065, ARREQ067 |
| `ANA-016` | ARREQ067–ARREQ070, ARREQ119 |
| `ANA-017` | ARREQ013, ARREQ071–ARREQ072 |
| `ANA-018` | ARREQ025 |
| `ANA-019` | ARREQ031, ARREQ037 |
| `ANA-020` | ARREQ032 |
| `ANA-021` | ARREQ105–ARREQ107 |
| `ANA-022` | ARREQ078 |
| `ANA-023` | ARREQ014, ARREQ052, ARREQ055 |
| `ANA-024` | ARREQ053 |
| `ANA-025` | ARREQ054, ARREQ108 |
| `ANA-026` | ARREQ097–ARREQ103 |
| `ANA-027` | ARREQ019–ARREQ021 |
| `ANA-028` | ARREQ024, ARREQ070, ARREQ115 |
| `ANA-029` | ARREQ023, ARREQ059, ARREQ096 |
| `ANA-030` | ARREQ060, ARREQ083, ARREQ086 |
| `ANA-031` | ARREQ051 |
| `ANA-032` | ARREQ045–ARREQ047 |
| `ANA-033` | ARREQ044 |
| `ANA-034` | ARREQ004–ARREQ005, ARREQ128 |
| `ANA-035` | ARREQ112 |
| `ANA-036` | ARREQ062–ARREQ064 |
| `ANA-037` | ARREQ055–ARREQ056, ARREQ118 |
| `ANA-038` | ARREQ068–ARREQ070 |

# Team-capability crosswalk

The labels below refer to delivery capabilities in `docs/workstreams/teams/analytics-reporting.md`, not product-rule IDs.

| Team capability | Meaning | P6 requirement coverage |
|---|---|---|
| `team ANA-001` | Source-event/reporting-contract inventory | ARREQ009–ARREQ016 |
| `team ANA-002` | Metric definition registry | ARREQ017–ARREQ024 |
| `team ANA-003` | Analytical projection foundation | ARREQ025–ARREQ032 |
| `team ANA-004` | Projection identity/idempotency | ARREQ026, ARREQ029 |
| `team ANA-005` | Projection ordering/late-event policy | ARREQ027–ARREQ028 |
| `team ANA-006` | Projection rebuild/backfill | ARREQ031–ARREQ032 |
| `team ANA-007` | Account/workspace metrics | ARREQ049, ARREQ079–ARREQ080 |
| `team ANA-008` | WorkManagement analytics | ARREQ033–ARREQ040, ARREQ073 |
| `team ANA-009` | Documents/Collaboration analytics | ARREQ074–ARREQ075 |
| `team ANA-010` | Automation/Integrations analytics | ARREQ076–ARREQ077 |
| `team ANA-011` | Billing/usage analytics | ARREQ078–ARREQ080 |
| `team ANA-012` | Cross-context report composition | ARREQ081–ARREQ088 |
| `team ANA-013` | Report/query API | ARREQ089–ARREQ091 |
| `team ANA-014` | Dashboard/report frontend | ARREQ092–ARREQ096 |
| `team ANA-015` | Filters/date/timezone semantics | ARREQ019–ARREQ023, ARREQ089 |
| `team ANA-016` | Freshness/staleness contract | ARREQ057–ARREQ064 |
| `team ANA-017` | Export | ARREQ097–ARREQ104 |
| `team ANA-018` | Authorization/visibility | ARREQ049–ARREQ056 |
| `team ANA-019` | Retention/data lifecycle | ARREQ065–ARREQ072 |
| `team ANA-020` | Performance/storage strategy | ARREQ105–ARREQ108 |
| `team ANA-021` | Observability | ARREQ109–ARREQ112 |
| `team ANA-022` | Data quality/reconciliation | ARREQ109–ARREQ110 |
| `team ANA-023` | Migration/versioning | ARREQ113–ARREQ120 |
| `team ANA-024` | Hardening | ARREQ121–ARREQ128 |

# TAC / backend architecture-closure reuse

| TAC flow | Meaning | P6 requirements | Current exact-source route | Preparation state |
|---|---|---|---|---|
| `AR-FLOW-01` | Live Work placement projection | ARREQ026–ARREQ029, ARREQ033–ARREQ035, ARREQ038 | BoardItemMoved/Created/Archived V2 → Analytics consumers → WorkspaceWorkItemPlacementService → reporting projection | `IMPLEMENTED_REFERENCE / exact-candidate rerun` |
| `AR-FLOW-02` | Analytics local read | ARREQ030, ARREQ036, ARREQ039 | GetWorkspacePlacementsQuery → IReportingDbContext → local projection only | `IMPLEMENTED_INTERNAL / exact-candidate rerun` |
| `AR-FLOW-03` | Producer-backed rebuild | ARREQ012, ARREQ031–ARREQ032, ARREQ035, ARREQ037 | RebuildWorkspacePlacementsCommand → Analytics port → delegate adapter → WorkManagement public snapshot → reconcile | `IMPLEMENTED_REFERENCE / exact-candidate rerun` |
| `AR-FLOW-04` | Projection failure and recovery | ARREQ029, ARREQ032, ARREQ038, ARREQ124 | projection failure → transaction/claim rollback → Platform redelivery → convergence; source untouched | `IMPLEMENTED_REFERENCE / exact-candidate rerun` |

Rules for TAC evidence reuse:

- `AR-FLOW-01..04` certify the Work placement reference mechanism, not every P6 Metric/report.
- source-status prose from older audit documents is not final candidate evidence.
- changes to Work event V2, producer revision, Analytics port/adapter, reporting persistence, RLS, dedup/transaction ownership or rebuild algorithm invalidate affected flow proof.
- `ReportingSnapshot` does not satisfy the Work placement reference.
- an internal local placement query does not imply report API/frontend readiness.

# Readiness targets

| Capability | Required target |
|---|---|
| Authority/source inventory | D5 |
| Metric semantics | D5 per released Metric |
| Projection foundation | D5 |
| Work placement reference | D5 |
| Source-context projection | D4+ / D5 per source criticality |
| Dashboard/Application/API lifecycle | D5 if released |
| Widget/source contract | D5 |
| Authorization/tenant/privacy | D5 |
| Freshness/completeness | D4+; D5 when interpretation depends on it |
| Snapshot/report artifact | D4+ / D5 if historical reporting relies on it |
| Cross-context composition | D4+ per report after participating sources are D4+ |
| Frontend consumer | D4+ with D5 producer/public contract |
| Export/report job | D4+ per released path |
| Performance/data quality/ops | D4+; critical reconciliation D5 |
| Migration/RLS/compatibility | D5 |
| Architecture/CI/exact-candidate evidence | D5 |

# Canonical handoff schemas

## Source → Analytics

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

Append:

```text
Candidate SHA:
Source posture:
Breaking/additive:
Migration/backfill:
Backfill scope:
Backfill rate limit:
Production-impact guard:
Checkpoint/resume strategy:
Backfill validation:
Evidence locator:
Known debt/blocker:
Invalidation trigger:
Rollback/forward-fix:
```

## Metric handoff

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

Append:

```text
Metric semantic version:
Historical comparability:
Null/zero/unknown behavior:
Source cutoff:
Candidate SHA:
Evidence locator:
Invalidation trigger:
```

## Report handoff

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

Append:

```text
Completeness/cutoff strategy:
Drill-down authorization:
Cache identity:
Candidate SHA:
Evidence locator:
Known debt:
Invalidation trigger:
```

## Scheduled-report delivery extension

Scheduled reports are `DEFERRED / NON_REACHABLE` in the initial P6 slice. Any future admission extends the Report handoff with:

```text
Schedule:
Recipient identity:
Recipient authorization source:
Authorization time:
Delivery owner/channel:
Snapshot/source cutoff:
Retry/idempotency:
Expiration/retention:
Delivery failure visibility:
Tests:
```

Analytics owns report semantics; Collaboration/Platform or another explicitly approved capability owns delivery mechanics.

# Certification invalidation rules

Re-certify affected records after material change to source contracts, Metric semantics, projection identity/order/rebuild, Dashboard/Widget contracts, authorization/privacy, freshness/retention, snapshots, cross-context joins, API/frontend state identity, export security, cache/pre-aggregation identity, migration/backlog compatibility or production runtime ownership.

# Escalation boundary

The team may decide locally: private projection structure, internal query helpers, index/query optimization preserving semantics, Dashboard component composition, test fixtures, backfill implementation preserving the approved contract, and cache implementation preserving all semantic/security dimensions.

Escalate before implementation when closure requires direct private source-table access, a new source business event/semantic change, Analytics mutation of source state, a new cross-tenant product model, global authorization/privacy changes, a new service/data platform/warehouse, new export storage/security architecture, retention/data-residency changes, global realtime/messaging changes, externally committed Metric semantic changes, or synchronous transactional dependency on Analytics.

# Stop conditions

Stop rather than guess when source fact ownership is unclear; Metric meaning depends on UI label/table shape; required facts exist only in forbidden private persistence; a projection cannot be rebuilt/reconciled within promised fidelity; Dashboard visibility broadens source access; a cross-context report overstates consistency; export would bypass authorization; stale/partial data would be presented as exact current truth; migration would silently reinterpret persisted Metric/Widget/Snapshot data; or a new data platform/service is proposed without measured need.

# P6 completion contract

The workstream is document-complete only when PLAN, TESTS and CERTIFICATION bind all `ARREQ001..ARREQ128`.

Implementation is release-complete only for the explicitly named slice where source owner/contract, Metric semantics, projection/query correctness, tenant/privacy authorization, freshness/completeness, Dashboard/report/export/frontend consumers where released, migration/compatibility, observability/recovery and exact-candidate evidence all meet required readiness.

A green Work placement projection is valuable reference evidence. It is **not** permission to call Analytics & Reporting complete.
