---
document_id: WRK-CERT-BACKEND-BOUNDARIES-V3
document_type: execution-certification
status: draft
owner: backend-architecture
version: 3
audit_snapshot:
  repository: Nqv1208/Notrelix
  branch: main
  commit: 6030d06051e8bbb4844746150be5c1d5d4c53bbd
depends_on:
  - WRK-SPEC-BACKEND-BOUNDARIES-V3
  - WRK-PLAN-BACKEND-BOUNDARIES-V3
  - WRK-TESTS-BACKEND-BOUNDARIES-V3
higher_authorities:
  - RULE.md
  - AGENTS.md
  - backend/AGENTS.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/cross-team-dependencies.md
  - backend/docs/architecture/backend-overview.md
  - backend/docs/architecture/domain-modeling.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/docs/architecture/api-and-contracts.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/testing-and-quality-gates.md
applies_to:
  - backend-boundary-certification
  - cross-context-feature-certification
  - architecture-gate-certification
  - parallel-team-handoff
  - future-service-extraction-admission
review_on:
  - backend-boundary-spec-change
  - backend-boundary-plan-change
  - backend-boundary-tests-change
  - bounded-context-map-change
  - cross-team-contract-change
  - authorization-pipeline-change
  - entitlement-contract-change
  - architecture-gate-change
  - service-extraction-proposal
---

# CERTIFICATION — Backend Boundary Execution V3

## 1. Purpose

This document defines the evidence required to certify backend bounded-context boundary execution.

It does not declare the current repository complete.

It defines:

```text
what must be proven
which boundary milestone is being certified
which test evidence is acceptable
which source evidence is required
which CI evidence is required
which unresolved issues block certification
which debt may remain temporarily
when downstream teams may rely on a boundary
when a future extraction candidate may enter extraction design
```

A Coding Agent MUST NOT mark a boundary task "done" merely because:

```text
code compiles
focused unit tests pass
a new interface exists
a new adapter exists
folder structure looks correct
```

Certification requires executable evidence.

---

## 2. Certification authority chain

Certification is valid only when evidence is consistent with:

```text
SPEC
→ PLAN
→ TESTS
→ actual candidate source
→ actual migrations if any
→ actual focused test execution
→ actual architecture test execution
→ actual required CI/gate execution
→ exact candidate SHA
```

A document checklist without executable evidence is not certification.

A Coding Agent summary without test evidence is not certification.

---

## 3. Certification principles

### Principle 1 — Evidence over intent

A boundary is not certified because:

```text
the design looks clean
the agent says it is future-proof
the port/adapter pattern exists
```

It is certified only when required evidence proves the intended dependency direction and behavior.

---

### Principle 2 — Exact candidate source

Final certification evidence must refer to the exact source state being certified.

At minimum record:

```text
Source baseline SHA
Candidate SHA
```

If work is performed outside Git:

```text
record equivalent immutable source snapshot identifier
```

before final repository integration.

---

### Principle 3 — Source-first validity

Certification fails if the implementation was based on an idealized folder diagram while ignoring actual frozen mechanisms.

Examples:

```text
replacing authorization pipeline with handler port
adding Workspace query when execution context already owns the fact
adding adapter wrappers with no semantic/runtime value
```

Architecture correctness includes preserving valid source authority.

---

### Principle 4 — No skipped critical evidence

A required test suite that did not execute is not a PASS.

A skipped test is:

```text
NOT_EVALUATED
or
BLOCKED
```

depending on why it did not execute.

---

### Principle 5 — Boundary debt must be explicit

Legacy debt may remain when it:

```text
predates the candidate change
does not grow
has concrete source identity
has owner
has risk classification
has migration trigger
```

Hidden debt blocks certification.

---

### Principle 6 — Current modular monolith is a valid target

Certification MUST NOT require:

```text
HTTP between BCs
gRPC between BCs
service discovery
independent databases
service mesh
distributed tracing
network retry
```

unless an actual extraction exists.

Premature distribution is a certification failure when it adds unsupported runtime complexity.

---

### Principle 7 — Service extraction is separate admission

A healthy semantic boundary may remain in-process forever.

Boundary certification does not imply:

```text
extract this BC
```

Extraction requires separate operational evidence and architecture decision.

---

## 4. Certification status values

Use existing repository-style status vocabulary:

```text
NOT_EVALUATED
BLOCKED
PARTIALLY_VERIFIED
VERIFIED
STABLE
NOT_APPLICABLE
```

Do not introduce:

```text
C0
C1
C2
C3
C4
```

or another numeric lifecycle.

---

## 5. Status meaning

### NOT_EVALUATED

Required evidence has not yet been collected.

---

### BLOCKED

A required architecture decision, source ownership, test, migration, security rule, consistency rule or dependency is unresolved.

---

### PARTIALLY_VERIFIED

Some required evidence exists, but the certification gate is incomplete.

This may be acceptable during active implementation.

It is not downstream-stable.

---

### VERIFIED

Implementation and tests satisfy the scoped boundary contract.

Equivalent in maturity intent to a verified/D4-style capability.

It may still require downstream integration or broader stability proof before other teams treat it as frozen.

---

### STABLE

Boundary is safe for broad downstream dependency within its declared compatibility scope.

Equivalent in maturity intent to D5.

Do not grant STABLE solely because one consumer works.

---

### NOT_APPLICABLE

Requirement genuinely does not apply.

Must include rationale.

Example:

```text
Network transport tests:
NOT_APPLICABLE
Reason:
all interactions remain in-process in current modular monolith.
```

---

## 6. Certification severity values

For findings use:

```text
BLOCKER
MAJOR
MINOR
DEBT
INFO
```

These are finding severities, not certification statuses.

---

## 7. BLOCKER

Blocks merge/release of the affected slice unless an explicit architecture exception is accepted.

Examples:

```text
foreign context mutation through foreign DbContext/repository
unclear mutation authority
foreign Domain aggregate used as consumer contract
foreign internal MediatR request from another context
cross-tenant authorization leak
Application direct network/provider client
hidden cross-owner atomicity requirement
cross-context cascade required for business lifecycle
dual authoritative writers
unclear source of truth
```

---

## 8. MAJOR

Significant boundary issue that normally blocks `VERIFIED`.

Examples:

```text
producer Public contract leaks internal enum
consumer hard-codes Billing tier
adapter contains business policy
technical failure leaks raw transport exception
event semantics are ambiguous
projection freshness/rebuild semantics missing
```

---

## 9. MINOR

Does not change semantic ownership or correctness but should be corrected or recorded.

Examples:

```text
naming ambiguity
non-canonical folder in accepted-current layout
missing focused failure message
insufficient documentation of rationale
```

---

## 10. DEBT

Accepted pre-existing issue with:

```text
exact source
Rule ID
risk R0-R3
owner
migration trigger
```

