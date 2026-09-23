---
document_id: WRK-SPEC-WORK-MANAGEMENT
document_type: workstream-specification
status: active
owner: work-management-team
applies_to:
  - backend
  - work-management
  - p3
  - boards
  - board-items
  - board-fields
  - field-values
  - groups
  - ordering
  - checklists
  - authorization
  - concurrency
  - migrations
evidence:
  - docs/product/work-management.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/teams/work-management.md
  - docs/workstreams/teams/workspace-governance.md
  - docs/workstreams/cross-team-dependencies.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/domain-modeling.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/testing-and-quality-gates.md
  - docs/delivery/contract-first-delivery.md
  - docs/delivery/migration-policy.md
review_on:
  - work-management-core-contract-change
  - p2-p3-gate-change
  - board-or-item-authority-change
  - field-value-model-change
  - ordering-model-change
  - authorization-contract-change
  - migration-strategy-change
---

# SPEC — Work Management Transactional Core

## 1. Purpose

This SPEC defines the executable scope for Priority 3 of the backend roadmap.

P3 is a brownfield closure and hardening workstream. Existing WorkManagement source is evidence to classify, preserve, repair or retire; it is not proof that the P3 contract is complete.

P3 establishes the canonical transactional work model required before view-heavy and downstream product expansion.

The critical spine is:

```text
Board
→ BoardItem
→ BoardField
→ FieldValue
→ Grouping
→ Ordering
→ Checklist baseline
```

## 2. Authority

This SPEC does not redefine product meaning.

Authority remains:

1. `docs/product/work-management.md` for product semantics;
2. repository architecture docs for bounded-context/data ownership;
3. backend architecture docs for implementation boundaries;
4. accepted ADRs;
5. delivery policy;
6. this execution package;
7. source/tests/CI as implementation evidence.

When source conflicts with higher authority, classify the conflict before changing either side.

## 3. Entry gates

P3 uses two gates inherited from Workspace/Governance execution.

### P3-A — Domain/Data parallelization

May open when all are true:

- Account/Workspace containment is at least D4;
- Workspace/resource containment is at least D4;
- WorkManagement resource category is agreed;
- WorkManagement-owned Action vocabulary needed by the core is at least D4;
- no unresolved ownership ambiguity requires WorkManagement to read Governance private persistence.

P3-A permits:

- Domain hardening;
- persistence mapping/index/RLS work;
- migration preparation;
- internal fixtures;
- query-shape preparation;
- tests that do not claim protected API release readiness.

### P3-B — Protected Application/API release

Requires:

- WorkspaceMember baseline D4+;
- Permission semantics D5 for the representative WorkManagement slice;
- built-in role policy D4+;
- central authorization enforcement D5;
- resource-scope resolution proven from authoritative server data;
- allowed, denied and cross-tenant integration evidence.

P3 may not be certified for protected release from P3-A evidence alone.

## 4. Canonical P3 scope

### WM-P3-01 — Board

Must establish:

- stable Board identity;
- exactly one Account and Workspace containment path;
- explicit lifecycle: active/archive/delete/restore according to product authority;
- no view-specific state promoted into Board authority;
- resource registration/action vocabulary compatible with Governance;
- deterministic events for committed product facts.

### WM-P3-02 — BoardItem

Must establish:

- Item as authoritative work record;
- Board/Workspace/Account scope consistency;
- parent/child rules where supported;
- deterministic lifecycle transitions;
- optimistic concurrency for competing material writes;
- idempotent retry semantics where transport/retry can repeat an intent.

### WM-P3-03 — BoardField

Must establish:

- stable field identity independent of label rename;
- one semantic contract per field type;
- settings/default validation;
- explicit system-field restrictions;
- safe field lifecycle without silently corrupting existing values.

### WM-P3-04 — FieldValue

Must establish:

- value conforms to current field schema;
- normalization and semantic no-op behavior;
- no arbitrary JSON scan as the required query model for query-heavy values;
- bulk update semantics preserve atomicity/concurrency expectations.

### WM-P3-05 — Grouping

Must establish:

- BoardGroup semantics remain distinct from universal status;
- grouping configuration does not create duplicate Item truth;
- group lifecycle and item reassignment behavior are explicit.

### WM-P3-06 — Ordering

Must establish:

- deterministic persisted order;
- concurrency-aware move/reorder;
- stable no-op semantics;
- bounded rebalance behavior;
- no client-only order authority.

### WM-P3-07 — Checklist baseline

Must establish:

- checklist is subordinate work, not a second Board/Item authority;
- parent target lifecycle is explicit;
- completion semantics and deletion behavior are deterministic.

## 5. Explicitly out of P3 core

The following are not required to block P3 core unless they alter a core contract:

- BoardView expansion;
- saved filters/preferences;
- calendar/timeline/dashboard;
- forms;
- approvals;
- workload;
- relations/mirror/rollups/formulas;
- templates beyond compatibility needed by Board creation;
- collaboration UX;
- automation execution depth;
- analytics projections;
- advanced realtime recovery.

They may be classified and preserved, but do not enlarge P3 core certification.

## 6. Brownfield source rule

Current source already contains broad WorkManagement areas in Domain, Application, Infrastructure and API.

