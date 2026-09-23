---
document_id: WRK-TESTS-WORK-MANAGEMENT-VIEWS
document_type: workstream-test-plan
status: active
owner: work-management-team
applies_to: [backend, work-management, p4a, testing]
evidence:
  - docs/workstreams/executions/work-management-views/work-management-views.spec.md
  - docs/workstreams/executions/work-management-views/work-management-views.plan.md
review_on: [p4a-requirement-change]
---

# TESTS — P4A Work Management Views

## 1. Shared query tests

- `WMV-TST-QRY-001` typed filter semantics by field type.
- `WMV-TST-QRY-002` deterministic sort including ties/nulls.
- `WMV-TST-QRY-003` pagination has no missing/duplicate rows across stable snapshot.
- `WMV-TST-QRY-004` grouping uses canonical values.
- `WMV-TST-QRY-005` foreign tenant/resource cannot leak through search/filter.
- `WMV-TST-QRY-PERF-001` representative large-board plan is bounded/indexable.

## 2. Table

- `WMV-TST-TBL-001` row identity == BoardItem identity.
- `WMV-TST-TBL-002` inline edit uses canonical mutation.
- `WMV-TST-TBL-003` hidden/unauthorized values are not returned.

## 3. Kanban

- `WMV-TST-KAN-001` column projection does not duplicate status truth.
- `WMV-TST-KAN-002` drag changes canonical grouping/order.
- `WMV-TST-KAN-CONC-001` concurrent drag resolves without invalid order.

## 4. Calendar

- `WMV-TST-CAL-001` selected temporal field is explicit.
- `WMV-TST-CAL-002` timezone/date-only semantics preserved.
- `WMV-TST-CAL-003` drag/resize routes through canonical command.

## 5. Timeline

- `WMV-TST-TIM-001` derived layout does not mutate source.
- `WMV-TST-TIM-002` dependency/date updates use canonical commands.

## 6. Dashboard

- `WMV-TST-DSH-001` aggregation semantics explicit.
- `WMV-TST-DSH-002` authorization is preserved under aggregation.
- `WMV-TST-DSH-PERF-001` dashboard query avoids unbounded board scans.

## 7. Form

- `WMV-TST-FRM-001` submission validates against current field schema.
- `WMV-TST-FRM-002` public form is write-scoped only.
- `WMV-TST-FRM-003` duplicate submission behavior follows idempotency contract.

## 8. Persisted configuration

- `WMV-TST-CFG-001` deleted field produces explicit invalid/degraded config.
- `WMV-TST-CFG-002` renamed field remains bound by stable ID.
- `WMV-TST-CFG-MIG-001` persisted config version upgrades safely.

## 9. Realtime

- `WMV-TST-RT-001` duplicate/out-of-order event converges to authoritative state.
- `WMV-TST-RT-002` gap recovery restores current state.

## 10. Certification group

Shared query/filter/sort cannot exceed D4 without QRY-001..005 and performance/security evidence. Individual view certification additionally requires its mutation/reconciliation tests.