New debt is not automatically DEBT.

A new violation normally blocks unless explicitly approved.

---

## 11. INFO

Observation that does not affect certification.

Example:

```text
candidate adapter may later become gRPC if extraction occurs
```

Do not inflate INFO into speculative implementation work.

---

## 12. Certification record structure

Each boundary certification record should contain:

```text
Scope:
Boundary/use case:
Owning BC:
Workflow owner:
Mutation authorities:

SPEC Rule IDs:
PLAN milestone/work units:
TEST evidence IDs/classes:

Source baseline SHA:
Candidate SHA:

Created files:
Modified files:
Deleted files:

Migration:
Architecture evidence:
Application evidence:
Domain evidence:
Infrastructure evidence:
Integration evidence:
API evidence:
Security evidence:
CI/gate evidence:

Known debt:
Exceptions:
NOT_APPLICABLE rationale:

Status:
Reviewer:
Decision date:
```

Do not prefill successful evidence before execution.

---

## 13. Certification granularity

Certification applies to precise units.

Valid units:

```text
one use case
one producer Public contract
one Consumer Port/adapter seam
one integration event
one projection
one Process Manager
one architecture enforcement wave
one extraction candidate
```

Do not blanket-certify:

```text
WorkManagement
Billing
all backend boundaries
```

without evidence covering that scope.

---

## 14. Boundary certification outcomes

Use four named outcomes for human interpretation:

```text
LOCAL-SAFE
BOUNDARY-VERIFIED
PARALLEL-STABLE
EXTRACTION-READY
```

These outcomes are descriptions over the status/evidence model.

They are not another lifecycle.

---

## 15. LOCAL-SAFE outcome

A local feature is LOCAL-SAFE when:

```text
one BC owns all authoritative mutations
owned persistence only
Domain/Application behavior proven
pipeline behavior preserved
applicable architecture gates pass
no material cross-context boundary is introduced
```

Expected status:

```text
VERIFIED
or
STABLE
```

depending on roadmap maturity.

---

## 16. BOUNDARY-VERIFIED outcome

A cross-context slice is BOUNDARY-VERIFIED when:

```text
consumer/producer known
workflow owner known if any
mutation authorities known
interaction mechanism classified
pipeline-owned vs handler-owned concern correct
Producer.Public vs Consumer Port choice correct
ACL used only when semantic translation exists
adapter used only when runtime/mechanism value exists
no foreign persistence/domain/internal dependency
transaction boundary correct
business vs technical failure model correct
required tests/gates pass
```

Expected certification status:

```text
VERIFIED
```

---

## 17. PARALLEL-STABLE outcome

A producer boundary is PARALLEL-STABLE when:

```text
BOUNDARY-VERIFIED
producer contract has roadmap D4/D5 evidence as appropriate
compatibility behavior understood
downstream consumer contract no longer under active redesign
failure semantics stable
tenant/resource scope stable
architecture enforcement prevents bypass
```

Expected certification status:

```text
STABLE
```

---

## 18. EXTRACTION-READY outcome

Only applies to an approved concrete extraction candidate.

Requires:

```text
PARALLEL-STABLE semantic boundary
foreign persistence/domain/internal access removed on extraction path
Semantic/Integration/Transport contracts classified
remote idempotency/retry policy defined
service auth defined
mixed-version compatibility defined
exactly-one-writer cutover defined
data ownership/cutover defined
observability defined
deployment defined
rollback/roll-forward defined
network/distributed tests pass
operational value exceeds distributed cost
accepted extraction ADR
```

Expected status:

```text
STABLE
```

for the extraction design scope.

EXTRACTION-READY does not mean extraction is already deployed.

---

## 19. Certification relationship to BND milestones

The PLAN defines:

```text
BND-M0..BND-M9
```

Certification does not require every milestone to be complete before feature work.

Each milestone has its own exit evidence.

---

## 20. BND-M0 certification — candidate source inventory

### Required evidence

```text
candidate source SHA
canonical docs read
priority context roots identified
owned DbContext abstractions identified
pipeline involvement identified
current cross-context dependency paths identified
existing architecture tests identified
```

### Required status

```text
VERIFIED
```

for inventory quality.

### BLOCKER conditions

```text
source ownership unclear
candidate source not actually inspected
implementation plan based only on docs
pipeline responsibility unresolved
```

---

## 21. BND-M1 certification — boundary debt baseline

### Required evidence

Each debt entry contains:

```text
DebtId
RuleId
Risk R0-R3
ConsumerBC
ProducerBC
Exact source type/file
Violation
Owner
MigrationTrigger
TargetPattern
```

### Required proof

```text
no wildcard baseline
new violation would still fail
baseline is deterministic
duplicate/unknown entries handled if generic loader exists
```

### Required status

```text
VERIFIED
```

Baseline may contain debt.

Baseline itself must be trustworthy.

---

## 22. BND-M2 certification — Architecture Wave 1

Scope:

```text
ARCH-BC-001
ARCH-BC-002
ARCH-BC-003
```

Required outcome:

```text
new critical boundary violations blocked by normal CI
```

---

## 23. ARCH-BC-001 certification

### Required evidence

```text
existing DbContextBoundaryArchitectureTests coverage preserved
foreign persistence violation detected
owned DbContext allowed
baseline exact if needed
```

### Minimum examples

```text
WorkManagement → IWorkManagementDbContext
PASS

WorkManagement → IWorkspaceDbContext
FAIL
```

### BLOCKER

Architecture gate cannot distinguish owned vs foreign DbContext reliably.

---

## 24. ARCH-BC-002 certification

### Required evidence

```text
Domain→foreign Domain reference coverage
Application→foreign Domain reference coverage
generic/nested referenced type coverage where applicable
stable IDs/Public facts allowed
```

### BLOCKER

Consumer can still depend on producer aggregate/entity as a contract without test failure.

---

## 25. ARCH-BC-003 certification

### Required evidence

```text
producer Public surface allowed
producer internal command/query rejected
foreign internal MediatR request rejected
producer may use MediatR internally
```

### BLOCKER

A consumer can compile/use:

```text
Producer internal command/query
```

without architecture failure.

---

## 26. BND-M2 exit certification

Required:

```text
ARCH-BC-001 VERIFIED
ARCH-BC-002 VERIFIED
ARCH-BC-003 VERIFIED
normal architecture CI lane passes
current baseline does not grow silently
failure messages actionable
```

Outcome:

```text
BOUNDARY-VERIFIED for enforcement Wave 1
```

---

## 27. BND-M3 certification — dependency spine

Scope is the real semantic dependencies currently required by roadmap.

Do not certify speculative Public/Port surfaces.

---

## 28. Pipeline ownership certification gate

For every protected feature:

PASS if:

```text
authentication remains pipeline-owned
Account/Workspace request scope remains pipeline-owned where canonical
resource authorization remains access-pipeline-owned
idempotency remains pipeline-owned
data-session/transaction policy remains pipeline-owned
```

FAIL if Coding Agent created parallel handler dependencies that duplicate these responsibilities.

---

## 29. Workspace Public contract certification

When a new Workspace Public fact exists, prove:

```text
consumer need is real
fact is not already supplied by execution/access context
Workspace owns semantic meaning
contract is narrow
contract does not expose Workspace Domain type
freshness/race semantics known
```

If no extra Workspace fact is needed:

```text
NOT_APPLICABLE
```

is correct.

Do not fail certification because no `IWorkspaceFacts` was created.

---

## 30. Governance boundary certification

For standard protected request:

PASS if:

```text
IRequirePermission/resource/action descriptor remains canonical
AccessControlBehavior remains enforcement owner
producer facts feed evaluator through narrow facts
handler does not hard-code role/policy
handler does not inject duplicate auth decision port
```

A special non-standard workflow may use a Consumer Port only with explicit semantic reason.

---

## 31. Billing capability certification

For each touched consumer classify:

```text
PIPELINE_GATE
USE_CASE_FACT
BILLING_MUTATION
```

PASS requires correct mechanism.

---

## 32. PIPELINE_GATE certification

PASS if:

```text
request declares stable capability/feature requirement
pipeline evaluates producer-owned entitlement/grant semantics
handler does not inject legacy checker
consumer does not hard-code plan/tier
```

---

## 33. USE_CASE_FACT certification

PASS if:

```text
Consumer Port speaks consumer language
Billing Public speaks Billing-owned semantics
ACL exists only if semantics differ
Infrastructure adapter implements runtime call
consumer business code does not know Billing tier/plan
```

---

## 34. BILLING_MUTATION certification

PASS if:

```text
Billing owns mutation
usage/reservation/consumption semantics explicit
consistency model explicit
```

BLOCKER if product mutation directly updates Billing table.

BLOCKER if atomicity with Billing is required but not designed.

---

## 35. Common/Entitlements migration certification

For one migrated consumer, evidence must prove:

```text
consumer inventory performed
touched path migrated
untouched consumers remain compatible
no new tier-name dependency
legacy interface not deleted while live consumers exist
DI remains valid
DevNull/testing path remains safe
```

Do not certify deletion-driven migration.

---

## 36. BND-M3 exit certification

PASS when:

```text
dependency-spine patterns proven on real source
pipeline not duplicated
new product consumers no longer need foreign persistence/internal/domain access
new tier/role semantic leakage prevented
```

Not all legacy consumers need to be migrated.

Outcome:

```text
BOUNDARY-VERIFIED
```

---

## 37. BND-M4 certification — CreateBoardInWorkspace reference slice

This is the canonical pipeline-integrated reference slice.

---

## 38. CreateBoard source-shape certification

Candidate source evidence must confirm current request/handler shape.

Expected existing concepts include:

```text
IWriteRequest
IRequirePermission
IAuthenticatedRequest
IWorkspaceRequest
IIdempotentRequest
IWorkManagementDbContext
ICurrentRequestContext
```

Exact types must be re-read from candidate SHA.

Docs are not enough.

---

## 39. CreateBoard ownership certification

PASS if:

```text
OwningBC = WorkManagement
Board mutation authority = WorkManagement
default BoardField mutation authority = WorkManagement
owned persistence = IWorkManagementDbContext
```

FAIL if:

```text
Workspace/Governance/Billing state mutates in same Work handler transaction
```

---

## 40. CreateBoard authorization certification

PASS if:

```text
permission declared through canonical request/pipeline contract
AccessControlBehavior evaluates it
handler contains no duplicated role/policy logic
```

FAIL if handler-level auth port was added solely to mirror the pipeline.

---

## 41. CreateBoard Workspace dependency certification

If current execution context/access pipeline already provides all required Workspace semantics:

```text
extra Workspace query = unnecessary architecture
```

Certification may fail as overengineering if redundant dependency was added.

If additional Workspace lifecycle fact is truly required:

prove:

```text
producer ownership
narrow Public contract
business failure
freshness/race behavior
```

---

## 42. CreateBoard Billing certification

If feature currently has no Billing requirement:

```text
NOT_APPLICABLE
```

is correct.

Do not add entitlement dependency only to prove architecture.

If Billing gate exists:

prove selected:

```text
PIPELINE_GATE
or
USE_CASE_FACT
```

and corresponding tests.

---

## 43. CreateBoard transaction certification

PASS if:

```text
only Work-owned mutation participates semantically
outbox/realtime enrollment follows existing owned mechanism
transaction remains canonical pipeline/data-session behavior
```

BLOCKER if:

```text
foreign BC state update added into Work transaction
```

without approved consistency design.

---

## 44. CreateBoard architecture evidence

Required:

```text
ARCH-BC-001 PASS
ARCH-BC-002 PASS
ARCH-BC-003 PASS
frozen pipeline tests PASS
relevant application tests PASS
relevant integration tests PASS
```

Outcome target:

```text
BOUNDARY-VERIFIED
```

---

## 45. BND-M5 certification — Automation → Work target-owned mutation

This proves a real context-to-context mutation request.

---

## 46. Automation ownership certification

PASS if:

```text
Automation owns rule evaluation
Automation owns execution lifecycle
Automation owns reaction/orchestration
WorkManagement owns Board/BoardItem mutation
```

FAIL if Automation mutates Work persistence.

---

## 47. Consumer Port certification

Expected:

```text
Automation/Ports/Work/IWorkActionPort
```

or semantically equivalent port.

PASS if:

```text
port represents Automation's need
cohesive
does not expose Work DbContext/aggregate/internal command
```

FAIL if generic:

```text
IWorkService
IDataManager
```

without semantic ownership.

---

## 48. Work Public action certification

PASS if:

```text
Work-owned Public action/facade exists
stable IDs/input
no Work aggregate exposure
producer validates own invariants
producer owns commit
```

Consumer must not reference producer internal MediatR request.

---

## 49. In-process adapter certification

PASS if adapter:

```text
implements Consumer Port
calls Work Public action
maps runtime/technical concerns
contains no Work business rule
```

A zero-value adapter is acceptable only if it implements a necessary Consumer Port/runtime boundary.

Do not create adapter merely for direct same-language Public query.

---

## 50. Background actor/security certification

Automation-triggered Work mutation must prove:

```text
actor/system identity classification
Account scope
Workspace scope
resource scope
authorization semantics
```

In-process trusted code does not automatically bypass producer security contract.

---

## 51. Remote idempotency readiness certification

For current in-process path:

```text
real network retry tests = NOT_APPLICABLE
```

But if operation identity is required by product/retry semantics, record it.

Before future remote retry:

```text
idempotency classification must be VERIFIED
```

---

## 52. Transaction certification for Automation → Work

PASS if architecture does not depend on:

```text
one EF transaction rolling back Automation + Work
```

Required outcome semantics include:

```text
Work success / Automation follow-up failure
Work rejection
duplicate retry
unknown outcome future case
```

If exact cross-owner atomicity is required:

```text
BLOCKED
```

pending workflow/consistency design.

---

## 53. BND-M5 evidence matrix

Required:

```text
Automation Application tests
Work producer tests
adapter tests
DI/integration test
ARCH-BC-001
ARCH-BC-002
ARCH-BC-003
security/scope evidence
idempotency evidence where applicable
```

Outcome:

```text
BOUNDARY-VERIFIED
```

---

## 54. BND-M6 certification — Events

Only touched/new event scope is certified.

No requirement to migrate all historical events.

---

## 55. Event producer certification

PASS if:

```text
producer owns fact
Domain Event vs Integration Event distinction preserved
outward event versioned
canonical registry/outbox path used
state + outbox local commit proven
```

---

## 56. Event semantics certification

Reviewer must answer:

```text
Does this event describe a completed producer-owned fact?
```

If event primarily tells one consumer what to do:

```text
MAJOR/BLOCKER
```

depending on coupling.

Machine name smell is insufficient alone.

Human/agent semantic reasoning is required.

---

## 57. Event consumer certification

PASS if:

```text
consumer owns reaction
duplicate delivery safe
consumer local transaction
retry/poison behavior uses canonical mechanism
```

Producer tests do not need to execute consumer business behavior.

---

## 58. BND-M6 certification — ResourceRef

PASS if:

```text
foreign relationship represented by stable identity
authorization/existence not assumed from reference
cross-context ORM navigation absent
cascade lifecycle removed/explicit
```

Physical FK may remain as:

```text
DEBT
```

with reviewed integrity rationale.

---

## 59. Resource lifecycle certification

For deleted/archived target, explicit behavior must exist.

Examples:

```text
retain foreign record
hide it
mark target unavailable
event-driven cleanup
```

BLOCKER if behavior is merely:

```text
database cascade
```

across BCs.

---

## 60. BND-M6 certification — Projection

PASS if:

```text
source authority clear
consumer projection owner clear
freshness/staleness defined
revision/order defined
rebuild defined
duplicate safe
tenant scope proven
```

Projection never becomes source authority.

---

## 61. Security projection certification

Additional:

```text
fail closed
revocation propagation
wrong-tenant rejection
stale-state policy
revision/invalidation
```

Unauthorized access due to stale projection is BLOCKER.

---

## 62. BND-M6 exit certification

Each mechanism can be independently:

```text
VERIFIED
NOT_APPLICABLE
```

Do not require event + ResourceRef + projection all to exist.

---

## 63. BND-M7 certification — rolling team adoption

This milestone is behavioral/operational.

PASS when new cross-context feature work consistently applies the boundary card and tests.

---

## 64. Per-feature certification card

Every material cross-context feature should record:

```text
UseCase:
OwningBC:
WorkflowOwner:
MutationAuthorities:

ForeignDependencies:
  ProviderBC:
  Need:
  PipelineOrUseCase:
  Mechanism:
  ProducerPublic:
  ConsumerPort:
  ACL:
  Adapter:
  Freshness:
  BusinessFailures:
  TechnicalFailurePolicy:
  Idempotency:
  FutureRemoteImpact:

Transaction:
Events:
Projection:
ResourceRef:

RuleIds:
Tests:
KnownDebt:
Status:
```

---

## 65. Rolling adoption merge gate

A cross-context PR should not merge if reviewer cannot answer:

```text
who owns mutation?
why this mechanism?
why this contract shape?
why pipeline vs handler?
why Public vs Port?
why adapter or why no adapter?
what happens on failure?
what tests prove it?
```

---

## 66. BND-M8 certification — Wave 2

Scope:

```text
ARCH-BC-004
ARCH-BC-005
ARCH-BC-006
```

---

## 67. ARCH-BC-004 certification

PASS if architecture tests reliably reject:

```text
cross-BC ORM navigation
cross-BC cascade
```

while permitting:

```text
stable scalar ID
ResourceRef
same-BC relationships
reviewed physical FK integrity constraint
```

---

## 68. ARCH-BC-005 certification

PASS if:

```text
Features/*/Public/**
```

cannot expose:

```text
DbContext
EF type
Infrastructure type
provider SDK
generated gRPC
ASP.NET type
producer Domain aggregate/entity
internal handler/request
```

No Public folder is required for contexts with no consumer.

---

## 69. ARCH-BC-006 certification

PASS if Domain/Application cannot directly use boundary transport/provider clients.

Examples rejected:

```text
HttpClient in handler
generated gRPC client in Application
provider SDK request in Domain/Application
```

Allowed:

```text
Infrastructure adapter → HttpClient
Application → semantic Port
```

---

## 70. BND-M8 Wave 2 exit

Required:

```text
ARCH-BC-004 VERIFIED
ARCH-BC-005 VERIFIED
ARCH-BC-006 VERIFIED
baseline exact
CI stable
```

Outcome:

```text
STABLE architecture enforcement for critical structural boundaries
```

---

## 71. BND-M8 certification — Wave 3

Scope:

```text
ARCH-BC-007
ARCH-BC-008
ARCH-BC-009 optional
```

---

## 72. ARCH-BC-007 certification

Use existing Events authority.

PASS if:

```text
public outward events registered
versions valid
ownership mapped
new/touched event semantics reviewed
```

---

## 73. ARCH-BC-008 certification

PASS when:

```text
new Common business semantic leakage is blocked/reviewed
Common/Entitlements legacy baseline cannot silently grow
new tier/role/product context enums require owner review
```

Do not require total removal of Common/Entitlements to certify initial anti-regression.

---

## 74. ARCH-BC-009 certification

Default:

```text
NOT_APPLICABLE
```

until dependency catalog admission criteria are met.

If implemented:

prove catalog contains only:

```text
context ID
namespace/path roots
owned DbContext
Public root
reviewed exceptions
```

and not:

```text
future service grouping
transport
roadmap priority
database split
```

---

## 75. BND-M9 certification — extraction admission

BND-M9 remains:

```text
NOT_APPLICABLE
```

unless an extraction candidate exists.

Never mark BND-M9 incomplete as a defect of current modular monolith.

---

## 76. Extraction candidate prerequisite

Before EXTRACTION-READY review:

```text
semantic boundary = PARALLEL-STABLE
consumer dependency inventory complete
foreign persistence removed on path
foreign Domain removed on path
foreign internal Application dependency removed
```

If semantic boundary is not healthy in-process:

```text
BLOCKED
```

Do not use extraction to fix it.

