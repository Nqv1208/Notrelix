---
document_id: WRK-PLAN-ANALYTICS-REPORTING
document_type: workstream-plan
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
  - migrations
  - certification
evidence:
  - docs/workstreams/executions/analytics-reporting/analytics-reporting.spec.md
  - docs/product/analytics.md
  - docs/workstreams/teams/analytics-reporting.md
  - docs/workstreams/cross-team-dependencies.md
  - docs/workstreams/executions/backend-team-architecture-closure/
review_on:
  - accepted-candidate-sha-change
  - source-contract-readiness-change
  - metric-semantic-change
  - projection-runtime-change
  - dashboard-api-change
  - analytics-authorization-change
  - snapshot-export-change
  - migration-or-rls-change
  - final-certification-change
baseline:
  ref: pull/160-head
  sha: 902bc9c5b39a003df3dce6673edc124984d2b398
---

# PLAN — P6 Analytics & Reporting

## 1. Execution objective

Implement P6 as a **brownfield downstream Analytics capability**, not as a new generic data platform.

The plan converts `ARREQ001..ARREQ128` into 64 deterministic capability work units plus four aggregate gates.

```text
64 work units
= 16 requirement lanes × 4 implementation units

4 aggregate release gates
= semantic/projection
+ dashboard/security/history
+ source/product delivery
+ scale/migration/certification
```

No work unit authorizes a source context, Metric formula, privacy policy or public capability that higher authority has not admitted.

## 2. Non-negotiable implementation decisions

```text
Metric registry
  → code-owned typed/versioned catalog in P6
  → not user-editable CRUD
  → concrete Metric definitions require Product + Source handoff

Projection persistence
  → new Application contracts are framework-neutral repositories/query stores
  → migrate Analytics use cases away from IReportingDbContext DbSet exposure
  → no GenericRepository
  → no second UnitOfWork; retain canonical request/DataSession transaction boundary

Work placement
  → retain producer revision + local-read + producer-owned rebuild
  → AR-FLOW-01..04 are reference mechanism only

Dashboard
  → implement Application/API lifecycle
  → explicit private owner
  → Private + Workspace released
  → Public non-reachable in P6

Widget
  → DashboardWidgetType is canonical P6 persisted/public vocabulary
  → duplicate Widgets.WidgetType is migrated/retired/guarded
  → JSONB remains storage detail behind typed/versioned config

Snapshot
  → relocate ReportingSnapshot out of Domain LegacyGap
  → Application/Infrastructure reporting artifact with Metric/report lineage and cutoff

Source onboarding
  → no private table joins
  → source without D4+ handoff is BLOCKED_BY_SOURCE_CONTRACT

Frontend
  → current Workspace dashboard is not P6 readiness evidence
  → dedicated Analytics feature boundary for released P6 UI

Realtime
  → not required in initial P6
  → authoritative query/refetch
  → future realtime only after public contract + gap recovery

Export
  → per-report admission
  → no globally implied export capability

Data platform/service extraction
  → out of P6
  → relational reporting store remains default until measured evidence justifies change
```

## 2.1. Frozen initial released slice

```text
Metric: work-item-count:v1
Report: work-placement-overview:v1
Source: WorkspaceWorkItemPlacementProjection from WorkManagement placement V2 facts
Dashboard: Private + Workspace
API/frontend: typed Work Placement Overview + Dashboard lifecycle + dedicated Analytics surface
Cross-context report: DEFERRED in initial P6
Export: DEFERRED / NON_REACHABLE
Scheduled report: DEFERRED / NON_REACHABLE
Analytics realtime: NOT REQUIRED / NON_REACHABLE
```

No work unit may substitute a different first Metric/report without synchronized authority review.

## 3. Canonical execution waves

### Wave 0 — Authority and release-scope freeze

**Lanes:** `AR-01`

**Exit:** Freeze exact-source posture and execution/evidence rules before implementation.

### Wave 1 — Source / Metric / Projection foundation

**Lanes:** `AR-02`, `AR-03`, `AR-04`, `AR-05`

**Exit:** Build source handoffs, code-owned Metric semantics, framework-neutral projection stores and recertify Work reference.

### Wave 2 — Dashboard / Security / Freshness / History

**Lanes:** `AR-06`, `AR-07`, `AR-08`, `AR-09`

**Exit:** Make persisted Analytics configuration and historical artifacts safe and queryable.

### Wave 3 — Source onboarding / Reports / Frontend / Export

**Lanes:** `AR-10`, `AR-11`, `AR-12`, `AR-13`

**Exit:** Ship only source/report slices whose product and source handoffs are ready.

### Wave 4 — Scale / Migration / Certification

**Lanes:** `AR-14`, `AR-15`, `AR-16`

**Exit:** Harden measured workload, compatibility, architecture and exact-candidate release evidence.

## 4. Current source-readiness matrix

This matrix is a planning gate, not a Metric definition.

| Source context | Preparation readiness | Current public evidence | P6 execution rule |
|---|---|---|---|
| WorkManagement | `REFERENCE_READY` | BoardItemMoved/Created/ArchivedIntegrationEventV2 + IWorkItemProjectionSource | Work placement reference may be retained; each new Work Metric still needs a Metric handoff. |
| Workspace | `SOURCE_CONTRACT_PARTIAL` | WorkspaceCreated + WorkspaceMemberAdded/Removed integration events | May support approved workspace/member metrics after privacy/semantic handoff; do not infer all workspace state. |
| Documents | `SOURCE_CONTRACT_PARTIAL` | PageCreatedIntegrationEvent + PageArchivedIntegrationEvent | Page lifecycle counts may be admitted only with Documents semantic/authorization handoff; no raw content analytics by default. |
| Collaboration | `SOURCE_CONTRACT_PARTIAL` | CommentCreatedIntegrationEvent + MentionCreatedIntegrationEvent | Only approved collaboration facts may onboard; Activity/Audit semantics remain separate. |
| Automation | `BLOCKED_BY_P5_PUBLIC_FACTS` | Execution Domain lifecycle exists, but P5 public/realtime mapping is not yet a stable Analytics source contract | Do not build execution success-rate projections from private Domain events or execution tables. |
| Integrations | `SOURCE_CONTRACT_INCOMPLETE` | IntegrationConnectionRevokedIntegrationEvent + CalendarWebhookProcessingRequestedV1 | These do not define all terminal provider/sync outcomes needed for health/failure Metrics. |
| Billing | `SOURCE_CONTRACT_PARTIAL` | SubscriptionChanged/Canceled integration events | Subscription trends may be admitted by Billing handoff; billable usage/invoice/entitlement metrics need explicit Billing source contracts. |
| Identity | `RESTRICTED / PRODUCT_HANDOFF_REQUIRED` | Identity lifecycle events exist | Only explicitly approved non-sensitive facts may enter Analytics; no convenience replication of profile/credential data. |

## 5. Test-ID contract for the canonical TESTS file

`analytics-reporting.tests.md` MUST preserve:

```text
AR-TST-REQ-001 .. AR-TST-REQ-128
→ one primary executable scenario per ARREQ

plus all 37 legacy ANA-TST-* IDs
→ retained as compatibility/semantic aliases and mapped to the new primary scenarios
```

PLAN work units route the primary IDs now so PLAN → TESTS traceability is deterministic before TESTS is changed.

# Work units

# Lane AR-01 — Authority, brownfield classification and release governance

**Lane disposition:** `IMPLEMENT_AND_FREEZE`  
**Canonical source anchors:**
- `docs/product/analytics.md`
- `docs/workstreams/teams/analytics-reporting.md`
- `docs/workstreams/cross-team-dependencies.md`
- `analytics-reporting.spec.md`

**Primary implementation targets:**
- `docs/workstreams/executions/analytics-reporting/analytics-reporting.plan.md`
- `analytics-reporting.tests.md`
- `analytics-reporting.certification.md`

**Lane dependencies:**
- `none beyond authority/source baseline`

## AR-01-INV-001 — Freeze authority and exact-source baseline

**Requirements:** `ARREQ001` — Analytics remains derived state; `ARREQ002` — Analytics never hides source mutation

**Execution disposition:** `IMPLEMENT_AND_FREEZE`

### Objective

Close `001–002` as one executable unit: freeze authority and exact-source baseline. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `docs/product/analytics.md`
- `docs/workstreams/teams/analytics-reporting.md`
- `docs/workstreams/cross-team-dependencies.md`
- `analytics-reporting.spec.md`

### Implementation actions

1. Record candidate SHA and re-run source inventory before any implementation batch.
2. Classify every P6 capability using the SPEC posture taxonomy; do not convert source existence into readiness.
3. Capture the two overloaded ANA namespaces and require ARREQ/AR-TST/AR-CERT for execution artifacts.

### Concrete target areas

- `docs/workstreams/executions/analytics-reporting/analytics-reporting.plan.md`
- `analytics-reporting.tests.md`
- `analytics-reporting.certification.md`

### Data / migration impact

No schema change.

### Security / tenant impact

No security weakening; source classification must identify sensitive/cross-tenant surfaces.

### Cross-team dependencies

- `accepted exact source baseline`
- `P6 authority documents`
- `none beyond authority/source baseline`

Primary canonical TEST IDs: `AR-TST-REQ-001`, `AR-TST-REQ-002`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-001` and `AR-TST-REQ-002` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ001` and `ARREQ002` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

A changed candidate or authority document invalidates this inventory.

## AR-01-CORE-001 — Freeze the released P6 execution slice

**Requirements:** `ARREQ003` — Authority precedence is explicit; `ARREQ004` — Brownfield source posture precedes implementation

**Execution disposition:** `IMPLEMENT_AND_FREEZE`

### Objective

Close `003–004` as one executable unit: freeze the released p6 execution slice. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `docs/product/analytics.md`
- `docs/workstreams/teams/analytics-reporting.md`
- `docs/workstreams/cross-team-dependencies.md`
- `analytics-reporting.spec.md`

### Implementation actions

1. Publish the frozen decisions from SPEC as non-optional implementation constraints.
2. Define initial P6 as modular-monolith Analytics with code-owned Metric definitions, user-managed Dashboard config and query/refetch authority.
3. Mark Public Dashboard, realtime Analytics, new warehouse/service and unadmitted source Metrics non-reachable until their explicit gate is satisfied.

### Concrete target areas

- `docs/workstreams/executions/analytics-reporting/analytics-reporting.plan.md`
- `analytics-reporting.tests.md`
- `analytics-reporting.certification.md`

### Data / migration impact

No schema change.

### Security / tenant impact

No feature is advertised before its authorization/source contract exists.

### Cross-team dependencies

- `accepted exact source baseline`
- `P6 authority documents`
- `none beyond authority/source baseline`

Primary canonical TEST IDs: `AR-TST-REQ-003`, `AR-TST-REQ-004`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-003` and `AR-TST-REQ-004` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ003` and `ARREQ004` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

An implementation PR that reopens a frozen decision is BLOCKED.

## AR-01-SEC-001 — Enforce no-overclaim and no-private-table rules

**Requirements:** `ARREQ005` — Product-source gaps remain explicit; `ARREQ006` — Exact-candidate evidence is mandatory

**Execution disposition:** `IMPLEMENT_AND_FREEZE`

### Objective

Close `005–006` as one executable unit: enforce no-overclaim and no-private-table rules. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `docs/product/analytics.md`
- `docs/workstreams/teams/analytics-reporting.md`
- `docs/workstreams/cross-team-dependencies.md`
- `analytics-reporting.spec.md`

### Implementation actions

1. Add/extend architecture documentation/tests so Domain-only, Infrastructure-only and internal reference paths cannot be reported as released API/frontend capabilities.
2. Freeze the rule that Analytics never mutates source state and never invents private-table source contracts.
3. Require stop/escalate on cross-tenant, sensitive-data or foreign-DbContext shortcuts.

### Concrete target areas

- `docs/workstreams/executions/analytics-reporting/analytics-reporting.plan.md`
- `analytics-reporting.tests.md`
- `analytics-reporting.certification.md`

### Data / migration impact

Architecture tests only.

### Security / tenant impact

Protects tenant/privacy boundaries at design time.

### Cross-team dependencies

- `accepted exact source baseline`
- `P6 authority documents`
- `none beyond authority/source baseline`

Primary canonical TEST IDs: `AR-TST-REQ-005`, `AR-TST-REQ-006`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-005` and `AR-TST-REQ-006` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ005` and `ARREQ006` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Any deliberate exception requires architecture authority, not a comment/TODO.

## AR-01-COMPAT-001 — Establish evidence, handoff and invalidation governance

**Requirements:** `ARREQ007` — Consumer-specific readiness replaces one global score; `ARREQ008` — No broad cleanup PR

**Execution disposition:** `IMPLEMENT_AND_FREEZE`

### Objective

Close `007–008` as one executable unit: establish evidence, handoff and invalidation governance. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `docs/product/analytics.md`
- `docs/workstreams/teams/analytics-reporting.md`
- `docs/workstreams/cross-team-dependencies.md`
- `analytics-reporting.spec.md`

### Implementation actions

1. Define exact-candidate evidence schema used by TESTS/CERT.
2. Preserve team-authoritative Source→Analytics, Metric and Report handoff fields.
3. Define invalidation triggers for source contract, Metric, projection, authz, migration, API/frontend and runtime changes.

### Concrete target areas

- `docs/workstreams/executions/analytics-reporting/analytics-reporting.plan.md`
- `analytics-reporting.tests.md`
- `analytics-reporting.certification.md`

### Data / migration impact

Docs/evidence only.

### Security / tenant impact

Security evidence is candidate-bound and cannot be inherited from unrelated green CI.

### Cross-team dependencies

- `accepted exact source baseline`
- `P6 authority documents`
- `none beyond authority/source baseline`

Primary canonical TEST IDs: `AR-TST-REQ-007`, `AR-TST-REQ-008`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-007` and `AR-TST-REQ-008` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ007` and `ARREQ008` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Final gate remains NOT_EVALUATED until non-zero exact-candidate evidence exists.

# Lane AR-02 — Source contracts and source inventory