Execution starts with classification:

```text
KEEP
→ semantics and architecture already match authority

HARDEN
→ semantic owner is correct but evidence/invariant is incomplete

REFACTOR
→ behavior is valid but implementation boundary is wrong

MIGRATE
→ durable representation or identity must change

RETIRE
→ duplicate/obsolete authority

BLOCKED
→ higher-order decision/dependency is unresolved
```

Do not rewrite working modules simply to make the source resemble this SPEC.

## 7. Application boundary

Application owns complete use-case orchestration.

Protected WorkManagement commands must use the canonical request pipeline for:

- validation;
- tenant/resource resolution;
- authorization;
- expected-version/concurrency;
- transaction policy;
- idempotency when applicable;
- post-commit enrollment.

Handler-local role/permission checks are not the canonical security model.

### Existing EF coupling

`IWorkManagementDbContext` currently exposes EF `DbSet<>` types from Application.

This is transitional evidence under the repository's existing Application EF exception.

P3 rules:

- do not add new EF-specific abstractions merely because this interface exists;
- do not introduce new handler-local persistence patterns;
- when touching a use case, prefer use-case-oriented ports/query abstractions where practical;
- removing the full exception is not a P3 prerequisite unless a touched slice requires it.

## 8. Domain requirements

Domain owns:

- Board/Item/Field/Value/Group/Checklist invariants;
- valid state transitions;
- semantic no-op decisions;
- version changes where business concurrency semantics require them;
- Domain events for owned facts.

Domain must not own:

- authorization policy lookup;
- HTTP concepts;
- EF;
- RLS;
- provider/runtime mechanics.

## 9. Data ownership and tenancy

WorkManagement owns its own persistence.

Every tenant-scoped row must have enough authoritative scope to prevent cross-Account/Workspace access.

Required defense-in-depth:

```text
Application authorization
+
authoritative scope resolution
+
Infrastructure query scoping/RLS
```

No WorkManagement handler may query Governance private tables as its normal authorization contract.

## 10. Authorization contract

WorkManagement owns resource/action meaning for its resources.

Governance owns effective permission evaluation.

The representative P3 protected slice must prove at least:

- allowed Board mutation;
- denied Board mutation;
- cross-tenant denial;
- inactive membership denial;
- stale permission/revocation safety;
- Item authorization resolving through Board/Workspace scope without trusting client scope.

## 11. Concurrency

At minimum, competing writes must be analyzed for:

- Board rename/lifecycle;
- Item mutation;
- Item move/group change;
- field schema change versus value write;
- bulk value updates;
- reorder/rebalance;
- archive/delete versus mutation.

A stale writer must not silently overwrite a committed newer state.

## 12. Idempotency

Idempotency is required where retries can create duplicate authoritative effects.

Candidate cases include:

- create Item from retryable transport;
- move/reorder commands;
- bulk field updates;
- post-commit event enrollment.

Do not add idempotency mechanically to every command. Record the protected duplicate-effect property.

## 13. Events

Public/integration events must expose stable product facts, not aggregate dumps.

P3 event baseline must classify:

- Board created/renamed/archived/deleted/restored;
- Item created/moved/updated/completed/deleted/restored;
- field schema/value changes required by consumers;
- ordering changes only when downstream meaning requires them.

Domain event != integration event.

Compatibility is governed by contract-first delivery.

## 14. API contract

P3 API work must:

- preserve stable resource semantics;
- keep HTTP status/error mapping explicit;
- expose concurrency conflict distinctly from validation/authorization;
- avoid leaking internal EF/domain exception details;
- regenerate OpenAPI/client contracts when public shape changes.

## 15. Migration contract

Any schema-affecting change must classify:

- current durable state;
- target state;
- compatibility window;
- clean-database path;
- supported upgrade path;
- RLS/index/constraint impact;
- rollback or forward-fix strategy;
- pending-model-change evidence.

No destructive migration without explicit data-loss analysis.

## 16. Performance baseline

P3 must protect realistic Board workload characteristics:

- paginated item reads;
- bounded field-value queries;
- indexed Board/Workspace scope;
- efficient group/order retrieval;
- no accidental N+1 authorization or value loading on core list paths.

Optimization must follow measured protected properties, not speculative cache introduction.

## 17. Required handoff to P4

P3 can unlock downstream expansion when:

| Capability | Minimum |
|---|---|
| Board | D5 |
| BoardItem | D5 |
| BoardField | D4+ |
| FieldValue | D4+ |
| Grouping | D4+ |
| Ordering | D4+ |
| Workspace/Governance integration | D5 for protected slice |
| WorkManagement resource/actions | D5 |
| event baseline | D3-D4 where a named consumer requires it |

## 18. Definition of Done

P3 core is done only when:

- canonical core semantics are implemented, not merely represented by tables/endpoints;
- protected requests traverse one authorization path;
- cross-tenant and stale-authority cases fail closed;
- concurrency conflicts are explicit;
- migration evidence passes for every touched schema;
- source contains no newly introduced cross-context private persistence coupling;
- core event/API contracts are intentionally compatible;
- required test groups execute in CI;
- certification records exact candidate SHA and evidence.