---

## 77. Extraction operational evidence gate

Required measurement:

### Co-host affinity

```text
sync interaction density
consistency pressure
latency sensitivity
hot-path coupling
failure coupling
change coupling
```

### Extraction pressure

```text
independent scaling
runtime specialization
provider isolation
security isolation
SLO/reliability isolation
deployment cadence
cost
data residency
```

Without evidence:

```text
BLOCKED
```

for extraction decision.

---

## 78. Service topology certification

Acceptable conclusions:

```text
remain modular monolith
extract worker only
extract one BC
co-host multiple BCs in one service
one BC produces multiple deployables
defer
```

Certification MUST NOT assume:

```text
1 BC = 1 service
```

---

## 79. Contract-category certification after extraction

Required classification:

```text
Semantic Contract
Integration Contract
Transport Contract
```

BLOCKER if:

```text
generated protobuf DTO becomes Application business model
Domain Event becomes external wire contract by default
HTTP DTO becomes Domain model
```

---

## 80. Remote adapter certification

PASS if:

```text
consumer Application still depends on same semantic Port/Public contract
remote adapter is Infrastructure/runtime
transport concerns isolated
technical errors translated
business rules not moved into adapter
```

---

## 81. Remote inbound adapter certification

PASS if:

```text
gRPC/HTTP/message endpoint
→ producer Application use case
```

FAIL if transport endpoint independently reimplements business rules.

---

## 82. Service identity/security certification

Required:

```text
service identity
actor propagation
Account propagation
Workspace propagation
resource scope
authorization contract
secret/credential lifecycle
```

Internal network is not implicit trust.

---

## 83. Remote command idempotency certification

Before automatic retry:

```text
naturally idempotent
or
stable idempotency key
or
retry forbidden
```

must be proven.

Required unknown-outcome test:

```text
producer commits
response lost
consumer retries
no duplicate semantic mutation
```

---

## 84. Network failure certification

Required according to actual production mechanism:

```text
deadline
timeout
unavailable
auth failure
retry
circuit/load-shed if enabled
fallback/projection if enabled
```

Do not certify/test unused resilience mechanisms.

---

## 85. Exactly-one-writer certification

Mandatory BLOCKER if extraction topology permits:

```text
old runtime authoritative writer
+
new runtime authoritative writer
```

Safe:

```text
shadow/read-only
```

or:

```text
explicit writer cutover
```

Shared physical DB is not a blocker.

Dual logical writer is.

---

## 86. Data cutover certification

Required stage declaration:

```text
shared physical DB + sole logical writer
or
physically separated owned data
```

Prove consumer does not continue to bypass owner through old tables after cutover.

---

## 87. Mixed-version certification

Independent deployables require compatibility evidence.

Test supported matrix:

```text
new consumer → old producer
old consumer → new producer
```

and event version windows as applicable.

No such test is required while all five current projects deploy atomically as one runtime boundary.

---

## 88. Observability certification

Extracted service requires:

```text
health/readiness
logs
metrics
dependency metrics
tracing
alerts
capacity assumptions
failure runbook
```

Do not put observability data into semantic contracts unnecessarily.

---

## 89. Deployment certification

Required:

```text
build artifact
deployment owner
runtime configuration
service discovery/addressing
secrets
health checks
rollout order
```

---

## 90. Rollback/roll-forward certification

Must answer:

```text
who is writer before cutover?
who is writer after cutover?
how is old writer disabled?
can traffic return?
what happens to writes during rollback?
how are event versions handled?
how is partial rollout detected?
```

No answer:

```text
BLOCKED
```

---

## 91. Extraction ADR gate

Even when all technical evidence passes:

```text
EXTRACTION-READY
```

requires accepted architecture decision/ADR.

Boundary execution documents do not self-authorize topology change.

---

## 92. Source placement certification

New files should follow SPEC placement.

But certification does not fail solely because accepted legacy file lives in older valid folder.

Classify:

```text
TARGET
ACCEPTED-CURRENT
MIGRATE-ON-TOUCH
BOUNDARY-DEBT
STOP
```

---

## 93. Folder overengineering certification

Review/FAIL when implementation adds:

```text
empty Public/
empty Ports/
empty CrossContext/
empty Processes/
empty Projections/
```

or placeholder interfaces solely for future architecture.

No semantic need:

```text
do not create file/folder
```

---

## 94. Adapter overengineering certification

Potential MAJOR if:

```text
Producer.Public same-language direct call
```

is wrapped by:

```text
Adapter → Facade → Gateway → Manager
```

without semantic/runtime value.

Every abstraction must answer:

```text
what boundary responsibility does this own?
```

---

## 95. Public contract overexposure certification

Review contract fields/methods.

FAIL if consumer could:

```text
reconstruct producer aggregate internals
branch on producer-private lifecycle state
depend on provider/database shape
```

Prefer smallest stable fact/action.

---

## 96. Consumer Port semantic certification

Port must speak consumer language.

Good:

```text
IWorkEntitlementPort.CanCreateBoard
```

Bad:

```text
IWorkEntitlementPort.HasSubscriptionTier("Pro")
```

Bad:

```text
IBillingService.GetSubscriptionEntity
```

---

## 97. ACL semantic certification

PASS if ACL maps:

```text
producer stable semantic result
→ consumer stable semantic result
```

FAIL if ACL invents producer-owned business policy.

Example forbidden:

```text
plan == Pro → allowed
```

when Billing owns plan/grant mapping.

---

## 98. Infrastructure adapter semantic certification

PASS if adapter owns:

```text
runtime invocation
transport mapping
scope/correlation
technical failure translation
projection storage
```

FAIL if adapter owns:

```text
authorization policy
entitlement policy
aggregate invariant
workflow decision
```

---

## 99. DI certification

For Consumer Port:

PASS if:

```text
Application defines abstraction
Infrastructure registers concrete adapter
API uses normal composition root
```

FAIL if:

```text
Application constructs Infrastructure adapter
API contains business rule choosing semantic behavior
```

---

## 100. Domain purity certification

PASS if Domain remains free from:

```text
cross-context integration topology
Ports
Public transport contracts
Infrastructure
gRPC/HTTP
provider SDK
foreign aggregate dependency
```

---

## 101. API boundary certification

PASS if API owns:

```text
binding
auth middleware integration
Application request construction
HTTP result
OpenAPI
```

FAIL if API coordinates:

```text
Billing mutation
Work mutation
Workspace mutation
```

as business transaction.

---

## 102. Platform boundary certification

PASS if Platform remains mechanism owner.

FAIL if new Platform code branches on:

```text
Board state
Billing plan
Workspace lifecycle
Automation rule semantics
```

---

## 103. Transaction exception certification

Cross-BC transaction may be accepted only with explicit exception record.

Required:

```text
ExceptionId
RuleId
BusinessInvariant
Why owner-local/event/process alternatives fail
Affected BCs
Physical DB dependency
Extraction debt
Owner
Review trigger
Removal plan
Tests
```

Without this:

```text
BLOCKED
```

---

## 104. General exception record

Any boundary exception requires:

```text
ExceptionId:
RuleId:
Scope:
ConsumerBC:
ProducerBC:
Source:
BusinessReason:
WhyPreferredPatternFails:
Risk:
Owner:
ReviewTrigger:
RemovalPlan:
Tests:
Decision:
```

No blanket exception.

---

## 105. Exception expiry/review

Exception must be re-reviewed when:

```text
affected code is materially touched
producer contract changes
runtime extraction proposed
database boundary changes
security model changes
roadmap phase requires dependency stability
```

---

## 106. Legacy baseline certification

A baseline file itself requires evidence.

PASS if:

```text
entries exact
scanner detects before suppression
no wildcard
new violation fails
entry owner/risk known
```

FAIL if baseline is just:

```text
approved current output
```

without semantic ownership.

---

## 107. Baseline shrink certification

When a debt is removed:

```text
violation removed
tests pass without exception
baseline entry deleted
```

All three are required.

Do not keep stale baseline entries.

---

## 108. Test quality certification

Architecture gate is not certified merely because it passes current source.

It must also prove:

```text
known bad fixture fails
known good fixture passes
ordering deterministic
message actionable
```

for custom gate helpers where appropriate.

---

## 109. Architecture-test performance certification

Blocking CI architecture tests must remain:

```text
fast
deterministic
no network
no DB
```

If Roslyn is used:

```text
scope/cache compilation
```

to keep normal PR lane reasonable.

---

## 110. CI certification principle

Boundary tests integrate into existing backend CI lanes.

Do not create a parallel CI architecture unless repository CI authority explicitly requires it.

Certification should record:

```text
focused test jobs
architecture job
affected project jobs
canonical backend gate
```

according to candidate CI.

---

## 111. Exact candidate SHA gate

Final `VERIFIED`/`STABLE` requires test/CI evidence from exact candidate SHA.

If local execution occurred on a different source state:

```text
PARTIALLY_VERIFIED
```

until exact candidate evidence exists.

---

## 112. Missing CI signal

If required CI did not run:

```text
NOT_EVALUATED
```

or:

```text
BLOCKED
```

depending on whether CI itself is unavailable or failed to schedule.

Do not treat missing as green.

---

## 113. Flaky test rule

A required boundary gate that is flaky cannot support STABLE certification.

Classify:

```text
test bug
environment bug
real nondeterminism
```

Fix or remove from blocking evidence with explicit decision.

Do not rerun until green and call that proof.

---

## 114. Migration certification

Boundary-only refactor should normally be schema-neutral.

If DB migration exists, record:

```text
why required
owner
forward migration
rollback/restore policy
cross-context FK impact
cascade impact
RLS impact
```

---

## 115. Physical FK certification

A cross-BC physical FK may be:

```text
DEBT
```

rather than automatic BLOCKER if:

```text
no ORM navigation
no cascade
no mutation authority leak
integrity rationale explicit
extraction debt recorded
```

---

## 116. RLS/tenant certification

Any boundary change that changes query/store path must re-prove:

```text
Account scope
Workspace scope
RLS behavior where relevant
cross-tenant rejection
```

Do not certify architecture at expense of tenant isolation.

---

## 117. Security criticality override

A normally MAJOR issue becomes BLOCKER when it can cause:

```text
authorization bypass
cross-tenant access
service identity bypass
privilege escalation
stale authorization grant
```

Security correctness outranks boundary elegance.

---

## 118. Error model certification

PASS if:

```text
business failures stable
technical failures translated
API maps Application failures consistently
```

FAIL if user/product layer observes raw:

```text
EF exception
SQLSTATE
RpcException
HttpRequestException
provider SDK error
```

---

## 119. Performance certification

Boundary abstraction must not introduce unnecessary current-runtime cost.

Review:

```text
duplicate DB reads
duplicate policy evaluation
serial sync call chains
unnecessary allocations/mapping
network simulation in monolith
```

A semantically correct but pathologically inefficient boundary may be MAJOR.

---

## 120. Sync chain certification

Normal request path:

```text
A → B → C → D
```

is a STOP/review if those boundaries may later become remote and the chain is latency/failure sensitive.

Current in-process chain still needs topology risk classification.

Do not automatically reject all multi-step in-process calls.

---

## 121. Read composition certification

For BFF/API composition:

PASS if:

```text
read-only/presentation aggregation
authorization preserved
partial failure explicit
```

FAIL if composition becomes distributed mutation coordinator.

---

## 122. Process Manager certification

PASS if:

```text
workflow owner clear
durable state where required
participant local transaction
retry/timeout/compensation explicit
duplicate/out-of-order handling
```

FAIL if:

```text
generic saga service owns business meaning
```

---

## 123. Documentation/coding-agent semantic drift gate

Certification reviewer compares implementation with SPEC Rule IDs.

FAIL if agent invented:

```text
new generic service layer
new global Common business vocabulary
new service grouping
new transport abstraction
new pipeline
```

not authorized by execution.

---

## 124. Expected-diff certification

For each implementation compare actual diff with declared:

```text
EXPECTED CREATE
EXPECTED MODIFY
EXPECTED DELETE
FORBIDDEN
```

Unexpected architecture file creation requires explanation.

---

## 125. Deletion certification

A legacy abstraction can be deleted only after proving:

```text
all consumers enumerated
all consumers migrated
DI no longer references it
tests no longer reference it
API/event/generated consumers unaffected
```

Otherwise:

```text
BLOCKED
```

for deletion.

---

## 126. Compatibility adapter certification

Temporary compatibility adapter may be accepted when:

```text
migration is staged
legacy consumers still exist
new canonical contract exists
adapter direction is clear
removal trigger exists
```

It must not become permanent duplicate authority.

---

## 127. "No code change required" certification

A milestone may legitimately certify:

```text
source already conforms
```

with:

```text
architecture tests
application tests
source inspection
```

Do not require production code churn for certification.

This is especially relevant to `CreateBoardInWorkspace`.

---

## 128. Coding Agent completion declaration format

When an agent completes a boundary task, its final report should contain:

```text
Scope:
Candidate source:
Rules implemented:
Milestone:
Outcome:

Created:
Modified:
Deleted:

Tests run:
Architecture gates:
CI:

Debt removed:
Debt remaining:
Exceptions:

NOT_APPLICABLE:
Future remote impact:

Certification status:
```

No vague:

```text
Done
All tests pass
Architecture improved
```

---

## 129. Required answer: future remote impact

For every sync cross-context boundary, agent must state:

```text
If producer becomes remote, which consumer business code changes?
```