**Lane disposition:** `IMPLEMENT_SOURCE_GATES`  
**Canonical source anchors:**
- `backend/src/Notrelix.Application/Events/`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkItemProjectionSourceAdapter.cs`
- `backend/src/Notrelix.Infrastructure/CrossContext/Analytics/WorkManagement/WorkItemProjectionSourceAdapter.cs`
- `docs/workstreams/teams/analytics-reporting.md`

**Primary implementation targets:**
- `docs P6 source-handoff ledger`
- `backend/src/Notrelix.Application/Features/Analytics/Sources/ (only minimal descriptors if runtime lookup is needed)`
- `producer-owned Public contracts; never foreign persistence`

**Lane dependencies:**
- `AR-01 complete`

## AR-02-INV-001 — Inventory admitted analytical sources

**Requirements:** `ARREQ009` — Every analytical source has an approved semantic owner; `ARREQ010` — Source handoff records stable identity and scope

**Execution disposition:** `IMPLEMENT_SOURCE_GATES`

### Objective

Close `009–010` as one executable unit: inventory admitted analytical sources. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Application/Events/`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkItemProjectionSourceAdapter.cs`
- `backend/src/Notrelix.Infrastructure/CrossContext/Analytics/WorkManagement/WorkItemProjectionSourceAdapter.cs`
- `docs/workstreams/teams/analytics-reporting.md`

### Implementation actions

1. Build one Source→Analytics handoff for every candidate source fact before creating a Metric/projection.
2. Record owner, logical contract/version, tenant/resource scope, business timestamp, producer revision/ordering, correction/delete and replay/backfill availability.
3. Classify source readiness using the project-specific matrix in this PLAN.

### Concrete target areas

- `docs P6 source-handoff ledger`
- `backend/src/Notrelix.Application/Features/Analytics/Sources/ (only minimal descriptors if runtime lookup is needed)`
- `producer-owned Public contracts; never foreign persistence`

### Data / migration impact

No migration until a projection is admitted.

### Security / tenant impact

Sensitive fields/privacy class are mandatory handoff data.

### Cross-team dependencies

- `source bounded-context owners`
- `Platform event contract/delivery rules`
- `Governance/privacy classification`
- `AR-01 complete`

Primary canonical TEST IDs: `AR-TST-REQ-009`, `AR-TST-REQ-010`.


### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-009` and `AR-TST-REQ-010` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ009` and `ARREQ010` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Missing semantic owner or replay/rebuild source blocks onboarding.

## AR-02-CORE-001 — Normalize source ports and producer-owned adapters

**Requirements:** `ARREQ011` — Analytics does not create private-table source contracts; `ARREQ012` — Source queries are producer-owned public contracts

**Execution disposition:** `IMPLEMENT_SOURCE_GATES`

### Objective

Close `011–012` as one executable unit: normalize source ports and producer-owned adapters. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Application/Events/`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkItemProjectionSourceAdapter.cs`
- `backend/src/Notrelix.Infrastructure/CrossContext/Analytics/WorkManagement/WorkItemProjectionSourceAdapter.cs`
- `docs/workstreams/teams/analytics-reporting.md`

### Implementation actions

1. Use integration events directly when payload is sufficient.
2. When a rebuild/current snapshot is needed, define an Analytics-owned port and Infrastructure delegate adapter to a producer-owned Public Application contract.
3. Do not add source-specific DbContext references to Analytics.
4. Do not create a generic source gateway abstraction unless at least two concrete sources require identical mechanics.

### Concrete target areas

- `docs P6 source-handoff ledger`
- `backend/src/Notrelix.Application/Features/Analytics/Sources/ (only minimal descriptors if runtime lookup is needed)`
- `producer-owned Public contracts; never foreign persistence`

### Data / migration impact

No cross-context FK/cascade.

### Security / tenant impact

Tenant scope must be restored before protected producer query.

### Cross-team dependencies

- `source bounded-context owners`
- `Platform event contract/delivery rules`
- `Governance/privacy classification`
- `AR-01 complete`

Primary canonical TEST IDs: `AR-TST-REQ-011`, `AR-TST-REQ-012`.

Legacy stable TEST IDs to preserve/route: `ANA-TST-SRC-001`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-011` and `AR-TST-REQ-012` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ011` and `ARREQ012` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Private-table shortcut fails architecture gate.

## AR-02-SEC-001 — Propagate source authorization, deletion and privacy semantics

**Requirements:** `ARREQ013` — Source correction and deletion semantics are admitted before projection; `ARREQ014` — Source security classification propagates into Analytics

**Execution disposition:** `IMPLEMENT_SOURCE_GATES`

### Objective

Close `013–014` as one executable unit: propagate source authorization, deletion and privacy semantics. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Application/Events/`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkItemProjectionSourceAdapter.cs`
- `backend/src/Notrelix.Infrastructure/CrossContext/Analytics/WorkManagement/WorkItemProjectionSourceAdapter.cs`
- `docs/workstreams/teams/analytics-reporting.md`

### Implementation actions

1. Record whether aggregate results can be reused across principals or require auth-sensitive partitioning.
2. Define source deletion/anonymization treatment before materialization.
3. Exclude credentials, document rich content and unnecessary PII from analytical payloads by default.

### Concrete target areas

- `docs P6 source-handoff ledger`
- `backend/src/Notrelix.Application/Features/Analytics/Sources/ (only minimal descriptors if runtime lookup is needed)`
- `producer-owned Public contracts; never foreign persistence`

### Data / migration impact

Derived retention policy may require later migration but not source-table cascade.

### Security / tenant impact

Governance remains permission authority; Analytics cannot grant source visibility.

### Cross-team dependencies

- `source bounded-context owners`
- `Platform event contract/delivery rules`
- `Governance/privacy classification`
- `AR-01 complete`

Primary canonical TEST IDs: `AR-TST-REQ-013`, `AR-TST-REQ-014`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-013` and `AR-TST-REQ-014` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ013` and `ARREQ014` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Any new sensitive dimension requires product/security approval.

## AR-02-COMPAT-001 — Freeze source version/backlog/replay compatibility

**Requirements:** `ARREQ015` — Source-event compatibility includes backlog and replay; `ARREQ016` — Reference projection does not define all source semantics

**Execution disposition:** `IMPLEMENT_SOURCE_GATES`

### Objective

Close `015–016` as one executable unit: freeze source version/backlog/replay compatibility. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Application/Events/`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkItemProjectionSourceAdapter.cs`
- `backend/src/Notrelix.Infrastructure/CrossContext/Analytics/WorkManagement/WorkItemProjectionSourceAdapter.cs`
- `docs/workstreams/teams/analytics-reporting.md`

### Implementation actions

1. For each consumed event/reporting contract, document supported versions and retained backlog/replay window.
2. Require producer + Analytics consumer compatibility test before removing old versions.
3. For snapshot/rebuild sources, freeze the payload/version used for deterministic rebuild.

### Concrete target areas

- `docs P6 source-handoff ledger`
- `backend/src/Notrelix.Application/Features/Analytics/Sources/ (only minimal descriptors if runtime lookup is needed)`
- `producer-owned Public contracts; never foreign persistence`

### Data / migration impact

May trigger projection rebuild/migration when producer semantics change.

### Security / tenant impact

Unsupported old messages fail visibly; do not reinterpret as current semantics.

### Cross-team dependencies

- `source bounded-context owners`
- `Platform event contract/delivery rules`
- `Governance/privacy classification`
- `AR-01 complete`

Primary canonical TEST IDs: `AR-TST-REQ-015`, `AR-TST-REQ-016`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-015` and `AR-TST-REQ-016` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ015` and `ARREQ016` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Source breaking change without consumer/backfill plan blocks release.

# Lane AR-03 — Metric semantics and canonical registry

**Lane disposition:** `IMPLEMENT_TYPED_CODE_OWNED_REGISTRY`  
**Canonical source anchors:**
- `docs/product/analytics.md`
- `backend/src/Notrelix.Domain/Analytics/ (no Metrics folder at baseline)`

**Primary implementation targets:**
- `backend/src/Notrelix.Domain/Analytics/Metrics/MetricDefinition.cs`
- `backend/src/Notrelix.Domain/Analytics/Metrics/MetricKey.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Metrics/IMetricCatalog.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricCatalog.cs`

**Lane dependencies:**
- `AR-01 complete`

## AR-03-INV-001 — Define Metric semantic contract

**Requirements:** `ARREQ017` — Metric identity is stable and versioned; `ARREQ018` — Metric definition has one canonical owner

**Execution disposition:** `IMPLEMENT_TYPED_CODE_OWNED_REGISTRY`

### Objective

Close `017–018` as one executable unit: define metric semantic contract. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `docs/product/analytics.md`
- `backend/src/Notrelix.Domain/Analytics/ (no Metrics folder at baseline)`

### Implementation actions

1. Create immutable MetricKey/version/value semantics types; P6 Metric definitions are code-owned, not user-editable CRUD.
2. Require source facts, unit, scope, dimensions, filters, time basis, timezone, freshness and privacy class in every definition.
3. Do not admit a concrete Metric until its Product/Source handoff is complete.

### Concrete target areas

- `backend/src/Notrelix.Domain/Analytics/Metrics/MetricDefinition.cs`
- `backend/src/Notrelix.Domain/Analytics/Metrics/MetricKey.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Metrics/IMetricCatalog.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricCatalog.cs`

### Data / migration impact

No Metric DB table in P6 unless product later requires user-managed Metric definitions.

### Security / tenant impact

Metric definitions carry privacy/authz classification.

### Cross-team dependencies

- `approved Metric handoff from Product + source owner`
- `AR-02 source handoff`
- `AR-01 complete`

Primary canonical TEST IDs: `AR-TST-REQ-017`, `AR-TST-REQ-018`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-MET-001`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-017` and `AR-TST-REQ-018` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ017` and `ARREQ018` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Product-specific formula guessing is a stop condition.

## AR-03-CORE-001 — Implement canonical Metric catalog and deterministic value semantics

**Requirements:** `ARREQ019` — Displayed precision and comparison basis require defined meaning; `ARREQ020` — Rate and percentage metrics define numerator and denominator

**Execution disposition:** `IMPLEMENT_TYPED_CODE_OWNED_REGISTRY`

### Objective

Close `019–020` as one executable unit: implement canonical metric catalog and deterministic value semantics. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `docs/product/analytics.md`
- `backend/src/Notrelix.Domain/Analytics/ (no Metrics folder at baseline)`

### Implementation actions

1. Implement one Application catalog resolving by `(MetricKey, SemanticVersion)` and register the frozen `work-item-count:v1` definition as the first released Metric.
2. Add explicit numerator/denominator/zero-denominator behavior for rates and explicit null/unknown/unavailable states.
3. Expose definitions to report query/application code; implement `work-placement-overview:v1` against `work-item-count:v1`; frontend never becomes formula authority.

### Concrete target areas

- `backend/src/Notrelix.Domain/Analytics/Metrics/MetricDefinition.cs`
- `backend/src/Notrelix.Domain/Analytics/Metrics/MetricKey.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Metrics/IMetricCatalog.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricCatalog.cs`

### Data / migration impact

Pure code contract unless a later persisted registry is separately approved.

### Security / tenant impact

No sensitive Metric can be enumerated without authorization metadata.

### Cross-team dependencies

- `approved Metric handoff from Product + source owner`
- `AR-02 source handoff`
- `AR-01 complete`

Primary canonical TEST IDs: `AR-TST-REQ-019`, `AR-TST-REQ-020`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-MET-002`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-019` and `AR-TST-REQ-020` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ019` and `ARREQ020` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Duplicate semantic key/version is build/test failure.

## AR-03-SEC-001 — Implement time, correction and precision semantics

**Requirements:** `ARREQ021` — Time-based Metrics define calendar and timezone semantics; `ARREQ022` — Metric corrections are explicit

**Execution disposition:** `IMPLEMENT_TYPED_CODE_OWNED_REGISTRY`

### Objective

Close `021–022` as one executable unit: implement time, correction and precision semantics. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `docs/product/analytics.md`
- `backend/src/Notrelix.Domain/Analytics/ (no Metrics folder at baseline)`

### Implementation actions

1. Centralize time-bucket semantics per Metric definition including timezone/DST/boundaries.
2. Define correction/reversal policy and historical bucket behavior.
3. Define rounding/unit/display metadata separately from raw value to avoid client-side semantic drift.

### Concrete target areas

- `backend/src/Notrelix.Domain/Analytics/Metrics/MetricDefinition.cs`
- `backend/src/Notrelix.Domain/Analytics/Metrics/MetricKey.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Metrics/IMetricCatalog.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricCatalog.cs`

### Data / migration impact

No schema migration for catalog itself; affected projections may rebuild.

### Security / tenant impact

Unauthorized/unknown is not zero and must survive API mapping.

### Cross-team dependencies

- `approved Metric handoff from Product + source owner`
- `AR-02 source handoff`
- `AR-01 complete`

Primary canonical TEST IDs: `AR-TST-REQ-021`, `AR-TST-REQ-022`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-MET-TIME-001`, `ANA-TST-PRJ-CORR-001`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-021` and `AR-TST-REQ-022` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ021` and `ARREQ022` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Browser locale/chart defaults cannot change backend Metric values.

## AR-03-COMPAT-001 — Version Metric semantics and historical comparison

**Requirements:** `ARREQ023` — Zero, no data, unknown, unavailable and unauthorized are distinct; `ARREQ024` — Breaking Metric meaning requires historical-comparability policy

**Execution disposition:** `IMPLEMENT_TYPED_CODE_OWNED_REGISTRY`

### Objective

Close `023–024` as one executable unit: version metric semantics and historical comparison. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `docs/product/analytics.md`
- `backend/src/Notrelix.Domain/Analytics/ (no Metrics folder at baseline)`

### Implementation actions

1. Classify semantic changes as implementation fix vs new Metric version.
2. Bind Snapshot/export/cache/report identities to Metric version.
3. Define old/new history comparison or break markers before retiring a version; previous-period comparisons use explicit window boundaries, timezone/calendar basis and compatible Metric versions.

### Concrete target areas

- `backend/src/Notrelix.Domain/Analytics/Metrics/MetricDefinition.cs`
- `backend/src/Notrelix.Domain/Analytics/Metrics/MetricKey.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Metrics/IMetricCatalog.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricCatalog.cs`

### Data / migration impact

Metric version changes may require rebuild/backfill and persisted Dashboard config migration.

### Security / tenant impact

Security class changes invalidate caches and exports.

### Cross-team dependencies

- `approved Metric handoff from Product + source owner`
- `AR-02 source handoff`
- `AR-01 complete`

Primary canonical TEST IDs: `AR-TST-REQ-023`, `AR-TST-REQ-024`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-MET-003`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-023` and `AR-TST-REQ-024` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ023` and `ARREQ024` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Silent formula change under same semantic version is forbidden.

# Lane AR-04 — Projection foundation and framework-neutral persistence ports

**Lane disposition:** `IMPLEMENT_AND_MIGRATE_ANALYTICS_EF_COUPLING`  
**Canonical source anchors:**
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IReportingDbContext.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Projections/WorkItemPlacement/WorkspaceWorkItemPlacementProjection.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/`

**Primary implementation targets:**
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkspaceWorkItemPlacementStore.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IDashboardRepository.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IReportingSnapshotStore.cs`
- `backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/`

**Lane dependencies:**
- `AR-01 complete`
- `AR-02 source contract conventions frozen`

## AR-04-INV-001 — Define projection identity, ordering and store contracts

**Requirements:** `ARREQ025` — Projection names its authoritative source and derived identity; `ARREQ026` — Projection update is idempotent

**Execution disposition:** `IMPLEMENT_AND_MIGRATE_ANALYTICS_EF_COUPLING`

### Objective

Close `025–026` as one executable unit: define projection identity, ordering and store contracts. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IReportingDbContext.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Projections/WorkItemPlacement/WorkspaceWorkItemPlacementProjection.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/`

### Implementation actions

1. For each projection define key, source version/revision, update/reconcile semantics and rebuild authority.
2. Introduce feature-owned repository/query-store contracts with domain/application DTOs only; no `DbSet<T>`/EF types in new contracts.
3. Inventory all current `IReportingDbContext` call sites and migration order.

### Concrete target areas

- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkspaceWorkItemPlacementStore.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IDashboardRepository.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IReportingSnapshotStore.cs`
- `backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/`

### Data / migration impact

No schema change yet.

### Security / tenant impact

Store methods require explicit tenant/workspace inputs where scope is not ambient.

### Cross-team dependencies

- `canonical request transaction/DataSession mechanism`
- `Platform messaging/dedup D5 for async projections`
- `AR-01 complete`
- `AR-02 source contract conventions frozen`

Primary canonical TEST IDs: `AR-TST-REQ-025`, `AR-TST-REQ-026`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-PRJ-IDEMP-001`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-025` and `AR-TST-REQ-026` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ025` and `ARREQ026` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Do not create a generic repository or second UnitOfWork.

## AR-04-CORE-001 — Migrate Analytics use cases off IReportingDbContext

**Requirements:** `ARREQ027` — Ordering is the smallest semantics-required scope; `ARREQ028` — Late and out-of-order facts cannot regress projection state

**Execution disposition:** `IMPLEMENT_AND_MIGRATE_ANALYTICS_EF_COUPLING`

### Objective

Close `027–028` as one executable unit: migrate analytics use cases off ireportingdbcontext. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IReportingDbContext.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Projections/WorkItemPlacement/WorkspaceWorkItemPlacementProjection.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/`

### Implementation actions

1. Move placement query/service persistence behind `IWorkspaceWorkItemPlacementStore`.
2. Move Dashboard persistence behind `IDashboardRepository`; snapshot persistence behind a snapshot store in AR-09.
3. Implement Infrastructure EF repositories/stores under the existing canonical transaction/DataSession boundary.
4. Delete `IReportingDbContext` after all Analytics references are migrated; remove Application EF dependency only if repo-wide usage allows it in the responsible architecture workstream.

### Concrete target areas

- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkspaceWorkItemPlacementStore.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IDashboardRepository.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IReportingSnapshotStore.cs`
- `backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/`

### Data / migration impact

Database tables stay owned by reporting schema; no table rename required solely for abstraction migration.

### Security / tenant impact

RLS/session context continues to apply in Infrastructure.

### Cross-team dependencies

- `canonical request transaction/DataSession mechanism`
- `Platform messaging/dedup D5 for async projections`
- `AR-01 complete`
- `AR-02 source contract conventions frozen`

Primary canonical TEST IDs: `AR-TST-REQ-027`, `AR-TST-REQ-028`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-PRJ-ORDER-001`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-027` and `AR-TST-REQ-028` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ027` and `ARREQ028` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Application tests must compile without mocking DbSet/IQueryable for new P6 code.

## AR-04-SEC-001 — Prove idempotent apply, ordering and checkpoint settlement

**Requirements:** `ARREQ029` — Projection failure commits no false checkpoint; `ARREQ030` — Projection normal reads are local

**Execution disposition:** `IMPLEMENT_AND_MIGRATE_ANALYTICS_EF_COUPLING`

### Objective

Close `029–030` as one executable unit: prove idempotent apply, ordering and checkpoint settlement. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IReportingDbContext.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Projections/WorkItemPlacement/WorkspaceWorkItemPlacementProjection.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/`

### Implementation actions

1. Projection apply must distinguish duplicate, stale and distinct occurrences.
2. Commit derived state before consumer settlement/checkpoint; crash-before-commit must be redeliverable.
3. Ordering scope is per source semantics, never a global timestamp shortcut.

### Concrete target areas

- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkspaceWorkItemPlacementStore.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IDashboardRepository.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IReportingSnapshotStore.cs`
- `backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/`

### Data / migration impact

Indexes/unique constraints may be added where logical projection identity needs physical protection.

### Security / tenant impact

Cross-tenant keys are invalid; account/workspace scope cannot be inferred from untrusted payload.

### Cross-team dependencies

- `canonical request transaction/DataSession mechanism`
- `Platform messaging/dedup D5 for async projections`
- `AR-01 complete`
- `AR-02 source contract conventions frozen`

Primary canonical TEST IDs: `AR-TST-REQ-029`, `AR-TST-REQ-030`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-PRJ-RESTART-001`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-029` and `AR-TST-REQ-030` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ029` and `ARREQ030` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Failure must not advance checkpoint or poison unrelated consumers.

## AR-04-COMPAT-001 — Build rebuild/backfill foundation without a generic analytics engine

**Requirements:** `ARREQ031` — Projection rebuild and backfill are operationally bounded; `ARREQ032` — Backfill and live traffic converge without double counting

**Execution disposition:** `IMPLEMENT_AND_MIGRATE_ANALYTICS_EF_COUPLING`

### Objective

Close `031–032` as one executable unit: build rebuild/backfill foundation without a generic analytics engine. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IReportingDbContext.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Projections/WorkItemPlacement/WorkspaceWorkItemPlacementProjection.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/`

### Implementation actions

1. Define reusable conventions for Account/Workspace/resource/date scope, approved source, rate limit, production-impact guard, durable checkpoint, resumability, duplicate handling and reconciliation evidence; avoid a speculative framework.
2. Require live/backfill overlap strategy and restart-from-checkpoint proof before every source onboarding.
3. Validate final rebuilt state against an approved producer snapshot/invariant and record fidelity limits when retention cannot reproduce older results.

### Concrete target areas

- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkspaceWorkItemPlacementStore.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IDashboardRepository.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IReportingSnapshotStore.cs`
- `backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/`

### Data / migration impact

Projection schema change chooses migrate/rebuild/cutover explicitly.

### Security / tenant impact

Backfill always restores tenant/RLS context.

### Cross-team dependencies

- `canonical request transaction/DataSession mechanism`
- `Platform messaging/dedup D5 for async projections`
- `AR-01 complete`
- `AR-02 source contract conventions frozen`

Primary canonical TEST IDs: `AR-TST-REQ-031`, `AR-TST-REQ-032`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-PRJ-REPLAY-001`, `ANA-TST-PRJ-BF-001`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-031` and `AR-TST-REQ-032` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ031` and `ARREQ032` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

A projection with no repair path cannot be D5.

# Lane AR-05 — WorkManagement placement reference projection

**Lane disposition:** `RETAIN_AND_RECERTIFY_REFERENCE`  
**Canonical source anchors:**
- `backend/src/Notrelix.Application/Features/Analytics/Projections/WorkItemPlacement/WorkspaceWorkItemPlacementProjection.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Placements/Services/WorkspaceWorkItemPlacementService.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Analytics/WorkItemPlacementConsumers.cs`
- `backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/BoardItemMovedPlacementFailureRecoveryRuntimeTests.cs`

**Primary implementation targets:**
- `same production paths after AR-04 store migration`
- `TAC AR-FLOW-01..04 exact-candidate evidence`

**Lane dependencies:**
- `AR-04 store migration design frozen`
- `WorkManagement source V2 contract available`

## AR-05-INV-001 — Re-baseline AR-FLOW-01 and AR-FLOW-02

**Requirements:** `ARREQ033` — Work placement reference keeps producer revision authority; `ARREQ034` — Work placement live consumers use event facts when sufficient

**Execution disposition:** `RETAIN_AND_RECERTIFY_REFERENCE`

### Objective

Close `033–034` as one executable unit: re-baseline ar-flow-01 and ar-flow-02. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Application/Features/Analytics/Projections/WorkItemPlacement/WorkspaceWorkItemPlacementProjection.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Placements/Services/WorkspaceWorkItemPlacementService.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Analytics/WorkItemPlacementConsumers.cs`
- `backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/BoardItemMovedPlacementFailureRecoveryRuntimeTests.cs`

