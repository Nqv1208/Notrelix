---
document_id: WRK-PLAN-BACKEND-BOUNDARIES
document_type: workstream-plan
status: active
owner: backend-architecture
applies_to:
  - backend
  - bounded-contexts
  - cross-context-dependencies
  - backend-parallel-delivery
  - architecture-fitness-functions
spec:
  - docs/workstreams/executions/backend-boundaries/backend-boundaries.spec.md
canonical_sources:
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/cross-team-dependencies.md
  - docs/architecture/bounded-context-map.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/architecture/capability-extraction-strategy.md
review_on:
  - boundary-spec-change
  - dependency-spine-change
  - architecture-gate-change
  - cross-context-contract-change
---

# PLAN — Backend Boundary Execution

## 1. Purpose

This PLAN introduces the minimum boundary discipline required before and during parallel backend feature delivery.

It is not a repository-wide refactor program.

It follows the existing backend roadmap rule:

```text
close only the architecture blocker required by the next real product slice
```

The plan therefore combines a small global baseline with rolling per-use-case adoption.

## 2. Master objective

Move the backend from:

```text
semantic boundaries mostly documented
+
some executable persistence boundaries
```

to:

```text
semantic ownership
+
explicit cross-context interaction patterns
+
reference implementations
+
progressive architecture fitness functions
+
team execution integration
```

without stopping normal feature development for a large architecture rewrite.

## 3. Execution sequence

```text
G0  Boundary rule integration
        ↓
G1  Hotspot baseline
        ↓
G2  Dependency-spine contracts
        ↓
G3  Reference slice + first fitness gates
        ↓
G4  Rolling adoption by teams
        ↓
G5  Extraction readiness only when operationally justified
```

G0-G3 are the pre-team hardening minimum.

G4 runs continuously with product work.

G5 is not authorized merely by completing G0-G4.

# G0 — Boundary rule integration

## 4. Objective

Make the execution package discoverable from the existing delivery system without creating a second semantic authority.

## 5. Work

1. Treat `backend-boundaries.spec.md` as operational guidance subordinate to canonical architecture docs.
2. Ensure team PLANs reference this package when a milestone has cross-context dependencies.
3. Reuse the existing D0-D5 dependency readiness model.
4. Do not create a new readiness vocabulary.
5. Do not freeze candidate service groupings.

## 6. G0 exit gate

G0 passes when:

- execution ownership is clear;
- no canonical architecture rule is contradicted;
- team execution authors know where cross-BC rules live;
- no new project/service/database topology is authorized.

# G1 — Hotspot baseline

## 7. Objective

Find high-risk existing coupling without requiring a perfect whole-repository audit.

## 8. Required baseline checks

Audit source for at least:

```text
foreign DbContext injection
foreign repository access
foreign Domain aggregate/entity references
cross-context EF navigation/cascade
business semantics in Application.Common
cross-context synchronous orchestration
cross-context transaction assumptions
Domain Events used as transport contracts
provider DTO/SDK leakage
private shared-DB joins used as integration APIs
```

## 9. Prioritization

Prioritize the current dependency spine and known hotspots:

```text
Identity / Accounts
        ↓
Workspaces
        ↓
Governance
        ↓
WorkManagement

plus

Billing entitlement consumers
```

High-value known seam:

```text
Application.Common.Entitlements
```

because plan/tier language can leak Billing semantics into multiple consumers.

## 10. Baseline policy

Existing violations may be recorded as temporary debt where immediate refactoring would block product delivery.

Rules:

```text
known old violation
→ may be baselined with owner and removal trigger

new violation
→ forbidden

modified/touched violation
→ repair unless explicitly reviewed
```

Do not add blanket suppression for an entire rule family.

## 11. G1 output

A concise baseline inventory grouped by:

```text
edge
owner
current mechanism
risk
required target mechanism
remediation trigger
```

The inventory may live in execution evidence/issue tracking; it does not need one permanent document per edge.

## 12. G1 exit gate

G1 passes when:

- critical dependency-spine violations are known;
- no unknown red-severity foreign write path remains in the audited critical spine;
- new-violation prevention can begin;
- G2 contract work has concrete targets.

# G2 — Dependency-spine contracts

## 13. Objective

Formalize only the cross-context surfaces required by near-term consumers.

Initial targets:

```text
Workspace facts
Governance authorization
Billing entitlement/capability
```

