---
document_id: WRK-CERT-WORK-MANAGEMENT
document_type: workstream-certification
status: active
revision: final-audit-v3
owner: work-management-team
candidate_baseline:
  branch: develop
  sha: 35702d0fa9fb01ed68b0667bab500030d60bd028
supersedes: work-management.certification.v2.md
---

# CERTIFICATION — Work Management Transactional Core (Final-Audit V3)

## 1. Purpose

V3 is the final release-decision artifact. It separates source posture from executable certification and incorporates the final audit findings for ordering serialization, ordinal persistence, read-side Version propagation, brownfield normalization and semantic test completeness.

## 2. State model

### Source posture
- `IMPLEMENTED_UNCERTIFIED` — source direction appears correct but final exact-candidate proof is absent.
- `GAP_CONFIRMED` — source contradicts a normative P3 requirement.
- `LEGACY_DUPLICATE` — duplicate semantic authority exists.
- `DEPENDENCY_BLOCKED` — upstream/platform contract blocks closure.
- `UNKNOWN` — evidence inventory insufficient.

### Certification
- `NOT_EVALUATED` — exact-candidate required evidence not yet executed.
- `BLOCKED` — known material gap prevents release.
- `VERIFIED` — capability-focused exact-candidate evidence passes.
- `STABLE` — D5; compatibility/migration/security/ops/full-CI evidence also passes.
- `NOT_APPLICABLE` — requirement is demonstrably irrelevant with authority.

No `PARTIALLY_VERIFIED` status is used.

## 3. Exact candidate rule

```text
Baseline audit SHA: 35702d0fa9fb01ed68b0667bab500030d60bd028
Final certification SHA: <post-implementation exact SHA>
```

Baseline inspection can establish `GAP_CONFIRMED` and therefore `BLOCKED`; it cannot establish `VERIFIED`.

## 4. Baseline status matrix
| Area | Source posture | Certification | Baseline reason |
|---|---|---|---|
| Board | `IMPLEMENTED_UNCERTIFIED` | `NOT_EVALUATED` | strong core; final version/read/API/mapping proof pending |
| BoardItem | `GAP_CONFIRMED` | `BLOCKED` | numeric/ignored placement + phantom update + duplicate ordering |
| BoardField/Value | `GAP_CONFIRMED` | `BLOCKED` | type/update drift, version propagation, value race evidence |
| BoardGroup | `GAP_CONFIRMED` | `BLOCKED` | normal full-list reorder + string successor |
| FieldOption | `LEGACY_DUPLICATE` | `BLOCKED` | two command families + noncanonical option ordering |
| Checklist | `GAP_CONFIRMED` | `BLOCKED` | aggregate bypass + PATCH toggle defect + ordering/version |
| Ordering lock/adjacency | `GAP_CONFIRMED` | `BLOCKED` | no scope serialization/adjacency mechanism |
| Ordering persistence | `GAP_CONFIRMED` | `BLOCKED` | no explicit C collation; old varchar limits; nonunique indexes |
| Ordering brownfield migration | `UNKNOWN` | `BLOCKED` | duplicate/invalid data not yet inventoried/normalized |
| Direct-scope app RLS | `IMPLEMENTED_UNCERTIFIED` | `NOT_EVALUATED` | mechanism exists; exact focused runtime proof pending |
| Child-table app RLS | `GAP_CONFIRMED` | `BLOCKED` | generic helper skips scope-less child tables |
| Authorization | `IMPLEMENTED_UNCERTIFIED` | `NOT_EVALUATED` | canonical Governance path exists; P3-B exact matrix pending |
| ExpectedVersion write contract | `GAP_CONFIRMED` | `BLOCKED` | nullable→0 pattern conflicts with required runtime |
| Read-side Version | `GAP_CONFIRMED` | `BLOCKED` | canonical DTOs currently omit aggregate Version |
| Idempotency producer | `IMPLEMENTED_UNCERTIFIED` | `NOT_EVALUATED` | server store/mechanism substantial |
| Idempotency first-party | `GAP_CONFIRMED` | `BLOCKED` | logical key ownership/retry propagation incomplete |
| Events/Outbox | `IMPLEMENTED_UNCERTIFIED` | `NOT_EVALUATED` | strong reference flow; final contract reverify needed |
| Cross-context | `IMPLEMENTED_UNCERTIFIED` | `NOT_EVALUATED` | direction correct; reverify after ordering/API cutover |
| API/OpenAPI | `GAP_CONFIRMED` | `BLOCKED` | producer shapes must change |
| First-party consumer | `GAP_CONFIRMED` | `BLOCKED` | ordering/version/idempotency migration required |
| Relational migration | `GAP_CONFIRMED` | `BLOCKED` | normalization/collation/uniqueness/width migration absent |
| RLS deployment | `GAP_CONFIRMED` | `BLOCKED` | explicit child policies absent |
| Worker scope | `DEPENDENCY_BLOCKED` | `NOT_EVALUATED` | Platform-owned semantics |
| Performance/observability | `UNKNOWN` | `NOT_EVALUATED` | final budgets/telemetry evidence absent |
| Semantic test coverage | `IMPLEMENTED_UNCERTIFIED` | `NOT_EVALUATED` | V3 defines 154/154; implementation tests not yet executed |
| P3 final | `GAP_CONFIRMED` | `BLOCKED` | material source and evidence blockers remain |

