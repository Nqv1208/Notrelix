---
document_id: WRK-SPEC-WORK-MANAGEMENT-VIEWS
document_type: workstream-specification
status: active
owner: work-management-team
applies_to: [backend, work-management, p4a, views, query-filter-sort, table, kanban, calendar, timeline, dashboard, forms]
evidence:
  - docs/product/work-management.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/teams/work-management.md
  - docs/workstreams/executions/work-management/work-management.certification.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/security-tenancy-authorization.md
review_on: [p4a-scope-change, view-model-change, query-contract-change, ordering-change]
---

# SPEC — P4A Work Management Views

## 1. Objective

P4A delivers view/query surfaces over the canonical P3 WorkManagement model.

Sequence:

```text
Query / Filter / Sort
→ Table
→ Kanban
→ Calendar
→ Timeline
→ Dashboard
→ Form
```

After shared query/filter/sort reaches D4+, Calendar/Timeline/Dashboard/Form may proceed in parallel.

## 2. Entry gate

Required from P3:

| Contract | Readiness |
|---|---|
| Board | D5 |
| BoardItem | D5 |
| BoardField | D4+ |
| FieldValue | D4+ |
| Grouping | D4+ |
| Ordering | D4+ |
| protected authz integration | D5 for released surfaces |

## 3. Foundational invariant

All views consume one canonical model:

```text
Board + BoardItem + BoardField + FieldValue + Grouping + Ordering
```

No view may create:

- duplicate Item persistence;
- independent status truth;
- independent ordering truth;
- independent field-value truth;
- authorization rules that bypass WorkManagement/Governance.

## 4. Shared query/filter/sort

Must define:

- typed filter operators by field type;
- stable sort semantics including nulls/ties;
- grouping semantics;
- pagination/cursor behavior;
- server/client responsibility;
- persisted configuration compatibility;
- tenant/resource authorization;
- query-cost bounds.

Arbitrary client expressions do not become trusted database predicates.

## 5. Table

Table is the reference projection.

It must prove:

- canonical rows are BoardItems;
- displayed columns derive from BoardFields/FieldValues;
- inline edits execute normal WorkManagement commands;
- sort/filter/group state does not mutate source data by itself;
- pagination and selection remain identity-stable.

## 6. Kanban

Kanban is a projection over grouping + ordering.

Dragging a card must mutate canonical grouping/ordering state according to product semantics.

It must not create a separate Kanban column/status database.

## 7. Calendar

Calendar maps canonical temporal fields into a temporal projection.

Calendar drag/resize must invoke explicit WorkManagement mutations and preserve timezone/date semantics.

No implicit date-field creation or mutation outside the selected field contract.

## 8. Timeline

Timeline/Gantt-like presentation consumes canonical temporal/dependency data.

It may derive layout, ranges and lanes, but may not become source-of-truth for Item dates, dependencies or order.

## 9. Dashboard

Dashboard owns visualization/query configuration, not WorkManagement facts.

Aggregations must:

- identify authoritative fields;
- define denominator/null behavior;
- respect authorization;
- avoid full-board unbounded scans.

## 10. Form

Form is an input surface mapping submissions to authoritative WorkManagement commands.

Public form capability, when enabled, is write-scoped and does not imply Workspace read visibility.

Validation must be derived from canonical field semantics.

## 11. Persisted view configuration

Persisted filters/views/preferences must be versioned and validated against current Board schema.

Field deletion/type change must not leave silently executable invalid config.

Expected outcomes include explicit invalid/degraded configuration states.

## 12. Authorization

Read access to a view never grants access to hidden source data.

Mutation through a view uses the same resource/action authorization path as direct API mutation.

## 13. Realtime

Realtime view updates must reconcile to authoritative WorkManagement state.

Duplicate/out-of-order delivery is expected.

Realtime-heavy hardening requires Platform recovery D4+.

## 14. Performance

Required properties:

- bounded pagination;
- indexable common filters;
- stable sorting;
- no N+1 field-value loading;
- no per-row authorization calls;
- bounded aggregation for dashboard surfaces.

## 15. Exit gate

P4A is ready when shared query/filter/sort is stable enough that each view consumes it without redefining WorkManagement truth, and every released mutation surface is protected by the P3 authorization/concurrency contract.