### Implementation actions

1. Verify V2 event payload fields, producer revision and registered production consumers on accepted candidate.
2. Verify local query reads only reporting projection through the new framework-neutral query/store contract after AR-04 migration.
3. Record exact source and migration head.

### Concrete target areas

- `same production paths after AR-04 store migration`
- `TAC AR-FLOW-01..04 exact-candidate evidence`

### Data / migration impact

No new business schema required.

### Security / tenant impact

Verify Account/Workspace scope and RLS for query/consumer.

### Cross-team dependencies

- `Work V2 event contracts`
- `IWorkItemProjectionSource`
- `Platform MassTransit/dedup/tenant runtime`
- `AR-04 store migration design frozen`
- `WorkManagement source V2 contract available`

Primary canonical TEST IDs: `AR-TST-REQ-033`, `AR-TST-REQ-034`.


### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-033` and `AR-TST-REQ-034` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ033` and `ARREQ034` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Historical TAC status cannot substitute for rerun after store/runtime change.

## AR-05-CORE-001 — Retain producer-revision apply and local-read behavior

**Requirements:** `ARREQ035` — Work placement incomplete events use the producer public source; `ARREQ036` — Work placement local query remains Analytics-owned

**Execution disposition:** `RETAIN_AND_RECERTIFY_REFERENCE`

### Objective

Close `035–036` as one executable unit: retain producer-revision apply and local-read behavior. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Application/Features/Analytics/Projections/WorkItemPlacement/WorkspaceWorkItemPlacementProjection.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Placements/Services/WorkspaceWorkItemPlacementService.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Analytics/WorkItemPlacementConsumers.cs`
- `backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/BoardItemMovedPlacementFailureRecoveryRuntimeTests.cs`

### Implementation actions

1. Preserve strictly-newer live apply and equal-revision rebuild repair semantics.
2. Moved/archived consumers use event data when sufficient; created consumer uses producer public snapshot only because GroupId is absent.
3. Do not add report/API semantics directly to the projection model.

### Concrete target areas

- `same production paths after AR-04 store migration`
- `TAC AR-FLOW-01..04 exact-candidate evidence`

### Data / migration impact

Keep `(WorkspaceId, ItemId)` unique identity and xmin/concurrency protection unless a measured replacement is proven.

### Security / tenant impact

No foreign Work DbContext access.

### Cross-team dependencies

- `Work V2 event contracts`
- `IWorkItemProjectionSource`
- `Platform MassTransit/dedup/tenant runtime`
- `AR-04 store migration design frozen`
- `WorkManagement source V2 contract available`

Primary canonical TEST IDs: `AR-TST-REQ-035`, `AR-TST-REQ-036`.


### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-035` and `AR-TST-REQ-036` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ035` and `ARREQ036` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

SourceRevision remains semantic ordering authority.

## AR-05-SEC-001 — Re-prove AR-FLOW-03 rebuild safety

**Requirements:** `ARREQ037` — Work placement rebuild preserves newer live facts; `ARREQ038` — Work placement crash recovery remains atomic

**Execution disposition:** `RETAIN_AND_RECERTIFY_REFERENCE`

### Objective

Close `037–038` as one executable unit: re-prove ar-flow-03 rebuild safety. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Application/Features/Analytics/Projections/WorkItemPlacement/WorkspaceWorkItemPlacementProjection.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Placements/Services/WorkspaceWorkItemPlacementService.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Analytics/WorkItemPlacementConsumers.cs`
- `backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/BoardItemMovedPlacementFailureRecoveryRuntimeTests.cs`

### Implementation actions

1. Run empty/multiple/moved/archived/drift/cross-workspace rebuild cases through producer-substitution path.
2. Prove absent snapshot rows are revalidated before delete and newer live rows survive old snapshots.
3. Prove source outage fails without destructive local convergence.

### Concrete target areas

- `same production paths after AR-04 store migration`
- `TAC AR-FLOW-01..04 exact-candidate evidence`

### Data / migration impact

Any changed data migration requires clean/upgrade proof.

### Security / tenant impact

Rebuild command requires current authorization and tenant scope.

### Cross-team dependencies

- `Work V2 event contracts`
- `IWorkItemProjectionSource`
- `Platform MassTransit/dedup/tenant runtime`
- `AR-04 store migration design frozen`
- `WorkManagement source V2 contract available`

Primary canonical TEST IDs: `AR-TST-REQ-037`, `AR-TST-REQ-038`.


### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-037` and `AR-TST-REQ-038` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ037` and `ARREQ038` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

No admin/system shortcut without explicit trusted execution context.

## AR-05-COMPAT-001 — Re-prove AR-FLOW-04 failure and recovery

**Requirements:** `ARREQ039` — Reference projection is internally consumable but not falsely exposed as a product report; `ARREQ040` — Reference projection exact-candidate proof is reused correctly

**Execution disposition:** `RETAIN_AND_RECERTIFY_REFERENCE`

### Objective

Close `039–040` as one executable unit: re-prove ar-flow-04 failure and recovery. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Application/Features/Analytics/Projections/WorkItemPlacement/WorkspaceWorkItemPlacementProjection.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Placements/Services/WorkspaceWorkItemPlacementService.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Analytics/WorkItemPlacementConsumers.cs`
- `backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/BoardItemMovedPlacementFailureRecoveryRuntimeTests.cs`

### Implementation actions

1. Run real message chain with persistence failure before commit and verify rollback of effect + dedup settlement then successful redelivery.
2. Verify old V1 backlog/version compatibility according to producer support window.
3. Record final reference readiness separately from Metric/report readiness.

### Concrete target areas

- `same production paths after AR-04 store migration`
- `TAC AR-FLOW-01..04 exact-candidate evidence`

### Data / migration impact

No projection checkpoint/dedup migration may reset identity silently.

### Security / tenant impact

Crash/retry must not duplicate cross-tenant rows.

### Cross-team dependencies

- `Work V2 event contracts`
- `IWorkItemProjectionSource`
- `Platform MassTransit/dedup/tenant runtime`
- `AR-04 store migration design frozen`
- `WorkManagement source V2 contract available`

Primary canonical TEST IDs: `AR-TST-REQ-039`, `AR-TST-REQ-040`.


### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-039` and `AR-TST-REQ-040` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ039` and `ARREQ040` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

D5 reference proof does not certify other source projections.

# Lane AR-06 — Dashboard, DashboardSource and Widget lifecycle

**Lane disposition:** `IMPLEMENT_APPLICATION_API_AND_HARDEN_DOMAIN`  
**Canonical source anchors:**
- `backend/src/Notrelix.Domain/Analytics/Dashboards/`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/DashboardConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/DashboardSourceConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/DashboardWidgetConfiguration.cs`

**Primary implementation targets:**
- `backend/src/Notrelix.Application/Features/Analytics/Dashboards/`
- `backend/src/Notrelix.API/Endpoints/Analytics/Dashboards/`
- `Dashboard owner/config schema migrations`

**Lane dependencies:**
- `AR-GATE-001 passed for affected foundation`
- `AR-03 work-item-count:v1 + work-placement-overview:v1 identity available`

## AR-06-INV-001 — Harden Dashboard ownership and canonical lifecycle

**Requirements:** `ARREQ041` — Dashboard is user-managed Analytics configuration; `ARREQ042` — Private Dashboard has explicit owner

**Execution disposition:** `IMPLEMENT_APPLICATION_API_AND_HARDEN_DOMAIN`

### Objective

Close `041–042` as one executable unit: harden dashboard ownership and canonical lifecycle. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Domain/Analytics/Dashboards/`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/DashboardConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/DashboardSourceConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/DashboardWidgetConfiguration.cs`

### Implementation actions

1. Add explicit private owner identity to Dashboard instead of using audit CreatedBy as authorization owner.
2. Freeze release visibility to Private + Workspace; reject Public in create/update API and architecture reachability until a separate share/public contract exists.
3. Define archive/delete/restore policy and retained child configuration behavior.

### Concrete target areas

- `backend/src/Notrelix.Application/Features/Analytics/Dashboards/`
- `backend/src/Notrelix.API/Endpoints/Analytics/Dashboards/`
- `Dashboard owner/config schema migrations`

### Data / migration impact

Migration adds owner column/backfill from trusted historical creator where valid; unresolved rows require explicit migration policy.

### Security / tenant impact

Private ownership and workspace scope are authorization inputs, not frontend-only filters.

### Cross-team dependencies

- `AR-03 Metric catalog`
- `AR-04 repositories`
- `Governance pipeline`
- `AR-GATE-001 passed for affected foundation`
- `AR-03 work-item-count:v1 + work-placement-overview:v1 identity available`

Primary canonical TEST IDs: `AR-TST-REQ-041`, `AR-TST-REQ-042`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-041` and `AR-TST-REQ-042` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ041` and `ARREQ042` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Public enum may remain for compatibility but is non-reachable.

## AR-06-CORE-001 — Implement Dashboard Application lifecycle

**Requirements:** `ARREQ043` — Released Dashboard visibility is Governance-backed; `ARREQ044` — Dashboard archive/delete is non-destructive

**Execution disposition:** `IMPLEMENT_APPLICATION_API_AND_HARDEN_DOMAIN`

### Objective

Close `043–044` as one executable unit: implement dashboard application lifecycle. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Domain/Analytics/Dashboards/`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/DashboardConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/DashboardSourceConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/DashboardWidgetConfiguration.cs`

### Implementation actions

1. Create commands/queries for create/list/detail/rename/change visibility/archive and required delete/restore semantics.
2. Create source/widget commands for add/update/remove/move with optimistic concurrency/version checks.
3. Use `IDashboardRepository` and canonical transaction pipeline; no DbSet in handlers.

### Concrete target areas

- `backend/src/Notrelix.Application/Features/Analytics/Dashboards/`
- `backend/src/Notrelix.API/Endpoints/Analytics/Dashboards/`
- `Dashboard owner/config schema migrations`

### Data / migration impact

May add aggregate version/concurrency mapping if not already physically enforced.

### Security / tenant impact

Every command/query implements the canonical auth/request markers.

### Cross-team dependencies

- `AR-03 Metric catalog`
- `AR-04 repositories`
- `Governance pipeline`
- `AR-GATE-001 passed for affected foundation`
- `AR-03 work-item-count:v1 + work-placement-overview:v1 identity available`

Primary canonical TEST IDs: `AR-TST-REQ-043`, `AR-TST-REQ-044`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-043` and `AR-TST-REQ-044` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ043` and `ARREQ044` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

No Dashboard operation mutates source context data.

## AR-06-SEC-001 — Normalize source and widget vocabularies

**Requirements:** `ARREQ045` — Dashboard Source references stable analytical/source contracts; `ARREQ046` — DashboardWidgetType is the canonical P6 widget-kind vocabulary

**Execution disposition:** `IMPLEMENT_APPLICATION_API_AND_HARDEN_DOMAIN`

### Objective

Close `045–046` as one executable unit: normalize source and widget vocabularies. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Domain/Analytics/Dashboards/`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/DashboardConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/DashboardSourceConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/DashboardWidgetConfiguration.cs`

### Implementation actions

1. Use `DashboardWidgetType` as the only persisted/public P6 widget-kind enum.
2. Migrate/retire/guard standalone `Widgets.WidgetType` so it cannot become a second public vocabulary.
3. Keep DashboardSource `Search`/`External` non-reachable until source contracts are admitted; map released sources to stable Metric/report/source IDs.

### Concrete target areas

- `backend/src/Notrelix.Application/Features/Analytics/Dashboards/`
- `backend/src/Notrelix.API/Endpoints/Analytics/Dashboards/`
- `Dashboard owner/config schema migrations`

### Data / migration impact

Persisted discriminator migrations require old-value readers before removal.

### Security / tenant impact

Source config cannot embed unauthorized foreign resource data.

### Cross-team dependencies

- `AR-03 Metric catalog`
- `AR-04 repositories`
- `Governance pipeline`
- `AR-GATE-001 passed for affected foundation`
- `AR-03 work-item-count:v1 + work-placement-overview:v1 identity available`

Primary canonical TEST IDs: `AR-TST-REQ-045`, `AR-TST-REQ-046`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-045` and `AR-TST-REQ-046` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ045` and `ARREQ046` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Unknown discriminator fails explicitly, never falls through to arbitrary JSON.

## AR-06-COMPAT-001 — Replace JSON-shape-only validation with typed versioned config

**Requirements:** `ARREQ047` — Widget configuration is typed and versioned by widget kind; `ARREQ048` — Widget layout never changes source ordering

**Execution disposition:** `IMPLEMENT_APPLICATION_API_AND_HARDEN_DOMAIN`

### Objective

Close `047–048` as one executable unit: replace json-shape-only validation with typed versioned config. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Domain/Analytics/Dashboards/`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/DashboardConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/DashboardSourceConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/DashboardWidgetConfiguration.cs`

### Implementation actions

1. Create per-kind config DTO/value contracts carrying schema version and Metric/source identity.
2. Retain JSONB persistence only as serialization detail behind typed readers/writers.
3. Migrate existing valid configs or mark invalid legacy configs explicitly; no silent reinterpretation.

### Concrete target areas

- `backend/src/Notrelix.Application/Features/Analytics/Dashboards/`
- `backend/src/Notrelix.API/Endpoints/Analytics/Dashboards/`
- `Dashboard owner/config schema migrations`

### Data / migration impact

Config schema migration required for stored dashboards; add compatibility readers first.

### Security / tenant impact

Config validation includes source authorization compatibility, not only GUID syntax.

### Cross-team dependencies

- `AR-03 Metric catalog`
- `AR-04 repositories`
- `Governance pipeline`
- `AR-GATE-001 passed for affected foundation`
- `AR-03 work-item-count:v1 + work-placement-overview:v1 identity available`

Primary canonical TEST IDs: `AR-TST-REQ-047`, `AR-TST-REQ-048`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-047` and `AR-TST-REQ-048` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ047` and `ARREQ048` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Layout changes remain Analytics-only and source-order neutral.

# Lane AR-07 — Authorization, tenancy, privacy and cache isolation

**Lane disposition:** `IMPLEMENT_AND_PROVE_D5`  
**Canonical source anchors:**
- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql`
- `backend/src/Notrelix.Domain/Governance/Permissions/PermissionAction.cs`
- `canonical Application pipeline/ResourceRef rules`

**Primary implementation targets:**
- `Analytics ResourceRef policy mappings`
- `report/Dashboard authorization services/queries`
- `privacy-aware cache/query tests`

**Lane dependencies:**
- `AR-GATE-001 passed for affected foundation`
- `AR-03 work-item-count:v1 + work-placement-overview:v1 identity available`

## AR-07-INV-001 — Define Analytics resource/action matrix without speculative global permissions

**Requirements:** `ARREQ049` — Analytics request scope is explicit Account and Workspace; `ARREQ050` — Dashboard visibility does not grant source permission

**Execution disposition:** `IMPLEMENT_AND_PROVE_D5`

### Objective

Close `049–050` as one executable unit: define analytics resource/action matrix without speculative global permissions. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql`
- `backend/src/Notrelix.Domain/Governance/Permissions/PermissionAction.cs`
- `canonical Application pipeline/ResourceRef rules`

### Implementation actions

1. Map Dashboard/report/export/rebuild operations onto existing Governance actions where semantically valid; do not add PermissionAction values merely for symmetry.
2. Define ResourceRef kinds such as existing `analytics.placement` and Dashboard/report identities with Account/Workspace parent scope.
3. Document private-owner override/intersection rules separately from workspace permission.

### Concrete target areas

- `Analytics ResourceRef policy mappings`
- `report/Dashboard authorization services/queries`
- `privacy-aware cache/query tests`

### Data / migration impact

No schema unless Dashboard owner/privacy metadata needs persistence.

### Security / tenant impact

Authorization runs before data exposure; RLS is defense in depth.

### Cross-team dependencies

- `Governance authority`
- `RLS/DataSession`
- `AR-06 Dashboard owner/visibility`
- `AR-GATE-001 passed for affected foundation`
- `AR-03 work-item-count:v1 + work-placement-overview:v1 identity available`

Primary canonical TEST IDs: `AR-TST-REQ-049`, `AR-TST-REQ-050`.


### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-049` and `AR-TST-REQ-050` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ049` and `ARREQ050` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

If existing actions cannot represent required policy, escalate to Governance rather than bypass.

## AR-07-CORE-001 — Enforce aggregate and drill-down authorization separately

**Requirements:** `ARREQ051` — Aggregate visibility does not imply drill-down visibility; `ARREQ052` — Aggregation does not erase confidentiality

**Execution disposition:** `IMPLEMENT_AND_PROVE_D5`

### Objective

Close `051–052` as one executable unit: enforce aggregate and drill-down authorization separately. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql`
- `backend/src/Notrelix.Domain/Governance/Permissions/PermissionAction.cs`
- `canonical Application pipeline/ResourceRef rules`

### Implementation actions

1. Report/Dashboard query returns only permitted aggregate scope.
2. Drill-down invokes the source/report authorization contract again; aggregate visibility never grants row visibility.
3. Workspace Dashboard visibility does not bypass private Boards/Pages or sensitive Billing/Identity dimensions.

### Concrete target areas

- `Analytics ResourceRef policy mappings`
- `report/Dashboard authorization services/queries`
- `privacy-aware cache/query tests`

### Data / migration impact

Query-store methods accept authorization-constrained parameters/approved reusable aggregate boundaries.

### Security / tenant impact

Cross-tenant data is never fetched then filtered in browser.

### Cross-team dependencies

- `Governance authority`
- `RLS/DataSession`
- `AR-06 Dashboard owner/visibility`
- `AR-GATE-001 passed for affected foundation`
- `AR-03 work-item-count:v1 + work-placement-overview:v1 identity available`

Primary canonical TEST IDs: `AR-TST-REQ-051`, `AR-TST-REQ-052`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-SEC-002`, `ANA-TST-PRIV-001`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-051` and `AR-TST-REQ-052` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ051` and `ARREQ052` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Forbidden and unavailable remain distinguishable without leaking resource existence.