Healthy answer:

```text
consumer business handler unchanged;
runtime adapter/DI/transport/package references change
```

Possible acceptable caveat:

```text
semantic contract namespace/package reference moves
```

Unhealthy answer:

```text
handler must be rewritten around HTTP/gRPC
```

That blocks BOUNDARY-VERIFIED.

---

## 130. Required answer: pipeline impact

Agent must state:

```text
Which request concerns remain owned by frozen pipeline?
```

For protected normal requests this may include:

```text
authn
execution context
workspace/account scope
authorization
idempotency
transaction
```

If agent moved these into handler without architecture decision:

```text
BLOCKED
```

---

## 131. Required answer: authoritative writes

Agent must list:

```text
every authoritative state changed
→ owning BC
→ transaction
```

If same state has two owners:

```text
BLOCKED
```

---

## 132. Required answer: foreign reads

For each foreign read:

```text
why authoritative sync?
why projection?
why stable reference only?
```

No answer:

```text
because same DB
```

is acceptable.

---

## 133. Required answer: failures

Agent must separate:

```text
BusinessFailures
TechnicalFailurePolicy
```

Do not certify a semantic contract polluted with future network errors.

---

## 134. Required answer: idempotency

For retryable mutation:

```text
operation identity
same-key/same-request behavior
same-key/different-request behavior
unknown outcome behavior
```

must be explicit.

---

## 135. Required answer: event ownership

For outward event:

```text
Producer:
Completed fact:
Version:
Consumers:
Idempotency:
```

must be clear.

Producer should not own consumer reaction.

---

## 136. Required answer: projection ownership

For projection:

```text
SourceAuthority:
ProjectionOwner:
Freshness:
Revision:
Rebuild:
Failure:
```

must be clear.

---

## 137. Merge gate for normal local feature

May merge when:

```text
LOCAL-SAFE
required project tests pass
architecture tests pass
no new boundary debt
```

No cross-context certification card required if feature is truly local.

---

## 138. Merge gate for cross-context feature

May merge when:

```text
BOUNDARY-VERIFIED
all BLOCKER/MAJOR findings resolved or explicitly accepted
required focused tests pass
required architecture gates pass
exact candidate source evidence available
```

Pre-existing DEBT may remain.

---

## 139. Downstream handoff gate

A producer contract may be advertised for broad downstream use when:

```text
PARALLEL-STABLE
roadmap D-level appropriate
compatibility/failure semantics stable
producer contract tests pass
architecture purity passes
```

---

## 140. Release gate

Release certification follows repository canonical release/CI gates.

Boundary certification adds:

```text
no unresolved BLOCKER in release path
no unreviewed MAJOR boundary regression
exact candidate CI evidence
```

It does not replace release governance.

---

## 141. Extraction proposal gate

A service-extraction proposal may enter implementation only when:

```text
EXTRACTION-READY
+
accepted architecture ADR
```

Without both:

```text
remain current topology
```

---

## 142. Certification anti-patterns

Never certify based on:

```text
folder presence
interface count
mock call count
"looks like hexagonal architecture"
"microservice-ready" claim
local happy path only
manual UI smoke only
PR approval alone
baseline snapshot alone
rerunning flaky test until green
```

---

## 143. Architecture anti-overengineering gate

A boundary implementation may fail certification for unnecessary complexity.

Examples:

```text
gRPC between current in-process BCs
service discovery without services
remote-style retries for local method calls
project per BC
database per BC without extraction
zero-value adapters
empty Public/Ports trees
generic saga framework with no workflow
generic IContextService
```

---

## 144. Architecture underengineering gate

Also fail when boundary is bypassed for convenience.

Examples:

```text
direct foreign table query
foreign aggregate import
foreign internal command
Common dumping ground
producer instruction event
cross-context cascade
```

Certification balances both extremes.

---

## 145. Evidence matrix — core rules

| Rule family | Minimum evidence |
|---|---|
| `BOUND-OWN-*` | source ownership + Application/Domain tests |
| `BOUND-PUB-*` | contract tests + ARCH-BC-005 when active |
| `BOUND-PORT-*` | consumer Application tests |
| `BOUND-ACL-*` | pure table-driven ACL tests |
| `BOUND-INFRA-*` | adapter + DI/integration tests |
| `BOUND-FAIL-*` | semantic + technical failure tests |
| `BOUND-CMD-*` | producer/consumer/idempotency evidence |
| `BOUND-EVT-*` | event registry/outbox + consumer tests |
| `BOUND-REF-*` | reference/lifecycle tests |
| `BOUND-PROJ-*` | projection/rebuild/freshness tests |
| `BOUND-DATA-*` | architecture + integration state evidence |
| `BOUND-MEDIATOR-*` | ARCH-BC-003 |
| `BOUND-API-*` | API/architecture tests |
| `BOUND-EXT-*` | extraction ADR + distributed runtime tests |

---

## 146. Evidence matrix — architecture gates

| Gate | Certification requirement |
|---|---|
| ARCH-BC-001 | known bad fails, known good passes, baseline exact |
| ARCH-BC-002 | Domain + Application foreign-model path protected |
| ARCH-BC-003 | producer Internal path protected |
| ARCH-BC-004 | cross-BC navigation/cascade protected |
| ARCH-BC-005 | Public purity protected |
| ARCH-BC-006 | Application transport/provider purity protected |
| ARCH-BC-007 | event registry/version/owner protected |
| ARCH-BC-008 | Common anti-regression protected |
| ARCH-BC-009 | only if admitted; catalog correctness proven |

---

## 147. Evidence matrix — reference slices

### CreateBoardInWorkspace

```text
pipeline markers/descriptor
authorization pipeline
owned Work persistence
Domain/Application behavior
idempotency
event/realtime output
ARCH-BC-001..003
```

### Automation → Work

```text
Automation Consumer Port
Work Public action
in-process adapter
producer mutation tests
consumer reaction tests
security/scope
idempotency
ARCH-BC-001..003
```

---

## 148. Evidence freshness

Evidence must correspond to candidate source.

Old test output from before relevant change cannot certify current candidate.

Documentation references may be older authority, but executable evidence must be current.

---

## 149. Reviewer decision format

Recommended:

```text
CERTIFICATION DECISION

Scope:
Outcome:
Status:

PASS
- ...

BLOCKERS
- none / ...

MAJOR
- none / ...

DEBT
- ...

NOT_APPLICABLE
- ...

Candidate SHA:
Required CI:
Decision:
```

---

## 150. Example — CreateBoardInWorkspace certification record

