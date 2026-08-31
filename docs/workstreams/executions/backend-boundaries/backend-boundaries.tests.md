---
document_id: WRK-TESTS-BACKEND-BOUNDARIES
document_type: workstream-tests
status: active
owner: backend-architecture
applies_to:
  - backend
  - bounded-contexts
  - cross-context-contracts
  - architecture-tests
  - integration-tests
spec:
  - docs/workstreams/executions/backend-boundaries/backend-boundaries.spec.md
plan:
  - docs/workstreams/executions/backend-boundaries/backend-boundaries.plan.md
canonical_sources:
  - backend/docs/architecture/testing-and-quality-gates.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
review_on:
  - boundary-spec-change
  - architecture-gate-change
  - cross-context-contract-change
  - integration-test-policy-change
---

# TESTS — Backend Boundary Execution

## 1. Purpose

This TESTS file defines the evidence required to prove that cross-bounded-context feature delivery follows the architecture without requiring premature service extraction.

The test strategy validates four things separately:

```text
semantic ownership
code dependency direction
data/transaction ownership
cross-context runtime behavior
```

A passing feature test alone is not sufficient proof of a valid boundary.

## 2. Test classes

Use the existing repository test projects and quality-gate structure.

Required proof classes are:

```text
Domain tests
Application/use-case tests
Architecture tests
Infrastructure/data tests
Contract tests
Integration tests
Event/idempotency tests where applicable
Tenant/security tests where applicable
```

Do not introduce a new test project solely for this execution unless existing projects cannot express a required gate.

# A. Architecture fitness functions

## 3. BF-001 — Foreign DbContext access

### Intent

A feature handler may not inject/use another bounded context's DbContext abstraction as its integration API.

### Initial implementation

Extend/harden the existing `DbContextBoundaryArchitectureTests` rather than replacing working coverage.

### Required cases

At minimum verify representative forbidden pairs such as:

```text
WorkManagement → IWorkspaceDbContext
WorkManagement → IGovernanceDbContext
WorkManagement → IBillingDbContext
Documents → IWorkManagementDbContext
Collaboration → IDocumentDbContext
Automation → IWorkManagementDbContext
Integrations → IWorkManagementDbContext
```

### Failure message

Must identify:

```text
consumer context
forbidden persistence dependency
source file/type
expected approved alternative
```

### Exit

No new non-baselined violations.

## 4. BF-002 — Foreign mutable Domain type

### Intent

Application code in Context A must not depend on mutable aggregate/entity implementation owned by Context B.

Allowed exceptions must be limited to approved shared-kernel primitives/value types and explicit stable contract types.

### Required checks

Detect references such as:

```text
Features.WorkManagement → Domain.Workspaces.Workspace aggregate
Features.Collaboration → Domain.WorkManagement.BoardItem aggregate
Features.Automation → Domain.Documents.Page aggregate
```

### Preferred implementation

Use compiled/reflection/namespace-aware analysis where practical rather than plain filename substring matching.

### Exit

No new foreign mutable aggregate/entity references.

## 5. BF-003 — Producer private/Internal access

### Intent

Cross-context dependencies must target approved Public/contract surfaces, not producer internals.

### Scope

Start enforcement only for contexts where an explicit Public boundary exists.

Do not force empty Public namespaces across all contexts merely to make this test easy.

### Cases

```text
consumer → Producer.Public.*
→ allowed when catalogued/approved

consumer → Producer.Internal.*
→ forbidden
```

## 6. BF-004 — Cross-context EF navigation/cascade

Introduce when implementation signal is reliable.

Prove that:

- EF navigation does not create mutable aggregate ownership across contexts;
- cross-context cascade delete is not used as lifecycle orchestration;
- reviewed physical FKs are classified and do not expose foreign write authority.

This gate may initially run as targeted mapping inspection rather than a universal analyzer.

## 7. BF-005 — Public-contract leakage

Producer Public contracts must not expose:

```text
DbContext
EF entity
repository implementation
provider SDK DTO
internal Domain implementation detail
network-specific client type
```

## 8. BF-006 — Integration event contract discipline

For versioned cross-context Integration Events, verify where technically expressible:

```text
logical contract identity exists
version is explicit
producer/semantic owner is known
required tenant/resource scope is represented or resolvable
Domain Event implementation type is not being used accidentally as the durable transport contract
```

## 9. BF-007 — Common semantic leakage

Introduce after baseline classification.

The test/review gate should reject new `Application.Common` abstractions that encode business ownership such as:

```text
plan tiers
resource-specific permissions
workspace business state
feature-specific entitlement vocabulary
```

Mechanism-only shared primitives remain allowed.

# B. Contract tests

## 10. Producer Public contract tests

Each producer contract used cross-context must prove:

```text
semantic response meaning
not-found behavior
inactive/invalid state behavior
scope/tenant handling
compatibility expectations
```

Do not test only DTO serialization.

## 11. Consumer-port tests

When a consumer-owned port exists, test the consumer against its own language.

Example:

```text
WorkManagement
IWorkCapabilityPort.CanCreateBoardAsync
```

Tests must not require Billing plan names.

Required outcomes may include:

```text
allowed
denied
limit exceeded
fact unavailable
stale/revision conflict where defined
```

## 12. ACL translation tests

When producer and consumer semantics differ, prove mapping explicitly.

Example categories:

```text
Billing entitlement → Work capability decision
Provider connection status → Integration product state
Governance policy decision → feature authorization result
```

The ACL test should protect against leaking producer-private enum/string semantics into the consumer.

