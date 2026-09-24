---
document_id: WRK-PLAN-WORK-MANAGEMENT
document_type: workstream-plan
status: active
owner: work-management-team
applies_to:
  - backend
  - work-management
  - p3
  - transactional-core
  - brownfield-hardening
evidence:
  - docs/workstreams/executions/work-management/work-management.spec.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/teams/work-management.md
  - docs/product/work-management.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - docs/delivery/definition-of-done.md
review_on:
  - p3-sequence-change
  - p2-p3-gate-change
  - work-management-core-scope-change
---

# PLAN — Work Management Transactional Core

## 1. Objective

Convert the existing broad WorkManagement implementation into a certified P3 transactional core without reopening the frozen backend architecture and without treating existing source shape as authority.

The execution strategy is:

```text
inventory
→ classify
→ lock producer contracts
→ harden core Domain/Data
→ integrate authorization
→ close concurrency/idempotency
→ verify migrations/contracts
→ certify
```

## 2. Execution principles

1. Source-first, authority-controlled: inspect candidate SHA before modifying.
2. Brownfield before rewrite: preserve valid behavior.
3. Core before views/extensions.
4. P3-A and P3-B are separate gates.
5. No new cross-context table access.
6. No expansion of the Application EF exception.
7. Migration and contract changes are first-class work, not cleanup after coding.
8. Each PR must protect a coherent property and leave the candidate integrable.

## 3. Phase 0 — Baseline and semantic inventory

### WM-INV-001 — Capture candidate

Record:

- branch;
- HEAD SHA;
- migration head;
- solution/project inventory;
- active backend CI jobs;
- current OpenAPI contract SHA;
- current WorkManagement test inventory.

### WM-INV-002 — Domain inventory

Classify current:

- Boards;
- BoardItems;
- BoardFields/FieldValues;
- BoardGroups;
- ordering primitive;
- Checklists;
- Views/Forms/Approvals/Relations/Templates/Workload.

For each: KEEP / HARDEN / REFACTOR / MIGRATE / RETIRE / BLOCKED.

### WM-INV-003 — Application/API inventory

For every core command/query record:

- request type;
- authorization markers/descriptor;
- resource resolver;
- persistence dependency;
- transaction behavior;
- expected-version semantics;
- idempotency need;
- public endpoint and error mapping.

### WM-INV-004 — Data inventory

Record:

- tables/keys/FKs;
- AccountId/WorkspaceId coverage;
- indexes;
- concurrency tokens/version mapping;
- RLS policies;
- ordering persistence;
- field-value representation;
- migration compatibility.

### WM-INV-005 — Existing test inventory

Map current tests to requirements instead of assuming file count means coverage.

Exit: no unresolved ownership ambiguity in the P3 core.

## 4. Phase 1 — P3-A gate verification

Prove or record blocker for:

- Workspace identity/containment D4+;
- WorkManagement resource category;
- WorkManagement Action vocabulary;
- resource ownership handshake;
- no Governance private persistence dependency.

If P3-A is not open, only inventory/decision work proceeds.

## 5. Phase 2 — Board core

Tasks:

- verify Board Account/Workspace containment;
- harden lifecycle invariants;
- validate Board resource facts;
- remove duplicate Board authority if found;
- verify persistence constraints/indexes/RLS;
- establish Board Domain/API event contract required by named consumers.

Required proof:

- stable identity;
- same-tenant read/write;
- cross-tenant denial at persistence boundary;
- archive/delete/restore semantics;
- optimistic conflict where competing writes matter.

Preferred PR: `PR-WM-01 Board core`.

## 6. Phase 3 — BoardItem core

Tasks:

- verify Item authority and Board containment;
- harden root/child rules;
- make group/move semantics explicit;
- prove version/conflict behavior;
- classify Item key/sequence generation;
- verify item-level resource facts only if the Governance contract requires independent Item authorization.

Required proof:

- no foreign Board/Workspace assignment;
- no silent stale overwrite;
- move is deterministic;
- repeated retry does not duplicate an effect where idempotency applies.

Preferred PR: `PR-WM-02 BoardItem core`.

## 7. Phase 4 — BoardField and FieldValue

### Field schema

- stable field identity;
- type/settings validation;
- system-field capability restrictions;
- schema evolution rules.

### Values

- validation against current schema;
- normalization;
- semantic no-op;
- bulk mutation transaction semantics;
- query representation review.

Required proof:

- incompatible value rejected;
- field rename preserves identity/value association;
- field type/settings change cannot silently invalidate stored values;
- bulk update failure does not partially claim success unless product semantics explicitly allow partial outcome.

Preferred PR: `PR-WM-03 Fields and values`.