# Milestone A — P3-A data foundation

## P3A-V3-001 — Tenant/data-session prerequisites

**Baseline source posture:** `IMPLEMENTED_UNCERTIFIED`  
**Baseline certification:** `NOT_EVALUATED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ080–104  
**PLAN:** WM-V3-GATE-001  
**TESTS:** RLS-SESSION-001 + prerequisite suites  

### Required criteria
- [ ] request transaction exists before ordering handler work
- [ ] RLS session lifecycle safe
- [ ] Account/Workspace containment reverified

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3A-V3-002 — Ordering serialization and persistence

**Baseline source posture:** `GAP_CONFIRMED`  
**Baseline certification:** `BLOCKED`  
**Target:** `STABLE`  
**SPEC:** WMREQ041–055,098–104  
**PLAN:** WM-V3-ORDER-001..004; WM-V3-MIG-ORDER-001/002; WM-V3-DATA-001/002  
**TESTS:** ORDER-LOCK-001, ORDER-PLACE-001, ORDER-DB-001..003, ORDER-CONC-001, ORDER-NORM-001  

### Required criteria
- [ ] scope lock occurs before sibling read
- [ ] adjacency exact
- [ ] DB ordinal order equals Domain
- [ ] position text/C collation
- [ ] brownfield normalization complete
- [ ] unique active indexes installed
- [ ] rebalance path preserves logical order

### Baseline blockers
- [ ] none of the lock/collation/normalization/unique V3 mechanism is implemented at baseline

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3A-V3-003 — Parent-derived app RLS

**Baseline source posture:** `GAP_CONFIRMED`  
**Baseline certification:** `BLOCKED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ080–093  
**PLAN:** WM-V3-DATA-003  
**TESTS:** RLS-OPTION-001, RLS-VALUE-001, RLS-CHK-001, RLS-DEPLOY-001  

### Required criteria
- [ ] three explicit child policies exist
- [ ] same-tenant CRUD allowed
- [ ] cross-tenant/null denied
- [ ] BoardItemValue enforces Item+Field same Board
- [ ] policy verification cannot silently skip

### Baseline blockers
- [ ] generic helper currently skips scope-less child tables

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3A-V3-004 — Persistence/validator compatibility

**Baseline source posture:** `GAP_CONFIRMED`  
**Baseline certification:** `BLOCKED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ094–104  
**PLAN:** WM-V3-DATA-004  
**TESTS:** MAP-VALID-001  

### Required criteria
- [ ] Board description 5000 persists
- [ ] validator limits do not exceed Domain
- [ ] DB may be wider than Domain

### Baseline blockers
- [ ] Board description storage currently narrower than Domain

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3A-V3-005 — P3-A gate

**Baseline source posture:** `DEPENDENCY_BLOCKED`  
**Baseline certification:** `BLOCKED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ080–104  
**PLAN:** WM-V3-GATE-001 + P3A units  
**TESTS:** all P3-A families  

### Required criteria
- [ ] P3A-V3-001..004 VERIFIED
- [ ] no unresolved tenant/persistence blocker
- [ ] clean+upgrade deployment path defined and executable