## AR-07-SEC-001 — Implement privacy and cross-tenant isolation

**Requirements:** `ARREQ053` — Cross-tenant analytics is privileged and explicit; `ARREQ054` — Authorization-sensitive caching is partitioned

**Execution disposition:** `IMPLEMENT_AND_PROVE_D5`

### Objective

Close `053–054` as one executable unit: implement privacy and cross-tenant isolation. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql`
- `backend/src/Notrelix.Domain/Governance/Permissions/PermissionAction.cs`
- `canonical Application pipeline/ResourceRef rules`

### Implementation actions

1. Add low-cardinality/sensitive-dimension suppression hooks where Product/Security handoff requires them.
2. Deny ordinary cross-account/cross-workspace queries; global/admin analytics stays separate/not released without explicit capability.
3. Ensure caches/materialized views preserve tenant and privacy partition.

### Concrete target areas

- `Analytics ResourceRef policy mappings`
- `report/Dashboard authorization services/queries`
- `privacy-aware cache/query tests`

### Data / migration impact

Potential privacy metadata migration belongs to Metric/report definitions.

### Security / tenant impact

PII minimization is default; exports inherit same policy.

### Cross-team dependencies

- `Governance authority`
- `RLS/DataSession`
- `AR-06 Dashboard owner/visibility`
- `AR-GATE-001 passed for affected foundation`
- `AR-03 work-item-count:v1 + work-placement-overview:v1 identity available`

Primary canonical TEST IDs: `AR-TST-REQ-053`, `AR-TST-REQ-054`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-SEC-001`, `ANA-TST-SEC-004`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-053` and `AR-TST-REQ-054` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ053` and `ARREQ054` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Cross-tenant leakage is a release-blocking security failure.

## AR-07-COMPAT-001 — Prove RLS plus application authorization and cache partition

**Requirements:** `ARREQ055` — Derived data inherits security and data-location obligations; `ARREQ056` — RLS is defense in depth, not the sole report authorization policy

**Execution disposition:** `IMPLEMENT_AND_PROVE_D5`

### Objective

Close `055–056` as one executable unit: prove rls plus application authorization and cache partition. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql`
- `backend/src/Notrelix.Domain/Governance/Permissions/PermissionAction.cs`
- `canonical Application pipeline/ResourceRef rules`

### Implementation actions

1. Run exact-candidate RLS tests for dashboards/widgets/sources/snapshots/projections.
2. Prove worker/rebuild tenant session setup and account/workspace switch isolation.
3. Invalidate caches when authorization class, Metric version or source visibility semantics change.

### Concrete target areas

- `Analytics ResourceRef policy mappings`
- `report/Dashboard authorization services/queries`
- `privacy-aware cache/query tests`

### Data / migration impact

RLS policy deploy ordering covered in AR-15.

### Security / tenant impact

No security claim may rely solely on UI filtering.

### Cross-team dependencies

- `Governance authority`
- `RLS/DataSession`
- `AR-06 Dashboard owner/visibility`
- `AR-GATE-001 passed for affected foundation`
- `AR-03 work-item-count:v1 + work-placement-overview:v1 identity available`

Primary canonical TEST IDs: `AR-TST-REQ-055`, `AR-TST-REQ-056`.


### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-055` and `AR-TST-REQ-056` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ055` and `ARREQ056` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

D5 requires negative-path production composition.

# Lane AR-08 — Freshness, completeness and optional realtime

**Lane disposition:** `IMPLEMENT_FRESHNESS_DEFER_REALTIME`  
**Canonical source anchors:**
- `docs/product/analytics.md ANA-014/030/036`
- `WorkspaceWorkItemPlacementProjection.SourceRevision/LastOccurredAt`

**Primary implementation targets:**
- `Analytics report response freshness metadata`
- `frontend stale/partial state model`
- `no realtime producer in initial slice unless separately admitted`

**Lane dependencies:**
- `AR-GATE-001 passed for affected foundation`
- `AR-03 work-item-count:v1 + work-placement-overview:v1 identity available`

## AR-08-INV-001 — Define freshness classes and source-cutoff vocabulary

**Requirements:** `ARREQ057` — Every released Metric/report has a freshness class; `ARREQ058` — Freshness metadata is queryable

**Execution disposition:** `IMPLEMENT_FRESHNESS_DEFER_REALTIME`

### Objective

Close `057–058` as one executable unit: define freshness classes and source-cutoff vocabulary. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `docs/product/analytics.md ANA-014/030/036`
- `WorkspaceWorkItemPlacementProjection.SourceRevision/LastOccurredAt`

### Implementation actions

1. Each Metric/report declares freshness class and accepted lag semantics.
2. Standardize `dataThrough/sourceCutoff`, generated/last-updated and degraded/partial metadata at Application contract level.
3. Define which value is semantic source time vs projection processing time.

### Concrete target areas

- `Analytics report response freshness metadata`
- `frontend stale/partial state model`
- `no realtime producer in initial slice unless separately admitted`

### Data / migration impact

May add projection watermark columns only where current state lacks required evidence.

### Security / tenant impact

Freshness metadata must not leak hidden source identity.

### Cross-team dependencies

- `Metric definitions`
- `projection/source cutoffs`
- `Platform realtime only if later admitted`
- `AR-GATE-001 passed for affected foundation`
- `AR-03 work-item-count:v1 + work-placement-overview:v1 identity available`

Primary canonical TEST IDs: `AR-TST-REQ-057`, `AR-TST-REQ-058`.


### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-057` and `AR-TST-REQ-058` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ057` and `ARREQ058` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Calling an async projection live without target evidence is forbidden.

## AR-08-CORE-001 — Implement stale, partial and unavailable query semantics

**Requirements:** `ARREQ059` — Stale, unavailable and not-yet-projected are distinct; `ARREQ060` — Multi-source reports declare completeness and cutoff strategy

**Execution disposition:** `IMPLEMENT_FRESHNESS_DEFER_REALTIME`

### Objective

Close `059–060` as one executable unit: implement stale, partial and unavailable query semantics. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `docs/product/analytics.md ANA-014/030/036`
- `WorkspaceWorkItemPlacementProjection.SourceRevision/LastOccurredAt`

### Implementation actions

1. Report application responses distinguish fresh/stale/partial/unavailable/not-yet-projected/unauthorized.
2. Multi-source report computes a declared common cutoff or returns per-source completeness metadata.
3. Frontend/API mapping preserves these states without zero coercion.

### Concrete target areas

- `Analytics report response freshness metadata`
- `frontend stale/partial state model`
- `no realtime producer in initial slice unless separately admitted`

### Data / migration impact

No mandatory DB migration if values derive from projection metadata; otherwise add explicit watermarks.

### Security / tenant impact

Do not reveal which hidden source caused forbidden state.

### Cross-team dependencies

- `Metric definitions`
- `projection/source cutoffs`
- `Platform realtime only if later admitted`
- `AR-GATE-001 passed for affected foundation`
- `AR-03 work-item-count:v1 + work-placement-overview:v1 identity available`

Primary canonical TEST IDs: `AR-TST-REQ-059`, `AR-TST-REQ-060`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-FRESH-001`, `ANA-TST-FRESH-002`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-059` and `AR-TST-REQ-060` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ059` and `ARREQ060` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Transactional source operations remain independent of Analytics degradation.

## AR-08-SEC-001 — Keep initial P6 query/refetch authoritative

**Requirements:** `ARREQ061` — Analytics failure does not block unrelated transactions; `ARREQ062` — Realtime is freshness, not Analytics authority

**Execution disposition:** `IMPLEMENT_FRESHNESS_DEFER_REALTIME`

### Objective

Close `061–062` as one executable unit: keep initial p6 query/refetch authoritative. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `docs/product/analytics.md ANA-014/030/036`
- `WorkspaceWorkItemPlacementProjection.SourceRevision/LastOccurredAt`

### Implementation actions

1. Do not build a websocket producer merely to satisfy symmetry.
2. Ensure UI can refresh/refetch authoritative report/Dashboard state and recover after navigation/workspace switch.
3. Architecture/test gates reject fixture-only realtime as production evidence.

### Concrete target areas

- `Analytics report response freshness metadata`
- `frontend stale/partial state model`
- `no realtime producer in initial slice unless separately admitted`

### Data / migration impact

No realtime sequence persistence needed in initial release.

### Security / tenant impact

No unauthorized broad subscription exists because none is released.

### Cross-team dependencies

- `Metric definitions`
- `projection/source cutoffs`
- `Platform realtime only if later admitted`
- `AR-GATE-001 passed for affected foundation`
- `AR-03 work-item-count:v1 + work-placement-overview:v1 identity available`

Primary canonical TEST IDs: `AR-TST-REQ-061`, `AR-TST-REQ-062`.


### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-061` and `AR-TST-REQ-062` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ061` and `ARREQ062` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

This is a deliberate product/runtime decision, not missing work.

## AR-08-COMPAT-001 — Gate any future Analytics realtime behind recoverability

**Requirements:** `ARREQ063` — Initial P6 certification does not require Analytics realtime; `ARREQ064` — Any Analytics realtime path is gap-recoverable

**Execution disposition:** `IMPLEMENT_FRESHNESS_DEFER_REALTIME`

### Objective

Close `063–064` as one executable unit: gate any future analytics realtime behind recoverability. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `docs/product/analytics.md ANA-014/030/036`
- `WorkspaceWorkItemPlacementProjection.SourceRevision/LastOccurredAt`

### Implementation actions

1. If a concrete realtime consumer is later admitted, add versioned public contract, narrow subscription scope and gap/reconnect recovery via authoritative refetch.
2. Require mixed-client compatibility and duplicate/out-of-order tests.
3. Certify that realtime loss never changes durable Analytics truth.

### Concrete target areas

- `Analytics report response freshness metadata`
- `frontend stale/partial state model`
- `no realtime producer in initial slice unless separately admitted`

### Data / migration impact

Realtime version/sequence changes require compatibility review.

### Security / tenant impact

Subscription authorization follows current Governance.

### Cross-team dependencies

- `Metric definitions`
- `projection/source cutoffs`
- `Platform realtime only if later admitted`
- `AR-GATE-001 passed for affected foundation`
- `AR-03 work-item-count:v1 + work-placement-overview:v1 identity available`

Primary canonical TEST IDs: `AR-TST-REQ-063`, `AR-TST-REQ-064`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-063` and `AR-TST-REQ-064` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ063` and `ARREQ064` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Until these proofs exist, realtime status is NOT_APPLICABLE rather than PARTIAL release.

# Lane AR-09 — ReportingSnapshot, historical lineage and retention

**Lane disposition:** `RELOCATE_LEGACY_GAP_AND_IMPLEMENT_ARTIFACT_LIFECYCLE`  
**Canonical source anchors:**
- `backend/src/Notrelix.Domain/Analytics/Snapshots/ReportingSnapshot.cs`
- `backend/src/Notrelix.Domain/Analytics/Snapshots/ReportSnapshotPayload.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/ReportingSnapshotConfiguration.cs`
- `backend/tests/Notrelix.Architecture.Tests/DomainPurity/DomainHardeningArchitectureTests.cs`

**Primary implementation targets:**
- `backend/src/Notrelix.Application/Features/Analytics/Snapshots/ReportingSnapshotRecord.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IReportingSnapshotStore.cs`
- `backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ReportingSnapshotStore.cs`
- `snapshot schema migration`

**Lane dependencies:**
- `AR-GATE-001 passed for affected foundation`
- `AR-03 work-item-count:v1 + work-placement-overview:v1 identity available`

## AR-09-INV-001 — Relocate ReportingSnapshot out of Domain LegacyGap

**Requirements:** `ARREQ065` — Reporting Snapshot is a derived report artifact, not transactional Domain authority; `ARREQ066` — Reporting Snapshot leaves the Domain LegacyGap classification

**Execution disposition:** `RELOCATE_LEGACY_GAP_AND_IMPLEMENT_ARTIFACT_LIFECYCLE`

### Objective

Close `065–066` as one executable unit: relocate reportingsnapshot out of domain legacygap. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Domain/Analytics/Snapshots/ReportingSnapshot.cs`
- `backend/src/Notrelix.Domain/Analytics/Snapshots/ReportSnapshotPayload.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/ReportingSnapshotConfiguration.cs`
- `backend/tests/Notrelix.Architecture.Tests/DomainPurity/DomainHardeningArchitectureTests.cs`

### Implementation actions

1. Introduce an Application projection/artifact record carrying AccountId, WorkspaceId, ReportId/Metric key+version, SchemaVersion, SourceCutoff, CapturedAt and payload.
2. Map existing reporting_snapshots table to the new non-Domain type.
3. Remove DomainHardening LegacyGap entry only after no Domain dependency remains.

### Concrete target areas

- `backend/src/Notrelix.Application/Features/Analytics/Snapshots/ReportingSnapshotRecord.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IReportingSnapshotStore.cs`
- `backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ReportingSnapshotStore.cs`
- `snapshot schema migration`

### Data / migration impact

Migration adds lineage columns and backfills only where provenance is trustworthy; legacy unknown lineage remains explicitly versioned/limited.

### Security / tenant impact

Snapshots remain tenant-scoped under RLS.

### Cross-team dependencies

- `AR-03 Metric versioning`
- `AR-04 store boundary`
- `retention/privacy policy`
- `AR-GATE-001 passed for affected foundation`
- `AR-03 work-item-count:v1 + work-placement-overview:v1 identity available`

Primary canonical TEST IDs: `AR-TST-REQ-065`, `AR-TST-REQ-066`.


### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-065` and `AR-TST-REQ-066` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ065` and `ARREQ066` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Do not promote snapshot to aggregate/source authority.

## AR-09-CORE-001 — Implement version-aware capture and read contracts

**Requirements:** `ARREQ067` — Snapshot identity contains reproducibility lineage; `ARREQ068` — Snapshot schema readers are version-aware

**Execution disposition:** `RELOCATE_LEGACY_GAP_AND_IMPLEMENT_ARTIFACT_LIFECYCLE`

### Objective

Close `067–068` as one executable unit: implement version-aware capture and read contracts. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Domain/Analytics/Snapshots/ReportingSnapshot.cs`
- `backend/src/Notrelix.Domain/Analytics/Snapshots/ReportSnapshotPayload.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/ReportingSnapshotConfiguration.cs`
- `backend/tests/Notrelix.Architecture.Tests/DomainPurity/DomainHardeningArchitectureTests.cs`

### Implementation actions

1. Create capture/query use cases through `IReportingSnapshotStore`.
2. Require snapshot identity and payload schema reader selected by stored version.
3. Expose historical 'reported then' metadata independently from current recomputation.

### Concrete target areas

- `backend/src/Notrelix.Application/Features/Analytics/Snapshots/ReportingSnapshotRecord.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IReportingSnapshotStore.cs`
- `backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ReportingSnapshotStore.cs`
- `snapshot schema migration`

### Data / migration impact

Add indexes on workspace/report/metric/captured time as measured by query pattern.

### Security / tenant impact

Snapshot reads follow report visibility and sensitive-field rules.

### Cross-team dependencies

- `AR-03 Metric versioning`
- `AR-04 store boundary`
- `retention/privacy policy`
- `AR-GATE-001 passed for affected foundation`
- `AR-03 work-item-count:v1 + work-placement-overview:v1 identity available`

Primary canonical TEST IDs: `AR-TST-REQ-067`, `AR-TST-REQ-068`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-SNAP-001`, `ANA-TST-SNAP-MIG-001`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-067` and `AR-TST-REQ-068` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ067` and `ARREQ068` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Old payload is never deserialized as new schema silently.

## AR-09-SEC-001 — Implement historical-comparison and privacy semantics

**Requirements:** `ARREQ069` — Snapshot and current rebuilt projection may legitimately differ; `ARREQ070` — Historical comparisons preserve Metric compatibility

**Execution disposition:** `RELOCATE_LEGACY_GAP_AND_IMPLEMENT_ARTIFACT_LIFECYCLE`

### Objective

Close `069–070` as one executable unit: implement historical-comparison and privacy semantics. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Domain/Analytics/Snapshots/ReportingSnapshot.cs`
- `backend/src/Notrelix.Domain/Analytics/Snapshots/ReportSnapshotPayload.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/ReportingSnapshotConfiguration.cs`
- `backend/tests/Notrelix.Architecture.Tests/DomainPurity/DomainHardeningArchitectureTests.cs`

### Implementation actions

1. Prevent continuous trend comparison across incompatible Metric versions without explicit mapping/recompute.
2. Define anonymize/purge/retain-aggregate behavior for source/account/user deletion.
3. Record source cutoff and retention class in snapshot/artifact metadata.

### Concrete target areas

- `backend/src/Notrelix.Application/Features/Analytics/Snapshots/ReportingSnapshotRecord.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IReportingSnapshotStore.cs`
- `backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ReportingSnapshotStore.cs`
- `snapshot schema migration`

### Data / migration impact

Retention/anonymization may need data migrations/jobs.

### Security / tenant impact

Snapshot may outlive source active state only under approved policy.

### Cross-team dependencies

- `AR-03 Metric versioning`
- `AR-04 store boundary`
- `retention/privacy policy`
- `AR-GATE-001 passed for affected foundation`
- `AR-03 work-item-count:v1 + work-placement-overview:v1 identity available`

Primary canonical TEST IDs: `AR-TST-REQ-069`, `AR-TST-REQ-070`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-069` and `AR-TST-REQ-070` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ069` and `ARREQ070` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Historical artifact cannot re-expose revoked sensitive detail.

## AR-09-COMPAT-001 — Certify snapshot schema evolution and lifecycle

**Requirements:** `ARREQ071` — Source deletion follows explicit derived-data policy; `ARREQ072` — Snapshot/export retention and access are explicit

**Execution disposition:** `RELOCATE_LEGACY_GAP_AND_IMPLEMENT_ARTIFACT_LIFECYCLE`

### Objective