Do not scaffold Public/Ports for all bounded contexts.

## 14. Workspace facts

Consumer needs may include:

```text
workspace existence/state
workspace scope/account containment
membership fact when ownership requires it
```

Requirements:

- producer-owned semantic facts;
- no Workspace aggregate leakage;
- explicit not-found/inactive behavior;
- tenant/account/workspace scope preserved;
- cohesive surface rather than one interface per field.

## 15. Governance authorization

Define a producer-owned authorization capability or approved consumer adaptation that preserves:

```text
principal
resource/action identity
tenant/workspace scope
decision semantics
fail-closed behavior
```

Feature contexts must not depend on role-name implementation details as their authorization API.

Do not force every future hot-path authorization check into a remote call; projection/cache strategy is evaluated later based on real performance/failure needs.

## 16. Billing entitlement/capability

Migration target:

```text
consumer asks for product capability decision
```

not:

```text
consumer asks whether plan >= "Pro"
```

Rules:

- Billing owns plan/subscription/commercial language;
- consumer may own a capability-oriented port if semantic translation is needed;
- decision freshness/revision/race semantics must be explicit for mutation gating;
- existing `Common.Entitlements` consumers are migrated incrementally, not through a mandatory big-bang rewrite.

## 17. Adapter structure

For each real cross-context dependency:

```text
consumer use case
    ↓
producer Public contract
```

or:

```text
consumer use case
    ↓
consumer Port
    ↓
ACL
    ↓
producer Public contract
```

Current topology may use in-process implementation.

Business code must not import network-specific clients.

## 18. G2 tests

At minimum:

- producer semantic contract tests;
- consumer behavior tests with port/contract substitute;
- integration test for producer + consumer happy/denied/not-found paths;
- architecture test proving no foreign DbContext shortcut was added.

## 19. G2 exit gate

G2 passes when at least the contracts required by the first reference product slice are D4 or have an explicit staged D2/D3 consumer plan consistent with the backend roadmap.

# G3 — Reference slice and first fitness functions

## 20. Objective

Prove the model using real feature code, not only interfaces and diagrams.

Primary reference slice:

```text
CreateBoard
```

because it exercises:

```text
WorkManagement ownership
Workspace fact dependency
Governance authorization dependency
Billing entitlement dependency
owned Work transaction
post-commit event behavior
```

## 21. CreateBoard target flow

```text
API
 ↓
CreateBoardCommand
 ↓
resolve execution scope
 ↓
Workspace fact
 ↓
Work authorization decision
 ↓
Work capability/entitlement decision
 ↓
Board domain mutation
 ↓
IWorkManagementDbContext only
 ↓
local transaction
 ├── Board state
 └── outbox/post-commit enrollment if required
 ↓
commit
```

Forbidden:

```text
CreateBoardHandler
→ IWorkspaceDbContext
→ IGovernanceDbContext
→ IBillingDbContext
```

## 22. First architecture fitness functions

Implement narrowly-scoped gates first.

### BF-001 — Foreign DbContext

Context handlers must not inject another context's DbContext abstraction.

Extend the existing test rather than replacing it unnecessarily.

### BF-002 — Foreign Domain aggregate/entity

Cross-context Application code must not reference another context's mutable Domain aggregate/entity implementation except explicit approved SharedKernel primitives.

Prefer semantic/namespace analysis over fragile filename-only checks where practical.

### BF-003 — Private/Internal cross-context dependency

Cross-context dependencies must not target namespaces/types designated Internal/private to the producer.

Public surface enforcement may begin with explicit namespaces actually introduced by G2 rather than requiring every context to adopt a Public folder immediately.

## 23. Gate evolution rule

Do not build every future analyzer before feature delivery.

Subsequent waves may add:

```text
BF-004 cross-context EF navigation/cascade
BF-005 public-contract implementation leakage
BF-006 integration-event ownership/versioning
BF-007 Common business-semantic leakage
BF-008 forbidden provider/network usage in Application
```

Add a gate when:

- real source risk exists;
- rule can be expressed with acceptable signal/noise;
- legacy behavior can be baselined narrowly if necessary.

## 24. Additional reference slices

After CreateBoard, prefer one event-driven proof and one projection proof when those features are actually scheduled:

```text
Automation reacts to Work fact and requests Work-owned mutation
Subscription/entitlement change updates a consumer-owned projection
```

