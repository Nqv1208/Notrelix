---
document_id: WRK-TESTS-WORK-MANAGEMENT
document_type: workstream-test-plan
status: active
owner: work-management-team
applies_to:
  - backend
  - work-management
  - p3
  - verification
evidence:
  - docs/workstreams/executions/work-management/work-management.spec.md
  - docs/workstreams/executions/work-management/work-management.plan.md
  - backend/docs/architecture/testing-and-quality-gates.md
  - docs/quality/testing-strategy.md
review_on:
  - p3-requirement-change
  - test-topology-change
  - certification-gate-change
---

# TESTS — Work Management Transactional Core

## 1. Purpose

This document defines executable evidence for P3.

A source type, endpoint, table or test file does not itself prove a requirement. Every critical property below must map to a test at the cheapest reliable seam, with PostgreSQL-realistic evidence where the protected property depends on PostgreSQL/RLS/migration behavior.

## 2. Test layers

```text
T0 static/source/compile guard
T1 Domain
T2 Application
T3 Infrastructure
T4 API/OpenAPI
T5 Architecture
T6 Integration/PostgreSQL
T7 Security/tenant isolation
T8 Concurrency/idempotency
T9 Migration
T10 Performance/reliability
T11 Contract/event
```

## 3. Core traceability

| Requirement | Minimum proof |
|---|---|
| Board identity/containment | T1 + T6 |
| Board lifecycle | T1 + T2 |
| BoardItem authority | T1 + T2 + T6 |
| Field schema/value compatibility | T1 + T2 + T6 |
| Grouping | T1 + T2 |
| Ordering | T1 + T8 |
| Protected authorization | T2 + T6 + T7 |
| Cross-tenant isolation | T6 + T7 |
| Concurrency | T8 |
| RLS | T6 |
| Migration | T9 |
| API/OpenAPI | T4 |
| event compatibility | T11 |

## 4. Board tests

### WM-TST-BOARD-DOM-001 — stable identity

Board rename/settings changes do not replace Board identity.

### WM-TST-BOARD-DOM-002 — Account/Workspace containment

Board cannot be created with empty or inconsistent required scope.

### WM-TST-BOARD-DOM-003 — lifecycle

Valid archive/delete/restore transitions succeed; invalid transitions fail with stable business outcomes.

### WM-TST-BOARD-INT-001 — tenant isolation

Account A/Workspace A Board is not visible or mutable from Account B/Workspace B.

PostgreSQL/RLS-realistic.

### WM-TST-BOARD-APP-001 — protected mutation

Allowed principal succeeds through the canonical authorization path.

### WM-TST-BOARD-APP-002 — denied mutation

Authenticated but unauthorized principal is denied before business side effect.

### WM-TST-BOARD-SEC-001 — stale authority

Removed/inactive membership cannot continue mutating Board via stale cached permission.

## 5. BoardItem tests

### WM-TST-ITEM-DOM-001 — authoritative Item

Create/update/move mutates one canonical Item identity.

### WM-TST-ITEM-DOM-002 — containment

Item cannot reference a foreign Board/Workspace/Account combination.

### WM-TST-ITEM-DOM-003 — parent hierarchy

Root/child transitions obey current product depth/parent rules.

### WM-TST-ITEM-APP-001 — move semantics

Move updates group/order through one use case and produces correct result.

### WM-TST-ITEM-CONC-001 — stale write conflict

Two writers from the same version cannot silently overwrite each other.

### WM-TST-ITEM-IDEMP-001 — retry safety

A retryable create/move command does not produce duplicate authoritative effects when the use case is classified as idempotent.

### WM-TST-ITEM-SEC-001 — scope resolution

Client-supplied Workspace/Account values cannot broaden authority for an Item.

## 6. Field tests

### WM-TST-FIELD-DOM-001 — stable field identity

Rename preserves identity and associated values.

### WM-TST-FIELD-DOM-002 — settings validation

Invalid settings/type combinations are rejected.

### WM-TST-FIELD-DOM-003 — system field restriction

Protected system field behavior cannot be changed by an ordinary generic field mutation.

### WM-TST-FIELD-MIG-001 — persisted type evolution

Persisted type/settings identity change is accompanied by explicit migration/compatibility proof.

## 7. FieldValue tests

### WM-TST-VALUE-DOM-001 — schema compatibility

Value incompatible with current field type/settings is rejected.

### WM-TST-VALUE-DOM-002 — normalization

Equivalent inputs normalize to one semantic representation.

### WM-TST-VALUE-DOM-003 — semantic no-op

Writing the same semantic value does not produce unnecessary state/event churn.

### WM-TST-VALUE-APP-001 — bulk atomicity

Bulk update either follows the documented all-or-nothing contract or returns explicit partial outcomes if product authority allows partial behavior.

### WM-TST-VALUE-CONC-001 — schema/value race

Field schema change racing with value update cannot commit an invalid authoritative state.

### WM-TST-VALUE-PERF-001 — bounded query

Representative filtered/list query does not require full-board arbitrary JSON scanning.

## 8. Grouping tests

### WM-TST-GROUP-DOM-001 — group semantics

BoardGroup does not become implicit universal status.

### WM-TST-GROUP-APP-001 — move updates canonical grouping state

### WM-TST-GROUP-DOM-002 — group lifecycle

Archive/delete/reassignment behavior preserves valid Items.

## 9. Ordering tests

### WM-TST-ORDER-DOM-001 — deterministic order

Given persisted positions, server reconstruction is deterministic.

### WM-TST-ORDER-DOM-002 — no-op reorder

No-op reorder does not create false mutation/event churn.

### WM-TST-ORDER-CONC-001 — concurrent moves