### Baseline blockers
- [ ] ordering/data/RLS blockers open

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```
# Milestone B — P3-B authorization

## P3B-V3-001 — Governance-owned protected path

**Baseline source posture:** `IMPLEMENTED_UNCERTIFIED`  
**Baseline certification:** `NOT_EVALUATED`  
**Target:** `STABLE`  
**SPEC:** WMREQ105–109,145  
**PLAN:** WM-V3-GATE-002  
**TESTS:** AUTH-ARCH-001, AUTH-INT-001  

### Required criteria
- [ ] canonical resource/action descriptors
- [ ] allow/deny/cross-tenant/revoked matrix
- [ ] no handler-local role ladder
- [ ] denial commits no effect

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3B-V3-002 — Automation target authority

**Baseline source posture:** `IMPLEMENTED_UNCERTIFIED`  
**Baseline certification:** `NOT_EVALUATED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ109,127  
**PLAN:** WM-V3-X-001  
**TESTS:** AUTH-AUT-001, X-AUT-001  

### Required criteria
- [ ] explicit actor/scope/resource reconstructed
- [ ] Work reauthorizes target
- [ ] semantic placement intent only
- [ ] idempotency preserved

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```
# Milestone C — core

## P3-V3-BOARD-001 — Board

**Baseline source posture:** `IMPLEMENTED_UNCERTIFIED`  
**Baseline certification:** `NOT_EVALUATED`  
**Target:** `STABLE`  
**SPEC:** WMREQ001–010  
**PLAN:** WM-V3-DOM-BOARD-001; APP-BOARD-001  
**TESTS:** BOARD-DOM-001/002, BOARD-APP-001, BOARD-CONC-001, BOARD-AUTH-001, BOARD-MAP-001  

### Required criteria
- [ ] identity/containment/lifecycle/default schema exact
- [ ] update fields exact
- [ ] Version/ExpectedVersion coherent
- [ ] auth/RLS/mapping pass

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3-V3-ITEM-001 — BoardItem

**Baseline source posture:** `GAP_CONFIRMED`  
**Baseline certification:** `BLOCKED`  
**Target:** `STABLE`  
**SPEC:** WMREQ011–025  
**PLAN:** WM-V3-DOM-ITEM-001; APP-ITEM-001/002  
**TESTS:** ITEM-* + ORDER-* + VER-*  

### Required criteria
- [ ] identity/scope/hierarchy/lifecycle pass
- [ ] create/move locked relative placement
- [ ] duplicate canonical
- [ ] update no phantom fields
- [ ] value atomicity
- [ ] Version/ExpectedVersion/idempotency exact

### Baseline blockers
- [ ] baseline ordering/update drift

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3-V3-FIELD-001 — BoardField / FieldValue

**Baseline source posture:** `GAP_CONFIRMED`  
**Baseline certification:** `BLOCKED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ026–040  
**PLAN:** WM-V3-DOM-FIELD-001; APP-FIELD-001  
**TESTS:** FIELD-*, VALUE-* , VER-*  

### Required criteria
- [ ] unknown type fails
- [ ] Field PATCH is exactly Name?/Settings?/ExpectedVersion
- [ ] BoardField.Rename and UpdateSettings own mutations
- [ ] Type not ordinary PATCH
- [ ] settings/default/options/value invariants pass
- [ ] schema/value race safe
- [ ] Version exposed/required

### Baseline blockers
- [ ] baseline create/update drift

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3-V3-OPTION-001 — FieldOption

**Baseline source posture:** `LEGACY_DUPLICATE`  
**Baseline certification:** `BLOCKED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ034–35,073–76  
**PLAN:** WM-V3-APP-OPTION-001  
**TESTS:** OPTION-ARCH-001, ORDER-PLACE-001, RLS-OPTION-001  

### Required criteria
- [ ] one active BoardField-owned authority
- [ ] normal option move relative/locked
- [ ] full-list reorder maintenance-only
- [ ] parent Field Version used
- [ ] parent-derived RLS

### Baseline blockers
- [ ] duplicate command families + noncanonical placement

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3-V3-GROUP-001 — BoardGroup

**Baseline source posture:** `GAP_CONFIRMED`  
**Baseline certification:** `BLOCKED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ041–055  
**PLAN:** WM-V3-APP-GROUP-001  
**TESTS:** ORDER-PLACE-001, ORDER-DUP-001, ORDER-CONC-001, VER-READ-001  

