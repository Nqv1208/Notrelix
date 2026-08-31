---
document_id: WRK-CERT-BACKEND-BOUNDARIES
document_type: workstream-certification
status: active
owner: backend-architecture
applies_to:
  - backend
  - bounded-contexts
  - cross-context-dependencies
  - backend-parallel-delivery
spec:
  - docs/workstreams/executions/backend-boundaries/backend-boundaries.spec.md
plan:
  - docs/workstreams/executions/backend-boundaries/backend-boundaries.plan.md
tests:
  - docs/workstreams/executions/backend-boundaries/backend-boundaries.tests.md
review_on:
  - boundary-spec-change
  - plan-exit-gate-change
  - architecture-fitness-function-change
  - service-extraction-proposal
---

# CERTIFICATION — Backend Boundary Execution

## 1. Purpose

This certification determines when the backend boundary execution is safe to use as a required precondition for broad parallel feature delivery.

Certification proves that the architecture is enforced enough to prevent new hidden cross-context coupling.

It does not certify that Notrelix is a microservice system or that any bounded context should be extracted.

## 2. Certification states

Use only the following states:

```text
NOT_READY
CONDITIONAL
CERTIFIED
```

### NOT_READY

One or more hard boundary blockers remain unresolved.

### CONDITIONAL

The architecture pattern is valid and usable for selected teams/slices, but required fitness gates or dependency-spine contracts are still incomplete.

### CERTIFIED

G0-G3 from PLAN are complete and the boundary model can be required for new cross-context feature work.

## 3. Hard certification invariants

CERTIFIED requires all of the following:

- existing five-project modular-monolith topology remains intact unless a separately accepted architecture decision changes it;
- no service-per-BC topology has been introduced by this execution;
- no candidate service-domain grouping is treated as current deployment authority;
- authoritative business ownership remains defined by canonical context architecture;
- cross-context writes are not implemented through foreign persistence access;
- the first boundary fitness gates prevent new critical violations;
- at least one real cross-context use case proves the model end-to-end;
- dependency readiness reuses D0-D5 rather than introducing a competing scale.

## 4. G0 certification checklist — Boundary integration

Required evidence:

```text
[ ] SPEC/PLAN/TESTS/CERTIFICATION are internally consistent
[ ] canonical architecture docs remain semantic authority
[ ] execution does not authorize new production projects/services
[ ] execution does not freeze Trust/Work/Ecosystem/etc. as service topology
[ ] team plans can reference this package without copying all rules
[ ] D0-D5 remains the only dependency readiness model
```

Failure of any item blocks CERTIFIED.

## 5. G1 certification checklist — Hotspot baseline

Required evidence:

```text
[ ] critical dependency-spine foreign DbContext access audited
[ ] known cross-context mutable Domain references classified
[ ] Common business-semantic hotspots classified
[ ] known cross-context transaction assumptions classified
[ ] event/transport contract hotspots classified
[ ] each retained critical debt has owner + removal trigger
[ ] no unknown red-severity direct foreign write remains in audited critical spine
```

A complete 11-context audit is NOT required for certification.

## 6. G2 certification checklist — Dependency-spine contracts

Required evidence for the first real consumer slice:

```text
[ ] Workspace fact dependency has an explicit semantic surface
[ ] Governance authorization dependency has an explicit semantic surface/adapter path
[ ] Billing entitlement/capability dependency does not require WorkManagement to know plan-tier semantics
[ ] producer contracts do not expose EF/repository/provider implementation types
[ ] consumer ports exist only where semantic translation is justified
[ ] ACL and transport responsibilities are not conflated
[ ] failure/freshness semantics are explicit for synchronous decisions
```

The exact interface names are implementation details; the semantic boundary is what is certified.

## 7. G3 certification checklist — Reference slice

Preferred reference slice: `CreateBoard` or the candidate-SHA equivalent representative protected WorkManagement mutation.

Required evidence:

```text
[ ] WorkManagement owns Board mutation
[ ] Workspace dependency is consumed through approved semantic contract
[ ] Governance dependency is consumed through approved authorization boundary
[ ] Billing dependency is consumed through capability/entitlement boundary
[ ] handler does not inject foreign DbContexts
[ ] handler does not mutate foreign aggregates
[ ] local transaction persists only Work-owned state plus approved local delivery state
[ ] denied/missing/unavailable foreign-dependency paths leave Work mutation uncommitted
[ ] architecture tests cover the relevant forbidden dependencies
[ ] transport-specific details are outside business handler code
```

## 8. Architecture fitness-function certification

Minimum required before broad certification:

```text
BF-001 Foreign DbContext
BF-002 Foreign mutable Domain type
BF-003 Producer Internal/private access
```

