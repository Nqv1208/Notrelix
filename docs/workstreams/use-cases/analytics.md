---
document_id: WRK-UC-ANALYTICS
document_type: workstream-use-case-catalog
status: active
owner: analytics-reporting-team
applies_to:
  - analytics
  - use-case-catalog
  - capability-acceptance
evidence:
  - docs/workstreams/use-cases/README.md
  - docs/product/analytics.md
  - docs/workstreams/teams/analytics-reporting.md
  - docs/workstreams/executions/analytics-reporting/analytics-reporting.spec.md
  - docs/workstreams/executions/analytics-reporting/analytics-reporting.tests.md
  - docs/workstreams/executions/analytics-reporting/analytics-reporting.certification.md
  - backend/src/Notrelix.API/Endpoints/Analytics/
  - backend/src/Notrelix.Application/Features/Analytics/
  - backend/src/Notrelix.Domain/Analytics/
  - frontend/packages/features/workspace/src/web/reports/
review_on:
  - metric-definition-change
  - dashboard-or-widget-change
  - projection-or-freshness-change
  - export-or-schedule-change
  - capability-state-change
---

# Use cases: Analytics and Reporting

Product meaning: [analytics](../../product/analytics.md). Team: [analytics-reporting](../teams/analytics-reporting.md). Execution pack: [spec](../executions/analytics-reporting/analytics-reporting.spec.md), [tests](../executions/analytics-reporting/analytics-reporting.tests.md), [certification](../executions/analytics-reporting/analytics-reporting.certification.md). Context code: ANA. Requirement ids cited below are `ARREQ001` to `ARREQ128` of the spec and its gap ids `AR-GAP-01` to `AR-GAP-18`. Rule ids are the `ANA` rules of the product doc, written as `ANA` plus a number in the Rules line of each use case.

# 1. Assessment

## 1.1 Capability map

Letters follow the capability list of the team spec (section 5), in order: ANA-001 is A, ANA-002 is B, and so on to ANA-024 which is X.

| Letter | Capability | Use cases | DEFINED | IMPLEMENTING | INTEGRATING | HARDENING | DONE | BLOCKED |
|---|---|---|---|---|---|---|---|---|
| A | Source-event/reporting-contract inventory | 2 | 1 | 1 | 0 | 0 | 0 | 0 |
| B | Metric definition registry | 2 | 2 | 0 | 0 | 0 | 0 | 0 |
| C | Analytical projection foundation | 2 | 0 | 2 | 0 | 0 | 0 | 0 |
| D | Projection identity/idempotency | 1 | 0 | 1 | 0 | 0 | 0 | 0 |
| E | Projection ordering/late-event policy | 1 | 0 | 1 | 0 | 0 | 0 | 0 |
| F | Projection rebuild/backfill | 2 | 1 | 1 | 0 | 0 | 0 | 0 |
| G | Account/workspace metrics | 2 | 1 | 0 | 0 | 0 | 0 | 1 |
| H | WorkManagement analytics | 5 | 1 | 0 | 2 | 2 | 0 | 0 |
| I | Documents/Collaboration analytics | 2 | 2 | 0 | 0 | 0 | 0 | 0 |
| J | Automation/Integrations analytics | 2 | 2 | 0 | 0 | 0 | 0 | 0 |
| K | Billing/usage analytics | 1 | 1 | 0 | 0 | 0 | 0 | 0 |
| L | Cross-context report composition | 1 | 1 | 0 | 0 | 0 | 0 | 0 |
| M | Report/query API (dashboard lifecycle) | 10 | 1 | 9 | 0 | 0 | 0 | 0 |
| N | Dashboard/report frontend | 4 | 0 | 2 | 1 | 1 | 0 | 0 |
| O | Filters/date/timezone semantics | 2 | 1 | 0 | 1 | 0 | 0 | 0 |
| P | Freshness/staleness contract | 3 | 1 | 1 | 1 | 0 | 0 | 0 |
| Q | Export | 3 | 2 | 1 | 0 | 0 | 0 | 0 |
| R | Authorization/visibility | 4 | 2 | 1 | 0 | 1 | 0 | 0 |
| S | Retention/data lifecycle | 4 | 3 | 1 | 0 | 0 | 0 | 0 |
| T | Performance/storage strategy | 1 | 0 | 1 | 0 | 0 | 0 | 0 |
| U | Observability | 1 | 1 | 0 | 0 | 0 | 0 | 0 |
| V | Data quality/reconciliation | 1 | 1 | 0 | 0 | 0 | 0 | 0 |
| W | Migration/versioning | 3 | 3 | 0 | 0 | 0 | 0 | 0 |
| X | Hardening | 2 | 1 | 0 | 0 | 0 | 0 | 1 |
| | Total | 61 | 28 | 22 | 5 | 4 | 0 | 2 |

No use case is DONE: none has an owner check or a 1-px design diff. HARDENING means a `frontend/e2e/real` spec runs it: only `work-report.real.e2e.spec.ts` exists for this context, so only the board and workspace work report (H01, H02, N01, R01) are HARDENING. IMPLEMENTING is used for code that cannot run end to end: the Dashboard, Widget and Snapshot aggregates (Domain and EF only), the placement projection (consumers and rebuild, no HTTP), and the report builder UI that runs only on the mock backend (`backendCapabilities.reports` is `mockApi`).

## 1.2 What exists today

- Two read-only endpoints serve a work report: `GET /api/v1/boards/{boardId}/report` and `GET /api/v1/workspaces/{workspaceId}/work-report`. Work Management aggregates the figures (`IWorkReportFacts`); Analytics filters by Governance visibility and maps names. There is no Metric registry, no stored Dashboard API, no export, no snapshot API.
- Domain has `Dashboard`, `DashboardSource`, `DashboardWidget`, `ReportingSnapshot` and rules `AnalyticsRuleCodes`, with Domain tests and EF persistence and RLS. No Application handler or endpoint uses them (`AR-GAP-02`).
- One production-hardened projection: `WorkspaceWorkItemPlacementProjection`, fed by three consumers, rebuilt by `RebuildWorkspacePlacementsCommand`, read by `GetWorkspacePlacementsQuery` (internal, no route).
- Frontend: `/workspaces/$workspaceId/reports` shows the real work report (`work-report-page.tsx`); the list, builder, drill-down, export and sharing pages in `frontend/packages/features/workspace/src/web/reports/` run only on the mock backend (`frontend/packages/dev/mock-backend/src/contexts/analytics/reports.handlers.ts`).

## 1.3 The ten most important gaps

1. No dashboard or widget API exists, although the Domain and the mock UI exist; users cannot save a dashboard (`AR-GAP-02`, `AR-GAP-09`). Use cases M01 to M10.
2. No Metric registry or stable Metric identity: the two reports compute figures inside Work Management with no Metric id or version (`AR-GAP-01`). B01, B02, H05.
3. No export of any kind on the real backend; the mock export is the only behaviour (`AR-GAP-11`). Q01 to Q03.
4. Freshness is only a `generatedAt` stamp; there is no source cutoff, lag or degraded state, so an async figure cannot say how old it is (`AR-GAP-13`). P01, P02.
5. Dashboard ownership and visibility are incomplete: `Dashboard` has no owner user id and its `Public` visibility has no share contract (`AR-GAP-03`). M09.
6. Two widget vocabularies coexist: `DashboardWidgetType` and `Widgets.WidgetType`, and widget config is only checked for JSON kind (`AR-GAP-04`, `AR-GAP-05`). M06, W02, W03.
7. `ReportingSnapshot` is an architecture legacy gap with no lineage (Metric version, source cutoff) and no retention lifecycle (`AR-GAP-06`, `AR-GAP-15`). S01 to S04.
8. Only Work Management feeds an analytical projection; Documents, Collaboration, Automation, Integrations and Billing have none (`AR-GAP-08`). I01 to K01, L01.
9. No Analytics observability or data-quality signal: no projection lag, divergence or report latency metric (`AR-GAP-16`). U01, V01.
10. The workspace report is authorized with `ViewWorkspace` (and `ManageSpaces` for a space), not with a report permission from Governance; there is no read-only "report viewer" decision (ARREQ049). R01, H03.

## 1.4 Defects in shipped code

- Missing report permission: `backend/src/Notrelix.Application/Features/Analytics/Reports/Queries/GetWorkspaceReport/GetWorkspaceReport.cs` uses `PermissionAction.ViewWorkspace`; `PermissionAction` has no report action. A guest with workspace view reads aggregate counts of the boards visible to them only because the handler filters by visibility. Acceptable today, fragile when more sources join.
- Missing authorization pipeline on the internal placement query and command: `GetWorkspacePlacements.cs` and `RebuildWorkspacePlacements.cs` carry `IRequirePermission`, but no endpoint maps them; the rebuild has no operator trigger and no audit (dead code from the product point of view).
- Unbounded read risk: `GetWorkspacePlacementsQuery` returns every placement row of a workspace as a list with no page or limit (`Placements/Queries/GetWorkspacePlacements/GetWorkspacePlacements.cs`). It is internal now; exposing it as is would be an unbounded read.
- Mock-only frontend paths: `report-list-surface.tsx`, `report-dashboard-surface.tsx`, `report-widgets.tsx`, `report-dialogs.tsx`, `report-drill-sheet.tsx` and `reports-api.ts` call operations (`reports.list`, `reports.create`, `reports.widgets.save`, `reports.export.start`, and so on) that have no producer; on the real backend the page falls back to the work report (`frontend/apps/web/src/routes/workspaces/$workspaceId/reports.tsx`).
- No expected version on competing writes: the Dashboard aggregate has a version but no handler passes it; a future update API must require it (ARREQ044).
- No idempotency: no mutation exists, so a future create, export start or refresh must carry an idempotency key (ARREQ097).
- Dead code: `WorkspaceUsageDaily` and `FeatureUsageDaily` persistence types have no producer (`AR-GAP-07`, no-growth by the spec).
- Report period is clamped by validators (`WorkReportPeriod`, 1 to 26 weeks), which is correct; the response always buckets weeks in UTC, which the UI does not disclose.

## 1.5 Enterprise baseline

Searched 2026-10-30. The web search returned mostly third-party pages, so only two sources are cited and the rest is stated as product knowledge to confirm against each vendor help centre.