### Required criteria
- [ ] normal drag one Group under Board scope lock
- [ ] duplicate canonical Group key
- [ ] child relative order preserved
- [ ] Group Version exposed

### Baseline blockers
- [ ] baseline full-list reorder/string successor

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3-V3-CHK-001 — Checklist

**Baseline source posture:** `GAP_CONFIRMED`  
**Baseline certification:** `BLOCKED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ056–068  
**PLAN:** WM-V3-DOM-CHK-001; APP-CHK-001  
**TESTS:** CHK-* + VER-*  

### Required criteria
- [ ] aggregate child ownership
- [ ] PATCH is exactly Title?/IsChecked?/ExpectedVersion
- [ ] DueDate/AssigneeId absent from this P3 PATCH
- [ ] Title uses RenameItem
- [ ] desired-state matrix uses SetItemCompletion
- [ ] toggle separate
- [ ] relative locked ordering
- [ ] Checklist parent Version
- [ ] child RLS

### Baseline blockers
- [ ] direct child mutation + PATCH defect

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```
# Milestone D — concurrency / idempotency

## P3-V3-VER-001 — Read/write optimistic concurrency contract

**Baseline source posture:** `GAP_CONFIRMED`  
**Baseline certification:** `BLOCKED`  
**Target:** `STABLE`  
**SPEC:** WMREQ110–114,137  
**PLAN:** WM-V3-CONC-001/002  
**TESTS:** VER-ARCH-001, VER-READ-001, VER-STALE-001, API-VERSION-001  

### Required criteria
- [ ] explicit P3 request manifest
- [ ] required non-null ExpectedVersion
- [ ] one target mapping
- [ ] owning aggregate Version in reads
- [ ] representative stale writer exact

### Baseline blockers
- [ ] baseline DTO reads omit Version; requests use nullable→0 pattern

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3-V3-ORDER-CONC-001 — Ordering concurrency independent of ExpectedVersion

**Baseline source posture:** `GAP_CONFIRMED`  
**Baseline certification:** `BLOCKED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ050–52,114  
**PLAN:** WM-V3-ORDER-001/003; DATA-002  
**TESTS:** ORDER-LOCK-001, ORDER-CONC-001, ORDER-CONFLICT-001  

### Required criteria
- [ ] same-gap different aggregates serialize
- [ ] unique index defense-in-depth
- [ ] unexpected named collision becomes 409
- [ ] no handler retry

### Baseline blockers
- [ ] mechanism absent

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3-V3-IDEMP-001 — Canonical-attempt idempotency

**Baseline source posture:** `GAP_CONFIRMED`  
**Baseline certification:** `BLOCKED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ115–117,143  
**PLAN:** WM-V3-IDEMP-001  
**TESTS:** IDEMP-FE-001, IDEMP-RETRY-001, IDEMP-INT-001, IDEMP-MISMATCH-001  

### Required criteria
- [ ] key created once per canonical request attempt
- [ ] adapter receives key
- [ ] unchanged-payload transport retry reuses key; changed-payload semantic rebase creates a new key
- [ ] server replay/mismatch exact

### Baseline blockers
- [ ] first-party ownership/propagation incomplete

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```
# Milestone E — migration / deployment

## P3-V3-MIG-001 — Brownfield ordering normalization

**Baseline source posture:** `UNKNOWN`  
**Baseline certification:** `BLOCKED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ055,098–102  
**PLAN:** WM-V3-MIG-ORDER-001/002  
**TESTS:** ORDER-PREFLIGHT-001, ORDER-NORM-001, MIG-UPGRADE-001  

### Required criteria
- [ ] all scopes inventoried
- [ ] affected scopes deterministically normalized
- [ ] ambiguous scope blocks
- [ ] stable IDs/logical order preserved

### Baseline blockers
- [ ] baseline data inventory not executed

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3-V3-MIG-002 — Relational migration

**Baseline source posture:** `GAP_CONFIRMED`  
**Baseline certification:** `BLOCKED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ094–104  
**PLAN:** WM-V3-DEPLOY-001  
**TESTS:** MIG-CLEAN-001, MIG-UPGRADE-001, MIG-HISTORY-001, MIG-PENDING-001, ORDER-DB-*  

### Required criteria
- [ ] upgrade order fixed: preflight→normalize→collation/type→unique indexes→width fixes
- [ ] clean/upgrade pass
- [ ] no pending model
- [ ] history append-only

### Baseline blockers
- [ ] migration absent

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3-V3-RLSDEP-001 — RLS policy deployment

**Baseline source posture:** `GAP_CONFIRMED`  
**Baseline certification:** `BLOCKED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ082–93,104  
**PLAN:** WM-V3-DEPLOY-002  
**TESTS:** RLS-DEPLOY-001 + child RLS  