Equivalent stronger tests are acceptable.

Each gate must:

- run deterministically;
- produce actionable failure output;
- avoid broad wildcard suppressions;
- identify legacy baseline exceptions narrowly;
- prevent new violations.

BF-004+ may be rolled in later as real source evidence requires them.

## 9. Data-boundary certification

Required evidence:

```text
[ ] shared physical database is not being treated as shared semantic ownership
[ ] audited handlers use own-context persistence abstractions
[ ] cross-context ORM navigation/cascade is not introduced in the reference slice
[ ] any retained physical cross-context FK is classified as a constraint/debt, not mutation authority
[ ] no feature relies on private cross-context DB join as the only permanent business contract
```

## 10. Transaction/consistency certification

Required evidence:

```text
[ ] default mutation transaction belongs to one semantic owner
[ ] any cross-BC atomicity requirement is explicitly reviewed
[ ] synchronous command is not being used as an implicit distributed transaction
[ ] eventual workflows identify commit point and retry/idempotency where applicable
[ ] synchronous fact/decision contracts document freshness/race behavior where relevant
```

## 11. Event-boundary certification

When a certified slice emits a cross-context event:

```text
[ ] source fact is committed before downstream consumption
[ ] Domain Event and Integration Event are conceptually separated
[ ] event represents producer fact, not consumer instruction
[ ] event identity/version/owner are defined
[ ] duplicate delivery behavior is safe where at-least-once delivery applies
```

If the reference slice has no cross-context event, this section may be `Not applicable`; it does not block G3.

## 12. Projection certification

When a local projection is introduced:

```text
[ ] source owner remains explicit
[ ] projection owner is explicit
[ ] freshness/lag is defined
[ ] tenant/security scope is preserved
[ ] duplicate/update/rebuild behavior is tested
[ ] projection is not used as authoritative truth beyond its contract
```

No projection is required merely to obtain certification.

## 13. Team-adoption certification

Before requiring this execution across teams:

```text
[ ] Boundary Card template is usable inside existing PLAN files
[ ] no separate per-use-case file is required
[ ] D2/D3 reversible consumer scaffolding is allowed where roadmap permits
[ ] D4/D5 producer readiness gates remain intact
[ ] STOP conditions pause only the affected boundary/slice, not unrelated team work
[ ] legacy touch-and-fix behavior is defined
```

## 14. Explicit certification failures

The execution MUST NOT be certified if any of the following has been introduced as part of the rollout:

```text
new per-BC production projects without separate ADR
new internal HTTP/gRPC calls merely to simulate future services
service grouping frozen without operational evidence
foreign DbContext/repository accepted as standard cross-context integration
cross-context aggregate graph accepted as standard model
cross-context cascade used for lifecycle orchestration
plan/role/provider implementation strings become consumer contract
GlobalSagaService/global business service locator introduced
Common becomes ownerless business-semantics bucket
```

## 15. Conditional certification

Use CONDITIONAL when the core architecture is valid but one or more rollout items remain incomplete.

Example:

```text
SPEC/PLAN accepted
Workspace/Governance contracts ready
Billing capability refactor still D3
BF-001 active
BF-002 implementation in progress
```

In this state:

- unaffected feature work may continue;
- consumers must obey existing approved boundaries;
- no new violation may be justified by the incomplete rollout.

## 16. Evidence record

Certification evidence should record:

```text
candidate commit SHA
architecture-test command/result
reference-slice test command/result
integration-test command/result
known exceptions/debt
current D-level of dependency-spine contracts
certification state
reviewer/date
```

Evidence may live in the repository's existing evidence/PR/CI mechanism; do not create a new permanent evidence folder unless the delivery architecture requires it.

## 17. Re-certification triggers

Re-run relevant certification when:

```text
bounded-context ownership changes
cross-context interaction mechanism changes
new Public contract class is introduced
transaction authority changes
service extraction is proposed
physical DB ownership changes
architecture fitness functions materially change
Common gains a new business-looking abstraction
```

## 18. Extraction is a separate certification

Completion of this package means:

```text
Notrelix is boundary-disciplined and extraction-ready by design
```

It does NOT mean:

```text
Notrelix should extract services now
```

Any extraction still requires the canonical capability-extraction readiness and a separate accepted architecture decision based on operational evidence.

## 19. Final certification condition

The package is CERTIFIED when the team can demonstrate this statement with source/test evidence:

> A representative consumer use case depends on another bounded context only through stable semantic contracts/adapters, owns its own mutation transaction, and could replace an in-process provider implementation with a remote/runtime adapter later without redesigning the business use case.