Close `071–072` as one executable unit: certify snapshot schema evolution and lifecycle. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Domain/Analytics/Snapshots/ReportingSnapshot.cs`
- `backend/src/Notrelix.Domain/Analytics/Snapshots/ReportSnapshotPayload.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/ReportingSnapshotConfiguration.cs`
- `backend/tests/Notrelix.Architecture.Tests/DomainPurity/DomainHardeningArchitectureTests.cs`

### Implementation actions

1. Test clean DB, legacy snapshot read, v1→v2 schema reader/migration and unsupported-version behavior.
2. Prove snapshot purge does not alter source state.
3. Record rollback/forward-fix when new lineage/config cannot be read by older code.

### Concrete target areas

- `backend/src/Notrelix.Application/Features/Analytics/Snapshots/ReportingSnapshotRecord.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IReportingSnapshotStore.cs`
- `backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ReportingSnapshotStore.cs`
- `snapshot schema migration`

### Data / migration impact

Migration and retained-reader window required.

### Security / tenant impact

Download/query authorization remains current even for historical artifacts.

### Cross-team dependencies

- `AR-03 Metric versioning`
- `AR-04 store boundary`
- `retention/privacy policy`
- `AR-GATE-001 passed for affected foundation`
- `AR-03 work-item-count:v1 + work-placement-overview:v1 identity available`

Primary canonical TEST IDs: `AR-TST-REQ-071`, `AR-TST-REQ-072`.

Legacy stable TEST IDs to preserve/route: `ANA-TST-PRJ-DEL-001`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-071` and `AR-TST-REQ-072` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ071` and `ARREQ072` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

LegacyGap closure is exact-candidate evidence, not a documentation deletion alone.

# Lane AR-10 — Source-context onboarding and domain analytics

**Lane disposition:** `BLOCK_UNTIL_SOURCE_HANDOFF_THEN_IMPLEMENT`  
**Canonical source anchors:**
- `backend/src/Notrelix.Application/Events/WorkManagement/`
- `backend/src/Notrelix.Application/Events/Documents/`
- `backend/src/Notrelix.Application/Events/Collaboration/`
- `backend/src/Notrelix.Application/Events/Automation/`
- `backend/src/Notrelix.Application/Events/Integrations/`
- `backend/src/Notrelix.Application/Events/Billing/`

**Primary implementation targets:**
- `source-specific Analytics projections under Application/Features/Analytics/Projections/`
- `source-specific Infrastructure consumers/adapters`
- `Metric handoffs and certification rows`

**Lane dependencies:**
- `AR-GATE-001 passed`
- `source-specific Source→Analytics + Metric handoff`

## AR-10-INV-001 — Admit Work/Workspace/Documents sources only from approved facts

**Requirements:** `ARREQ073` — WorkManagement analytics consumes canonical Work facts; `ARREQ074` — Documents analytics minimizes content exposure

**Execution disposition:** `BLOCK_UNTIL_SOURCE_HANDOFF_THEN_IMPLEMENT`

### Objective

Close `073–074` as one executable unit: admit work/workspace/documents sources only from approved facts. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Application/Events/WorkManagement/`
- `backend/src/Notrelix.Application/Events/Documents/`
- `backend/src/Notrelix.Application/Events/Collaboration/`
- `backend/src/Notrelix.Application/Events/Automation/`
- `backend/src/Notrelix.Application/Events/Integrations/`
- `backend/src/Notrelix.Application/Events/Billing/`

### Implementation actions

1. Retain Work placement as reference.
2. Inventory Workspace member/lifecycle and Documents page events for exact fields/version/scope before any Metric definition.
3. If a desired Metric needs data absent from the public contract, block and hand back to source owner; do not query private tables.

### Concrete target areas

- `source-specific Analytics projections under Application/Features/Analytics/Projections/`
- `source-specific Infrastructure consumers/adapters`
- `Metric handoffs and certification rows`

### Data / migration impact

No projection table until Metric/source contract is admitted.

### Security / tenant impact

Identity/member/document privacy classification required.

### Cross-team dependencies

- `AR-02 source handoff`
- `AR-03 Metric handoff`
- `AR-04 projection foundation`
- `AR-GATE-001 passed`
- `source-specific Source→Analytics + Metric handoff`

Primary canonical TEST IDs: `AR-TST-REQ-073`, `AR-TST-REQ-074`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-073` and `AR-TST-REQ-074` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ073` and `ARREQ074` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Potential/example metrics in product docs are not implementation authority.

## AR-10-CORE-001 — Admit Collaboration and Automation sources with semantic boundaries

**Requirements:** `ARREQ075` — Collaboration analytics remains distinct from Activity/Audit; `ARREQ076` — Automation analytics preserves Automation execution semantics

**Execution disposition:** `BLOCK_UNTIL_SOURCE_HANDOFF_THEN_IMPLEMENT`

### Objective

Close `075–076` as one executable unit: admit collaboration and automation sources with semantic boundaries. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Application/Events/WorkManagement/`
- `backend/src/Notrelix.Application/Events/Documents/`
- `backend/src/Notrelix.Application/Events/Collaboration/`
- `backend/src/Notrelix.Application/Events/Automation/`
- `backend/src/Notrelix.Application/Events/Integrations/`
- `backend/src/Notrelix.Application/Events/Billing/`

### Implementation actions

1. Collaboration comment/mention facts may onboard only for approved metrics and must remain distinct from Activity/Audit.
2. Automation execution analytics is BLOCKED until P5 exposes a stable public execution lifecycle contract; do not consume private execution tables/Domain events as shortcut.
3. Once unblocked, use Automation outcome taxonomy exactly.

### Concrete target areas

- `source-specific Analytics projections under Application/Features/Analytics/Projections/`
- `source-specific Infrastructure consumers/adapters`
- `Metric handoffs and certification rows`

### Data / migration impact

Projection schema is per admitted Metric/source, not a generic event dump.

### Security / tenant impact

No comment body/secret payload by default.

### Cross-team dependencies

- `AR-02 source handoff`
- `AR-03 Metric handoff`
- `AR-04 projection foundation`
- `AR-GATE-001 passed`
- `source-specific Source→Analytics + Metric handoff`

Primary canonical TEST IDs: `AR-TST-REQ-075`, `AR-TST-REQ-076`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-075` and `AR-TST-REQ-076` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ075` and `ARREQ076` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

P5 source readiness is a hard dependency, not a TODO.

## AR-10-SEC-001 — Admit Integrations and Billing sources without redefining authority

**Requirements:** `ARREQ077` — Integrations analytics preserves provider/sync semantics; `ARREQ078` — Billing analytics never becomes commercial authority

**Execution disposition:** `BLOCK_UNTIL_SOURCE_HANDOFF_THEN_IMPLEMENT`

### Objective

Close `077–078` as one executable unit: admit integrations and billing sources without redefining authority. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Application/Events/WorkManagement/`
- `backend/src/Notrelix.Application/Events/Documents/`
- `backend/src/Notrelix.Application/Events/Collaboration/`
- `backend/src/Notrelix.Application/Events/Automation/`
- `backend/src/Notrelix.Application/Events/Integrations/`
- `backend/src/Notrelix.Application/Events/Billing/`

### Implementation actions

1. ConnectionRevoked/webhook-processing request are insufficient to infer all provider health/sync outcomes; require terminal Integrations facts before those Metrics.
2. SubscriptionChanged/Canceled may support approved subscription metrics; billable usage/entitlement/invoice metrics require Billing public reporting facts.
3. Exclude provider credentials and commercial authority mutations.

### Concrete target areas

- `source-specific Analytics projections under Application/Features/Analytics/Projections/`
- `source-specific Infrastructure consumers/adapters`
- `Metric handoffs and certification rows`

### Data / migration impact

No direct Billing/Integrations DbContext reads.

### Security / tenant impact

Sensitive Billing reports may require stricter report action than Workspace analytics.

### Cross-team dependencies

- `AR-02 source handoff`
- `AR-03 Metric handoff`
- `AR-04 projection foundation`
- `AR-GATE-001 passed`
- `source-specific Source→Analytics + Metric handoff`

Primary canonical TEST IDs: `AR-TST-REQ-077`, `AR-TST-REQ-078`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-077` and `AR-TST-REQ-078` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ077` and `ARREQ078` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Analytics counts cannot become billable quantities.

## AR-10-COMPAT-001 — Certify every source independently and freeze no-growth legacy read models

**Requirements:** `ARREQ079` — Account/workspace metrics use canonical Workspace/Identity facts and legacy usage models remain no-growth; `ARREQ080` — Each source context is certified independently

**Execution disposition:** `BLOCK_UNTIL_SOURCE_HANDOFF_THEN_IMPLEMENT`

### Objective

Close `079–080` as one executable unit: certify every source independently and freeze no-growth legacy read models. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Application/Events/WorkManagement/`
- `backend/src/Notrelix.Application/Events/Documents/`
- `backend/src/Notrelix.Application/Events/Collaboration/`
- `backend/src/Notrelix.Application/Events/Automation/`
- `backend/src/Notrelix.Application/Events/Integrations/`
- `backend/src/Notrelix.Application/Events/Billing/`

### Implementation actions

1. Define Account/Workspace metrics only from AccountCreated, WorkspaceCreated, WorkspaceMemberAdded/Removed or another approved Workspace/Identity reporting contract; Analytics does not become a membership store.
2. Keep WorkspaceUsageDaily/FeatureUsageDaily no-growth until a source+Metric owner and production population/rebuild path exist.
3. For each admitted source, add source-version/backfill/rebuild tests and independent certification evidence.
4. A source failing D4+ stays BLOCKED without lowering other source readiness.

### Concrete target areas

- `source-specific Analytics projections under Application/Features/Analytics/Projections/`
- `source-specific Infrastructure consumers/adapters`
- `Metric handoffs and certification rows`

### Data / migration impact

Legacy unused tables may be retained until a separate cleanup/migration proves safety.

### Security / tenant impact

Tenant/privacy evidence is per source.

### Cross-team dependencies

- `AR-02 source handoff`
- `AR-03 Metric handoff`
- `AR-04 projection foundation`
- `AR-GATE-001 passed`
- `source-specific Source→Analytics + Metric handoff`

Primary canonical TEST IDs: `AR-TST-REQ-079`, `AR-TST-REQ-080`.

Legacy stable TEST IDs to preserve/route: `ANA-TST-X-001`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-079` and `AR-TST-REQ-080` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ079` and `ARREQ080` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

One green Work projection never certifies Documents/Automation/Billing.

# Lane AR-11 — Cross-context analytical composition

**Lane disposition:** `DEFER_INITIAL_P6_AND_ENFORCE_NON_REACHABILITY`  
**Canonical source anchors:**
- `docs/product/analytics.md cross-context sections`
- `docs/workstreams/teams/analytics-reporting.md ANA-012`

**Primary implementation targets:**
- `backend/src/Notrelix.Application/Features/Analytics/Reports/`
- `typed report definitions/composers over Analytics projections/public report contracts`

**Lane dependencies:**
- `participating AR-10 source projections D4+`
- `AR-07 auth`
- `AR-08 freshness`

## AR-11-INV-001 — Define report identity, join and cutoff contracts

**Requirements:** `ARREQ081` — Cross-context report composes approved derived/source contracts; `ARREQ082` — Cross-context joins use stable shared identity

**Execution disposition:** `DEFER_INITIAL_P6_AND_ENFORCE_NON_REACHABILITY`

### Objective

Keep cross-context reports outside the frozen initial P6 slice. Preserve ARREQ081–ARREQ088 as future admission semantics, enforce that no cross-context report/API/frontend catalog entry is reachable, and require a named Report handoff only after at least two source lanes reach required readiness.

### Current source authority

- `docs/product/analytics.md cross-context sections`
- `docs/workstreams/teams/analytics-reporting.md ANA-012`

### Implementation actions

1. Do NOT release a cross-context report in the initial P6 catalog.
2. Add architecture/API/frontend catalog tests proving no cross-context report is advertised.
3. Keep report-composer contracts non-reachable; do not implement private-table joins as a shortcut.
4. Future admission requires a named Report ID/version, at least two certified sources, shared identity, cutoff/completeness, authorization intersection, cache identity and correction policy.

### Concrete target areas

- `backend/src/Notrelix.Application/Features/Analytics/Reports/`
- `typed report definitions/composers over Analytics projections/public report contracts`

### Data / migration impact

No schema until chosen report requires materialization.

### Security / tenant impact

Join identity and source privacy classes determine authorization intersection.

### Cross-team dependencies

- `every participating source projection D4+`
- `AR-03 Metric catalog`
- `AR-07 authorization`
- `AR-08 freshness`
- `participating AR-10 source projections D4+`
- `AR-07 auth`
- `AR-08 freshness`

Primary canonical TEST IDs: `AR-TST-REQ-081`, `AR-TST-REQ-082`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-X-ARCH-001`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-081` and `AR-TST-REQ-082` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ081` and `ARREQ082` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Atomic cross-context consistency cannot be claimed without mechanism.

## AR-11-CORE-001 — Implement report composition over certified projections

**Requirements:** `ARREQ083` — Cross-context consistency is not overstated; `ARREQ084` — Authorization is the intersection of participating sources

**Execution disposition:** `DEFER_INITIAL_P6_AND_ENFORCE_NON_REACHABILITY`

### Objective

Keep cross-context reports outside the frozen initial P6 slice. Preserve ARREQ081–ARREQ088 as future admission semantics, enforce that no cross-context report/API/frontend catalog entry is reachable, and require a named Report handoff only after at least two source lanes reach required readiness.

### Current source authority

- `docs/product/analytics.md cross-context sections`
- `docs/workstreams/teams/analytics-reporting.md ANA-012`

### Implementation actions

1. Do NOT release a cross-context report in the initial P6 catalog.
2. Add architecture/API/frontend catalog tests proving no cross-context report is advertised.
3. Keep report-composer contracts non-reachable; do not implement private-table joins as a shortcut.
4. Future admission requires a named Report ID/version, at least two certified sources, shared identity, cutoff/completeness, authorization intersection, cache identity and correction policy.

### Concrete target areas

- `backend/src/Notrelix.Application/Features/Analytics/Reports/`
- `typed report definitions/composers over Analytics projections/public report contracts`

### Data / migration impact

Materialized cross-context projection is introduced only when query cost/consistency requires it.

### Security / tenant impact

Application query applies report + source visibility intersection.

### Cross-team dependencies

- `every participating source projection D4+`
- `AR-03 Metric catalog`
- `AR-07 authorization`
- `AR-08 freshness`
- `participating AR-10 source projections D4+`
- `AR-07 auth`
- `AR-08 freshness`

Primary canonical TEST IDs: `AR-TST-REQ-083`, `AR-TST-REQ-084`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-X-002`, `ANA-TST-X-003`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-083` and `AR-TST-REQ-084` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ083` and `ARREQ084` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Private-table join is architecture failure.

## AR-11-SEC-001 — Implement partial/degraded and correction behavior

**Requirements:** `ARREQ085` — Cross-context correction propagates deterministically; `ARREQ086` — Partial multi-source results are explicit

**Execution disposition:** `DEFER_INITIAL_P6_AND_ENFORCE_NON_REACHABILITY`

### Objective

Keep cross-context reports outside the frozen initial P6 slice. Preserve ARREQ081–ARREQ088 as future admission semantics, enforce that no cross-context report/API/frontend catalog entry is reachable, and require a named Report handoff only after at least two source lanes reach required readiness.

### Current source authority

- `docs/product/analytics.md cross-context sections`
- `docs/workstreams/teams/analytics-reporting.md ANA-012`

### Implementation actions

1. Do NOT release a cross-context report in the initial P6 catalog.
2. Add architecture/API/frontend catalog tests proving no cross-context report is advertised.
3. Keep report-composer contracts non-reachable; do not implement private-table joins as a shortcut.
4. Future admission requires a named Report ID/version, at least two certified sources, shared identity, cutoff/completeness, authorization intersection, cache identity and correction policy.

### Concrete target areas

- `backend/src/Notrelix.Application/Features/Analytics/Reports/`
- `typed report definitions/composers over Analytics projections/public report contracts`

### Data / migration impact

May require per-source watermark columns in composed materialization.

### Security / tenant impact

No default zero for missing source.

### Cross-team dependencies

- `every participating source projection D4+`
- `AR-03 Metric catalog`
- `AR-07 authorization`
- `AR-08 freshness`
- `participating AR-10 source projections D4+`
- `AR-07 auth`
- `AR-08 freshness`

Primary canonical TEST IDs: `AR-TST-REQ-085`, `AR-TST-REQ-086`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-FRESH-003`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-085` and `AR-TST-REQ-086` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ085` and `ARREQ086` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Cache invalidation includes source/Metric version and auth class.

## AR-11-COMPAT-001 — Version cross-context reports and cache identity

**Requirements:** `ARREQ087` — Cross-context cache identity includes every semantic dimension; `ARREQ088` — No synchronous transactional dependency on Analytics

**Execution disposition:** `DEFER_INITIAL_P6_AND_ENFORCE_NON_REACHABILITY`

### Objective

Keep cross-context reports outside the frozen initial P6 slice. Preserve ARREQ081–ARREQ088 as future admission semantics, enforce that no cross-context report/API/frontend catalog entry is reachable, and require a named Report handoff only after at least two source lanes reach required readiness.

### Current source authority

- `docs/product/analytics.md cross-context sections`
- `docs/workstreams/teams/analytics-reporting.md ANA-012`

### Implementation actions

1. Do NOT release a cross-context report in the initial P6 catalog.
2. Add architecture/API/frontend catalog tests proving no cross-context report is advertised.
3. Keep report-composer contracts non-reachable; do not implement private-table joins as a shortcut.
4. Future admission requires a named Report ID/version, at least two certified sources, shared identity, cutoff/completeness, authorization intersection, cache identity and correction policy.

### Concrete target areas

- `backend/src/Notrelix.Application/Features/Analytics/Reports/`
- `typed report definitions/composers over Analytics projections/public report contracts`

### Data / migration impact

Report version change may invalidate/rebuild materialized rows and saved Dashboard configs.

### Security / tenant impact

Old report IDs remain readable for agreed compatibility window or fail explicitly.

### Cross-team dependencies

- `every participating source projection D4+`
- `AR-03 Metric catalog`
- `AR-07 authorization`
- `AR-08 freshness`
- `participating AR-10 source projections D4+`
- `AR-07 auth`
- `AR-08 freshness`

Primary canonical TEST IDs: `AR-TST-REQ-087`, `AR-TST-REQ-088`.


### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-087` and `AR-TST-REQ-088` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ087` and `ARREQ088` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

No hidden synchronous transactional dependency on Analytics.

# Lane AR-12 — Report/Dashboard API and dedicated Analytics frontend

**Lane disposition:** `IMPLEMENT_PUBLIC_CONTRACT_AND_CONSUMER`  
**Canonical source anchors:**
- `no Analytics API endpoints at baseline`
- `frontend/apps/web/src/routes/workspaces/$workspaceId/dashboard.tsx`
- `frontend/packages/features/workspace/`