### Required criteria
- [ ] RLS scripts applied separately after relational migration
- [ ] script identity/hash recorded
- [ ] catalog+runtime app-role proof pass

### Baseline blockers
- [ ] explicit child policies absent

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3-V3-WORKER-001 — Worker-scope boundary

**Baseline source posture:** `DEPENDENCY_BLOCKED`  
**Baseline certification:** `NOT_EVALUATED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ089–90  
**PLAN:** WM-V3-DATA-003  
**TESTS:** RLS-WORKER-001  

### Required criteria
- [ ] P3 does not claim broad worker policy as tenant safety
- [ ] any released tenant worker path has Platform-approved scope semantics

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```
# Milestone F — events / cross-context / realtime

## P3-V3-EVT-001 — Events/outbox

**Baseline source posture:** `IMPLEMENTED_UNCERTIFIED`  
**Baseline certification:** `NOT_EVALUATED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ118–126  
**PLAN:** WM-V3-EVT-001  
**TESTS:** EVT-DOM-001, EVT-CONTRACT-001, OUTBOX-001, OUTBOX-RECOVERY-001, RT-001  

### Required criteria
- [ ] semantic events only
- [ ] published schema/version explicit
- [ ] outbox commit/rollback exact
- [ ] consumer failure isolated
- [ ] realtime post-commit
- [ ] rebalance represented as maintenance invalidation, not N moves

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3-V3-X-001 — Cross-context ownership

**Baseline source posture:** `IMPLEMENTED_UNCERTIFIED`  
**Baseline certification:** `NOT_EVALUATED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ127–129  
**PLAN:** WM-V3-X-001  
**TESTS:** X-AUT-001, X-AR-001, X-COLLAB-001, X-BILL-001, X-INT-001  

### Required criteria
- [ ] no peer private persistence
- [ ] Automation semantic target action
- [ ] Analytics rebuild converges
- [ ] Collaboration target stable

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```
# Milestone G — API / consumer

## P3-V3-API-001 — Producer API/OpenAPI

**Baseline source posture:** `GAP_CONFIRMED`  
**Baseline certification:** `BLOCKED`  
**Target:** `STABLE`  
**SPEC:** WMREQ130–138  
**PLAN:** WM-V3-API-001  
**TESTS:** API-TRANSPORT-001, API-PLACEMENT-001, API-UPDATES-001, API-VERSION-001, API-ERROR-001  

### Required criteria
- [ ] relative placement
- [ ] exact update fields
- [ ] required ExpectedVersion + read Version
- [ ] exact errors including stale-placement/order-conflict
- [ ] transport-only endpoints

### Baseline blockers
- [ ] current contract diverges

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3-V3-FE-001 — First-party consumer

**Baseline source posture:** `GAP_CONFIRMED`  
**Baseline certification:** `BLOCKED`  
**Target:** `STABLE`  
**SPEC:** WMREQ139–143  
**PLAN:** WM-V3-FE-001; IDEMP-001  
**TESTS:** FE-ITEM-001, FE-GROUP-001, FE-FIELD-001, FE-CHK-001, FE-GEN-001, IDEMP-FE-001  

### Required criteria
- [ ] generated V3 types consumed
- [ ] neighbor IDs not numeric position
- [ ] Version supplies ExpectedVersion
- [ ] logical idempotency key supplied
- [ ] reload/rebase on conflicts

### Baseline blockers
- [ ] current adapters require cutover

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```
# Milestone H — architecture / quality

## P3-V3-ARCH-001 — Application boundary

**Baseline source posture:** `IMPLEMENTED_UNCERTIFIED`  
**Baseline certification:** `NOT_EVALUATED`  
**Target:** `STABLE`  
**SPEC:** WMREQ069–79  
**PLAN:** WM-V3-APP-BOUNDARY-001/002  
**TESTS:** APP-ARCH-001, BOARD-SCHEMA-001, OPTION-ARCH-001, CROSS-ARCH-001  