```text
Scope:
WorkManagement/CreateBoardInWorkspace

Outcome:
BOUNDARY-VERIFIED

OwningBC:
WorkManagement

MutationAuthorities:
Board -> WorkManagement
Default BoardFields -> WorkManagement

Pipeline:
Authentication -> pipeline
Workspace scope -> pipeline
Authorization -> AccessControlBehavior
Idempotency -> pipeline
Transaction -> canonical data-session behavior

Foreign dependencies:
Workspace extra fact -> NOT_APPLICABLE unless source/product requires it
Billing capability -> NOT_APPLICABLE unless source/product requires it

Architecture:
ARCH-BC-001 PASS
ARCH-BC-002 PASS
ARCH-BC-003 PASS

Status:
VERIFIED
```

Do not manufacture Workspace/Billing dependencies to fill the record.

---

## 151. Example — Automation → Work certification record

```text
Scope:
Automation/<real-action> → WorkManagement/<public-action>

Outcome:
BOUNDARY-VERIFIED

WorkflowOwner:
Automation

MutationAuthority:
WorkManagement

ConsumerPort:
IWorkActionPort

ProducerPublic:
IWorkActions / equivalent

CurrentAdapter:
WorkActionInProcessAdapter

BusinessFailures:
Work semantic rejection set

TechnicalFailurePolicy:
current in-process policy;
future remote policy documented if relevant

Idempotency:
operation identity documented

Architecture:
ARCH-BC-001 PASS
ARCH-BC-002 PASS
ARCH-BC-003 PASS

Status:
VERIFIED
```

---

## 152. Example — legacy Common/Entitlements consumer

```text
Scope:
WorkManagement/<touched-use-case>

Current:
uses HasSubscriptionTierAsync("Pro")

Classification:
MIGRATE-ON-TOUCH
R1

Target:
stable Work capability semantic

Certification:
BLOCKED until touched path no longer depends on tier string

Untouched neighboring consumers:
DEBT
```

Do not block the entire backend solely because untouched legacy consumers remain.

---

## 153. Example — physical cross-BC FK

```text
Rule:
BOUND-DATA-003

ORM navigation:
none

Cascade:
none

Physical FK:
present

Reason:
database integrity during modular-monolith stage

Owner:
producer context

Extraction debt:
recorded

Finding:
DEBT

Certification:
may remain VERIFIED
```

if all semantic ownership rules are preserved.

---

## 154. Example — redundant adapter

```text
Consumer:
WorkManagement

Producer:
Workspaces

Need:
exact WorkspaceScopeFact semantics

Implementation:
IWorkWorkspacePort
→ WorkspaceAdapter
→ IWorkspaceFacts
```

If no translation/topology value exists:

```text
MAJOR overengineering
```

Target:

```text
Consumer → IWorkspaceFacts directly
```

Current in-process composition remains simple.

---

## 155. Example — future remote Billing

```text
Current:
IWorkEntitlementPort
→ BillingEntitlementInProcessAdapter

Future:
IWorkEntitlementPort
→ BillingEntitlementGrpcAdapter
→ Billing gRPC inbound adapter
```

Certification asks:

```text
Did CreateBoard business handler change?
```

Expected:

```text
No.
```

If yes:

```text
boundary was not extraction-ready.
```

---

## 156. Certification completion checklist for Coding Agent

Before claiming completion, verify all applicable items:

```text
[ ] Candidate source inspected
[ ] Canonical authority read
[ ] Owning BC identified
[ ] Workflow owner identified if needed
[ ] Every mutation authority identified
[ ] Foreign semantic needs listed
[ ] Pipeline-owned concerns preserved
[ ] Mechanism selected for each foreign need
[ ] Producer.Public vs Consumer Port justified
[ ] ACL justified or intentionally absent
[ ] Adapter justified or intentionally absent
[ ] No foreign persistence
[ ] No foreign Domain model
[ ] No foreign internal request
[ ] No handler auth duplication
[ ] Transaction boundary explicit
[ ] Business failures explicit
[ ] Technical failure policy explicit
[ ] Concurrency explicit
[ ] Idempotency explicit where relevant
[ ] Event ownership explicit
[ ] Projection ownership explicit where relevant
[ ] ResourceRef lifecycle explicit where relevant
[ ] Focused tests pass
[ ] Architecture tests pass
[ ] Integration tests pass where required
[ ] API tests pass where affected
[ ] Baseline does not grow silently
[ ] Expected diff matches actual diff
[ ] Legacy deletions proven safe
[ ] Required CI passed on candidate SHA
[ ] Debt recorded
[ ] Exceptions recorded
[ ] NOT_APPLICABLE items have rationale
[ ] Future remote impact stated
[ ] Certification status stated
```

---

## 157. Coding Agent may not self-waive BLOCKER

If implementation hits BLOCKER:

```text
agent must stop affected slice
report exact condition
report attempted architecture classification
report safe alternatives
```

Agent MUST NOT:

```text
add TODO
add temporary foreign DbContext
add temporary tier string
add wildcard baseline
add service-locator bypass
```

and continue.

---

## 158. Coding Agent may not invent architecture exception

Exception requires explicit architecture review/decision authority.

Agent may propose:

```text
Exception candidate
```

but cannot mark it accepted itself.

---

## 159. Coding Agent may classify NOT_APPLICABLE

Agent may mark an item NOT_APPLICABLE when source evidence clearly proves it.

Example:

```text
Network retry tests:
NOT_APPLICABLE because no remote boundary exists.
```

Rationale is mandatory.

---

## 160. Final certification condition

The backend boundary execution is considered established when:

```text
BND-M0 inventory VERIFIED
BND-M1 baseline VERIFIED
BND-M2 Wave-1 enforcement VERIFIED
BND-M3 dependency-spine pattern VERIFIED
BND-M4 CreateBoard reference VERIFIED
BND-M5 target-owned mutation reference VERIFIED
rolling feature adoption uses certification cards
```

The following may remain ongoing:

```text
legacy entitlement burn-down
event migration
ResourceRef cleanup
projection adoption
Wave-3 semantic enforcement
```

The following remains conditional:

```text
BND-M9 service extraction
```

---

## 161. Final architecture certification invariant

The final certification question is:

> Can the runtime topology change without changing business ownership, core use-case semantics, or the consumer's understanding of producer internals?

Healthy answer:

```text
YES

Current:
Consumer Application
→ semantic contract/port
→ optional ACL
→ in-process implementation

Future:
Consumer Application
→ same semantic contract/port
→ same semantic ACL
→ remote adapter
→ network
→ producer inbound adapter
→ same producer Application semantics
```

What changes:

```text
Infrastructure adapter
transport contract
DI/runtime composition
deployment
observability
possibly physical data placement
```

What should not change merely because the producer became remote:

```text
mutation ownership
consumer business language
producer invariants
consumer handler's core decision flow
authorization meaning
transaction ownership semantics
```

If those must be redesigned solely because of a runtime split:

```text
the pre-extraction boundary was not fully healthy
```

and certification must remain below `EXTRACTION-READY`.