**Primary implementation targets:**
- `backend/src/Notrelix.API/Endpoints/Analytics/`
- `backend public OpenAPI`
- `frontend/packages/features/analytics/`
- `web route/navigation for dedicated Analytics surface exposing work-placement-overview:v1`

**Lane dependencies:**
- `initial released slice: work-item-count:v1 + work-placement-overview:v1 + Dashboard`
- `AR-06/07/08 ready for that slice`

## AR-12-INV-001 — Define typed API surface and bounded query contract

**Requirements:** `ARREQ089` — Report/Metric API is typed and bounded; `ARREQ090` — Dashboard Application/API lifecycle is implemented before release

**Execution disposition:** `IMPLEMENT_PUBLIC_CONTRACT_AND_CONSUMER`

### Objective

Close `089–090` as one executable unit: define typed api surface and bounded query contract. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `no Analytics API endpoints at baseline`
- `frontend/apps/web/src/routes/workspaces/$workspaceId/dashboard.tsx`
- `frontend/packages/features/workspace/`

### Implementation actions

1. Create explicit endpoints for Dashboard lifecycle and the frozen `work-placement-overview:v1` / `work-item-count:v1` query contracts under Analytics API ownership.
2. Define filters/date/timezone/grouping/sort/pagination/limits/freshness error contracts.
3. No generic arbitrary expression/SQL endpoint.

### Concrete target areas

- `backend/src/Notrelix.API/Endpoints/Analytics/`
- `backend public OpenAPI`
- `frontend/packages/features/analytics/`
- `web route/navigation for dedicated Analytics surface exposing work-placement-overview:v1`

### Data / migration impact

OpenAPI changes are additive first.

### Security / tenant impact

Endpoint authorization uses canonical pipeline and ResourceRef.

### Cross-team dependencies

- `AR-03 Metrics`
- `AR-06 Dashboard`
- `AR-07 auth`
- `AR-08 freshness`
- `work-placement-overview:v1 report handoff`
- `initial released slice: work-item-count:v1 + work-placement-overview:v1 + Dashboard`
- `AR-06/07/08 ready for that slice`

Primary canonical TEST IDs: `AR-TST-REQ-089`, `AR-TST-REQ-090`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-API-001`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-089` and `AR-TST-REQ-090` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ089` and `ARREQ090` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Query-too-large/invalid/forbidden/unavailable are distinct.

## AR-12-CORE-001 — Implement Application→API production composition

**Requirements:** `ARREQ091` — No direct HTTP source-table analytics; `ARREQ092` — Frontend uses server-owned Metric semantics

**Execution disposition:** `IMPLEMENT_PUBLIC_CONTRACT_AND_CONSUMER`

### Objective

Close `091–092` as one executable unit: implement application→api production composition. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `no Analytics API endpoints at baseline`
- `frontend/apps/web/src/routes/workspaces/$workspaceId/dashboard.tsx`
- `frontend/packages/features/workspace/`

### Implementation actions

1. Handlers call Analytics repositories/query services only.
2. Add OpenAPI export/codegen and production DI tests for Metric catalog, repositories and report queries.
3. Keep GetWorkspacePlacements internal unless an admitted report explicitly consumes/exposes it through a stable DTO.

### Concrete target areas

- `backend/src/Notrelix.API/Endpoints/Analytics/`
- `backend public OpenAPI`
- `frontend/packages/features/analytics/`
- `web route/navigation for dedicated Analytics surface exposing work-placement-overview:v1`

### Data / migration impact

No direct API DbContext/foreign table access.

### Security / tenant impact

Request tenant scope and source authorization are mandatory.

### Cross-team dependencies

- `AR-03 Metrics`
- `AR-06 Dashboard`
- `AR-07 auth`
- `AR-08 freshness`
- `work-placement-overview:v1 report handoff`
- `initial released slice: work-item-count:v1 + work-placement-overview:v1 + Dashboard`
- `AR-06/07/08 ready for that slice`

Primary canonical TEST IDs: `AR-TST-REQ-091`, `AR-TST-REQ-092`.


### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-091` and `AR-TST-REQ-092` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ091` and `ARREQ092` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Production graph proof required; unit handlers alone are insufficient.

## AR-12-SEC-001 — Create dedicated frontend Analytics boundary

**Requirements:** `ARREQ093` — Workspace operational dashboard is not Analytics product evidence; `ARREQ094` — P6 frontend has a dedicated Analytics contract boundary

**Execution disposition:** `IMPLEMENT_PUBLIC_CONTRACT_AND_CONSUMER`

### Objective

Close `093–094` as one executable unit: create dedicated frontend analytics boundary. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `no Analytics API endpoints at baseline`
- `frontend/apps/web/src/routes/workspaces/$workspaceId/dashboard.tsx`
- `frontend/packages/features/workspace/`

### Implementation actions

1. Create `frontend/packages/features/analytics` following repo feature-package rules; use generated contracts and canonical runtime dependencies.
2. Do not reinterpret `/workspaces/$workspaceId/dashboard.tsx` as Analytics readiness; keep Workspace operational surface separate.
3. No mock/demo repository fallback in production composition.

### Concrete target areas

- `backend/src/Notrelix.API/Endpoints/Analytics/`
- `backend public OpenAPI`
- `frontend/packages/features/analytics/`
- `web route/navigation for dedicated Analytics surface exposing work-placement-overview:v1`

### Data / migration impact

No DB impact.

### Security / tenant impact

Query state is tenant/report/Metric scoped and protected on account/workspace switch.

### Cross-team dependencies

- `AR-03 Metrics`
- `AR-06 Dashboard`
- `AR-07 auth`
- `AR-08 freshness`
- `work-placement-overview:v1 report handoff`
- `initial released slice: work-item-count:v1 + work-placement-overview:v1 + Dashboard`
- `AR-06/07/08 ready for that slice`

Primary canonical TEST IDs: `AR-TST-REQ-093`, `AR-TST-REQ-094`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-093` and `AR-TST-REQ-094` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ093` and `ARREQ094` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Frontend cannot broaden server authorization.

## AR-12-COMPAT-001 — Prove frontend semantic parity and tenant-safe state

**Requirements:** `ARREQ095` — Frontend query keys partition tenant and report semantics; `ARREQ096` — Frontend state distinguishes zero/no-data/stale/partial/forbidden/unavailable

**Execution disposition:** `IMPLEMENT_PUBLIC_CONTRACT_AND_CONSUMER`

### Objective

Close `095–096` as one executable unit: prove frontend semantic parity and tenant-safe state. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `no Analytics API endpoints at baseline`
- `frontend/apps/web/src/routes/workspaces/$workspaceId/dashboard.tsx`
- `frontend/packages/features/workspace/`

### Implementation actions

1. Canonical query keys include Account/Workspace/report or Metric version/filter/timezone/grouping.
2. Render zero/no-data/stale/partial/forbidden/unavailable distinctly.
3. Add OpenAPI/codegen drift, route/component, switch-isolation and contract-version tests.

### Concrete target areas

- `backend/src/Notrelix.API/Endpoints/Analytics/`
- `backend public OpenAPI`
- `frontend/packages/features/analytics/`
- `web route/navigation for dedicated Analytics surface exposing work-placement-overview:v1`

### Data / migration impact

Saved filters/dashboard configs migrate with public contract changes.

### Security / tenant impact

No client-side reaggregation that changes Metric meaning.

### Cross-team dependencies

- `AR-03 Metrics`
- `AR-06 Dashboard`
- `AR-07 auth`
- `AR-08 freshness`
- `work-placement-overview:v1 report handoff`
- `initial released slice: work-item-count:v1 + work-placement-overview:v1 + Dashboard`
- `AR-06/07/08 ready for that slice`

Primary canonical TEST IDs: `AR-TST-REQ-095`, `AR-TST-REQ-096`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-095` and `AR-TST-REQ-096` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ095` and `ARREQ096` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Old generated contract removal waits for consumer migration.

# Lane AR-13 — Export and generated report artifact delivery

**Lane disposition:** `DEFER_INITIAL_P6_AND_ENFORCE_NON_REACHABILITY`  
**Canonical source anchors:**
- `docs/product/analytics.md export/report generation semantics`
- `no Analytics export runtime at baseline`

**Primary implementation targets:**
- `backend/src/Notrelix.Application/Features/Analytics/Exports/`
- `backend/src/Notrelix.API/Endpoints/Analytics/Exports/`
- `existing approved file/artifact storage if available; otherwise escalate`

**Lane dependencies:**
- `a Report handoff explicitly admits Export`
- `AR-12 report query contract ready`

## AR-13-INV-001 — Admit export per report, not globally

**Requirements:** `ARREQ097` — Export is a protected Analytics operation; `ARREQ098` — Export reuses the same query semantics as interactive reporting

**Execution disposition:** `DEFER_INITIAL_P6_AND_ENFORCE_NON_REACHABILITY`

### Objective

Keep export and scheduled-report delivery outside the frozen initial P6 slice while preserving their future contract. Prove non-reachability now; do not invent artifact storage or delivery architecture merely for symmetry.

### Current source authority

- `docs/product/analytics.md export/report generation semantics`
- `no Analytics export runtime at baseline`

### Implementation actions

1. `work-placement-overview:v1` declares `Export=none` and `ScheduledDelivery=none`.
2. Do NOT register export endpoints/workers/artifact routes or scheduled-report jobs/recipient delivery in initial P6.
3. Add API/OpenAPI/frontend/background registration tests proving non-reachability.
4. Preserve ARREQ097–ARREQ103 for future export admission.
5. Future scheduled-report admission requires recipient, authorization-at-delivery, cutoff, delivery owner/channel, retry/idempotency, expiration/retention and failure visibility.

### Concrete target areas

- `backend/src/Notrelix.Application/Features/Analytics/Exports/`
- `backend/src/Notrelix.API/Endpoints/Analytics/Exports/`
- `existing approved file/artifact storage if available; otherwise escalate`

### Data / migration impact

Initial P6 creates no export artifact schema/storage or scheduled-delivery persistence.

### Security / tenant impact

Export authorization is same-or-stricter than interactive report.

### Cross-team dependencies

- `certified report query`
- `AR-07 authorization`
- `retention/storage authority`
- `a Report handoff explicitly admits Export`
- `AR-12 report query contract ready`

Primary canonical TEST IDs: `AR-TST-REQ-097`, `AR-TST-REQ-098`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-SEC-003`, `ANA-TST-EXP-001`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-097` and `AR-TST-REQ-098` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ097` and `ARREQ098` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Initial P6 defers export and scheduled delivery; non-reachability is the PASS condition and does not block the released Dashboard/report query slice.

## AR-13-CORE-001 — Implement durable export job only where synchronous limits are exceeded

**Requirements:** `ARREQ099` — Large export uses asynchronous report job semantics; `ARREQ100` — Export artifact captures a source cutoff

**Execution disposition:** `DEFER_INITIAL_P6_AND_ENFORCE_NON_REACHABILITY`

### Objective

Keep export and scheduled-report delivery outside the frozen initial P6 slice while preserving their future contract. Prove non-reachability now; do not invent artifact storage or delivery architecture merely for symmetry.

### Current source authority

- `docs/product/analytics.md export/report generation semantics`
- `no Analytics export runtime at baseline`

### Implementation actions

1. `work-placement-overview:v1` declares `Export=none` and `ScheduledDelivery=none`.
2. Do NOT register export endpoints/workers/artifact routes or scheduled-report jobs/recipient delivery in initial P6.
3. Add API/OpenAPI/frontend/background registration tests proving non-reachability.
4. Preserve ARREQ097–ARREQ103 for future export admission.
5. Future scheduled-report admission requires recipient, authorization-at-delivery, cutoff, delivery owner/channel, retry/idempotency, expiration/retention and failure visibility.

### Concrete target areas

- `backend/src/Notrelix.Application/Features/Analytics/Exports/`
- `backend/src/Notrelix.API/Endpoints/Analytics/Exports/`
- `existing approved file/artifact storage if available; otherwise escalate`

### Data / migration impact

May require reporting export_jobs/artifact metadata tables; storage provider remains an approved shared mechanism.

### Security / tenant impact

Background actor/tenant context restored explicitly.

### Cross-team dependencies

- `certified report query`
- `AR-07 authorization`
- `retention/storage authority`
- `a Report handoff explicitly admits Export`
- `AR-12 report query contract ready`

Primary canonical TEST IDs: `AR-TST-REQ-099`, `AR-TST-REQ-100`.


### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-099` and `AR-TST-REQ-100` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ099` and `ARREQ100` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

No unbounded request thread or ad-hoc background Task.

## AR-13-SEC-001 — Protect artifact contents and download access

**Requirements:** `ARREQ101` — Export download remains authorized; `ARREQ102` — Export artifact contains no hidden sensitive dimensions

**Execution disposition:** `DEFER_INITIAL_P6_AND_ENFORCE_NON_REACHABILITY`

### Objective

Keep export and scheduled-report delivery outside the frozen initial P6 slice while preserving their future contract. Prove non-reachability now; do not invent artifact storage or delivery architecture merely for symmetry.

### Current source authority

- `docs/product/analytics.md export/report generation semantics`
- `no Analytics export runtime at baseline`

### Implementation actions

1. `work-placement-overview:v1` declares `Export=none` and `ScheduledDelivery=none`.
2. Do NOT register export endpoints/workers/artifact routes or scheduled-report jobs/recipient delivery in initial P6.
3. Add API/OpenAPI/frontend/background registration tests proving non-reachability.
4. Preserve ARREQ097–ARREQ103 for future export admission.
5. Future scheduled-report admission requires recipient, authorization-at-delivery, cutoff, delivery owner/channel, retry/idempotency, expiration/retention and failure visibility.

### Concrete target areas

- `backend/src/Notrelix.Application/Features/Analytics/Exports/`
- `backend/src/Notrelix.API/Endpoints/Analytics/Exports/`
- `existing approved file/artifact storage if available; otherwise escalate`

### Data / migration impact

Artifact retention/cleanup metadata must be persisted if async.

### Security / tenant impact

Secret/provider payloads never enter export.

### Cross-team dependencies

- `certified report query`
- `AR-07 authorization`
- `retention/storage authority`
- `a Report handoff explicitly admits Export`
- `AR-12 report query contract ready`

Primary canonical TEST IDs: `AR-TST-REQ-101`, `AR-TST-REQ-102`.

Legacy stable TEST IDs to preserve/route: `ANA-TST-EXP-AUTHZ-001`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-101` and `AR-TST-REQ-102` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ101` and `ARREQ102` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Revoked access must not be bypassed by stale artifact URL.

## AR-13-COMPAT-001 — Prove idempotent retry, retention and export/report parity

**Requirements:** `ARREQ103` — Export job retries cannot duplicate artifacts/effects unsafely; `ARREQ104` — Export and scheduled-report delivery are independently certifiable

**Execution disposition:** `DEFER_INITIAL_P6_AND_ENFORCE_NON_REACHABILITY`

### Objective

Keep export and scheduled-report delivery outside the frozen initial P6 slice while preserving their future contract. Prove non-reachability now; do not invent artifact storage or delivery architecture merely for symmetry.

### Current source authority

- `docs/product/analytics.md export/report generation semantics`
- `no Analytics export runtime at baseline`

### Implementation actions

1. `work-placement-overview:v1` declares `Export=none` and `ScheduledDelivery=none`.
2. Do NOT register export endpoints/workers/artifact routes or scheduled-report jobs/recipient delivery in initial P6.
3. Add API/OpenAPI/frontend/background registration tests proving non-reachability.
4. Preserve ARREQ097–ARREQ103 for future export admission.
5. Future scheduled-report admission requires recipient, authorization-at-delivery, cutoff, delivery owner/channel, retry/idempotency, expiration/retention and failure visibility.

### Concrete target areas

- `backend/src/Notrelix.Application/Features/Analytics/Exports/`
- `backend/src/Notrelix.API/Endpoints/Analytics/Exports/`
- `existing approved file/artifact storage if available; otherwise escalate`

### Data / migration impact

Artifact schema/format evolution gets explicit version where retained.

### Security / tenant impact

Download authorization tested after role/membership change.

### Cross-team dependencies

- `certified report query`
- `AR-07 authorization`
- `retention/storage authority`
- `a Report handoff explicitly admits Export`
- `AR-12 report query contract ready`

Primary canonical TEST IDs: `AR-TST-REQ-103`, `AR-TST-REQ-104`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-103` and `AR-TST-REQ-104` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ103` and `ARREQ104` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Export certification is independent per format/job path.

# Lane AR-14 — Performance, storage, cache, data quality and observability

**Lane disposition:** `IMPLEMENT_MEASURED_HARDENING`  
**Canonical source anchors:**
- `docs/quality/performance-and-scalability.md`
- `reporting schema configurations`
- `reference projection tests/logging`

**Primary implementation targets:**
- `Analytics query/load tests`
- `projection/report quality metrics`
- `reconciliation jobs/queries`
- `cache implementation only where measured`

**Lane dependencies:**
- `representative implemented report/projection workload exists`

## AR-14-INV-001 — Measure query patterns before storage expansion

**Requirements:** `ARREQ105` — Enterprise dashboard requests do not full-scan arbitrary JSON; `ARREQ106` — Storage technology follows measured need

**Execution disposition:** `IMPLEMENT_MEASURED_HARDENING`

### Objective

Close `105–106` as one executable unit: measure query patterns before storage expansion. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `docs/quality/performance-and-scalability.md`
- `reporting schema configurations`
- `reference projection tests/logging`

### Implementation actions

1. Define representative tenant sizes, date windows, dimensions and Dashboard/report query budgets.
2. Capture query plans/row counts for admitted reports.
3. Keep relational reporting store as default; no warehouse/service extraction without measured need.

### Concrete target areas

- `Analytics query/load tests`
- `projection/report quality metrics`
- `reconciliation jobs/queries`
- `cache implementation only where measured`

### Data / migration impact

No speculative partition/index migration.

### Security / tenant impact

Performance tests always include tenant predicate and auth filters.

### Cross-team dependencies

- `real report query patterns`
- `AR-03/04/10/11 implemented slices`
- `representative implemented report/projection workload exists`