### Required criteria
- [ ] no EF baseline growth
- [ ] ordering lock is a port
- [ ] one BoardSchema authority
- [ ] one FieldOption authority
- [ ] no private cross-context persistence

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3-V3-SEC-001 — Security

**Baseline source posture:** `UNKNOWN`  
**Baseline certification:** `NOT_EVALUATED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ146  
**PLAN:** WM-V3-QUAL-001  
**TESTS:** SEC-JSON-001, SEC-QUERY-001, RLS-* , AUTH-*  

### Required criteria
- [ ] flexible payload bounded
- [ ] query isolation exact
- [ ] raw payload not logged

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3-V3-PERF-001 — Performance

**Baseline source posture:** `UNKNOWN`  
**Baseline certification:** `NOT_EVALUATED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ147–148  
**PLAN:** WM-V3-QUAL-002  
**TESTS:** PERF-BOARD-001, PERF-AUTH-001  

### Required criteria
- [ ] large Board bounded/paginated
- [ ] auth facts no N+1
- [ ] normal drag one entity

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3-V3-OBS-001 — Observability / ordering health

**Baseline source posture:** `UNKNOWN`  
**Baseline certification:** `NOT_EVALUATED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ149–150  
**PLAN:** WM-V3-QUAL-002  
**TESTS:** OBS-001  

### Required criteria
- [ ] failure classes distinguishable
- [ ] max key length/stale-placement/order-conflict/rebalance metrics available
- [ ] safe correlation

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```
# Milestone I — semantic coverage / CI

## P3-V3-TEST-001 — Semantic WMREQ coverage

**Baseline source posture:** `IMPLEMENTED_UNCERTIFIED`  
**Baseline certification:** `NOT_EVALUATED`  
**Target:** `VERIFIED`  
**SPEC:** WMREQ001–154  
**PLAN:** WM-V3-TEST-001  
**TESTS:** work-management.tests.v3.md matrix  

