---
document_id: WRK-PLAN-DOCUMENTS-COLLABORATION
document_type: workstream-plan
status: active
owner: documents-collaboration-team
applies_to: [backend, documents, collaboration, p4b]
evidence:
  - docs/workstreams/executions/documents-collaboration/documents-collaboration.spec.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/teams/documents-collaboration.md
review_on: [p4b-sequence-change, target-contract-change]
---

# PLAN — P4B Documents & Collaboration

## 1. Sequence

```text
Phase 0 inventory + P2 gate
Phase 1 Page core
Phase 2 hierarchy/move
Phase 3 Block/content
Phase 4 ordering/query/editor
Phase 5 Comment core
Phase 6 target adapters + authorization
Phase 7 realtime/recovery
Phase 8 lifecycle/retention
Phase 9 contract/migration/performance
Phase 10 certification
```

## 2. Phase 0

Inventory current Domain/Application/Infrastructure/API source for Documents and Collaboration.

Classify KEEP/HARDEN/REFACTOR/MIGRATE/RETIRE/BLOCKED.

Record candidate SHA, Workspace/Governance readiness and Platform realtime readiness.

## 3. Phase 1 — Page

Lock Page identity, containment, lifecycle and authorization resource/action contract.

Preferred PR: `PR-DC-01 Page core`.

## 4. Phase 2 — Hierarchy

Prove:

- no cycle;
- same permitted scope;
- deterministic move/reparent;
- stale move conflict;
- deletion/reparent behavior.

Preferred PR: `PR-DC-02 hierarchy`.

## 5. Phase 3 — Blocks

Lock block identity/type/content validation and compatibility.

Preferred PR: `PR-DC-03 block contract`.

## 6. Phase 4 — Ordering/query/editor

Harden block ordering and query loading; define editor reconciliation without inventing unsupported collaborative editing.

Preferred PR: `PR-DC-04 editor/query`.

## 7. Phase 5 — Comment core

Lock Comment identity/lifecycle/history and author attribution.

Preferred PR: `PR-DC-05 comments`.

## 8. Phase 6 — Target contract

Implement target-owner adapters for Documents first.

Add WorkManagement targets only after resource contract D4+.

Prove no foreign private persistence access.

Preferred PR: `PR-DC-06 target authorization`.

## 9. Phase 7 — Realtime/recovery

Implement event-to-realtime projection, duplicate/out-of-order tolerance and gap recovery.

Preferred PR: `PR-DC-07 realtime`.

## 10. Phase 8 — Lifecycle/retention

Resolve target deletion, comment retention, historical attribution and privacy semantics.

## 11. Phase 9 — Closure

Run migration, OpenAPI/event compatibility and performance evidence.

## 12. Stop conditions

Stop if:

- Documents and Collaboration need merged persistence authority;
- comments require direct foreign-table joins;
- unsupported CRDT/OT is introduced implicitly;
- target deletion semantics are unknown;
- realtime hardening lacks recovery contract;
- cross-tenant target resolution cannot fail closed.