Primary canonical TEST IDs: `AR-TST-REQ-105`, `AR-TST-REQ-106`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-PERF-001`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-105` and `AR-TST-REQ-106` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ105` and `ARREQ106` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

A slow query is not authority to bypass security.

## AR-14-CORE-001 — Add targeted indexes/materialization/pre-aggregation and safe cache

**Requirements:** `ARREQ107` — Index/partition/pre-aggregation is metric-driven; `ARREQ108` — Analytics cache keys are semantically complete

**Execution disposition:** `IMPLEMENT_MEASURED_HARDENING`

### Objective

Close `107–108` as one executable unit: add targeted indexes/materialization/pre-aggregation and safe cache. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `docs/quality/performance-and-scalability.md`
- `reporting schema configurations`
- `reference projection tests/logging`

### Implementation actions

1. Add indexes/partitions/pre-aggregates only for measured access paths.
2. Every pre-aggregate retains source Metric version, tenant scope, correction and rebuild semantics.
3. Cache key includes all semantic/security dimensions; avoid cache entirely when safe sharing cannot be proven.

### Concrete target areas

- `Analytics query/load tests`
- `projection/report quality metrics`
- `reconciliation jobs/queries`
- `cache implementation only where measured`

### Data / migration impact

Schema/index migrations are explicit and load-tested.

### Security / tenant impact

No cross-tenant/shared-principal leakage.

### Cross-team dependencies

- `real report query patterns`
- `AR-03/04/10/11 implemented slices`
- `representative implemented report/projection workload exists`

Primary canonical TEST IDs: `AR-TST-REQ-107`, `AR-TST-REQ-108`.


### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-107` and `AR-TST-REQ-108` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ107` and `ARREQ108` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Multiple inconsistent aggregates for same Metric are forbidden.

## AR-14-SEC-001 — Implement data-quality reconciliation

**Requirements:** `ARREQ109` — Data-quality checks detect projection divergence; `ARREQ110` — Critical Metrics have an approved reconciliation source

**Execution disposition:** `IMPLEMENT_MEASURED_HARDENING`

### Objective

Close `109–110` as one executable unit: implement data-quality reconciliation. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `docs/quality/performance-and-scalability.md`
- `reporting schema configurations`
- `reference projection tests/logging`

### Implementation actions

1. Detect duplicate/stale apply, lag, impossible counts, orphan identities, unsupported versions and source/projection divergence.
2. Critical Metric reconciliation uses approved producer snapshot/invariant, never casual private-table reads.
3. Define mismatch severity and governed repair/rebuild command.

### Concrete target areas

- `Analytics query/load tests`
- `projection/report quality metrics`
- `reconciliation jobs/queries`
- `cache implementation only where measured`

### Data / migration impact

Quality tables/records only if operationally required; do not make them product source truth.

### Security / tenant impact

Reconciliation respects tenant/data-region boundaries.

### Cross-team dependencies

- `real report query patterns`
- `AR-03/04/10/11 implemented slices`
- `representative implemented report/projection workload exists`

Primary canonical TEST IDs: `AR-TST-REQ-109`, `AR-TST-REQ-110`.


### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-109` and `AR-TST-REQ-110` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ109` and `ARREQ110` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Repair cannot mutate source owner state.

## AR-14-COMPAT-001 — Implement observability and degraded-readiness evidence

**Requirements:** `ARREQ111` — Operational observability exposes lag/backfill/report health; `ARREQ112` — Operational telemetry is not product Analytics

**Execution disposition:** `IMPLEMENT_MEASURED_HARDENING`

### Objective

Close `111–112` as one executable unit: implement observability and degraded-readiness evidence. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `docs/quality/performance-and-scalability.md`
- `reporting schema configurations`
- `reference projection tests/logging`

### Implementation actions

1. Emit projection lag/oldest age, failed apply, backfill progress, report latency, export backlog and reconciliation result signals.
2. Keep operational telemetry separate from product Metric catalog.
3. Tie alerts/readiness to capability-specific SLOs; Analytics degradation normally does not fail unrelated transactional readiness.

### Concrete target areas

- `Analytics query/load tests`
- `projection/report quality metrics`
- `reconciliation jobs/queries`
- `cache implementation only where measured`

### Data / migration impact

No sensitive payload in metric/log labels.

### Security / tenant impact

Observability dimension cardinality is bounded.

### Cross-team dependencies

- `real report query patterns`
- `AR-03/04/10/11 implemented slices`
- `representative implemented report/projection workload exists`

Primary canonical TEST IDs: `AR-TST-REQ-111`, `AR-TST-REQ-112`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-PERF-002`, `ANA-TST-OBS-001`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-111` and `AR-TST-REQ-112` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ111` and `ARREQ112` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

D5 critical paths require actionable failure evidence, not only happy-path latency.

# Lane AR-15 — Migration, RLS and compatibility

**Lane disposition:** `IMPLEMENT_D5_MIGRATION_DISCIPLINE`  
**Canonical source anchors:**
- `backend/src/Notrelix.Infrastructure/Data/Migrations/`
- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql`
- `persisted Dashboard/Snapshot/projection configs`

**Primary implementation targets:**
- `new reporting migrations`
- `compatibility readers/backfills`
- `migration CI evidence`

**Lane dependencies:**
- `schema/config decisions from AR-04/06/09/13/14 frozen`

## AR-15-INV-001 — Plan clean/upgrade migration and rebuild strategy per change

**Requirements:** `ARREQ113` — Reporting schema migration has clean and supported-upgrade proof; `ARREQ114` — Projection migration chooses migrate or rebuild explicitly

**Execution disposition:** `IMPLEMENT_D5_MIGRATION_DISCIPLINE`

### Objective

Close `113–114` as one executable unit: plan clean/upgrade migration and rebuild strategy per change. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Infrastructure/Data/Migrations/`
- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql`
- `persisted Dashboard/Snapshot/projection configs`

### Implementation actions

1. For every P6 schema change classify migrate-in-place, rebuild-v2, dual-read/cutover or discard/recompute.
2. Record supported upgrade baseline and retained source/backfill availability.
3. Do not use pending-model warning suppression as evidence.

### Concrete target areas

- `new reporting migrations`
- `compatibility readers/backfills`
- `migration CI evidence`

### Data / migration impact

Creates explicit migration files only after target model is frozen.

### Security / tenant impact

RLS/session policy included in migration plan.

### Cross-team dependencies

- `final schema from AR-04/06/09/13/14`
- `Platform/Data migration policy`
- `schema/config decisions from AR-04/06/09/13/14 frozen`

Primary canonical TEST IDs: `AR-TST-REQ-113`, `AR-TST-REQ-114`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-MIG-001`, `ANA-TST-MIG-003`, `ANA-TST-MIG-002`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-113` and `AR-TST-REQ-114` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ113` and `ARREQ114` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Unknown legacy data is not silently coerced.

## AR-15-CORE-001 — Version Metric/Dashboard/Widget persisted contracts

**Requirements:** `ARREQ115` — Metric semantic changes version public contracts; `ARREQ116` — Persisted Dashboard/Widget configuration is migration-aware

**Execution disposition:** `IMPLEMENT_D5_MIGRATION_DISCIPLINE`

### Objective

Close `115–116` as one executable unit: version metric/dashboard/widget persisted contracts. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Infrastructure/Data/Migrations/`
- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql`
- `persisted Dashboard/Snapshot/projection configs`

### Implementation actions

1. Add compatibility readers/migrations for Metric version references and Dashboard source/widget config schema.
2. Add owner/snapshot lineage/export-job columns with deterministic backfill policy.
3. Retire duplicate widget/source discriminators only after persisted rows are migrated/readable.

### Concrete target areas

- `new reporting migrations`
- `compatibility readers/backfills`
- `migration CI evidence`

### Data / migration impact

Schema/data migration plus model snapshot update.

### Security / tenant impact

Security class/owner backfill must fail closed on ambiguity.

### Cross-team dependencies

- `final schema from AR-04/06/09/13/14`
- `Platform/Data migration policy`
- `schema/config decisions from AR-04/06/09/13/14 frozen`

Primary canonical TEST IDs: `AR-TST-REQ-115`, `AR-TST-REQ-116`.


Legacy stable TEST IDs to preserve/route: `ANA-TST-OAS-001`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-115` and `AR-TST-REQ-116` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ115` and `ARREQ116` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Rollback cannot reinterpret new config under old code.

## AR-15-SEC-001 — Preserve source-version backlog and RLS deployment safety

**Requirements:** `ARREQ117` — Source contract version changes preserve consumer/backfill compatibility; `ARREQ118` — RLS policy deploy order is safe

**Execution disposition:** `IMPLEMENT_D5_MIGRATION_DISCIPLINE`

### Objective

Close `117–118` as one executable unit: preserve source-version backlog and rls deployment safety. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Infrastructure/Data/Migrations/`
- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql`
- `persisted Dashboard/Snapshot/projection configs`

### Implementation actions

1. Keep source event/reporting versions for agreed backlog/replay window.
2. Order table→data backfill→RLS/policy→application cutover so no tenant-broad interval exists.
3. Prove background rebuild/export sessions apply required RLS context.

### Concrete target areas

- `new reporting migrations`
- `compatibility readers/backfills`
- `migration CI evidence`

### Data / migration impact

RLS/index/constraint migrations reviewed together.

### Security / tenant impact

Security regressions block rollout even if app tests pass.

### Cross-team dependencies

- `final schema from AR-04/06/09/13/14`
- `Platform/Data migration policy`
- `schema/config decisions from AR-04/06/09/13/14 frozen`

Primary canonical TEST IDs: `AR-TST-REQ-117`, `AR-TST-REQ-118`.


### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-117` and `AR-TST-REQ-118` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ117` and `ARREQ118` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Consumer version removal requires backlog proof.

## AR-15-COMPAT-001 — Prove historical artifacts and rollback/forward-fix

**Requirements:** `ARREQ119` — Historical artifacts remain readable after code evolution; `ARREQ120` — Rollback/forward-fix preserves analytical meaning

**Execution disposition:** `IMPLEMENT_D5_MIGRATION_DISCIPLINE`

### Objective

Close `119–120` as one executable unit: prove historical artifacts and rollback/forward-fix. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/src/Notrelix.Infrastructure/Data/Migrations/`
- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql`
- `persisted Dashboard/Snapshot/projection configs`

### Implementation actions

1. Run clean DB, supported upgrade, no pending model drift and historical snapshot/config readers.
2. Test forward-fix when rollback is unsafe due to new semantic versions.
3. Record exact migration head in CERT and invalidate on any later migration.

### Concrete target areas

- `new reporting migrations`
- `compatibility readers/backfills`
- `migration CI evidence`

### Data / migration impact

All persisted Analytics-owned artifacts covered.

### Security / tenant impact

No destructive rollback that broadens access or corrupts historical meaning.

### Cross-team dependencies

- `final schema from AR-04/06/09/13/14`
- `Platform/Data migration policy`
- `schema/config decisions from AR-04/06/09/13/14 frozen`

Primary canonical TEST IDs: `AR-TST-REQ-119`, `AR-TST-REQ-120`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-119` and `AR-TST-REQ-120` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ119` and `ARREQ120` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Migration D5 is required for final P6 slice.

# Lane AR-16 — Architecture, TAC reuse, CI and final certification

**Lane disposition:** `VERIFY_AND_CERTIFY_EXACT_RELEASED_SLICE`  
**Canonical source anchors:**
- `backend/tests/Notrelix.Architecture.Tests/`
- `docs/workstreams/executions/backend-team-architecture-closure/`
- `GitHub CI workflows`

**Primary implementation targets:**
- `analytics-reporting.tests.md`
- `analytics-reporting.certification.md`
- `architecture guards for Analytics repositories/source adapters/frontend package`

**Lane dependencies:**
- `all released child lanes ready for candidate certification`

## AR-16-INV-001 — Close architecture boundaries and production ownership

**Requirements:** `ARREQ121` — Architecture gates forbid foreign persistence ownership and new Application EF coupling; `ARREQ122` — Production runtime owner is proved

**Execution disposition:** `VERIFY_AND_CERTIFY_EXACT_RELEASED_SLICE`

### Objective

Close `121–122` as one executable unit: close architecture boundaries and production ownership. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/tests/Notrelix.Architecture.Tests/`
- `docs/workstreams/executions/backend-team-architecture-closure/`
- `GitHub CI workflows`

### Implementation actions

1. Add architecture gates preventing foreign DbContext/private repository dependencies and new Application `DbSet` exposure in P6.
2. Verify actual DI composition for repositories, Metric catalog, consumers, query/report handlers and source adapters.
3. Keep TAC AR-FLOW-01..04 mapped as reference evidence only.

### Concrete target areas

- `analytics-reporting.tests.md`
- `analytics-reporting.certification.md`
- `architecture guards for Analytics repositories/source adapters/frontend package`

### Data / migration impact

No schema.

### Security / tenant impact

Architecture gate includes negative fixture proving forbidden dependency fails.

### Cross-team dependencies

- `all released child lanes`
- `exact accepted candidate`
- `all released child lanes ready for candidate certification`

Primary canonical TEST IDs: `AR-TST-REQ-121`, `AR-TST-REQ-122`.


### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-121` and `AR-TST-REQ-122` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ121` and `ARREQ122` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

A green unit test cannot substitute for production graph.

## AR-16-CORE-001 — Execute required failure and production-runtime scenarios

**Requirements:** `ARREQ123` — TAC AR-FLOW reference is reused without becoming product authority; `ARREQ124` — Required failure paths are executable evidence

**Execution disposition:** `VERIFY_AND_CERTIFY_EXACT_RELEASED_SLICE`

### Objective

Close `123–124` as one executable unit: execute required failure and production-runtime scenarios. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/tests/Notrelix.Architecture.Tests/`
- `docs/workstreams/executions/backend-team-architecture-closure/`
- `GitHub CI workflows`

### Implementation actions

1. TESTS must execute duplicate/stale/out-of-order, crash-before-commit, rebuild/source-unavailable, cross-tenant, forbidden drill-down, stale/partial, migration and query-limit cases where applicable.
2. Production claims use real DB/RLS/broker/browser or provider/storage boundary as needed.
3. Zero-discovery or required skipped scenario blocks certification.

### Concrete target areas

- `analytics-reporting.tests.md`
- `analytics-reporting.certification.md`
- `architecture guards for Analytics repositories/source adapters/frontend package`

### Data / migration impact

Test fixtures only; no production workaround branch.

### Security / tenant impact

Failure evidence must preserve tenant/security semantics.

### Cross-team dependencies

- `all released child lanes`
- `exact accepted candidate`
- `all released child lanes ready for candidate certification`

Primary canonical TEST IDs: `AR-TST-REQ-123`, `AR-TST-REQ-124`.

Legacy stable TEST IDs to preserve/route: `ANA-TST-REL-001`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-123` and `AR-TST-REQ-124` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ123` and `ARREQ124` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Historical CI can be context but not exact-candidate pass.

## AR-16-SEC-001 — Enforce CI non-zero and capability-specific readiness

**Requirements:** `ARREQ125` — CI reports non-zero execution and exact evidence; `ARREQ126` — Readiness targets are capability-specific

**Execution disposition:** `VERIFY_AND_CERTIFY_EXACT_RELEASED_SLICE`

### Objective