### Required criteria
- [ ] 154 rows present
- [ ] WMREQ001–150 each has >=1 substantive family
- [ ] no meta-only false coverage
- [ ] all referenced family IDs exist

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```

## P3-V3-CI-001 — Full/focused exact-SHA CI

**Baseline source posture:** `UNKNOWN`  
**Baseline certification:** `NOT_EVALUATED`  
**Target:** `STABLE`  
**SPEC:** WMREQ151–154  
**PLAN:** WM-V3-CERT-001  
**TESTS:** DISCOVERY-001, CERT-SHA-001, CERT-BLOCKER-001  

### Required criteria
- [ ] restore/format/build pass
- [ ] all backend projects non-zero/pass
- [ ] all focused P3 families non-zero/pass
- [ ] frontend/OpenAPI/docs green
- [ ] one exact SHA

### Execution record
```text
Candidate SHA:
Source posture:
Test families:
Commands/filters:
Discovered:
Executed:
Passed:
Failed:
Skipped:
PostgreSQL/runtime:
Relational migration evidence:
RLS pack evidence:
OpenAPI/frontend evidence:
CI job/link:
Final certification: NOT_EVALUATED
Reviewer:
Notes:
```
# P3 → P4 handoff
| Consumer | Required Work producer state | Baseline |
|---|---|---|
| Work Management Views P4A | Board/Item STABLE; Ordering STABLE; Field/Value/Group/Checklist VERIFIED+; P3-B STABLE; API/consumer STABLE | `BLOCKED` |
| Documents & Collaboration | BoardItem target/scope + P3-B + cross-context VERIFIED | `BLOCKED` |
| Automation & Integrations | Item/order/idempotency/public action VERIFIED | `BLOCKED` |
| Billing & Entitlements | neutral entitlement boundary VERIFIED when used | `NOT_EVALUATED` |
| Analytics & Reporting | placement snapshot/events + rebalance handling VERIFIED | `NOT_EVALUATED` |
| Platform/Realtime | post-commit identity/revision + Platform recovery dependency explicit | `NOT_EVALUATED` |

# Blocking debt registry
| ID | Baseline blocker |
|---|---|
| `WM3-BLK-001` | ordering advisory lock absent |
| `WM3-BLK-002` | adjacency/stale-placement contract absent |
| `WM3-BLK-003` | ordinal DB collation not explicit |
| `WM3-BLK-004` | old position varchar limits remain |
| `WM3-BLK-005` | brownfield ordering preflight/normalization not executed |
| `WM3-BLK-006` | unique active ordering indexes absent |
| `WM3-BLK-007` | legacy/noncanonical ordering writers remain |
| `WM3-BLK-008` | FieldOption duplicate authority |
| `WM3-BLK-009` | Checklist aggregate/PATCH defects |
| `WM3-BLK-010` | child-table app RLS absent |
| `WM3-BLK-011` | ExpectedVersion nullable contract |
| `WM3-BLK-012` | read-side aggregate Version absent |
| `WM3-BLK-013` | first-party logical idempotency ownership incomplete |
| `WM3-BLK-014` | Board mapping/validator drift |
| `WM3-BLK-015` | producer/consumer API cutover pending |
| `WM3-BLK-016` | exact-candidate V3 semantic suites not executed |

# Certification stop conditions
- **WM3-CERT-STOP-001** — candidate SHA differs across evidence;
- **WM3-CERT-STOP-002** — ordering lock is not held before sibling read;
- **WM3-CERT-STOP-003** — neighbors are not proven current first/last/adjacent;
- **WM3-CERT-STOP-004** — database order differs from Domain ordinal order;
- **WM3-CERT-STOP-005** — unique indexes are installed before deterministic normalization;
- **WM3-CERT-STOP-006** — handler-level collision retry is used despite SaveChanges being outside handler;
- **WM3-CERT-STOP-007** — required ExpectedVersion has no authoritative read-side Version source;
- **WM3-CERT-STOP-008** — idempotency key changes during an unchanged-payload transport retry, or is incorrectly reused after semantic rebase changes the canonical payload;
- **WM3-CERT-STOP-009** — scope-less child table remains unprotected under app role;
- **WM3-CERT-STOP-010** — worker broad policy is used as tenant-safety evidence;
- **WM3-CERT-STOP-011** — any WMREQ001–150 maps only to meta tests;
- **WM3-CERT-STOP-012** — required PostgreSQL focused group is skipped or zero;
- **WM3-CERT-STOP-013** — generated client is edited manually;
- **WM3-CERT-STOP-014** — open blocker is downgraded without evidence/authority.

# Final evidence record
```text
Candidate SHA:
P3-A:
P3-B:
Board:
BoardItem:
BoardField/Value:
FieldOption:
BoardGroup:
Checklist:
Ordering lock/adjacency:
Ordering collation/uniqueness:
Brownfield normalization:
ExpectedVersion/Version:
Idempotency:
App RLS direct:
App RLS child:
Worker dependency:
Relational migration:
RLS policy deployment:
Events/Outbox:
Cross-context:
Realtime:
API/OpenAPI:
First-party consumer:
Architecture:
Security:
Performance:
Observability:
Semantic coverage:
Full CI:
Focused CI:
Blocking debt:
P4A handoff:
FINAL CERTIFICATION: NOT_EVALUATED | BLOCKED | VERIFIED | STABLE
Reviewer:
Date:
```

# Baseline final decision
```text
Baseline SHA: 35702d0fa9fb01ed68b0667bab500030d60bd028
Source posture: GAP_CONFIRMED
P3-A: BLOCKED
P3-B: NOT_EVALUATED (release gate closed)
P3 Core: BLOCKED
P4A gate: BLOCKED
D5: NO
```

# D5 Definition of Done
- [ ] P3-A VERIFIED and P3-B STABLE.
- [ ] Board and BoardItem STABLE.
- [ ] Field/Value/Option/Group/Checklist reach required VERIFIED/STABLE thresholds.
- [ ] ordering scope lock, exact adjacency, ordinal persistence, normalization, uniqueness and maintenance strategy are verified.
- [ ] read Version/write ExpectedVersion and logical idempotency identity are end-to-end exact.
- [ ] direct + child app RLS pass on real PostgreSQL; worker dependency is not falsely certified.
- [ ] relational migration and RLS pack deployment both pass clean/upgrade/deployment evidence.
- [ ] events/outbox/realtime and cross-context consumers remain compatible.
- [ ] producer OpenAPI and first-party consumers agree.
- [ ] security/performance/observability minimums pass.
- [ ] TESTS semantic matrix is 154/154 and mandatory suites execute non-zero on one exact candidate.
- [ ] no WM3-BLK remains open.