- monday.com dashboards: a no-code widget library (charts, numbers, Gantt), dashboards across boards, PDF snapshot and scheduled email of a dashboard. Source: [monday.com dashboards](https://monday.com/features/dashboards-mobile) and a third-party scheduling note at [eazyBI](https://docs.eazybi.com/eazybi/data-import/data-from-monday-com).
- Jira, Asana and Smartsheet (not verified by this search): saved dashboards with private and shared visibility, gadget or widget filters, CSV export of a report, scheduled report email, permission-filtered figures (a viewer sees only what they may open), and drill-through from a number to its items.

Table-stakes use cases the product docs do not list, added as "(enterprise baseline)", state DEFINED: duplicate a dashboard (M10), schedule a recurring report (Q03), choose a report period (H04, already built), export to CSV with the same authorization as the screen (Q01).

# 2. Index

| ID | Use case | Capability | Actors | BE | FE | Design | Real e2e | State | Gap |
|---|---|---|---|---|---|---|---|---|---|
| UC-ANA-A01 | Register an analytical source contract | A | Source context team, Analytics team | no | no | n/a | no | DEFINED | no source handoff record |
| UC-ANA-A02 | Receive Work Management placement facts | A | Platform consumer | yes | n/a | n/a | no | IMPLEMENTING | only Work source |
| UC-ANA-B01 | Publish a Metric definition version | B | Analytics team | no | no | n/a | no | DEFINED | no registry |
| UC-ANA-B02 | Browse the Metric catalog | B | Workspace member | no | no | Final-Reports | no | DEFINED | no registry |
| UC-ANA-C01 | Keep a projection current from source events | C | Platform consumer | yes | n/a | n/a | no | IMPLEMENTING | one projection |
| UC-ANA-C02 | Read a projection locally | C | Application | yes | n/a | n/a | no | IMPLEMENTING | no route, unbounded |
| UC-ANA-D01 | A duplicate or replayed fact has no second effect | D | Platform consumer | yes | n/a | n/a | no | IMPLEMENTING | one projection |
| UC-ANA-E01 | A late or out-of-order fact is ignored | E | Platform consumer | yes | n/a | n/a | no | IMPLEMENTING | one projection |
| UC-ANA-F01 | Rebuild a workspace placement projection | F | Operator | yes | no | n/a | no | IMPLEMENTING | no trigger, no audit |
| UC-ANA-F02 | Backfill a projection beside live traffic | F | Operator | no | no | n/a | no | DEFINED | no cutover rule built |
| UC-ANA-G01 | Workspace activity and membership metrics | G | Workspace admin | no | no | Final-Reports | no | DEFINED | no source handoff |
| UC-ANA-G02 | Feature and workspace usage rollups | G | Workspace admin | no | no | none | no | BLOCKED | usage metric undecided |
| UC-ANA-H01 | Read a board work report | H | Board reader | yes | yes | Final-Reports | yes | HARDENING | design diff, states |
| UC-ANA-H02 | Read the workspace work report | H | Workspace member | yes | yes | Final-Reports | yes | HARDENING | design diff, states |
| UC-ANA-H03 | Narrow the workspace report to a department space | H | Workspace owner, admin | yes | yes | Final-Reports | no | INTEGRATING | no real e2e for space |
| UC-ANA-H04 | Choose the report period in weeks | H | Workspace member | yes | yes | Final-Reports | no | INTEGRATING | no real e2e for weeks |
| UC-ANA-H05 | Count work items by placement (work-item-count v1) | H | Workspace member | partial | no | Final-Reports | no | DEFINED | no Metric api |
| UC-ANA-I01 | Documents activity report | I | Workspace member | no | no | none | no | DEFINED | no projection |
| UC-ANA-I02 | Collaboration activity report | I | Workspace member | no | no | none | no | DEFINED | no projection |
| UC-ANA-J01 | Automation run outcome report | J | Workspace admin | no | no | none | no | DEFINED | no projection |
| UC-ANA-J02 | Integration sync health report | J | Workspace admin | no | no | none | no | DEFINED | no projection |
| UC-ANA-K01 | Seat and usage report from Billing facts | K | Workspace owner | no | no | none | no | DEFINED | no source contract |
| UC-ANA-L01 | Compose a multi-source report with completeness | L | Workspace member | no | no | none | no | DEFINED | no composer |
| UC-ANA-M01 | List the dashboards and reports I may open | M | Workspace member | no | mock | Final-Reports | no | IMPLEMENTING | no endpoint |
| UC-ANA-M02 | Open one report or dashboard | M | Workspace member | no | mock | Final-Reports | no | IMPLEMENTING | no endpoint |
| UC-ANA-M03 | Create a dashboard | M | Workspace member | domain | mock | Final-Reports | no | IMPLEMENTING | no handler |
| UC-ANA-M04 | Rename a dashboard | M | Dashboard owner | domain | mock | Final-Reports | no | IMPLEMENTING | no handler |
| UC-ANA-M05 | Archive, restore or delete a dashboard | M | Dashboard owner | domain | mock | Final-Reports | no | IMPLEMENTING | no handler, no restore rule |
| UC-ANA-M06 | Add and configure a widget | M | Dashboard owner | domain | mock | Final-Reports | no | IMPLEMENTING | untyped config |
| UC-ANA-M07 | Arrange widgets by move and resize | M | Dashboard owner | domain | mock | Final-Reports | no | IMPLEMENTING | no handler |
| UC-ANA-M08 | Remove a widget | M | Dashboard owner | domain | mock | Final-Reports | no | IMPLEMENTING | no handler |
| UC-ANA-M09 | Share a dashboard privately or with the workspace | M | Dashboard owner | domain | mock | Final-Reports | no | IMPLEMENTING | no owner id, no handler |
| UC-ANA-M10 | Duplicate a dashboard (enterprise baseline) | M | Workspace member | no | no | none | no | DEFINED | not specified |
| UC-ANA-N01 | Work report page on the real backend | N | Workspace member | yes | yes | Final-Reports | yes | HARDENING | states, design diff |
| UC-ANA-N02 | Report list and builder pages | N | Workspace member | no | mock | Final-Reports, Final-M-Reports | no | IMPLEMENTING | mock only |
| UC-ANA-N03 | Drill from a figure to the items behind it | N | Board reader | no | mock | Final-Reports | no | IMPLEMENTING | no endpoint |
| UC-ANA-N04 | The page says reports are not available | N | Workspace member | n/a | yes | Final-States | partial | INTEGRATING | reports text not asserted |
| UC-ANA-O01 | Filter a report by board, person or space | O | Workspace member | partial | partial | Final-Reports | partial | INTEGRATING | person filter mock only |
| UC-ANA-O02 | Time zone and week boundaries | O | Workspace member | partial | no | none | no | DEFINED | UTC only, undisclosed |
| UC-ANA-P01 | Show when figures were generated | P | Workspace member | partial | partial | Final-Reports | partial | INTEGRATING | no cutoff or lag |
| UC-ANA-P02 | Degraded or unavailable is never shown as zero | P | Workspace member | no | no | Final-States | no | DEFINED | no state in contract |
| UC-ANA-P03 | Recompute a stale widget | P | Dashboard owner | no | mock | Final-Reports | no | IMPLEMENTING | no endpoint |
| UC-ANA-Q01 | Export a report to a file | Q | Workspace member | no | mock | Final-Reports | no | IMPLEMENTING | no endpoint |
| UC-ANA-Q02 | Generate a large export in the background | Q | Workspace member | no | mock | Final-Reports | no | DEFINED | no job runtime |
| UC-ANA-Q03 | Schedule a recurring report (enterprise baseline) | Q | Dashboard owner | no | no | none | no | DEFINED | no owner for delivery |
| UC-ANA-R01 | See only figures from boards I may read | R | Workspace member, guest | yes | yes | Final-Reports | yes | HARDENING | guest case not run e2e |
| UC-ANA-R02 | Grouping by person respects confidentiality | R | Workspace member | no | no | none | no | DEFINED | no rule built |
| UC-ANA-R03 | Derived data stays inside its tenant | R | System | partial | n/a | n/a | no | IMPLEMENTING | no cross-tenant report test |
| UC-ANA-R04 | Cache keys carry tenant, metric version and audience | R | System | no | n/a | n/a | no | DEFINED | no cache yet |
| UC-ANA-S01 | Capture a reporting snapshot | S | System | domain | no | n/a | no | IMPLEMENTING | no lineage, legacy gap |
| UC-ANA-S02 | Read a historical snapshot by schema version | S | Workspace member | no | no | none | no | DEFINED | no read path |
| UC-ANA-S03 | Purge snapshots by retention and privacy | S | System, operator | no | no | n/a | no | DEFINED | no policy |
| UC-ANA-S04 | Handle deletion of a source | S | System | no | no | n/a | no | DEFINED | no rule built |
| UC-ANA-T01 | A report stays bounded on a large workspace | T | Workspace member | yes | no | n/a | no | IMPLEMENTING | no scale proof |
| UC-ANA-U01 | Operator sees projection lag and failures | U | Operator | no | n/a | n/a | no | DEFINED | no signals |
| UC-ANA-V01 | Reconcile a projection with its source | V | Operator | partial | n/a | n/a | no | DEFINED | no divergence report |
| UC-ANA-W01 | Publish a breaking Metric version | W | Analytics team | no | no | n/a | no | DEFINED | no registry |
| UC-ANA-W02 | Migrate widget config and snapshot payload versions | W | System | no | no | n/a | no | DEFINED | no migration path |
| UC-ANA-W03 | Retire the duplicate widget vocabulary | W | Analytics team | domain | mock | n/a | no | DEFINED | two enums |
| UC-ANA-X01 | A live number updates and recovers from a gap | X | Workspace member | no | no | none | no | DEFINED | no producer |
| UC-ANA-X02 | Cross-tenant administrative analytics | X | Platform operator | no | no | none | no | BLOCKED | principal undecided |

Endpoints with no use case: none. The two report routes appear in H01 to H04, N01, O01 and R01. The `/health` routes belong to [platform-foundation](platform-foundation.md).

# 3. Use cases

Shared evidence. Report routes: `GET /api/v1/boards/{boardId}/report` (`GetBoardReportEndpoint.cs`, `ViewBoard`, board not visible is NotFound), `GET /api/v1/workspaces/{workspaceId}/work-report` (`GetWorkspaceReportEndpoint.cs`, `ViewWorkspace`, `ManageSpaces` with `spaceId`). Backend tests: `backend/tests/Notrelix.Application.Tests/Features/Analytics/WorkReportQueryTests.cs`, `backend/tests/Notrelix.Integration.Tests/Analytics/WorkReportFactsIntegrationTests.cs`, `WorkspacePlacementProjectionIntegrationTests.cs`, `ReportingSnapshotPersistenceTests.cs`, Domain tests under `backend/tests/Notrelix.Domain.Tests/Analytics/`. Frontend: `frontend/apps/web/src/routes/workspaces/$workspaceId/reports.tsx`, `frontend/packages/features/workspace/src/web/reports/`. Real e2e: `frontend/e2e/real/work-report.real.e2e.spec.ts`. Mock e2e: `frontend/e2e/mock/reports.mock.e2e.spec.ts`. Artboards: `Final-Reports`, `Final-M-Reports`, `Final-States` of the design canvas.

## Capability A: Source-event/reporting-contract inventory

### UC-ANA-A01 Register an analytical source contract
- Capability: A, ANA-001 Source-event/reporting-contract inventory
- Actors and permission: source context team and Analytics team (a design-time decision, no runtime actor).
- Preconditions: the source context has a public producer contract (query or integration event).
- Input: source id, owner context, stable identity, scope, correction and deletion semantics, security class.
- Output: a recorded source entry that a Metric may name; no runtime effect.
- Main flow: 1. Source team publishes its public contract. 2. Analytics records the handoff. 3. The source is admitted for named Metrics only.
- No-op / rejection / concurrency / retry: a source without owner, scope or deletion semantics is not admitted (ARREQ009, ARREQ013). Private tables are never a source (ARREQ011).
- UI states: none.
- Rules: product ANA005; ARREQ009 to ARREQ016.
- Acceptance: Given a source with no deletion semantics, When it is proposed, Then it is refused as not admitted. Given an admitted source, When a Metric names it, Then the registry accepts the reference.
- Evidence: BE none · FE none · Design n/a · Tests none · Real e2e none
- State: DEFINED — the inventory lives only in the team spec (section 7); it has no machine-readable form.

### UC-ANA-A02 Receive Work Management placement facts
- Capability: A, ANA-001
- Actors and permission: the Platform consumer host acting as the system, with the workspace tenant context restored from the event.
- Preconditions: Work Management published `BoardItemCreated`, `BoardItemMoved` or `BoardItemArchived` V2 with account id, workspace id and revision.
- Input: the integration event envelope.
- Output: a placement row per item in `reporting.workspace_work_item_placements`.
- Main flow: 1. The consumer claims the message. 2. It restores tenant context. 3. `WorkspaceWorkItemPlacementService` writes the row. 4. It completes the claim.
- No-op / rejection / concurrency / retry: a missing account id falls back to the restored tenant; a missing snapshot leaves the projection unchanged and the message retries; a failed projection recovers by replay.
- UI states: none.
- Rules: ARREQ033 to ARREQ040; platform-and-messaging contract for consumers.
- Acceptance: Given a created item, When its event is consumed, Then one placement row exists. Given a consumer failure, When it is retried, Then no duplicate row exists.
- Evidence: BE `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Analytics/WorkItemPlacementConsumers.cs` · FE n/a · Design n/a · Tests `WorkspacePlacementProjectionIntegrationTests.cs` (MovedConsumer, CreatedConsumer, ArchivedConsumer), `backend/tests/Notrelix.Integration.Tests/Messaging/BoardItemMovedPlacementFailureRecoveryRuntimeTests.cs` · Real e2e none
- State: IMPLEMENTING — the consumer path works, but only Work Management feeds Analytics and nothing reads the rows over HTTP.

## Capability B: Metric definition registry

### UC-ANA-B01 Publish a Metric definition version
- Capability: B, ANA-002 Metric definition registry
- Actors and permission: Analytics team (code change reviewed; no user actor).
- Preconditions: the Metric has a semantic owner and an admitted source.
- Input: metric id, version, meaning, source, scope, aggregation, filters, time basis, time zone, freshness, zero rule, authorization rule (frozen shape in spec section 6.1).
- Output: a Metric entry `<id>:v<n>` in the registry.
- Main flow: 1. Author adds the definition. 2. Registry validates required fields. 3. Definition becomes selectable by reports.
- No-op / rejection / concurrency / retry: an unchanged definition is a no-op; a changed meaning needs a new version (W01); two owners for one Metric id are refused.
- UI states: none.
- Rules: product ANA003, ANA004; ARREQ017 to ARREQ024.
- Acceptance: Given a definition without a zero rule, When registered, Then it is rejected. Given id `work-item-count` at v1, When registered twice, Then the second is a no-op.
- Evidence: BE none · FE none · Design n/a · Tests none · Real e2e none
- State: DEFINED — `AR-GAP-01`: no registry in source.

### UC-ANA-B02 Browse the Metric catalog
- Capability: B, ANA-002
- Actors and permission: any workspace member with report access (ViewWorkspace until a report permission exists, see R01).
- Preconditions: at least one Metric is released.
- Input: none, optional search text.
- Output: the list of released Metrics with name, meaning, unit and freshness mode.
- Main flow: 1. User opens the widget picker. 2. Client reads the catalog. 3. User picks a Metric.
- No-op / rejection / concurrency / retry: a Metric whose source the caller cannot read is hidden, not shown empty.
- UI states: loading, empty, success, permission denied, offline.
- Rules: ANA003, ANA027; ARREQ019.
- Acceptance: Given two released Metrics, When a member lists the catalog, Then both appear with their definitions. Given a Metric over a board the caller cannot read, When listed, Then it is absent.
- Evidence: BE none · FE none (mock widget picker `report-dialogs.tsx`) · Design Final-Reports · Tests none · Real e2e none
- State: DEFINED — no catalog endpoint.

## Capability C: Analytical projection foundation

### UC-ANA-C01 Keep a projection current from source events
- Capability: C, ANA-003 Analytical projection foundation
- Actors and permission: Platform consumer (system).
- Preconditions: the projection names its authoritative source and derived key.
- Input: source facts with identity, scope and revision.
- Output: derived rows scoped to one workspace.
- Main flow: 1. Consumer receives a fact. 2. Service compares revision. 3. Row is written under RLS.
- No-op / rejection / concurrency / retry: a fact with the same or older revision changes nothing; a failed write retries the message.
- UI states: none.
- Rules: ANA018; ARREQ025 to ARREQ032.
- Acceptance: Given facts for two workspaces, When consumed, Then each workspace sees only its own rows (`WorkspaceScopes_StayIsolated`).
- Evidence: BE `backend/src/Notrelix.Application/Features/Analytics/Projections/WorkItemPlacement/WorkspaceWorkItemPlacementProjection.cs`, `Placements/Services/WorkspaceWorkItemPlacementService.cs` · FE n/a · Design n/a · Tests `WorkspacePlacementProjectionIntegrationTests.cs` · Real e2e none
- State: IMPLEMENTING — one reference projection; no generic foundation for the others.

### UC-ANA-C02 Read a projection locally
- Capability: C, ANA-003
- Actors and permission: Application (internal); `IRequirePermission` on the query.
- Preconditions: the workspace projection is initialized.
- Input: workspace id.
- Output: list of `WorkItemPlacementDto`.
- Main flow: 1. A report handler calls the query. 2. The query reads only the local table.
- No-op / rejection / concurrency / retry: read only; no producer call.
- UI states: none.
- Rules: ARREQ033; product ANA021.
- Acceptance: Given rows in two workspaces, When one is read, Then only its rows return.
- Evidence: BE `Placements/Queries/GetWorkspacePlacements/GetWorkspacePlacements.cs` (no route) · FE n/a · Design n/a · Tests `GetWorkspacePlacements_ProductionHandler_ReadsLocalScopedPr...` · Real e2e none
- State: IMPLEMENTING — returns every row without a limit; must be bounded before any route exposes it (Part 1.4).

## Capability D: Projection identity/idempotency

### UC-ANA-D01 A duplicate or replayed fact has no second effect
- Capability: D, ANA-004 Projection identity/idempotency
- Actors and permission: Platform consumer (system).
- Preconditions: the fact carries a stable item identity and revision.
- Input: the same event delivered twice, or replayed after a crash.
- Output: one row, unchanged.
- Main flow: 1. Consumer reads the fact. 2. Dedup claim by event and consumer. 3. Service finds equal revision. 4. Nothing is written.
- No-op / rejection / concurrency / retry: duplicate is a semantic no-op; a crash before commit leaves the claim for reclaim.
- UI states: none.
- Rules: ANA019; ARREQ026; PFREQ051 to PFREQ057 (consumer dedup).
- Acceptance: Given a delivered fact, When it is delivered again, Then the row and revision are unchanged.
- Evidence: BE `WorkspaceWorkItemPlacementService.cs` · FE n/a · Design n/a · Tests `StaleOrDuplicateFact_DoesNotRegressProjection`, `Rebuild_Duplicate_IsIdempotent` · Real e2e none
- State: IMPLEMENTING — proven for placement only.

## Capability E: Projection ordering/late-event policy

### UC-ANA-E01 A late or out-of-order fact is ignored
- Capability: E, ANA-005 Projection ordering/late-event policy
- Actors and permission: Platform consumer (system).
- Preconditions: producer revision is the ordering authority.
- Input: a fact whose revision is older than the stored one.
- Output: row keeps the newer state.
- Main flow: 1. Fact arrives late. 2. Service compares revisions. 3. It discards the older fact.
- No-op / rejection / concurrency / retry: concurrent newer live fact during rebuild wins at the database (`Rebuild_ConcurrentNewerLiveFact_ConflictsAtDatabase`).
- UI states: none.
- Rules: ARREQ027; ANA020.
- Acceptance: Given revision 5 stored, When revision 3 arrives, Then revision 5 stays.
- Evidence: BE `WorkspaceWorkItemPlacementService.cs` · FE n/a · Design n/a · Tests `MovedFact_UpdatesProjection_LastWriteWins`, `Rebuild_OlderSnapshot_PreservesNewerLiveState` · Real e2e none
- State: IMPLEMENTING — one projection; the late-event policy for other sources is undefined.

## Capability F: Projection rebuild/backfill

### UC-ANA-F01 Rebuild a workspace placement projection
- Capability: F, ANA-006 Projection rebuild/backfill
- Actors and permission: platform operator (Lead decision: a system principal, see Part 4); today only an Application command.
- Preconditions: Work Management public `IWorkItemProjectionSource` is available.
- Input: workspace id.
- Output: projection equals producer truth; count of rows touched.
- Main flow: 1. Command reads the producer snapshot. 2. Service reconciles rows (insert, update, remove missing). 3. Returns count.
- No-op / rejection / concurrency / retry: producer unavailable fails without mutation and a retry completes; running twice is idempotent; other workspaces are untouched.
- UI states: none (operator tooling).
- Rules: ANA019; ARREQ028, ARREQ033 to ARREQ040.
- Acceptance: Given drifted rows, When rebuilt, Then rows equal the snapshot. Given the producer is down, When rebuilt, Then nothing changes.
- Evidence: BE `Placements/Commands/RebuildWorkspacePlacements/RebuildWorkspacePlacements.cs` (no route) · FE n/a · Design n/a · Tests `Rebuild_*` in `WorkspacePlacementProjectionIntegrationTests.cs` · Real e2e none
- State: IMPLEMENTING — no operator trigger and no audit record; the command carries a workspace permission, not a system principal.

### UC-ANA-F02 Backfill a projection beside live traffic
- Capability: F, ANA-006
- Actors and permission: platform operator (system principal).
- Preconditions: a new projection or a changed event schema.
- Input: workspace set, metric version, source cutoff.
- Output: projection complete up to the cutoff, live consumer continues.
- Main flow: 1. Operator starts backfill with a cutoff. 2. Backfill writes by revision. 3. Live consumer keeps writing. 4. Verify counts.
- No-op / rejection / concurrency / retry: revision compare prevents double count; resumable after a crash.
- UI states: none.
- Rules: ANA020; ARREQ028.
- Acceptance: Given live events during backfill, When both finish, Then each item appears once with the newest revision.
- Evidence: BE none · FE n/a · Design n/a · Tests none · Real e2e none
- State: DEFINED — rebuild covers one projection only.

## Capability G: Account/workspace metrics

### UC-ANA-G01 Workspace activity and membership metrics
- Capability: G, ANA-007 Account/workspace metrics
- Actors and permission: workspace owner and admin (Governance report permission, to be defined).
- Preconditions: Workspaces and Identity publish member and activity facts.
- Input: period, optional space.
- Output: counts of active members, new members, boards, pages over time.
- Main flow: 1. Admin opens the overview. 2. Metric reads the projection. 3. Figures show with freshness.
- No-op / rejection / concurrency / retry: a member without the permission receives NotFound.
- UI states: loading, empty, success, permission denied, stale.
- Rules: ARREQ073 to ARREQ080; ANA023.
- Acceptance: Given three joins this week, When the admin opens the overview, Then new members is three.
- Evidence: BE none · FE none · Design Final-Reports · Tests none · Real e2e none
- State: DEFINED — no source handoff from Workspaces.

### UC-ANA-G02 Feature and workspace usage rollups
- Capability: G, ANA-007
- Actors and permission: workspace owner.
- Preconditions: a decision on which usage Metric exists.
- Input: period.
- Output: daily usage by feature.
- Main flow: not decided.
- No-op / rejection / concurrency / retry: not decided.
- UI states: not decided.
- Rules: AR-GAP-07; ARREQ079; product ANA022, section 64 (Billing usage is not Analytics usage).
- Acceptance: Given the decision is made, When a rollup runs, Then it is populated by a named producer. Until then no number is shown.
- Evidence: BE persistence types `WorkspaceUsageDaily`, `FeatureUsageDaily` only · FE none · Design none · Tests none · Real e2e none
- State: BLOCKED — decide which usage fact is Analytics-owned and which is Billing-owned, and name the producer; the spec freezes both tables at no growth.

## Capability H: WorkManagement analytics

### UC-ANA-H01 Read a board work report
- Capability: H, ANA-008 WorkManagement analytics
- Actors and permission: board reader (`ViewBoard`).
- Preconditions: the board is visible to the caller.
- Input: board id, weeks (1 to 26, default 8).
- Output: `BoardReportDto`: total, open, done, overdue, by status, by assignee, finished per week, average completion seconds, `generatedAt`.
- Main flow: 1. Handler checks the board is in the caller's visible set. 2. Work Management aggregates. 3. Handler maps assignee names. 4. Client draws the widgets.
- No-op / rejection / concurrency / retry: unreadable or unknown board is the identical NotFound; weeks out of range fail validation; read only, so no version or key.
- UI states: loading, empty (no items), success, permission denied, error with retry, offline.
- Rules: ANA001, ANA023, ANA029; ARREQ049, ARREQ089.
- Acceptance: Given a board of five items, When the reader opens its report, Then total is five and by status sums to five. Given a board the caller cannot see, When requested, Then NotFound.
- Evidence: BE `GET /api/v1/boards/{boardId}/report`, `GetBoardReport.cs` · FE `work-report-page.tsx`, `work-report-surface.tsx` · Design Final-Reports · Tests `WorkReportQueryTests` (board cases), `WorkReportFactsIntegrationTests` · Real e2e `work-report.real.e2e.spec.ts`
- State: HARDENING — runs end to end; missing a 1-px design diff, owner check, a cross-tenant negative test and the states for conflict-free offline and empty.

### UC-ANA-H02 Read the workspace work report
- Capability: H, ANA-008
- Actors and permission: workspace member (`ViewWorkspace`), figures limited to boards the caller can read.
- Preconditions: member of the workspace.
- Input: workspace id, weeks.
- Output: `WorkspaceReportDto` with `boardCount`, merged status by category, per-assignee, per-week, `generatedAt`.
- Main flow: 1. Handler lists visible board ids. 2. Work Management aggregates over them. 3. Names are mapped.
- No-op / rejection / concurrency / retry: a caller with no readable boards gets a zero-board report, not an error; non-member gets NotFound.
- UI states: loading, empty, success, permission denied, error, offline.
- Rules: ANA023, ANA031; ARREQ049, ARREQ089.
- Acceptance: Given three boards, one hidden from the caller, When the member opens the report, Then `boardCount` is two and figures cover only those two.
- Evidence: BE `GET /api/v1/workspaces/{workspaceId}/work-report`, `GetWorkspaceReport.cs` · FE `work-report-page.tsx` · Design Final-Reports · Tests `WorkReportQueryTests` (`A_workspace_report_covers_exactly_the_readable_boards`), `WorkReportFactsIntegrationTests` · Real e2e `work-report.real.e2e.spec.ts`
- State: HARDENING — see H01; a zero-readable-board report must show the empty state, which no test proves.

### UC-ANA-H03 Narrow the workspace report to a department space
- Capability: H, ANA-008
- Actors and permission: workspace owner or admin (`ManageSpaces`).
- Preconditions: the space exists and the caller can see it.
- Input: `spaceId`.
- Output: report covering readable boards in that space and its descendants; `spaceId` echoed.
- Main flow: 1. Handler resolves the space subtree. 2. Intersects with visible boards. 3. Aggregates.
- No-op / rejection / concurrency / retry: a plain member asking for a space is refused (never widened); an invisible space is NotFound.
- UI states: loading, empty, success, permission denied.
- Rules: ANA008, ANA023; ARREQ049.
- Acceptance: Given a department with two boards, When an admin narrows to it, Then only those boards count. Given a member, When they pass `spaceId`, Then forbidden.
- Evidence: BE same route with `spaceId`; `WorkReportQueryTests` (`Narrowing_to_a_department...`, `A_space_the_caller_cannot_see_is_not_found`), `WorkReportFactsIntegrationTests` (`Boards_are_found_by_the_spaces_they_are_placed_in...`) · FE space selector in `work-report-api.ts` · Design Final-Reports · Real e2e none for space
- State: INTEGRATING — backend and frontend connect; the real e2e selects a board only.

### UC-ANA-H04 Choose the report period in weeks
- Capability: H, ANA-008
- Actors and permission: any report reader.
- Preconditions: none.
- Input: `weeks` from 1 to 26.
- Output: finished-per-week buckets for that many weeks, Monday 00:00 UTC each.
- Main flow: 1. User picks a period. 2. Client sends `weeks`. 3. Handler passes it on.
- No-op / rejection / concurrency / retry: out of range is a validation error; the same weeks gives the same shape.
- UI states: loading, success, validation error.
- Rules: ANA012, ANA013; ARREQ020, ARREQ021.
- Acceptance: Given `weeks=12`, When requested, Then twelve buckets return. Given `weeks=0`, Then validation error.
- Evidence: BE `WorkReportPeriod.cs`, `*QueryValidator.cs`; `WorkReportQueryTests` (`A_workspace_report_asks_for_the_requested_number_of_weeks`) · FE `work-report-api.ts` `board(boardId, weeks)` · Design Final-Reports · Real e2e none
- State: INTEGRATING — no real e2e picks a period; week boundaries are UTC and not disclosed (O02).

### UC-ANA-H05 Count work items by placement (work-item-count v1)
- Capability: H, ANA-008
- Actors and permission: workspace member; drill-down needs board and group visibility.
- Preconditions: Metric `work-item-count:v1` registered (B01).
- Input: archive state (active, archived, all), optional board and group grouping.
- Output: counts from the placement projection with `generatedAt` and freshness.
- Main flow: 1. Report `work-placement-overview:v1` reads the projection. 2. Applies visibility. 3. Returns views: active, archived, by board, by group.
- No-op / rejection / concurrency / retry: a date range is a validation error; unavailable projection is not zero (P02).
- UI states: loading, empty, success, permission denied, stale, degraded.
- Rules: ARREQ089; spec section 6.1; ANA012, ANA029.
- Acceptance: Given 7 active and 2 archived items, When the report opens, Then active is 7 and archived is 2. Given the projection is uninitialized, Then the report says unavailable.
- Evidence: BE projection and local query only · FE none · Design Final-Reports · Tests none for the report · Real e2e none
- State: DEFINED — the frozen released slice has no Metric, no route and no UI.

## Capability I: Documents/Collaboration analytics

### UC-ANA-I01 Documents activity report
- Capability: I, ANA-009 Documents/Collaboration analytics
- Actors and permission: workspace member; figures limited to pages the caller can read.
- Preconditions: Documents publishes a reporting contract.
- Input: period.
- Output: pages created and edited over time.
- Main flow: 1. Documents facts feed a projection. 2. Report applies page visibility.
- No-op / rejection / concurrency / retry: a page the caller cannot read is not counted.
- UI states: loading, empty, success, permission denied, stale.
- Rules: product section 58; ARREQ073 to ARREQ080.
- Acceptance: Given ten edits on a private page, When another member views the report, Then those edits are not counted.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — no source contract; no artboard.

### UC-ANA-I02 Collaboration activity report
- Capability: I, ANA-009
- Actors and permission: workspace member.
- Preconditions: Collaboration publishes counts, not comment text.
- Input: period.
- Output: comments and mentions per week.
- Main flow: as I01 over Collaboration facts.
- No-op / rejection / concurrency / retry: counts only; no content (ANA-privacy, PII minimization).
- UI states: loading, empty, success, stale.
- Rules: product section 59, section 106.
- Acceptance: Given comments on a hidden board, When the report opens, Then they are excluded.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED.

## Capability J: Automation/Integrations analytics

### UC-ANA-J01 Automation run outcome report
- Capability: J, ANA-010 Automation/Integrations analytics
- Actors and permission: workspace admin.
- Preconditions: Automation publishes execution outcome facts.
- Input: period, automation id optional.
- Output: runs, successes, failures, failure reasons by category.
- Main flow: execution facts feed a projection; report reads it.
- No-op / rejection / concurrency / retry: Automation owns the execution truth; the report never retries a run.
- UI states: loading, empty, success, permission denied, stale.
- Rules: product section 60; ARREQ073 to ARREQ080.
- Acceptance: Given two failed and eight successful runs, When opened, Then the failure rate is 20 percent.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED.

### UC-ANA-J02 Integration sync health report
- Capability: J, ANA-010
- Actors and permission: workspace admin.
- Preconditions: Integrations publishes sync outcomes.
- Input: period, connection optional.
- Output: sync count, failures, last success per connection.
- Main flow: as J01.
- No-op / rejection / concurrency / retry: no provider secret ever enters the projection.
- UI states: loading, empty, success, stale.
- Rules: product section 61.
- Acceptance: Given a connection failing for two days, When opened, Then it shows two days since last success.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED.

## Capability K: Billing/usage analytics

### UC-ANA-K01 Seat and usage report from Billing facts
- Capability: K, ANA-011 Billing/usage analytics
- Actors and permission: workspace owner or billing manager.
- Preconditions: Billing publishes seat and usage facts.
- Input: period.
- Output: seats and usage against plan limits, trend.
- Main flow: Analytics reads Billing facts; shows a trend; never charges or changes entitlement.
- No-op / rejection / concurrency / retry: read only.
- UI states: loading, empty, success, permission denied, stale.
- Rules: ANA022; product sections 62 to 64.
- Acceptance: Given 8 of 10 seats used, When opened, Then the report shows 8 of 10 and the trend.
- Evidence: BE none · FE none (billing page shows current plan, separate) · Design none · Tests none · Real e2e none
- State: DEFINED.

## Capability L: Cross-context report composition

### UC-ANA-L01 Compose a multi-source report with completeness
- Capability: L, ANA-012 Cross-context report composition
- Actors and permission: workspace member.
- Preconditions: each source has a certified projection.
- Input: report id.
- Output: sections per source, each with its own freshness and completeness.
- Main flow: composer calls each source's projection; marks missing sources.
- No-op / rejection / concurrency / retry: one source down gives a partial report with that section marked unavailable, never a silent zero.
- UI states: loading, success, partial, stale, permission denied.
- Rules: ANA030; ARREQ081 to ARREQ088.
- Acceptance: Given Automation is unavailable, When the report opens, Then the Automation section says unavailable and the others show.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — `AR-GAP-12`.

## Capability M: Report/query API (dashboard lifecycle)

Dashboards live in the Analytics context (owner decision 17: a dashboard is an Analytics surface, not a Work Management view). The Domain aggregate exists (`backend/src/Notrelix.Domain/Analytics/Dashboards/Dashboard.cs`); every use case below lacks its Application handler and endpoint, and its frontend runs on the mock backend only. Shared Domain tests: `DashboardTests.cs`, `DashboardWidgetTests.cs`, `DashboardWidgetScopeTests.cs`. Shared mock operations: `reports.*` in `reports.handlers.ts`.

### UC-ANA-M01 List the dashboards and reports I may open
- Capability: M, ANA-013 Report/query API
- Actors and permission: workspace member; Governance visibility.
- Preconditions: member of the workspace.
- Input: workspace id, optional filter by owner, search.
- Output: list with title, owner, visibility, what the viewer may do.
- Main flow: 1. Read dashboards the caller owns or that are shared with the workspace. 2. Filter by source visibility. 3. Page the result.
- No-op / rejection / concurrency / retry: a private dashboard of another user is absent; list is paged (limit and cursor).
- UI states: loading, empty, success, permission denied, offline.
- Rules: ANA007, ANA008; ARREQ049, ARREQ089 to ARREQ091.
- Acceptance: Given one private dashboard of Ann, When Bob lists, Then it is absent. Given 500 dashboards, Then a page returns with a cursor.
- Evidence: BE none · FE `report-list-surface.tsx` (mock `reports.list`) · Design Final-Reports, Final-M-Reports · Tests `reports.interaction.component.test.tsx` · Real e2e none (mock `reports.mock.e2e.spec.ts` "lists the three reports")
- State: IMPLEMENTING — no endpoint; the mock lists three fixed reports.

### UC-ANA-M02 Open one report or dashboard
- Capability: M, ANA-013
- Actors and permission: viewer of the dashboard.
- Preconditions: dashboard exists and is visible.
- Input: dashboard id.
- Output: configuration, widgets with positions, and the widget figures with freshness.
- Main flow: 1. Read configuration. 2. For each widget, read its Metric under the viewer's authorization. 3. Return.
- No-op / rejection / concurrency / retry: an invisible dashboard is NotFound identical to a missing one; a widget over a source the viewer cannot read shows permission denied for that widget only.
- UI states: loading, success, partial, permission denied, stale, error, offline.
- Rules: ANA008, ANA031; ARREQ045, ARREQ089.
- Acceptance: Given a shared dashboard with one widget over a hidden board, When a member opens it, Then that widget shows access denied and the rest render.
- Evidence: BE none · FE `report-page.tsx`, `report-dashboard-surface.tsx` (mock `reports.get`) · Design Final-Reports · Tests `reports.interaction.component.test.tsx` · Real e2e none
- State: IMPLEMENTING.

### UC-ANA-M03 Create a dashboard
- Capability: M, ANA-013
- Actors and permission: workspace member (Lead decision: any member may create a private dashboard; `ManageDashboards` for workspace-visible ones, name to be confirmed with Governance).
- Preconditions: workspace active.
- Input: title (1 to 120), optional layout template, visibility (default private).
- Output: dashboard, owner = caller, status active, event `DashboardCreated`.
- Main flow: 1. User names the dashboard. 2. Command creates it with the owner id. 3. Client opens it empty.
- No-op / rejection / concurrency / retry: the same idempotency key returns the first dashboard; an empty title is rejected.
- UI states: empty start, validation error, pending, permission denied.
- Rules: ANA006, ANA007; ARREQ041 to ARREQ044.
- Acceptance: Given a title, When created twice with one idempotency key, Then one dashboard exists.
- Evidence: BE Domain `Dashboard.cs` only · FE `report-start.tsx`, `report-dialogs.tsx` (mock `reports.create`) · Design Final-Reports · Tests `DashboardTests.cs`, mock e2e "starts empty, takes a layout, a name, sharing and can be deleted" · Real e2e none
- State: IMPLEMENTING — the aggregate has no explicit owner user id (`AR-GAP-03`).

### UC-ANA-M04 Rename a dashboard
- Capability: M, ANA-013
- Actors and permission: dashboard owner or workspace admin.
- Preconditions: dashboard active.
- Input: dashboard id, new title, expected version.
- Output: new title and version; event `DashboardRenamed`.
- Main flow: 1. Owner edits the title. 2. Command checks version. 3. Saves.
- No-op / rejection / concurrency / retry: the same title is a no-op with no version bump and no event; a stale version is rejected as a conflict; an archived dashboard is read only (`Analytics_Dashboard_ArchivedReadOnly`).
- UI states: validation error, conflict, pending, read-only.
- Rules: ANA002; ARREQ044.
- Acceptance: Given version 3, When a rename arrives with version 2, Then 412 and no change.
- Evidence: BE Domain `Dashboard.cs` rename · FE mock `reports.rename` · Design Final-Reports · Tests `DashboardTests.cs` · Real e2e none
- State: IMPLEMENTING.

### UC-ANA-M05 Archive, restore or delete a dashboard
- Capability: M, ANA-013
- Actors and permission: dashboard owner or workspace admin.
- Preconditions: dashboard exists.
- Input: dashboard id, action, expected version.
- Output: status archived, active, or removed; widgets go with it; source boards and items are untouched.
- Main flow: 1. Owner chooses the action. 2. Confirm for delete. 3. Command applies it.
- No-op / rejection / concurrency / retry: archiving an archived dashboard is a no-op; delete asks for confirmation and is final (Lead decision: delete is permanent for configuration, snapshots follow retention); stale version rejected.
- UI states: destructive confirmation, pending, conflict, permission denied.
- Rules: ANA006, ANA033; product sections 19, 91, 92.
- Acceptance: Given a dashboard over board B, When it is deleted, Then board B and its items are unchanged.
- Evidence: BE Domain `DashboardArchivedDomainEvent` · FE mock `reports.delete` · Design Final-Reports · Tests `DashboardTests.cs`, mock e2e (delete) · Real e2e none
- State: IMPLEMENTING — no restore operation exists in Domain; Part 4 decides it.

### UC-ANA-M06 Add and configure a widget
- Capability: M, ANA-013
- Actors and permission: dashboard owner.
- Preconditions: dashboard active; a released Metric exists.
- Input: widget type, Metric id and version, filters, grouping, time range, limit, visual options, expected version.
- Output: widget with a position; event `DashboardWidgetAdded`.
- Main flow: 1. Pick type and Metric. 2. Client validates against the Metric. 3. Command validates the typed config. 4. Saves.
- No-op / rejection / concurrency / retry: an impossible filter, grouping or time range is rejected (`Analytics_Dashboard_InvalidWidgetConfig`); the widget limit is enforced (`Analytics_Dashboard_WidgetLimitExceeded`); idempotent by key.
- UI states: validation error, conflict, pending, permission denied.
- Rules: ANA010, ANA029, ANA032; ARREQ045 to ARREQ048.
- Acceptance: Given a bar widget grouped by a field the Metric does not support, When saved, Then rejected with the rule code.
- Evidence: BE Domain `DashboardWidget.cs`, `WidgetConfigValidator.cs` · FE `report-widgets.tsx`, `report-dialogs.tsx` (mock `reports.widgets.save`) · Design Final-Reports · Tests `DashboardWidgetTests.cs`; mock component test · Real e2e none
- State: IMPLEMENTING — config is validated for JSON kind only; typed per-kind contracts are missing (`AR-GAP-04`).

### UC-ANA-M07 Arrange widgets by move and resize
- Capability: M, ANA-013
- Actors and permission: dashboard owner.
- Preconditions: dashboard active.
- Input: widget id, x, y, width, height, expected version; or a layout template.
- Output: positions saved; event `DashboardWidgetMoved`; source order never changes.
- Main flow: 1. Drag or use keyboard. 2. Save on commit. 3. Others see the new layout on reload.
- No-op / rejection / concurrency / retry: same position is a no-op; negative coordinates or zero size are rejected (`Analytics_Widget_CoordinatesMustBeNonNegative`, `Analytics_Widget_DimensionsMustBePositive`); stale version conflicts.
- UI states: pending, conflict, cancelled by Esc, read-only.
- Rules: ANA011; ARREQ045.
- Acceptance: Given a widget moved, When the dashboard reloads, Then it is at the new place and no board item moved.
- Evidence: BE Domain `WidgetPosition.cs` · FE `report-dashboard-surface.tsx` · Design Final-Reports · Tests mock e2e "moves with the keyboard, cancels, resizes, adds a widget and saves" · Real e2e none
- State: IMPLEMENTING.

### UC-ANA-M08 Remove a widget
- Capability: M, ANA-013
- Actors and permission: dashboard owner.
- Preconditions: widget exists.
- Input: widget id, expected version.
- Output: widget removed; event `DashboardWidgetRemoved`.
- Main flow: 1. Choose remove from the widget menu. 2. Command removes it.
- No-op / rejection / concurrency / retry: unknown widget is `Analytics_Dashboard_WidgetNotFound`; removing twice is a no-op for the second call by key.
- UI states: pending, conflict, undo hint.
- Rules: ANA006; ARREQ045.
- Acceptance: Given a widget removed, When reloaded, Then it is gone and its Metric is unchanged.
- Evidence: BE Domain `DashboardWidget.cs` · FE mock "adds a note and removes a widget from the menu" · Design Final-Reports · Tests `DashboardWidgetTests.cs` · Real e2e none
- State: IMPLEMENTING.

### UC-ANA-M09 Share a dashboard privately or with the workspace
- Capability: M, ANA-013
- Actors and permission: dashboard owner (private to workspace needs the manage permission).
- Preconditions: dashboard active.
- Input: visibility Private or Workspace, expected version.
- Output: visibility changed; event `DashboardVisibilityChanged`; `Public` is not reachable (spec section 6).
- Main flow: 1. Owner opens sharing. 2. Chooses. 3. Command saves; widgets still obey source authorization for each viewer.
- No-op / rejection / concurrency / retry: same visibility is a no-op; choosing Public is rejected.
- UI states: pending, conflict, permission denied, confirmation when widening.
- Rules: ANA008, product section 90; ARREQ049 to ARREQ056.
- Acceptance: Given a workspace-shared dashboard with a widget over Ann's private board, When Bob opens it, Then that widget shows access denied.
- Evidence: BE Domain `DashboardVisibility.cs`, `DashboardVisibilityChangedDomainEvent` · FE mock `reports.sharing.get`, `reports.sharing.save` · Design Final-Reports · Tests `DashboardTests.cs` · Real e2e none
- State: IMPLEMENTING — no `OwnerUserId` (`AR-GAP-03`).

### UC-ANA-M10 Duplicate a dashboard (enterprise baseline)
- Capability: M, ANA-013
- Actors and permission: any member who may open it.
- Preconditions: dashboard visible to the caller.
- Input: dashboard id, new title.
- Output: a private copy owned by the caller; widgets whose source the caller cannot read are copied but show denied.
- Main flow: 1. Choose duplicate. 2. Command copies configuration only.
- No-op / rejection / concurrency / retry: idempotent by key; snapshots are not copied.
- UI states: pending, success, permission denied.
- Rules: ANA006, ANA008.
- Acceptance: Given a shared dashboard, When Bob duplicates it, Then Bob owns a private copy and the original is unchanged.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED.

## Capability N: Dashboard/report frontend

### UC-ANA-N01 Work report page on the real backend
- Capability: N, ANA-014 Dashboard/report frontend
- Actors and permission: workspace member.
- Preconditions: signed in, workspace active.
- Input: board or space selection, period.
- Output: widgets "Items by status", "Items by assignee", "Finished per week" and totals.
- Main flow: 1. Open `/workspaces/{id}/reports`. 2. Page loads the workspace report. 3. User narrows by board.
- No-op / rejection / concurrency / retry: errors are quiet and shown by the page; reloading is safe.
- UI states: loading, empty, success, permission denied, error, offline.
- Rules: ANA014; ARREQ092 to ARREQ096.
- Acceptance: Given a workspace admin, When they open Reports and select a board, Then the assignee region appears and no console error occurs.
- Evidence: BE the two report routes · FE `work-report-page.tsx`, `work-report-api.ts`, `work-report-surface.tsx` · Design Final-Reports · Tests `frontend/packages/dev/mock-backend/src/__tests__/work-report.unit.test.ts` · Real e2e `work-report.real.e2e.spec.ts`
- State: HARDENING — no 1-px diff at 1440, 1024, 390; no axe proof recorded.

### UC-ANA-N02 Report list and builder pages
- Capability: N, ANA-014
- Actors and permission: workspace member; owner edits.
- Preconditions: `backendCapabilities.reports` true (today only the mock backend).
- Input: clicks, keyboard.
- Output: report list, dashboard with widgets, dialogs for name, sharing, widget, layout.
- Main flow: as M01 to M09 in the UI.
- No-op / rejection / concurrency / retry: on the real backend the page shows the work report instead.
- UI states: all of README section 7 line 2 for the mock; none proven against a producer.
- Rules: ANA010, ANA011; ARREQ092 to ARREQ096.
- Acceptance: Given a producer exists, When the capability flag is switched on, Then the same journeys pass against it.
- Evidence: BE none · FE `report-list-surface.tsx`, `report-dashboard-surface.tsx`, `report-widgets.tsx`, `report-dialogs.tsx` · Design Final-Reports, Final-M-Reports · Tests `reports.interaction.component.test.tsx` · Real e2e none (mock `reports.mock.e2e.spec.ts`)
- State: IMPLEMENTING — mock-only.

### UC-ANA-N03 Drill from a figure to the items behind it
- Capability: N, ANA-014
- Actors and permission: board reader (aggregate visibility does not give detail visibility).
- Preconditions: a figure with a person or status bucket.
- Input: widget, bucket.
- Output: a read-only sheet listing the open items the viewer may read; Esc closes; an item opens its page.
- Main flow: 1. Click a figure. 2. Sheet reads items. 3. Click opens the board item.
- No-op / rejection / concurrency / retry: items the viewer cannot read are omitted and the count says so.
- UI states: loading, empty, success, permission denied.
- Rules: ANA031, ANA002.
- Acceptance: Given a person with three open items, one hidden from the viewer, When drilled, Then two show and a note says one is hidden.
- Evidence: BE none · FE `report-drill-sheet.tsx` (mock `reports.drilldown`) · Design Final-Reports · Tests mock e2e "opens the open items behind a person, read-only, and closes with Esc" · Real e2e none
- State: IMPLEMENTING.

### UC-ANA-N04 The page says reports are not available
- Capability: N, ANA-014
- Actors and permission: workspace member.
- Preconditions: backend has no producer for a report feature.
- Input: none.
- Output: a feature-unavailable surface that says "Reports are not available yet" (copy in `feature-availability.copy.ts`).
- Main flow: the route checks `backendCapabilities` and shows the surface instead of calling a 404 route.
- No-op / rejection / concurrency / retry: none.
- UI states: unavailable.
- Rules: ARREQ096.
- Acceptance: Given the real backend, When a member opens a report builder address, Then the unavailable message shows and no 404 request is sent.
- Evidence: BE n/a · FE `frontend/packages/features/workspace/src/web/unavailable/feature-unavailable-surface.tsx` · Design Final-States · Tests `feature-availability.interaction.component.test.tsx` · Real e2e `feature-availability.real.e2e.spec.ts` (asserts billing text; reports text not asserted)
- State: INTEGRATING — add a real e2e line for the reports text; remove the flag when the producer ships.

## Capability O: Filters/date/timezone semantics

### UC-ANA-O01 Filter a report by board, person or space
- Capability: O, ANA-015 Filters/date/timezone semantics
- Actors and permission: report reader; space needs `ManageSpaces`.
- Preconditions: report open.
- Input: board id, space id, person (mock only), period.
- Output: figures narrowed; the filter shows in the export scope text.
- Main flow: 1. Choose a filter. 2. Client asks again. 3. Figures redraw.
- No-op / rejection / concurrency / retry: filters use Metric fields only; an impossible combination is rejected (ANA-filters rule).
- UI states: loading, empty, success, validation error.
- Rules: product section 29; ARREQ020.
- Acceptance: Given a board filter, When applied, Then figures equal that board's report. Given a person filter on the real backend, Then it is available only when a producer supports it.
- Evidence: BE routes take `spaceId`, `weeks`; no person filter · FE `work-report-api.ts`; mock "narrows to one person and shows it in the export scope" · Design Final-Reports · Tests as H01 · Real e2e `work-report.real.e2e.spec.ts` (board only)
- State: INTEGRATING — person filter has no producer.

### UC-ANA-O02 Time zone and week boundaries
- Capability: O, ANA-015
- Actors and permission: report reader.
- Preconditions: none.
- Input: none (display only in v1).
- Output: the report states that weeks start Monday 00:00 UTC.
- Main flow: the UI prints the zone next to the weekly chart.
- No-op / rejection / concurrency / retry: none.
- UI states: success.
- Rules: ANA013; ARREQ021; Lead decision in Part 4.
- Acceptance: Given the weekly chart, When shown, Then its label names UTC week starts.
- Evidence: BE `WorkReportDto.cs` (`WeekStart` Monday UTC) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — the label does not exist.

## Capability P: Freshness/staleness contract

### UC-ANA-P01 Show when figures were generated
- Capability: P, ANA-016 Freshness/staleness contract
- Actors and permission: report reader.
- Preconditions: a report response.
- Input: none.
- Output: `generatedAt` on every report; later also source cutoff and lag.
- Main flow: the page shows the time next to the figures.
- No-op / rejection / concurrency / retry: transactional reports are current at `generatedAt`; projection reports add cutoff.
- UI states: success, stale.
- Rules: ANA014; ARREQ057 to ARREQ060.
- Acceptance: Given a report, When opened, Then `generatedAt` is visible and close to now.
- Evidence: BE `BoardReportDto.GeneratedAt`, `WorkspaceReportDto.GeneratedAt` · FE `work-report-surface.tsx` (verify display) · Design Final-Reports · Tests `WorkReportQueryTests` · Real e2e none
- State: INTEGRATING — no cutoff, lag or freshness mode field (`AR-GAP-13`).

### UC-ANA-P02 Degraded or unavailable is never shown as zero
- Capability: P, ANA-016
- Actors and permission: report reader.
- Preconditions: a projection is uninitialized, lagging or failed.
- Input: none.
- Output: the figure says unavailable or degraded with the reason; zero appears only when the source lane is healthy.
- Main flow: the response carries a status per figure; the UI picks the text.
- No-op / rejection / concurrency / retry: unauthorized is shown as denied, not zero.
- UI states: stale, degraded, unavailable, permission denied.
- Rules: ANA029, ANA014; ARREQ023.
- Acceptance: Given an uninitialized projection, When the report opens, Then the figure shows unavailable and not 0.
- Evidence: BE none · FE none · Design Final-States · Tests none · Real e2e none
- State: DEFINED.

### UC-ANA-P03 Recompute a stale widget
- Capability: P, ANA-016
- Actors and permission: dashboard viewer.
- Preconditions: widget is stale.
- Input: widget or dashboard id.
- Output: new figures and a new `generatedAt`.
- Main flow: 1. Viewer chooses refresh. 2. Server recomputes. 3. UI replaces the figure.
- No-op / rejection / concurrency / retry: repeated refresh inside a short window returns the cached result; read only.
- UI states: pending, success, error.
- Rules: product section 38; ARREQ058.
- Acceptance: Given a stale widget, When refreshed, Then its time moves forward.
- Evidence: BE none · FE mock `reports.refresh`; mock e2e "shows what was left out and a stale widget until it is recomputed" · Design Final-Reports · Tests mock component test · Real e2e none
- State: IMPLEMENTING — mock only.

## Capability Q: Export

### UC-ANA-Q01 Export a report to a file
- Capability: Q, ANA-017 Export
- Actors and permission: report reader; export is a protected action (Lead decision: needs the same report permission plus the export permission, name to be set with Governance).
- Preconditions: report open.
- Input: report id, format (CSV first), the current filters.
- Output: a file with the same figures and filters as the screen, `generatedAt`, and the metric version.
- Main flow: 1. User chooses Export. 2. Server recomputes with the caller's authorization. 3. File downloads.
- No-op / rejection / concurrency / retry: the file never includes rows the screen would hide; a retry with the same key returns the same artifact.
- UI states: pending, success, error, permission denied.
- Rules: ANA026; ARREQ097 to ARREQ104.
- Acceptance: Given a screen of 12 visible items, When exported, Then the file has the same 12 and no more.
- Evidence: BE none · FE mock `reports.export.start` · Design Final-Reports · Tests mock e2e "makes a file in the background, finishes on a check and leaves a link" · Real e2e none
- State: IMPLEMENTING — mock only; the spec defers export in the first released slice (`AR-GAP-11`).

### UC-ANA-Q02 Generate a large export in the background
- Capability: Q, ANA-017
- Actors and permission: report reader.
- Preconditions: result is above the synchronous limit.
- Input: as Q01.
- Output: a job that moves queued, running, completed, failed or expired; a download link for the owner only.
- Main flow: 1. Start job. 2. Poll or receive a notice. 3. Download.
- No-op / rejection / concurrency / retry: idempotent start by key; expired artifacts return a clear expired state; downloads re-check authorization.
- UI states: queued, running, completed, failed, expired.
- Rules: ANA026; product sections 102, 103; ARREQ100 to ARREQ104.
- Acceptance: Given a large report, When started, Then it completes into a link only the requester can use.
- Evidence: BE none · FE mock `reports.export.get` · Design Final-Reports · Tests none · Real e2e none
- State: DEFINED — no job runtime or artifact store.

### UC-ANA-Q03 Schedule a recurring report (enterprise baseline)
- Capability: Q, ANA-017
- Actors and permission: dashboard owner.
- Preconditions: Collaboration or Platform delivery exists.
- Input: report, cadence, recipients, format.
- Output: scheduled deliveries with a snapshot each time.
- Main flow: 1. Owner creates the schedule. 2. At the time, a snapshot is cut. 3. Each recipient receives a view filtered to what they may read.
- No-op / rejection / concurrency / retry: authorization is re-checked at send time; a removed recipient is skipped; a failed delivery retries by key.
- UI states: success, validation error, permission denied.
- Rules: product section 75; ANA015.
- Acceptance: Given a weekly schedule, When a recipient loses board access, Then the next report omits that board.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — delivery owner is undecided between Collaboration and Platform (Part 4).

## Capability R: Authorization/visibility

### UC-ANA-R01 See only figures from boards I may read
- Capability: R, ANA-018 Authorization/visibility
- Actors and permission: workspace member or guest.
- Preconditions: Governance visibility available.
- Input: any report request.
- Output: figures computed over visible boards only.
- Main flow: handler asks `IResourceVisibility.VisibleBoardIdsAsync` before aggregating.
- No-op / rejection / concurrency / retry: invisible board gives NotFound identical to missing.
- UI states: permission denied, empty.
- Rules: ANA008, ANA023; ARREQ049 to ARREQ056.
- Acceptance: Given a board hidden from Bob, When Bob opens the workspace report, Then it is not counted; When Bob asks for it by id, Then NotFound.
- Evidence: BE `GetBoardReport.cs`, `GetWorkspaceReport.cs` · FE `work-report-page.tsx` · Design Final-States · Tests `WorkReportQueryTests` (`A_board_the_caller_cannot_read_is_not_found`, `...exactly_the_readable_boards`) · Real e2e `work-report.real.e2e.spec.ts` (admin only)
- State: HARDENING — no real e2e with a restricted member or guest; no cross-tenant negative test for reports.

### UC-ANA-R02 Grouping by person respects confidentiality
- Capability: R, ANA-018
- Actors and permission: report reader.
- Preconditions: a figure grouped by a person.
- Input: none.
- Output: person rows only for people whose items the viewer can read.
- Main flow: aggregation runs after visibility filtering.
- No-op / rejection / concurrency / retry: Lead decision: no small-group suppression inside one workspace in v1.
- UI states: success.
- Rules: ANA023; product section 67.
- Acceptance: Given Ann has items only on a hidden board, When Bob opens by assignee, Then Ann is absent.
- Evidence: BE aggregation in Work Management `IWorkReportFacts` · FE none · Design none · Tests none specific · Real e2e none
- State: DEFINED — no test proves the hidden-assignee case.

### UC-ANA-R03 Derived data stays inside its tenant
- Capability: R, ANA-018
- Actors and permission: system.
- Preconditions: RLS on reporting tables.
- Input: any projection or report query.
- Output: results of one account and workspace only.
- Main flow: request context sets the tenant; RLS enforces it.
- No-op / rejection / concurrency / retry: a missing tenant context fails closed.
- UI states: none.
- Rules: ANA007, ANA024; ARREQ052; platform-and-messaging tenant context.
- Acceptance: Given rows in two tenants, When tenant A queries, Then no row of B appears.
- Evidence: BE RLS policies for dashboards, widgets, sources, snapshots, placements · FE n/a · Design n/a · Tests `WorkspaceScopes_StayIsolated`, `Rebuild_WorkspaceIsolation_...` · Real e2e `workspace-isolation.real.e2e.spec.ts` (not report specific)
- State: IMPLEMENTING — no cross-tenant test for the two report endpoints.

### UC-ANA-R04 Cache keys carry tenant, metric version and audience
- Capability: R, ANA-018
- Actors and permission: system.
- Preconditions: a cache is added to reports.
- Input: tenant, metric id and version, filters, period, audience.
- Output: a key that cannot cross tenant or audience.
- Main flow: key builder composes all parts; shared entries only for audience-safe aggregates.
- No-op / rejection / concurrency / retry: a key without tenant is refused.
- UI states: none.
- Rules: ANA025; product section 72.
- Acceptance: Given two members with different visible boards, When both open the report, Then neither receives the other's cached figures.
- Evidence: BE none (no report cache exists) · FE query keys follow `workspaceQueryKey` · Design n/a · Tests none · Real e2e none
- State: DEFINED.

## Capability S: Retention/data lifecycle

### UC-ANA-S01 Capture a reporting snapshot
- Capability: S, ANA-019 Retention/data lifecycle
- Actors and permission: system (scheduled report, export).
- Preconditions: Metric version and source cutoff known.
- Input: type, payload, schema version, cutoff, metric version.
- Output: an immutable snapshot with lineage.
- Main flow: 1. Job runs the Metric. 2. Stores payload with schema version.
- No-op / rejection / concurrency / retry: the same key and cutoff stores once; payload must be a JSON object.
- UI states: none.
- Rules: ANA015, ANA016; ARREQ065 to ARREQ072.
- Acceptance: Given a capture, When read later, Then payload and cutoff are identical.
- Evidence: BE `backend/src/Notrelix.Domain/Analytics/Snapshots/ReportingSnapshot.cs` · FE none · Design n/a · Tests `ReportingSnapshotTests.cs`, `ReportingSnapshotPersistenceTests.cs` · Real e2e none
- State: IMPLEMENTING — architecture legacy gap, no lineage fields, no producer (`AR-GAP-06`).

### UC-ANA-S02 Read a historical snapshot by schema version
- Capability: S, ANA-019
- Actors and permission: viewer of the report that created it.
- Preconditions: snapshot retained.
- Input: snapshot id.
- Output: the payload read with its original schema; a notice if Metric version differs from today.
- Main flow: reader chooses the deserializer by schema version.
- No-op / rejection / concurrency / retry: an unknown schema version fails with a clear error, never a guess.
- UI states: loading, success, expired, permission denied.
- Rules: ANA016, ANA028, ANA038.
- Acceptance: Given a v1 snapshot, When read after v2 ships, Then it renders as v1.
- Evidence: BE round-trip tests only · FE none · Design none · Tests `SchemaV1Snapshot_RoundTrips_AsVersion1`, `LegacyRowWithoutSchemaVersion_LoadsAsVersion1` · Real e2e none
- State: DEFINED — no read path.

### UC-ANA-S03 Purge snapshots by retention and privacy
- Capability: S, ANA-019
- Actors and permission: system, with operator override.
- Preconditions: retention policy set.
- Input: policy, age, privacy request.
- Output: expired snapshots and artifacts deleted; source data untouched.
- Main flow: a job purges by policy; counts are logged.
- No-op / rejection / concurrency / retry: purge twice is a no-op; legal hold blocks (Part 4).
- UI states: expired.
- Rules: ANA033; product sections 44, 93, 107.
- Acceptance: Given a snapshot older than the policy, When the job runs, Then it is gone and boards are unchanged.
- Evidence: BE none · FE none · Design n/a · Tests none · Real e2e none
- State: DEFINED — destructive policy values need an owner (Part 4 lead decision, still to be accepted).

### UC-ANA-S04 Handle deletion of a source
- Capability: S, ANA-019
- Actors and permission: system.
- Preconditions: a board or item is deleted or anonymized.
- Input: the deletion event.
- Output: live figures recompute without it; retained snapshots follow the explicit policy.
- Main flow: projection removes the row; no SQL cascade from source tables.
- No-op / rejection / concurrency / retry: duplicate delete event is a no-op.
- UI states: none.
- Rules: ANA017, ANA038; ARREQ013.
- Acceptance: Given a deleted board, When the report opens, Then it is not counted; the old snapshot still shows its recorded figures with a note.
- Evidence: BE `Rebuild_RowMissingFromSnapshot...` tests · FE none · Design n/a · Tests as listed · Real e2e none
- State: DEFINED — snapshot policy undecided.

## Capability T: Performance/storage strategy

### UC-ANA-T01 A report stays bounded on a large workspace
- Capability: T, ANA-020 Performance/storage strategy
- Actors and permission: workspace member.
- Preconditions: a workspace with many boards and items.
- Input: report request.
- Output: figures from pre-aggregated or indexed queries; no item is loaded; weeks capped at 26.
- Main flow: Work Management runs one aggregate over visible board ids.
- No-op / rejection / concurrency / retry: board id list is bounded by what the caller sees; a timeout returns an error, not a partial figure.
- UI states: loading, error.
- Rules: ANA021; ARREQ105 to ARREQ112.
- Acceptance: Given 10 000 items, When the report opens, Then it answers within the agreed budget (Part 4).
- Evidence: BE `GetBoardReport.cs` (no item load), `WorkReportPeriod.MaxWeeks` · FE none · Design n/a · Tests `WorkReportFactsIntegrationTests` (correctness, not scale) · Real e2e none
- State: IMPLEMENTING — no scale test or latency budget.

## Capability U: Observability

### UC-ANA-U01 Operator sees projection lag and failures
- Capability: U, ANA-021 Observability
- Actors and permission: platform operator.
- Preconditions: telemetry enabled.
- Input: none.
- Output: metrics for projection lag, consumer failures, rebuild runs, report latency.
- Main flow: components emit metrics with tenant-safe tags.
- No-op / rejection / concurrency / retry: no figure values or personal data in logs.
- UI states: none.
- Rules: ANA035; ARREQ109 to ARREQ112.
- Acceptance: Given a stalled consumer, When lag exceeds the threshold, Then an alert condition is visible.
- Evidence: BE targeted logs in the placement service only · FE none · Design n/a · Tests none · Real e2e none
- State: DEFINED — `AR-GAP-16`.

## Capability V: Data quality/reconciliation

### UC-ANA-V01 Reconcile a projection with its source
- Capability: V, ANA-022 Data quality/reconciliation
- Actors and permission: platform operator.
- Preconditions: producer snapshot available.
- Input: workspace id.
- Output: a divergence report (missing, extra, stale rows) before any repair.
- Main flow: compare projection with the producer snapshot; report; optionally rebuild (F01).
- No-op / rejection / concurrency / retry: read only until repair.
- UI states: none.
- Rules: ANA019; ARREQ109.
- Acceptance: Given one missing row, When reconciled, Then the report names it.
- Evidence: BE rebuild reconciles but does not report drift · FE none · Design n/a · Tests `Rebuild_*` · Real e2e none
- State: DEFINED.

## Capability W: Migration/versioning

### UC-ANA-W01 Publish a breaking Metric version
- Capability: W, ANA-023 Migration/versioning
- Actors and permission: Analytics team.
- Preconditions: Metric v1 in use.
- Input: new version with changed meaning.
- Output: v2 beside v1; widgets stay on v1 until moved; charts that span versions show a comparability notice.
- Main flow: publish; notify owners; migrate widgets; retire v1 later.
- No-op / rejection / concurrency / retry: a meaning change without a new version is rejected.
- UI states: notice.
- Rules: ANA003, ANA028; ARREQ024, ARREQ113 to ARREQ120.
- Acceptance: Given v2, When a widget still uses v1, Then it keeps its numbers and shows the version.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED.

### UC-ANA-W02 Migrate widget config and snapshot payload versions
- Capability: W, ANA-023
- Actors and permission: system migration.
- Preconditions: schema version bump.
- Input: stored config and payload rows.
- Output: rows readable under the new schema; old versions still readable.
- Main flow: expand, backfill, contract.
- No-op / rejection / concurrency / retry: idempotent; no destructive drop before consumers migrate.
- UI states: none.
- Rules: ANA016; ARREQ113.
- Acceptance: Given mixed v1 and v2 rows, When read, Then both load.
- Evidence: BE `ReportSnapshotPayload.cs` schema version · FE none · Design n/a · Tests `ReportingSnapshotPayloadTests.cs` · Real e2e none
- State: DEFINED.

### UC-ANA-W03 Retire the duplicate widget vocabulary
- Capability: W, ANA-023
- Actors and permission: Analytics team.
- Preconditions: two enums coexist.
- Input: code change.
- Output: one canonical widget type list (`DashboardWidgetType`); the other removed or mapped.
- Main flow: map, migrate stored values, delete the duplicate.
- No-op / rejection / concurrency / retry: stored rows must map without loss.
- UI states: none.
- Rules: ARREQ046, ARREQ116; `AR-GAP-05`.
- Acceptance: Given stored widgets of both vocabularies, When migrated, Then all map to one type.
- Evidence: BE `Dashboards/DashboardWidgetType.cs`, `Widgets/WidgetType.cs` · FE mock widget kinds · Design n/a · Tests none · Real e2e none
- State: DEFINED — source debt, no code change yet.

## Capability X: Hardening

### UC-ANA-X01 A live number updates and recovers from a gap
- Capability: X, ANA-024 Hardening
- Actors and permission: dashboard viewer.
- Preconditions: realtime gateway connected.
- Input: realtime notice for a Metric or dashboard.
- Output: the widget refetches; a missed or duplicate notice never leaves a wrong number.
- Main flow: notice arrives; client invalidates the query; REST is authoritative.
- No-op / rejection / concurrency / retry: gap triggers refetch (see [platform-foundation](platform-foundation.md) UC-PLT-I04).
- UI states: live, stale, reconnecting.
- Rules: ANA036; ARREQ062 to ARREQ064.
- Acceptance: Given a missed notice, When the stream gap is detected, Then the widget refetches and shows the current number.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — not required for the first release (spec section 6).

### UC-ANA-X02 Cross-tenant administrative analytics
- Capability: X, ANA-024
- Actors and permission: platform operator (undefined principal).
- Preconditions: a product decision.
- Input: not decided.
- Output: not decided.
- Main flow: not decided.
- No-op / rejection / concurrency / retry: must never reach ordinary users or shared caches.
- UI states: not decided.
- Rules: ANA024; product sections 68, 69.
- Acceptance: Given the decision is made, When an operator runs it, Then it is audited and invisible to workspace users.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: BLOCKED — decide the principal (platform operator or account owner), the allowed aggregates, and the audit trail; no tenant-isolation choice is made here.

# 4. Product decisions applied

- Product decision (owner, 2026-10-26): a dashboard is an Analytics surface, not a Work Management view. Used by M01 to M10.
- Product decision (owner, 2026-10-26): mobile Reports are read-only. Used by N01 and N02: mobile offers no builder or export action.
- Lead decision (2026-10-30): any workspace member may create and own a private dashboard; making it workspace-visible needs a manage permission set with Governance; `Public` stays unreachable (M03, M09).
- Lead decision (2026-10-30): deleting a dashboard is final for its configuration; snapshots follow retention, never the dashboard (M05, S03).
- Lead decision (2026-10-30): archived dashboards may be restored by their owner or a workspace admin (M05).
- Lead decision (2026-10-30): reports state their time zone; v1 is UTC with Monday week starts, and a workspace time zone is a later Metric version (O02, W01).
- Lead decision (2026-10-30): no small-group suppression inside one workspace in v1, because per-person counts only cover boards the reader may already read; revisit for tenant-wide people analytics (R02).
- Lead decision (2026-10-30): the report permission is a Governance action separate from `ViewWorkspace`, named `ViewReports`, granted to every workspace member by default and removable per role; export needs `ExportReports` (R01, Q01).
- Lead decision (2026-10-30): the rebuild and backfill of projections run under a platform operator system principal and are audited; no workspace user can trigger them (F01, F02).
- Lead decision (2026-10-30): scheduled report delivery is owned by Collaboration (notification and email) and triggered by an Analytics job; Platform only supplies the job runtime (Q03).
- Lead decision (2026-10-30): the synchronous export limit is 10 000 rows; above it the export runs as a background job and its artifact expires after 7 days (Q01, Q02).
- Lead decision (2026-10-30): a report answers within 3 seconds at the 95th percentile for a workspace of 10 000 items and 200 boards (T01).

# 5. Canonical updates required

For `docs/product/analytics.md`:

- Add a section "Report permission": `ViewReports` is a Governance action, default granted to every workspace member; `ExportReports` is separate; a report never shows figures from a resource the viewer cannot read.
- Add a section "Dashboard ownership": a Dashboard has an explicit owner user; a member creates private dashboards; workspace visibility needs the manage permission; `Public` is not released; an owner or admin may archive, restore or delete.
- Add to "Time semantics": the v1 weekly report uses Monday 00:00 UTC and says so.
- Add to "Small-group privacy": no suppression threshold inside a single workspace in v1.
- Add to "Export": synchronous export up to 10 000 rows; larger exports run as jobs; artifacts expire after 7 days; downloads re-check authorization.
- Add to "Scheduled report": Collaboration owns delivery; an Analytics job cuts the snapshot.
- Add to "Current source alignment": the two report endpoints exist and are computed by Work Management aggregates; the Dashboard APIs do not exist.
- Add a performance budget line: 3 seconds at the 95th percentile at 10 000 items and 200 boards.

For `docs/product/governance.md`: register `ViewReports` and `ExportReports` in the permission table.

# 6. Not covered

| Product section | Reason |
|---|---|
| Sections 1 to 4 (mission, owns, does not own, language) | Context definition; no behaviour of its own. |
| Sections 5 to 7 (derived state, editing configuration, no hidden mutation) | Rules, exercised by M03 to M09 and N03. |
| Section 12 to 14 (source facts, source event versus query) | Reached through A01, A02. |
| Sections 22 to 23 (Dashboard Source) | Reached through M06 (source binding); Search and External sources stay unreachable by the spec. |
| Section 32 to 34 (time semantics, ranges) | O02 and H04. |
| Sections 47 to 56 (projection, rebuild, backfill, scale) | C01 to F02, T01. |
| Section 57 to 64 (per-source sections) | H01 to K01. |
| Section 76 to 84 (numbers, comparison, null) | P02, W01; no separate comparison use case is added until a comparison widget is specified. |
| Section 85 to 89 (actions, drill-down, compatibility) | N03, M06. |
| Sections 96 to 98 (search versus Analytics, operational telemetry) | Boundary statements; telemetry is in [platform-foundation](platform-foundation.md). |
| Sections 104 to 109 (data residency, PII, retention) | S01 to S04, R03; data residency has no use case because no region rule exists yet. |
| Sections 110 to 124 (alignment, change impact, checklists, testing, stop conditions) | Review aids, not behaviour. |