## 8. Phase 5 — Grouping and ordering

Tasks:

- classify BoardGroup semantics;
- verify move/group mutation ownership;
- inspect `FractionalIndex` or candidate equivalent;
- prove deterministic ordering;
- define rebalance trigger and concurrency behavior;
- prevent client order from becoming authority.

Required proof:

- concurrent reorders do not create duplicate/invalid positions;
- no-op reorder does not create false version/event churn;
- rebalance preserves logical order.

Preferred PR: `PR-WM-04 Grouping and ordering`.

## 9. Phase 6 — Checklist baseline

Tasks:

- verify parent ownership;
- lifecycle under parent archive/delete;
- item completion/toggle concurrency;
- ensure Checklist cannot become alternate BoardItem authority.

Preferred PR: `PR-WM-05 Checklist baseline`.

## 10. Phase 7 — P3-B authorization integration

This phase cannot certify until Workspace/Governance protected gate is ready.

For each representative protected use case:

```text
request
→ tenant/resource resolution
→ authorization
→ transaction
→ Domain mutation
→ persistence
→ post-commit enrollment
→ result
```

Required cases:

- Board create/update allowed;
- Board update denied;
- cross-Account/Workspace denied;
- inactive membership denied;
- Item mutation inherits/resolves authoritative Board scope;
- resource/action mismatch fails closed;
- no API-only or handler-local bypass becomes canonical.

Preferred PR: `PR-WM-06 Protected authorization integration`.

## 11. Phase 8 — Application persistence-boundary hardening

Inventory all touched core handlers using `IWorkManagementDbContext`.

Rules:

- do not migrate every handler solely to satisfy aesthetics;
- any touched code must not deepen EF coupling;
- introduce use-case-oriented ports/query services where they remove material coupling;
- preserve transaction semantics;
- record remaining EF coupling as bounded debt under the existing exception.

Preferred PR may be folded into WM-01..06 when locality is better.

## 12. Phase 9 — Events and post-commit

For named consumers only:

- classify Domain versus integration event;
- verify stable identity/scope;
- ensure durable enrollment follows the same authoritative commit when required;
- verify duplicate/out-of-order tolerance expectations;
- update event contract registry if public contract changes.

Do not emit giant aggregate snapshots.

Preferred PR: `PR-WM-07 Core event baseline`.

## 13. Phase 10 — API/OpenAPI

For changed public endpoints:

- validate request/response semantics;
- explicit conflict/error taxonomy;
- regenerate `backend/contracts/openapi/notrelix.v1.json`;
- verify generated consumer compatibility;
- classify breaking changes before merge.

Preferred PR: `PR-WM-08 Contract closure`.

## 14. Phase 11 — Migration closure

For every schema change:

- create migration from real previous model;
- run clean database;
- run supported upgrade database;
- validate constraints/indexes/RLS;
- check pending model changes;
- document destructive/irreversible risk;
- verify rollback/forward-fix operational path.

A migration is not complete because `dotnet ef database update` succeeds once.

## 15. Phase 12 — Reliability/performance hardening

Measure and protect:

- large Board item pagination;
- field-value query shape;
- group/order access;
- authorization call amplification;
- transaction retry behavior;
- hot indexes;
- N+1 patterns.

Add cache only where authority/invalidation semantics are explicit.

## 16. Phase 13 — Certification

Record exact candidate SHA.

Certification has two decisions:

### P3-A certification

Can downstream Domain/Data work rely on WorkManagement core producer identities/containment without protected release claims?

### P3-B/P3 core certification

Can P4 consumers rely on the protected transactional core?

Do not mark D5 from static inspection.

## 17. PR decomposition

Preferred sequence:

```text
PR-WM-00 semantic inventory / decision notes
PR-WM-01 Board core
PR-WM-02 BoardItem core
PR-WM-03 Fields and values
PR-WM-04 Grouping and ordering
PR-WM-05 Checklist baseline
PR-WM-06 Protected authorization integration
PR-WM-07 Core event baseline
PR-WM-08 Contract/migration/certification closure
```

PRs may be combined only when the protected property remains reviewable.

## 18. Stop conditions

Stop and escalate when:

- Product authority and source imply two incompatible Board/Item meanings;
- P3 requires changing Governance permission ownership;
- a new bounded context/service appears necessary;
- a handler needs Governance private tables to authorize;
- persisted field/action identity must be broken without migration;
- cross-tenant correctness cannot be proven;
- migration requires destructive loss without explicit approval;
- central authorization order must change.

## 19. Completion gate

P3 is complete when the SPEC DoD is met, required TEST IDs execute, certification is populated with candidate-SHA evidence, and the roadmap P3→P4 readiness levels are justified by executable proof.