# C. Synchronous dependency tests

## 13. Freshness and race tests

For security/commercial decisions used before mutation, the use-case tests must cover the defined race model.

Possible accepted behaviors include:

```text
decision valid for operation/revision
re-check at commit/use-case boundary
fail on changed revision
explicitly tolerate small race by product rule
```

The test must match the contract; no universal strategy is imposed here.

## 14. Dependency unavailable behavior

Every synchronous foreign dependency must have explicit failure semantics.

Test at least one of the designed outcomes:

```text
fail closed
retryable failure
degraded read path
projection fallback when approved
```

Do not silently treat dependency failure as authorization/entitlement success.

# D. Local transaction and data tests

## 15. Owned transaction proof

For representative mutation use cases, prove that the local commit modifies only owned authoritative state plus approved local delivery/projection state.

Example `CreateBoard`:

```text
Work transaction
├── work-owned Board state
└── outbox/post-commit enrollment
```

The test must not require Workspace/Governance/Billing writes in the same transaction.

## 16. Cross-context atomicity review test

There is no generic automated test for semantic atomicity.

Instead, any use case proposing a multi-BC transaction must include explicit test-plan evidence answering:

```text
what invariant spans contexts
why temporary inconsistency is invalid
who owns rollback semantics
how future extraction would preserve correctness
```

Without this evidence, certification fails.

# E. Integration event tests

## 17. Producer commit-before-delivery

For required durable events, prove:

```text
source business state + outbox enrollment commit together
```

and no consumer-visible authoritative fact is treated as committed before source commit.

## 18. Idempotent consumer

Where delivery is at-least-once, prove duplicate delivery does not duplicate business side effects.

Dedup identity must be scoped correctly to consumer/operation semantics.

## 19. Ordering/revision

Only add ordering tests where business semantics require order.

Do not impose global ordering merely because broker infrastructure supports it.

# F. Projection tests

## 20. Projection correctness

For each consumer-owned local projection, prove:

```text
source event/fact applied
scope retained
revision/sequence handled where defined
duplicate event safe
deletion/tombstone handled
```

## 21. Projection rebuild/recovery

Where the projection is classified as rebuildable, test the supported rebuild path or at minimum verify rebuild inputs and deterministic reconstruction behavior.

## 22. Projection lag behavior

Consumer tests must distinguish:

```text
authoritative current state
```

from:

```text
controlled-staleness projection
```

A stale projection must not be used as a strong invariant unless the product contract explicitly permits it.

# G. Process-manager tests

## 23. Admission

Add these tests only when a real workflow requires a process manager.

Required categories:

```text
happy-path progression
participant retry
duplicate outcome
partial completion
compensation/terminal failure where defined
resume after restart
correlation/causation identity
```

Do not create process-manager test scaffolding before a workflow exists.

# H. Reference-slice tests

## 24. CreateBoard reference slice

Required use-case scenarios:

1. valid Workspace + allowed Governance + capability allowed → Board created;
2. Workspace missing/inactive → no Board mutation;
3. Governance denies → no Board mutation;
4. capability/entitlement denies → no Board mutation;
5. foreign dependency unavailable → behavior matches designed failure policy;
6. only `IWorkManagementDbContext` is used by the WorkManagement handler;
7. successful local commit emits/enrolls only the approved Work-owned fact(s);
8. no test depends on Billing tier strings inside WorkManagement behavior.

## 25. Event-to-command reference slice

When Automation→Work slice is scheduled, prove:

```text
Work fact committed
→ Automation consumes idempotently
→ Automation evaluates rule
→ target Work mutation goes through Work-owned command/port
→ Automation never mutates Work persistence directly
```

## 26. Projection reference slice

When entitlement projection is scheduled, prove:

```text
Billing fact/event
→ consumer projection update
→ consumer use-case reads local projection
→ stale/revision behavior is explicit
→ Billing remains authoritative
```

# I. Team execution evidence

## 27. Boundary Card verification

For each cross-context feature under review, TESTS/CERTIFICATION should verify the PLAN recorded:

```text
owner
foreign dependency
mechanism
freshness/consistency
contract/port/ACL
transaction boundary
failure semantics
current adapter
future remote impact
```

Missing fields are acceptable only when explicitly `None/Not applicable`.

## 28. Readiness evidence

Dependency D-level claims require evidence consistent with existing roadmap definitions.

Examples:

```text
D2
→ reviewed semantic contract/design

D3
→ implementation exists

D4
→ producer/consumer verification passes

D5
→ stable enough for broad parallel downstream dependency
```

# J. CI rollout

## 29. Rollout sequence

Introduce gates progressively:

```text
Wave A
BF-001 foreign DbContext
BF-002 foreign mutable Domain types
BF-003 Internal/private cross-context access

Wave B
BF-004 EF navigation/cascade
BF-005 contract leakage

Wave C
BF-006 event discipline
BF-007 Common semantic leakage
```

Each wave must have acceptable false-positive/legacy handling before becoming a required CI gate.

## 30. Legacy baseline rule

A baseline exception must contain:

```text
exact violation identity
owner
reason
removal trigger
```

Do not allow wildcard exceptions such as:

```text
ignore all WorkManagement cross-context references
```

## 31. Test exit gate

The execution is test-ready for broad team adoption when:

- CreateBoard/reference synchronous slice passes;
- BF-001..BF-003 are implemented or an equivalent stronger gate exists;
- no new critical dependency-spine violation is accepted;
- team PLAN Boundary Cards are reviewable;
- TESTS requirements can be satisfied using existing test projects and CI flow.
