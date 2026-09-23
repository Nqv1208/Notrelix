---
document_id: WRK-CERT-WORK-MANAGEMENT
document_type: workstream-certification
status: active
owner: work-management-team
applies_to:
  - backend
  - work-management
  - p3
  - transactional-core
  - certification
evidence:
  - docs/workstreams/executions/work-management/work-management.spec.md
  - docs/workstreams/executions/work-management/work-management.plan.md
  - docs/workstreams/executions/work-management/work-management.tests.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/teams/work-management.md
review_on:
  - p3-candidate-change
  - p3-gate-change
  - certification-evidence-change
---

# CERTIFICATION — Work Management Transactional Core

## 1. Purpose

This file is the authoritative execution record for P3 readiness.

It is intentionally unfilled at creation time. Documentation may define what must be proven; only executed source/test/CI evidence may certify a capability.

## 2. Allowed status values

```text
NOT_EVALUATED
BLOCKED
PARTIALLY_VERIFIED
VERIFIED
STABLE
NOT_APPLICABLE
```

D5 corresponds to STABLE and requires unresolved critical ownership/security/tenant/concurrency ambiguity to be absent.

## 3. Candidate record

```text
Branch:
Candidate SHA:
Baseline SHA:
Migration head:
OpenAPI contract SHA:
Backend CI run:
Integration/PostgreSQL run:
Architecture run:
Documentation governance run:
Reviewer:
Decision date:
```

Initial status: `NOT_EVALUATED`.

## 4. P3-A certification

### P3A-CERT-001 — Workspace/containment producer contract

Required:

- Account/Workspace containment D4+;
- no ambiguous WorkManagement tenant scope.

Status: NOT_EVALUATED

### P3A-CERT-002 — WorkManagement resource/action contract

Required:

- stable resource category;
- resource-owned Action vocabulary;
- no CLR/HTTP accidental identity.

Status: NOT_EVALUATED

### P3A-CERT-003 — Board core Domain/Data

Required:

- Board stable identity;
- containment;
- lifecycle;
- persistence/RLS evidence.

Status: NOT_EVALUATED

### P3A-CERT-004 — BoardItem core Domain/Data

Required:

- canonical Item identity;
- Board containment;
- core persistence/concurrency model.

Status: NOT_EVALUATED

### P3A-CERT-005 — ownership boundary

Required:

- no Governance private persistence dependency;
- no new production service/project;
- Domain purity.

Status: NOT_EVALUATED

### P3-A decision

```text
Decision: NOT_EVALUATED
Evidence:
Known debt:
Blocks:
Reviewer:
Date:
```

A P3-A PASS only opens safe Domain/Data parallelization. It does not certify protected Application/API release.

## 5. P3 core capability certification

### P3-CERT-001 — Board D5

Required TEST groups:

- WM-TST-BOARD-*;
- relevant WM-TST-AUTHZ-*;
- RLS/migration evidence.

Status: NOT_EVALUATED

### P3-CERT-002 — BoardItem D5

Required:

- Item authority/containment;
- conflict behavior;
- move/group/order interaction;
- protected authorization.

Status: NOT_EVALUATED

### P3-CERT-003 — BoardField D4+

Required:

- stable identity;
- type/settings validation;
- schema evolution safety.

Status: NOT_EVALUATED

### P3-CERT-004 — FieldValue D4+

Required:

- schema compatibility;
- normalization/no-op;
- bulk/concurrency behavior;
- bounded query representation.

Status: NOT_EVALUATED

### P3-CERT-005 — Grouping D4+

Required:

- canonical grouping semantics;
- valid lifecycle;
- no duplicate Item truth.

Status: NOT_EVALUATED

### P3-CERT-006 — Ordering D4+ / D5 for drag-heavy consumers

Required:

- deterministic positions;
- concurrent move proof;
- rebalance proof;
- bounded performance.

Status: NOT_EVALUATED

### P3-CERT-007 — Checklist baseline

Required:

- subordinate authority;
- parent lifecycle;
- concurrency.

Status: NOT_EVALUATED

## 6. Protected authorization certification

### P3-CERT-AUTHZ-001 — canonical authorization path

Required:

- one protected request path;
- authorization before side effect;
- no API-only/handler-local canonical bypass.

Status: NOT_EVALUATED

### P3-CERT-AUTHZ-002 — tenant security

Required:

- allowed;
- denied;
- cross-tenant;
- inactive membership;
- stale revocation.

Status: NOT_EVALUATED

### P3-CERT-AUTHZ-003 — resource scope resolution

Required:

- resource scope derived from authoritative server data;
- client identifiers cannot broaden access.

Status: NOT_EVALUATED

## 7. Persistence and migration certification

### P3-CERT-DATA-001 — RLS/scope

Status: NOT_EVALUATED

### P3-CERT-DATA-002 — clean database

Status: NOT_EVALUATED

### P3-CERT-DATA-003 — supported upgrade

Status: NOT_EVALUATED

### P3-CERT-DATA-004 — no pending model changes

Status: NOT_EVALUATED

### P3-CERT-DATA-005 — Application EF exception containment

Required:

- P3 did not expand EF coupling as a new architectural norm;
- remaining debt is explicitly bounded.

Status: NOT_EVALUATED

## 8. Contract certification

### P3-CERT-CON-001 — API/OpenAPI

Status: NOT_EVALUATED

### P3-CERT-CON-002 — event baseline

Status: NOT_EVALUATED

### P3-CERT-CON-003 — named consumer compatibility

Status: NOT_EVALUATED

## 9. Reliability/performance certification

### P3-CERT-REL-001 — concurrency conflicts

Status: NOT_EVALUATED

### P3-CERT-REL-002 — retry/idempotency

Status: NOT_EVALUATED

### P3-CERT-PERF-001 — large Board core path

Status: NOT_EVALUATED

## 10. Architecture certification

### P3-CERT-ARCH-001 — Domain purity

Status: NOT_EVALUATED

### P3-CERT-ARCH-002 — no cross-context private persistence

Status: NOT_EVALUATED

### P3-CERT-ARCH-003 — frozen project topology preserved

Status: NOT_EVALUATED

### P3-CERT-ARCH-004 — Application orchestration preserved

Status: NOT_EVALUATED

## 11. P3 → P4 handoff matrix

Populate only from executed evidence.

| Capability | Required | Actual | Evidence |
|---|---|---|---|
| Board | D5 | NOT_EVALUATED | |
| BoardItem | D5 | NOT_EVALUATED | |
| BoardField | D4+ | NOT_EVALUATED | |
| FieldValue | D4+ | NOT_EVALUATED | |
| Grouping | D4+ | NOT_EVALUATED | |
| Ordering | D4+ | NOT_EVALUATED | |
| Workspace/Governance integration | D5 protected slice | NOT_EVALUATED | |
| WorkManagement resource/actions | D5 | NOT_EVALUATED | |
| event baseline | D3-D4 where required | NOT_EVALUATED | |

## 12. Final decision

```text
P3 Core:
P3-B Protected Release:
P4A WorkManagement Views:
P4B Documents/Collaboration dependency impact:
P5 Automation producer-event readiness:

Critical blockers:
Known non-blocking debt:
CI failures:
Migration risks:
Contract risks:

Reviewer:
Decision date:
```

Initial decision: `NOT_EVALUATED`.

## 13. Certification rule

Do not convert NOT_EVALUATED to VERIFIED/STABLE from document review alone.

A capability reaches:

- VERIFIED when the intended contract is implemented and the required producer/consumer/security evidence executes successfully;
- STABLE when downstream teams can rely on that contract without active redesign and no critical unresolved debt remains.