Competing moves do not create invalid/duplicate positions or silent lost update.

### WM-TST-ORDER-CONC-002 — rebalance

Rebalance preserves logical ordering under realistic dataset size.

### WM-TST-ORDER-PERF-001 — bounded reorder

Single move does not rewrite an unbounded full Board under ordinary conditions unless the selected algorithm explicitly requires a bounded rebalance.

## 10. Checklist tests

### WM-TST-CHK-DOM-001 — subordinate authority

Checklist cannot exist as an independent duplicate Board/Item truth.

### WM-TST-CHK-DOM-002 — parent lifecycle

Parent archive/delete behavior is explicit.

### WM-TST-CHK-CONC-001 — toggle race

Concurrent toggle/update does not silently lose committed state.

## 11. Authorization and security tests

### WM-TST-AUTHZ-APP-001 — one decision path

Protected core commands declare/use the canonical resource/action authorization contract.

### WM-TST-AUTHZ-APP-002 — authentication is insufficient

Valid identity without permission is denied.

### WM-TST-AUTHZ-INT-001 — Board allowed

Representative allowed Board action passes.

### WM-TST-AUTHZ-INT-002 — Board denied

Representative denied Board action fails before effect.

### WM-TST-AUTHZ-INT-003 — cross-tenant denial

Foreign Account/Workspace cannot access resource despite guessed valid IDs.

### WM-TST-AUTHZ-INT-004 — inactive membership

Inactive/removed membership loses effective access.

### WM-TST-AUTHZ-ARCH-001 — no handler-local role protocol

Architecture/source guard prevents hard-coded role-name checks from becoming canonical P3 enforcement.

### WM-TST-AUTHZ-ARCH-002 — no Governance private persistence coupling

WorkManagement does not authorize by direct access to Governance private DbSets/tables.

## 12. Persistence/RLS tests

### WM-TST-RLS-001 — Board isolation

PostgreSQL session context constrains Board access.

### WM-TST-RLS-002 — Item isolation

Item access cannot escape parent Workspace/Account scope.

### WM-TST-RLS-003 — pooled connection hygiene

Tenant/RLS session state does not leak between pooled requests.

### WM-TST-INF-INDEX-001 — core scope indexes

Representative Board/Item lookup plans use intended scope/index strategy.

## 13. API/OpenAPI tests

### WM-TST-API-001 — Board contract

Request/response/error mapping matches public semantics.

### WM-TST-API-002 — Item conflict

Optimistic concurrency conflict is distinct from validation/authorization/not-found.

### WM-TST-API-003 — tenant privacy

Foreign resource is mapped according to canonical privacy/error policy.

### WM-TST-OAS-001 — OpenAPI drift

Generated OpenAPI is deterministic and committed when source changes.

## 14. Event tests

### WM-TST-EVT-001 — committed fact only

Integration/public event is not emitted as successful authoritative fact before local commit.

### WM-TST-EVT-002 — stable identity/scope

Event uses stable logical IDs and required Account/Workspace/resource scope.

### WM-TST-EVT-003 — no aggregate dump

Public event excludes unrelated internal state.

### WM-TST-EVT-004 — compatibility

Changed event passes compatibility rules for named consumers/backlog.

## 15. Migration tests

### WM-TST-MIG-001 — clean database

Current model migrates from empty supported database.

### WM-TST-MIG-002 — supported upgrade

Previous supported schema upgrades to candidate without violating WorkManagement invariants.

### WM-TST-MIG-003 — RLS/index/constraint

Migration installs/retains required RLS policies, indexes and constraints.

### WM-TST-MIG-004 — no pending model changes

Candidate has no unintended EF model drift.

### WM-TST-MIG-005 — persisted identity preservation

Field/action/type/order identity changes preserve or explicitly migrate durable meaning.

## 16. Architecture tests

### WM-TST-ARCH-001 — Domain purity

WorkManagement Domain has no EF/HTTP/Infrastructure/provider dependency.

### WM-TST-ARCH-002 — Application orchestration boundary

New core use cases do not add provider/Infrastructure coupling.

### WM-TST-ARCH-003 — no new production project/service

P3 remains inside the frozen five-project backend shape.

### WM-TST-ARCH-004 — EF exception does not expand

Touched core code does not add new unjustified EF-specific coupling beyond the governed transition.

## 17. Reliability/performance tests

### WM-TST-PERF-001 — large Board pagination

Representative large Board read is paginated/bounded.

### WM-TST-PERF-002 — authorization amplification

Core list/query path does not perform unbounded authorization round-trips.

### WM-TST-REL-001 — transaction retry

Transient retry does not duplicate committed semantic effect.

### WM-TST-REL-002 — post-commit retry

Durable enrolled event/work can retry without corrupting authoritative state.

## 18. P3-A gate test group

Minimum:

- WM-TST-BOARD-DOM-001/002;
- WM-TST-BOARD-INT-001;
- WM-TST-ITEM-DOM-001/002;
- WM-TST-AUTHZ-ARCH-002;
- core persistence scope/RLS evidence;
- producer resource/action contract tests.

P3-A does not imply protected release.

## 19. P3-B/core certification group

Must include:

- all P3-A proofs;
- WM-TST-AUTHZ-APP-001/002;
- WM-TST-AUTHZ-INT-001..004;
- Board/Item concurrency;
- Field/Value compatibility;
- Ordering concurrency;
- clean + upgrade migration;
- OpenAPI drift for changed endpoints;
- required event contract tests;
- architecture gates.

## 20. Evidence rule

Certification must name:

- candidate SHA;
- exact test command/job;
- executed test group;
- pass/fail result;
- skipped/blocked critical test;
- known debt;
- readiness decision.

A critical suite that did not execute is not PASS.