Close `125–126` as one executable unit: enforce ci non-zero and capability-specific readiness. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/tests/Notrelix.Architecture.Tests/`
- `docs/workstreams/executions/backend-team-architecture-closure/`
- `GitHub CI workflows`

### Implementation actions

1. CI publishes discovered/executed/passed/failed/skipped counts for required suites and exact run/job/artifact.
2. CERT tracks source projection, Metric, Dashboard/API/frontend, snapshot/export, auth/freshness, migration and ops separately.
3. A deferred capability is explicit; it cannot hide a dependency of an advertised surface.

### Concrete target areas

- `analytics-reporting.tests.md`
- `analytics-reporting.certification.md`
- `architecture guards for Analytics repositories/source adapters/frontend package`

### Data / migration impact

No schema.

### Security / tenant impact

Security/migration evidence is mandatory for affected capabilities.

### Cross-team dependencies

- `all released child lanes`
- `exact accepted candidate`
- `all released child lanes ready for candidate certification`

Primary canonical TEST IDs: `AR-TST-REQ-125`, `AR-TST-REQ-126`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-125` and `AR-TST-REQ-126` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ125` and `ARREQ126` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

Aggregate gate cannot be greener than required child record.

## AR-16-COMPAT-001 — Publish team handoffs and final exact-candidate decision

**Requirements:** `ARREQ127` — Cross-team handoffs preserve team authority; `ARREQ128` — P6 completion is an exact released-slice decision

**Execution disposition:** `VERIFY_AND_CERTIFY_EXACT_RELEASED_SLICE`

### Objective

Close `127–128` as one executable unit: publish team handoffs and final exact-candidate decision. The implementation must satisfy the full normative text in SPEC, not only the titles.

### Current source authority

- `backend/tests/Notrelix.Architecture.Tests/`
- `docs/workstreams/executions/backend-team-architecture-closure/`
- `GitHub CI workflows`

### Implementation actions

1. Complete Source→Analytics, Metric and Report handoffs for each released slice with candidate/evidence/invalidation metadata.
2. List exact released Metrics, projections, reports, Dashboard visibility/widget kinds, frontend surfaces and export formats/jobs.
3. Publish invalidation triggers and rollback/forward-fix per capability.

### Concrete target areas

- `analytics-reporting.tests.md`
- `analytics-reporting.certification.md`
- `architecture guards for Analytics repositories/source adapters/frontend package`

### Data / migration impact

No schema.

### Security / tenant impact

Final decision excludes Domain-only/Infrastructure-only/blocked capability from release claims.

### Cross-team dependencies

- `all released child lanes`
- `exact accepted candidate`
- `all released child lanes ready for candidate certification`

Primary canonical TEST IDs: `AR-TST-REQ-127`, `AR-TST-REQ-128`.

### Required proof before PASS

- exact accepted candidate SHA recorded;
- `AR-TST-REQ-127` and `AR-TST-REQ-128` are discovered/executed with non-zero counts when executable;
- positive path and the SPEC-required negative/failure/authorization/migration path are both proven;
- production-composition proof is used whenever the claim depends on DB/RLS/broker/background/browser/storage/runtime wiring;
- source/Metric/report handoff is attached when the unit crosses a bounded-context or product-semantic boundary;
- no unrelated green suite is used as substitute evidence.

### PASS condition

Both `ARREQ127` and `ARREQ128` meet their target contract, all blocking dependencies are at required readiness, and no unresolved source/security/migration debt is hidden by the unit.

### Stop / escalate

P6 completion is exact released-slice certification, never a single unqualified percentage.

# Aggregate release gates

## AR-GATE-001 — Semantic and projection foundation

**Requirements:** `ARREQ001` — Analytics remains derived state; `ARREQ002` — Analytics never hides source mutation; `ARREQ003` — Authority precedence is explicit; `ARREQ004` — Brownfield source posture precedes implementation; `ARREQ005` — Product-source gaps remain explicit; `ARREQ006` — Exact-candidate evidence is mandatory; `ARREQ007` — Consumer-specific readiness replaces one global score; `ARREQ008` — No broad cleanup PR; `ARREQ009` — Every analytical source has an approved semantic owner; `ARREQ010` — Source handoff records stable identity and scope; `ARREQ011` — Analytics does not create private-table source contracts; `ARREQ012` — Source queries are producer-owned public contracts; `ARREQ013` — Source correction and deletion semantics are admitted before projection; `ARREQ014` — Source security classification propagates into Analytics; `ARREQ015` — Source-event compatibility includes backlog and replay; `ARREQ016` — Reference projection does not define all source semantics; `ARREQ017` — Metric identity is stable and versioned; `ARREQ018` — Metric definition has one canonical owner; `ARREQ019` — Displayed precision and comparison basis require defined meaning; `ARREQ020` — Rate and percentage metrics define numerator and denominator; `ARREQ021` — Time-based Metrics define calendar and timezone semantics; `ARREQ022` — Metric corrections are explicit; `ARREQ023` — Zero, no data, unknown, unavailable and unauthorized are distinct; `ARREQ024` — Breaking Metric meaning requires historical-comparability policy; `ARREQ025` — Projection names its authoritative source and derived identity; `ARREQ026` — Projection update is idempotent; `ARREQ027` — Ordering is the smallest semantics-required scope; `ARREQ028` — Late and out-of-order facts cannot regress projection state; `ARREQ029` — Projection failure commits no false checkpoint; `ARREQ030` — Projection normal reads are local; `ARREQ031` — Projection rebuild and backfill are operationally bounded; `ARREQ032` — Backfill and live traffic converge without double counting; `ARREQ033` — Work placement reference keeps producer revision authority; `ARREQ034` — Work placement live consumers use event facts when sufficient; `ARREQ035` — Work placement incomplete events use the producer public source; `ARREQ036` — Work placement local query remains Analytics-owned; `ARREQ037` — Work placement rebuild preserves newer live facts; `ARREQ038` — Work placement crash recovery remains atomic; `ARREQ039` — Reference projection is internally consumable but not falsely exposed as a product report; `ARREQ040` — Reference projection exact-candidate proof is reused correctly

**Required lanes:** `AR-01`, `AR-02`, `AR-03`, `AR-04`, `AR-05`

Primary canonical TEST IDs: `AR-TST-REQ-001`, `AR-TST-REQ-002`, `AR-TST-REQ-003`, `AR-TST-REQ-004`, `AR-TST-REQ-005`, `AR-TST-REQ-006`, `AR-TST-REQ-007`, `AR-TST-REQ-008`, `AR-TST-REQ-009`, `AR-TST-REQ-010`, `AR-TST-REQ-011`, `AR-TST-REQ-012`, `AR-TST-REQ-013`, `AR-TST-REQ-014`, `AR-TST-REQ-015`, `AR-TST-REQ-016`, `AR-TST-REQ-017`, `AR-TST-REQ-018`, `AR-TST-REQ-019`, `AR-TST-REQ-020`, `AR-TST-REQ-021`, `AR-TST-REQ-022`, `AR-TST-REQ-023`, `AR-TST-REQ-024`, `AR-TST-REQ-025`, `AR-TST-REQ-026`, `AR-TST-REQ-027`, `AR-TST-REQ-028`, `AR-TST-REQ-029`, `AR-TST-REQ-030`, `AR-TST-REQ-031`, `AR-TST-REQ-032`, `AR-TST-REQ-033`, `AR-TST-REQ-034`, `AR-TST-REQ-035`, `AR-TST-REQ-036`, `AR-TST-REQ-037`, `AR-TST-REQ-038`, `AR-TST-REQ-039`, `AR-TST-REQ-040`.

### Exit condition

Authority/source/Metric/projection reference is deterministic and no foreign persistence shortcut exists.

### Gate evidence

- all child work units PASS or an explicitly non-required/deferred capability has a valid authority-backed disposition;
- no required child capability is BLOCKED;
- exact candidate tests report non-zero discovered/executed counts;
- required security/migration/production-runtime evidence is attached;
- gate status cannot be greener than its least-ready required child.

## AR-GATE-002 — Dashboard, security, freshness and history

**Requirements:** `ARREQ041` — Dashboard is user-managed Analytics configuration; `ARREQ042` — Private Dashboard has explicit owner; `ARREQ043` — Released Dashboard visibility is Governance-backed; `ARREQ044` — Dashboard archive/delete is non-destructive; `ARREQ045` — Dashboard Source references stable analytical/source contracts; `ARREQ046` — DashboardWidgetType is the canonical P6 widget-kind vocabulary; `ARREQ047` — Widget configuration is typed and versioned by widget kind; `ARREQ048` — Widget layout never changes source ordering; `ARREQ049` — Analytics request scope is explicit Account and Workspace; `ARREQ050` — Dashboard visibility does not grant source permission; `ARREQ051` — Aggregate visibility does not imply drill-down visibility; `ARREQ052` — Aggregation does not erase confidentiality; `ARREQ053` — Cross-tenant analytics is privileged and explicit; `ARREQ054` — Authorization-sensitive caching is partitioned; `ARREQ055` — Derived data inherits security and data-location obligations; `ARREQ056` — RLS is defense in depth, not the sole report authorization policy; `ARREQ057` — Every released Metric/report has a freshness class; `ARREQ058` — Freshness metadata is queryable; `ARREQ059` — Stale, unavailable and not-yet-projected are distinct; `ARREQ060` — Multi-source reports declare completeness and cutoff strategy; `ARREQ061` — Analytics failure does not block unrelated transactions; `ARREQ062` — Realtime is freshness, not Analytics authority; `ARREQ063` — Initial P6 certification does not require Analytics realtime; `ARREQ064` — Any Analytics realtime path is gap-recoverable; `ARREQ065` — Reporting Snapshot is a derived report artifact, not transactional Domain authority; `ARREQ066` — Reporting Snapshot leaves the Domain LegacyGap classification; `ARREQ067` — Snapshot identity contains reproducibility lineage; `ARREQ068` — Snapshot schema readers are version-aware; `ARREQ069` — Snapshot and current rebuilt projection may legitimately differ; `ARREQ070` — Historical comparisons preserve Metric compatibility; `ARREQ071` — Source deletion follows explicit derived-data policy; `ARREQ072` — Snapshot/export retention and access are explicit

**Required lanes:** `AR-06`, `AR-07`, `AR-08`, `AR-09`

Primary canonical TEST IDs: `AR-TST-REQ-041`, `AR-TST-REQ-042`, `AR-TST-REQ-043`, `AR-TST-REQ-044`, `AR-TST-REQ-045`, `AR-TST-REQ-046`, `AR-TST-REQ-047`, `AR-TST-REQ-048`, `AR-TST-REQ-049`, `AR-TST-REQ-050`, `AR-TST-REQ-051`, `AR-TST-REQ-052`, `AR-TST-REQ-053`, `AR-TST-REQ-054`, `AR-TST-REQ-055`, `AR-TST-REQ-056`, `AR-TST-REQ-057`, `AR-TST-REQ-058`, `AR-TST-REQ-059`, `AR-TST-REQ-060`, `AR-TST-REQ-061`, `AR-TST-REQ-062`, `AR-TST-REQ-063`, `AR-TST-REQ-064`, `AR-TST-REQ-065`, `AR-TST-REQ-066`, `AR-TST-REQ-067`, `AR-TST-REQ-068`, `AR-TST-REQ-069`, `AR-TST-REQ-070`, `AR-TST-REQ-071`, `AR-TST-REQ-072`.

### Exit condition

Dashboard/config/snapshot surfaces are authorized, tenant-safe, truthful about freshness and migration-safe.

### Gate evidence

- all child work units PASS or an explicitly non-required/deferred capability has a valid authority-backed disposition;
- no required child capability is BLOCKED;
- exact candidate tests report non-zero discovered/executed counts;
- required security/migration/production-runtime evidence is attached;
- gate status cannot be greener than its least-ready required child.

## AR-GATE-003 — Source onboarding and product delivery

**Requirements:** `ARREQ073` — WorkManagement analytics consumes canonical Work facts; `ARREQ074` — Documents analytics minimizes content exposure; `ARREQ075` — Collaboration analytics remains distinct from Activity/Audit; `ARREQ076` — Automation analytics preserves Automation execution semantics; `ARREQ077` — Integrations analytics preserves provider/sync semantics; `ARREQ078` — Billing analytics never becomes commercial authority; `ARREQ079` — Account/workspace metrics use canonical Workspace/Identity facts and legacy usage models remain no-growth; `ARREQ080` — Each source context is certified independently; `ARREQ081` — Cross-context report composes approved derived/source contracts; `ARREQ082` — Cross-context joins use stable shared identity; `ARREQ083` — Cross-context consistency is not overstated; `ARREQ084` — Authorization is the intersection of participating sources; `ARREQ085` — Cross-context correction propagates deterministically; `ARREQ086` — Partial multi-source results are explicit; `ARREQ087` — Cross-context cache identity includes every semantic dimension; `ARREQ088` — No synchronous transactional dependency on Analytics; `ARREQ089` — Report/Metric API is typed and bounded; `ARREQ090` — Dashboard Application/API lifecycle is implemented before release; `ARREQ091` — No direct HTTP source-table analytics; `ARREQ092` — Frontend uses server-owned Metric semantics; `ARREQ093` — Workspace operational dashboard is not Analytics product evidence; `ARREQ094` — P6 frontend has a dedicated Analytics contract boundary; `ARREQ095` — Frontend query keys partition tenant and report semantics; `ARREQ096` — Frontend state distinguishes zero/no-data/stale/partial/forbidden/unavailable; `ARREQ097` — Export is a protected Analytics operation; `ARREQ098` — Export reuses the same query semantics as interactive reporting; `ARREQ099` — Large export uses asynchronous report job semantics; `ARREQ100` — Export artifact captures a source cutoff; `ARREQ101` — Export download remains authorized; `ARREQ102` — Export artifact contains no hidden sensitive dimensions; `ARREQ103` — Export job retries cannot duplicate artifacts/effects unsafely; `ARREQ104` — Export and scheduled-report delivery are independently certifiable

**Required lanes:** `AR-10`, `AR-11`, `AR-12`, `AR-13`

Primary canonical TEST IDs: `AR-TST-REQ-073`, `AR-TST-REQ-074`, `AR-TST-REQ-075`, `AR-TST-REQ-076`, `AR-TST-REQ-077`, `AR-TST-REQ-078`, `AR-TST-REQ-079`, `AR-TST-REQ-080`, `AR-TST-REQ-081`, `AR-TST-REQ-082`, `AR-TST-REQ-083`, `AR-TST-REQ-084`, `AR-TST-REQ-085`, `AR-TST-REQ-086`, `AR-TST-REQ-087`, `AR-TST-REQ-088`, `AR-TST-REQ-089`, `AR-TST-REQ-090`, `AR-TST-REQ-091`, `AR-TST-REQ-092`, `AR-TST-REQ-093`, `AR-TST-REQ-094`, `AR-TST-REQ-095`, `AR-TST-REQ-096`, `AR-TST-REQ-097`, `AR-TST-REQ-098`, `AR-TST-REQ-099`, `AR-TST-REQ-100`, `AR-TST-REQ-101`, `AR-TST-REQ-102`, `AR-TST-REQ-103`, `AR-TST-REQ-104`.

### Exit condition

Only certified source/report slices are exposed through API/frontend/export; blocked sources stay blocked.

### Gate evidence

- all child work units PASS or an explicitly non-required/deferred capability has a valid authority-backed disposition;
- no required child capability is BLOCKED;
- exact candidate tests report non-zero discovered/executed counts;
- required security/migration/production-runtime evidence is attached;
- gate status cannot be greener than its least-ready required child.

## AR-GATE-004 — Scale, migration and final certification

**Requirements:** `ARREQ105` — Enterprise dashboard requests do not full-scan arbitrary JSON; `ARREQ106` — Storage technology follows measured need; `ARREQ107` — Index/partition/pre-aggregation is metric-driven; `ARREQ108` — Analytics cache keys are semantically complete; `ARREQ109` — Data-quality checks detect projection divergence; `ARREQ110` — Critical Metrics have an approved reconciliation source; `ARREQ111` — Operational observability exposes lag/backfill/report health; `ARREQ112` — Operational telemetry is not product Analytics; `ARREQ113` — Reporting schema migration has clean and supported-upgrade proof; `ARREQ114` — Projection migration chooses migrate or rebuild explicitly; `ARREQ115` — Metric semantic changes version public contracts; `ARREQ116` — Persisted Dashboard/Widget configuration is migration-aware; `ARREQ117` — Source contract version changes preserve consumer/backfill compatibility; `ARREQ118` — RLS policy deploy order is safe; `ARREQ119` — Historical artifacts remain readable after code evolution; `ARREQ120` — Rollback/forward-fix preserves analytical meaning; `ARREQ121` — Architecture gates forbid foreign persistence ownership and new Application EF coupling; `ARREQ122` — Production runtime owner is proved; `ARREQ123` — TAC AR-FLOW reference is reused without becoming product authority; `ARREQ124` — Required failure paths are executable evidence; `ARREQ125` — CI reports non-zero execution and exact evidence; `ARREQ126` — Readiness targets are capability-specific; `ARREQ127` — Cross-team handoffs preserve team authority; `ARREQ128` — P6 completion is an exact released-slice decision

**Required lanes:** `AR-14`, `AR-15`, `AR-16`

Primary canonical TEST IDs: `AR-TST-REQ-105`, `AR-TST-REQ-106`, `AR-TST-REQ-107`, `AR-TST-REQ-108`, `AR-TST-REQ-109`, `AR-TST-REQ-110`, `AR-TST-REQ-111`, `AR-TST-REQ-112`, `AR-TST-REQ-113`, `AR-TST-REQ-114`, `AR-TST-REQ-115`, `AR-TST-REQ-116`, `AR-TST-REQ-117`, `AR-TST-REQ-118`, `AR-TST-REQ-119`, `AR-TST-REQ-120`, `AR-TST-REQ-121`, `AR-TST-REQ-122`, `AR-TST-REQ-123`, `AR-TST-REQ-124`, `AR-TST-REQ-125`, `AR-TST-REQ-126`, `AR-TST-REQ-127`, `AR-TST-REQ-128`.

### Exit condition

Performance/data quality, migrations, architecture/CI and exact-candidate evidence close the released P6 slice.

### Gate evidence

- all child work units PASS or an explicitly non-required/deferred capability has a valid authority-backed disposition;
- no required child capability is BLOCKED;
- exact candidate tests report non-zero discovered/executed counts;
- required security/migration/production-runtime evidence is attached;
- gate status cannot be greener than its least-ready required child.

# Cross-team handoff execution

The team-authoritative schemas from SPEC are used as implementation inputs, not retrospective documentation.

## Source → Analytics execution handoff

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
Candidate SHA:
Source posture:
Breaking/additive:
Migration/backfill:
Evidence locator:
Known debt/blocker:
Invalidation trigger:
Rollback/forward-fix:
```

## Metric execution handoff

```text
Metric ID:
Metric semantic version:
Product meaning:
Source facts:
Aggregation:
Time basis:
Timezone:
Filters:
Authorization:
Freshness:
Correction behavior:
Historical comparability:
Null/zero/unknown behavior:
Source cutoff:
Tests:
Candidate SHA:
Evidence locator:
Invalidation trigger:
```

## Report execution handoff

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
Completeness/cutoff strategy:
Drill-down authorization:
Cache identity:
Tests:
Candidate SHA:
Evidence locator:
Known debt:
Invalidation trigger:
```

# Safe parallelization

After `AR-GATE-001` foundation is stable:

- independent source projections with already-approved handoffs may run in parallel;
- Dashboard Application/API and dedicated frontend shell may run in parallel against frozen contracts;
- snapshot migration and Metric registry tests may run in parallel once schema/identity are frozen;
- export implementation may run in parallel only for a report that explicitly admitted export;
- performance/index work begins after representative query workload exists.

Do **not** parallelize teams to invent independently:

- the same Metric formula;
- different timezone/bucket semantics;
- source private-table reads;
- multiple widget/public vocabularies;
- incompatible freshness states;
- independent report authorization models;
- separate projection identity/retry mechanisms.

# Migration order

For persisted P6 changes, preferred deployment order is:

```text
1. additive schema / indexes / owner-lineage columns
2. compatibility readers + dual-shape support
3. data backfill/rebuild with tenant/RLS context
4. exact reconciliation / validation
5. API/frontend producer moves to new shape
6. remove old writer/read shape only after backlog/consumer window closes
7. tighten constraints/RLS where additive migration could not do so safely earlier
```

Never create an interval where reporting data is less tenant-protected than before.

# Final implementation stop conditions

Stop and escalate rather than locally choose a shortcut when:

- a Metric formula/source fact is not product-authorized;
- a source required by Analytics is available only through private persistence;
- a new `PermissionAction` or global security model is needed;
- a projection cannot be rebuilt/reconciled to the promised fidelity;
- Public Dashboard/share semantics are proposed;
- a warehouse, search cluster, OLAP engine or new service is proposed;
- export requires a new global artifact-storage/security model;
- source retention/data-residency must change;
- Analytics would become a synchronous transactional dependency;
- Application must expose new EF Core `DbSet`/provider primitives;
- a change weakens RLS/tenant/privacy guarantees;
- historical Metric/config/snapshot data would be reinterpreted silently.

# PLAN completion contract

`analytics-reporting.plan.md` is complete when:

```text
64 / 64 work unit IDs are unique
ARREQ001..ARREQ128 are all routed
AR-TST-REQ-001..128 are all routed
37 legacy ANA-TST IDs are preserved/routed
AR-GATE-001..004 cover ARREQ001..128 without gaps
AR-FLOW-01..04 remain explicit reference dependencies
no implementation unit contains unresolved option language or authorizes private-persistence shortcuts
```

The canonical `analytics-reporting.tests.md` is synchronized with this PLAN. It must turn every routed `AR-TST-REQ-*` into a concrete Given/When/Then scenario and preserve the legacy `ANA-TST-*` aliases.
