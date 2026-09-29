---
document_id: WRK-PLAN-WORK-MANAGEMENT-VIEWS
document_type: workstream-plan
status: active
owner: work-management-team
applies_to: [backend, work-management, p4a]
evidence:
  - docs/workstreams/executions/work-management-views/work-management-views.spec.md
  - docs/workstreams/teams/work-management.md
  - docs/workstreams/backend-roadmap.md
review_on: [p4a-sequence-change, view-contract-change]
---

# PLAN — P4A Work Management Views

## 1. Execution order

```text
Phase 0  P3 gate + current view inventory
Phase 1  Shared query/filter/sort
Phase 2  Table reference projection
Phase 3  Kanban mutation consistency
Phase 4  Calendar
Phase 5  Timeline
Phase 6  Dashboard
Phase 7  Form
Phase 8  realtime/performance hardening
Phase 9  contract/migration closure
Phase 10 certification
```

## 2. Phase 0 — Inventory

Classify existing Views, SavedFilters, BoardPreferences, Forms and query services as KEEP/HARDEN/REFACTOR/MIGRATE/RETIRE/BLOCKED.

Record candidate SHA and P3 certification state.

## 3. Phase 1 — Query/filter/sort

Lock:

- typed operators;
- null behavior;
- deterministic tie-breaking;
- pagination;
- grouping;
- supported field types;
- query authorization;
- persisted config schema.

Preferred PR: `PR-WMV-01 query foundation`.

## 4. Phase 2 — Table

Use Table as reference consumer of shared query semantics.

Prove inline edits route through canonical commands.

Preferred PR: `PR-WMV-02 table`.

## 5. Phase 3 — Kanban

Map columns/cards to canonical grouping/order semantics.

Prove drag/drop under concurrent mutation.

Preferred PR: `PR-WMV-03 kanban`.

## 6. Phase 4 — Calendar

Resolve exact date/date-range field semantics, timezone behavior and drag mutation contract.

Preferred PR: `PR-WMV-04 calendar`.

## 7. Phase 5 — Timeline

Reuse temporal/dependency facts; do not introduce duplicate schedule truth.

Preferred PR: `PR-WMV-05 timeline`.

## 8. Phase 6 — Dashboard

Define query/aggregation contracts and performance bounds.

Preferred PR: `PR-WMV-06 dashboard`.

## 9. Phase 7 — Form

Map submission to canonical create/update use cases with explicit public/private authorization model.

Preferred PR: `PR-WMV-07 form`.

## 10. Phase 8 — Hardening

Verify:

- realtime reconciliation;
- large-board query plans;
- filter/sort indexes;
- no N+1;
- no authorization amplification;
- persisted config compatibility.

## 11. Phase 9 — Contract/migration

Any view/filter config change requires compatibility classification and migration where persisted representations change.

## 12. Stop conditions

Stop if a view requires:

- separate Item truth;
- separate status/order authority;
- bypass of canonical authz;
- full unbounded JSON scans as normal query model;
- breaking persisted config without migration.

## 13. Certification

Certify shared query foundation first, then individual view readiness. One incomplete optional view does not invalidate already-stable view contracts unless it changes shared semantics.