These do not block unrelated teams before they are needed.

## 25. G3 exit gate

G3 passes when:

- one real cross-context feature follows the model end-to-end;
- first fitness functions run in CI/test gates;
- transport-specific details are outside business use-case code;
- no new dependency-spine persistence bypass was introduced;
- evidence is documented in TESTS/CERTIFICATION.

# G4 — Rolling team adoption

## 26. Objective

Make boundary discipline part of normal feature delivery rather than a separate architecture program.

## 27. Per-use-case execution

When a team starts a use case:

```text
identify owning BC
      ↓
identify mutation owner
      ↓
list foreign facts/actions
      ↓
classify each dependency
      ↓
existing D4/D5 contract?
  ├── yes → consume it
  └── no  → producer handshake / D2 design
      ↓
implement behind approved boundary
      ↓
prove local transaction + cross-context behavior
```

## 28. Team PLAN requirement

For every cross-context use case, embed the Boundary Card from SPEC.

Do not create a separate document per use case.

The PLAN must also state:

```text
required producer readiness
parallel work allowed before D4
integration point
STOP condition
exit evidence
```

## 29. Parallel-delivery rule

A team does not wait for another team to complete its entire bounded context.

It waits only for the required dependency contract to reach the readiness level required by the roadmap.

Consumer scaffolding behind ports/adapters may proceed at D2/D3 when reversible.

## 30. Touch-and-fix rule

When feature work touches a known cross-context debt:

1. confirm whether the debt affects the use-case boundary;
2. repair it in the same slice if bounded and low-risk;
3. otherwise isolate it behind an explicit transitional adapter/exception;
4. record removal trigger;
5. prohibit spreading the debt to new code.

## 31. Team-specific near-term guidance

### Identity & Accounts

Produce stable identity/account facts without absorbing Workspace authorization semantics.

### Workspace & Governance

Same team ownership does not merge BCs. Workspace owns lifecycle/membership; Governance owns policy/permission meaning.

### WorkManagement

Treat Workspace, Governance, Billing as explicit dependencies and remain the mutation owner for Board/Item/Field state.

### Documents & Collaboration

Use stable resource references for collaboration targets; do not introduce cross-context ORM ownership.

### Automation & Integrations

Automation owns rule/execution orchestration; Integrations owns provider connection/provider-specific semantics. Provider SDKs must not become Automation domain language.

### Billing & Entitlements

Own plan/subscription semantics and expose stable entitlement/capability decisions without requiring feature contexts to know tier names.

### Analytics & Reporting

Consume events/read feeds/projections; do not force transactional contexts to expose private persistence as reporting contracts.

# G5 — Extraction readiness only when justified

## 32. Admission

G5 begins only for a concrete extraction proposal backed by operational evidence such as:

```text
independent scaling
runtime specialization
reliability isolation
security/trust isolation
provider/network isolation
SLO/deployment cadence
cost/data residency
```

A bounded context existing is not admission evidence.

## 33. Service cohesion analysis

Only at G5 evaluate candidate grouping using:

```text
synchronous interaction density
temporal coupling
failure coupling
change coupling
consistency need
latency sensitivity
scaling/runtime profile
security/SLO/provider isolation
```

Candidate group labels such as Trust/Work/Ecosystem remain hypotheses until an accepted extraction decision exists.

## 34. Extraction path

Follow canonical capability extraction strategy:

```text
contract boundary
→ runtime boundary
→ sole logical writer
→ remove foreign direct reads/writes
→ physical data move last if justified
```

No team may pre-create future service hosts under this PLAN.

# 35. Execution STOP conditions

Pause the affected slice for boundary review when:

```text
foreign persistence access appears necessary
foreign aggregate mutation appears necessary
cross-BC atomic transaction appears necessary
producer ownership is ambiguous
consumer must understand producer private enum/plan/role language
A→B→C→D synchronous chain is emerging
cross-context cascade is proposed
provider DTO leaks across product contexts
new Common business abstraction is proposed
```

The rest of the workstream may continue where independent.

# 36. Completion criteria

This PLAN is considered implemented when:

- G0-G3 are certified;
- teams use the Boundary Card for new cross-context slices;
- dependency readiness remains the existing D0-D5 model;
- architecture tests prevent the first classes of new violations;
- known high-risk legacy debt has owner/remediation triggers;
- no service grouping or physical DB split has been prematurely frozen.
