---
document_id: WRK-PLAN-BACKEND-BOUNDARIES-V3
document_type: execution-plan
status: draft
owner: backend-architecture
version: 3
audit_snapshot:
  repository: Nqv1208/Notrelix
  branch: main
  commit: 6030d06051e8bbb4844746150be5c1d5d4c53bbd
depends_on:
  - WRK-SPEC-BACKEND-BOUNDARIES-V3
higher_authorities:
  - RULE.md
  - AGENTS.md
  - backend/AGENTS.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/cross-team-dependencies.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/testing-and-quality-gates.md
applies_to:
  - backend-boundary-hardening
  - bounded-context-interactions
  - architecture-fitness
  - dependency-spine-hardening
  - incremental-source-migration
  - future-service-extraction-readiness
---

# PLAN — Backend Boundary Execution V3

## 1. Purpose

This PLAN converts `WRK-SPEC-BACKEND-BOUNDARIES-V3` into a deterministic implementation sequence.

The SPEC owns architectural meaning.

This PLAN owns:

```text
execution order
source-audit order
milestone scope
preferred code areas
migration order
expected diff
forbidden diff
test handoff
evidence handoff
stop/continue rules
```

This PLAN MUST NOT be used to redefine product semantics or service topology.

The implementation target is:

```text
existing five-project modular monolith
→ stronger semantic boundaries
→ stronger machine enforcement
→ representative boundary slices
→ rolling migration
→ optional future extraction readiness
```

The target is **not**:

```text
reorganize all folders
create projects per BC
introduce HTTP/gRPC between current BCs
replace the frozen pipeline
rewrite all entitlements
rewrite all events
```

---

## 2. Non-negotiable source-first rule

Every milestone begins from the candidate source state.

For every affected slice:

```text
read current source
→ classify current behavior
→ compare with canonical architecture
→ determine TARGET / ACCEPTED-CURRENT / MIGRATE-ON-TOUCH / BOUNDARY-DEBT / STOP
→ make smallest complete correction
→ prove it
```

Never:

```text
read ideal folder diagram
→ create all folders/interfaces
→ force current source into diagram
```

The source is evidence.

The canonical docs define intent.

When source and intended architecture disagree, classify the mismatch before coding.

---

## 3. Execution relationship to backend roadmap

The backend roadmap already owns:

```text
P0..P6
D0..D5
```

This boundary plan MUST NOT create another competing business priority sequence.

Boundary milestones are namespaced:

```text
BND-M0
BND-M1
...
```

They are horizontal architecture-enforcement milestones that may run alongside roadmap feature phases.

The boundary plan never means:

```text
finish all BND milestones
then resume feature development
```

Instead:

```text
feature teams continue
+
boundary hardening rolls forward
+
only the affected feature slice stops on unresolved boundary conditions
```

---

## 4. Master execution sequence

```text
BND-M0  Candidate-SHA boundary inventory
        ↓
BND-M1  Boundary ownership model + concrete debt baseline
        ↓
BND-M2  First architecture enforcement wave
        ↓
BND-M3  Dependency-spine semantic surfaces
        ↓
BND-M4  Reference Slice A — CreateBoardInWorkspace / pipeline-integrated sync boundary
        ↓
BND-M5  Reference Slice B — Automation → Work target-owned mutation
        ↓
BND-M6  Event / projection / ResourceRef hardening
        ↓
BND-M7  Rolling context adoption + hotspot migration
        ↓
BND-M8  Second/third architecture enforcement waves
        ↓
BND-M9  Extraction readiness only on approved candidate
```

Milestones are not necessarily one PR each.

Each PR should be:

```text
smallest complete
compatible
reviewable
testable
```

---

## 5. Parallelism model

### May run in parallel

After `BND-M1` establishes concrete ownership/debt:

```text
Wave-1 architecture tests
Workspace Public fact design
Billing capability contract analysis
Automation→Work dependency inventory
ResourceRef inventory
event ownership inventory
```

may proceed in parallel if they do not modify the same semantic contract.

### Must serialize

Serialize when:

```text
producer contract shape is still changing
consumer depends on that shape
architecture-test rule depends on unresolved namespace ownership
migration changes the same Common abstraction
pipeline descriptor semantics are changing
```

### Contract readiness

Use backend roadmap `D0-D5`.

A downstream implementation may:

```text
D2 → scaffold behind local fake/port where useful
D3 → continue isolated consumer implementation
D4 → integrate
D5 → treat as broad parallel stable dependency
```

Do not invent a new readiness model here.

---

## 6. BND-M0 — Candidate-SHA boundary inventory

### Objective

Establish executable truth before any boundary refactor.

### Required source areas

At minimum inspect:

```text
backend/src/Notrelix.Domain/
backend/src/Notrelix.Application/Features/
backend/src/Notrelix.Application/Common/
backend/src/Notrelix.Application/EventMappers/
backend/src/Notrelix.Application/Events/
backend/src/Notrelix.Infrastructure/
backend/src/Notrelix.Platform/
backend/src/Notrelix.API/
backend/tests/Notrelix.Application.Tests/
backend/tests/Notrelix.Architecture.Tests/
backend/tests/Notrelix.Integration.Tests/
```

### Required canonical docs

Read:

```text
RULE.md
AGENTS.md
backend/AGENTS.md
docs/architecture/bounded-context-map.md
docs/architecture/contract-boundaries.md
docs/architecture/data-ownership-and-consistency.md
docs/architecture/capability-extraction-strategy.md
docs/workstreams/backend-roadmap.md
docs/workstreams/cross-team-dependencies.md
backend/docs/architecture/application-model.md
backend/docs/architecture/infrastructure-and-data.md
backend/docs/architecture/platform-and-messaging.md
backend/docs/architecture/security-tenancy-authorization.md
backend/docs/architecture/testing-and-quality-gates.md
```

### Required inventory categories

For every business context:

```text
Domain namespace root
Application feature root
owned persistence abstraction
owned schemas/tables where known
current Public-like contracts
current Common dependencies
current cross-context reads
current cross-context writes
current event producers
current event consumers
current projection/read-model consumers
current resource-reference patterns
current provider/network dependencies
```

### Output

No new production architecture abstraction is required.

Produce an execution-time inventory/evidence section in the working notes/PR description or the team execution evidence mechanism.

Do not create a new permanent architecture catalog unless TESTS later proves a machine-readable catalog is necessary.

---

## 7. BND-M0 — Exact current facts to preserve

The audit snapshot already establishes several source facts.

#### Production projects

Exactly:

```text
Notrelix.Domain
Notrelix.Application
Notrelix.Infrastructure
Notrelix.Platform
Notrelix.API
```

#### Application feature roots

Current roots include:

```text
Accounts
Analytics
Automation
Billing
Collaboration
Documents
Governance
Identity
Integrations
Notifications
Operations
Search
WorkManagement
Workspaces
```

Do not interpret `Notifications`, `Operations`, or `Search` as automatic new business BCs.

#### WorkManagement

Current module-first shape exists:

```text
Features/WorkManagement/Boards/Commands/CreateBoardInWorkspace/
```

Preserve module-first placement.

#### Owned persistence abstraction

Current:

```text
Features/WorkManagement/Abstractions/IWorkManagementDbContext.cs
```

and corresponding context-local abstractions elsewhere.

#### Current pipeline-integrated CreateBoard

`CreateBoardInWorkspaceCommand` currently declares:

```text
IWriteRequest
IRequirePermission
IAuthenticatedRequest
IWorkspaceRequest
IIdempotentRequest
```

and its handler currently injects:

```text
IWorkManagementDbContext
ICurrentRequestContext
IDateTimeProvider
IRealtimeChangeMapper<...>
IIntegrationEventCollector
```

This is important.

Do **not** introduce Workspace/Governance constructor ports into the handler solely because a generic architecture example previously showed them.

Workspace scope and permission are already declared through pipeline contracts.

#### Current entitlement hotspot

Current:

```text
Application/Common/Entitlements/
```

includes subscription/tier APIs.

Treat as migration hotspot, not a mandate for immediate deletion.

---

## 8. BND-M0 STOP conditions

Stop inventory and resolve before design if:

```text
bounded-context ownership in canonical docs conflicts materially with source behavior
current pipeline responsibility is unclear
same authoritative state appears to have two logical writers
a supposed "read model" is actually authoritative mutation state
a current Common abstraction owns product semantics ambiguously
```

Do not code around unresolved authority.

---

## 9. BND-M0 exit gate

PASS when the team can answer for each prioritized slice:

```text
consumer BC
producer BC
mutation owner
workflow owner if any
current interaction path
current persistence path
current pipeline involvement
current event/projection involvement
current violations
migration classification
```

---

## 10. BND-M1 — Concrete boundary debt baseline

### Objective

Turn architectural concerns into a finite, non-wildcard list.

Do not attempt repo-wide cleanup yet.

### Risk levels

Use:

```text
R0 — BLOCKER
R1 — HIGH
R2 — MIGRATE-WHEN-TOUCHED
R3 — ACCEPTED-CURRENT
```

### Required debt categories

Inventory:

```text
foreign DbContext/repository
foreign Domain model reference
foreign internal Application namespace
foreign internal MediatR command/query
cross-context ORM navigation
cross-context cascade
cross-context transaction
Common business semantic leakage
hard-coded Billing tier/plan vocabulary
hard-coded Governance role/private permission vocabulary
Application provider/network client
producer event containing consumer-specific instruction
ambiguous source of truth
ambiguous long-running workflow ownership
```

---

## 11. BND-M1 — Baseline format

Each violation must be concrete.

Example:

```text
DebtId: BND-DEBT-017
RuleId: BOUND-DATA-002
Risk: R1
ConsumerBC: Automation
ProducerBC: WorkManagement
Source:
  backend/src/.../SomeHandler.cs
Violation:
  injects IWorkManagementDbContext
Owner:
  automation-integrations-team
MigrationTrigger:
  next material edit to affected use case
TargetPattern:
  IWorkActionPort → Work Public action
```

Forbidden baseline:

```text
ignore WorkManagement
ignore legacy
ignore Common
```

---

## 12. BND-M1 — Hotspot priority

Audit first:

```text
1. Common/Entitlements
2. access/pipeline fact providers
3. WorkManagement boundary consumers
4. Automation ↔ WorkManagement
5. Collaboration target relationships
6. Documents target relationships
7. Integration-provider interactions
8. Analytics/Search source reads
9. event consumers
```

Why:

```text
these areas have the highest chance of becoming future service blockers
or silent semantic coupling
```

---

## 13. BND-M1 expected diff

Expected production diff:

```text
none
```

unless a truly blocking defect is discovered.

Expected test/evidence diff may include:

```text
concrete baseline data
test fixture baseline entries
architecture-test helper data
```

only if current test infrastructure already requires it.

Do not create a new manifest prematurely.

---

## 14. BND-M1 forbidden diff

Do not:

```text
move all Common/Entitlements
move all Application features
rename every context namespace
create all Public/Ports folders
create service projects
introduce gRPC packages
replace pipeline
```

---

## 15. BND-M2 — Architecture enforcement Wave 1

### Objective

Prevent new critical coupling before large migrations.

Activate:

```text
ARCH-BC-001 — Foreign Persistence Dependency
ARCH-BC-002 — Foreign Domain Model Dependency
ARCH-BC-003 — Producer Internal/MediatR Dependency
```

The TESTS file will specify exact assertions.

This PLAN specifies source/test placement and rollout.

---

## 16. ARCH-BC-001 implementation direction

Current test:

```text
backend/tests/Notrelix.Architecture.Tests/DataAccess/
DbContextBoundaryArchitectureTests.cs
```

already source-scans `*Handler.cs`.

Do not delete it merely to introduce a cleaner test.

First:

```text
preserve current coverage
→ identify gaps
→ either harden file or add focused companion test
```

Preferred split if existing test would become overloaded:

```text
DataAccess/
├── DbContextBoundaryArchitectureTests.cs
└── CrossContextPersistenceBoundaryTests.cs
```

Coverage target expands beyond `*Handler.cs` where necessary to catch:

```text
Application services
event handlers
process managers
projection writers
```

False-positive policy is defined in TESTS.

---

## 17. ARCH-BC-002 implementation direction

Preferred test area:

```text
backend/tests/Notrelix.Architecture.Tests/DomainPurity/
```

or:

```text
ApplicationLayer/
```

depending on actual implementation mechanism.

Target detects:

```text
Features/{Consumer}
→ Notrelix.Domain.{Producer}
```

for producer Domain model types.

Allowed exceptions must be explicit and narrow.

Do not allow:

```text
"read-only foreign entity"
```

as general exception.

---

## 18. ARCH-BC-003 implementation direction

Preferred test area:

```text
backend/tests/Notrelix.Architecture.Tests/ApplicationLayer/
```

or a dedicated:

```text
Contracts/
CrossContextContractArchitectureTests.cs
```

if existing test organization supports it better.

Detect:

```text
Consumer Application
→ Producer Internal/private namespace
→ Producer internal handler/request
```

Specific high-value rule:

```text
foreign IMediator.Send(new ProducerInternalCommand(...))
```

must fail.

Do not ban all MediatR usage.

MediatR inside owning context remains valid.

---

## 19. BND-M2 enforcement progression

Use the least sophisticated mechanism that is reliable.

Order:

```text
1. existing source scan where precise enough
2. reflection/type namespace analysis
3. project/assembly dependency analysis
4. Roslyn semantic analysis only for unresolved source-level cases
```

Do not start by building a custom compiler/analyzer platform.

---

## 20. BND-M2 failure message contract

Every new architecture gate should report:

```text
Rule ID
consumer
producer
violating type/file
forbidden dependency
approved alternative
```

Example:

```text
ARCH-BC-003:
Automation type ExecuteRuleHandler references
WorkManagement internal UpdateBoardItemCommand.
Use Automation-owned IWorkActionPort and a WorkManagement Public action contract.
```

---

## 21. BND-M2 exit gate

PASS when:

```text
current baseline is explicit
new foreign persistence is blocked
new foreign Domain dependency is blocked
new producer Internal/MediatR dependency is blocked
architecture suite remains fast enough for normal CI
false-positive rate is acceptable
```

Do not wait for Wave 2/3.

---

## 22. BND-M3 — Dependency-spine semantic surfaces

### Objective

Harden only real high-value producer/consumer seams needed by current roadmap.

Initial semantic spine:

```text
Accounts / Identity
        ↓
Workspaces
        ↓
Governance
        ↓
WorkManagement

Billing
        ↓
product capability consumers
```

This does not mean every arrow becomes a constructor port.

First determine whether the dependency is already correctly owned by:

```text
execution context
request descriptor
access facts
authorization pipeline
feature-gate pipeline
```

---

## 23. BND-M3 — Pipeline ownership rule

The current Application architecture freezes a seven-behavior pipeline.

Boundary work MUST NOT create a duplicate pipeline around it.

For each dependency ask:

```text
Is this dependency already a pipeline-declared cross-cutting concern?
```

Examples:

```text
authentication
execution context
workspace/account scope
resource authorization
idempotency
transaction/data session
```

If yes:

```text
preserve marker/descriptor/pipeline path
harden its producer facts internally
```

Do not inject a parallel port into every handler.

---

## 24. Boundary decision matrix: pipeline vs handler dependency

| Need | Preferred owner |
|---|---|
| request authentication | frozen pipeline |
| Account/Workspace execution scope | frozen pipeline when declared |
| standard resource authorization | frozen access pipeline |
| standard request idempotency | frozen pipeline |
| transaction/data session | frozen pipeline |
| use-case-specific foreign business fact | handler/Application semantic contract |
| use-case-specific foreign mutation | Consumer Port → Producer Public action |
| consumer-specific semantic translation | Consumer Port + ACL |
| long-running cross-owner workflow | Process Manager |
| hot derived foreign read | projection |

This table is critical.

Do not "port-ify" pipeline concerns.

---

## 25. BND-M3-A — Workspace semantic facts

### Goal

Create/confirm a narrow producer Public surface only for Workspace facts that **are not already supplied authoritatively by current execution/access context**.

Candidate needs may include:

```text
workspace active/archive state
workspace containment fact
workspace lifecycle state
```

Do not duplicate:

```text
current AccountId
current WorkspaceId
already resolved request context
```

if frozen execution context already owns them.

### Target code area when needed

```text
backend/src/Notrelix.Application/Features/Workspaces/Public/
├── Queries/
└── Facts/
```

Only real files.

No empty folders.

---

## 26. Workspace Public contract design gate

Before adding `IWorkspaceFacts` answer:

```text
Which consumers need it?
Which fact is absent from execution/access pipeline?
Is the fact authoritative?
Can it be represented narrowly?
Does it expose Workspace Domain internals?
Does it need revision/freshness?
```

If current pipeline already resolves the exact required semantic:

```text
do not add IWorkspaceFacts
```

solely for architecture symmetry.

---

## 27. BND-M3-B — Governance authorization seam

Current source already declares authorization via:

```text
IRequirePermission
ResourceRef
PermissionAction
AccessControlBehavior
AccessFacts
IAccessPolicyEvaluator
```

Boundary execution should **harden this path**, not replace it with handler-local `IWorkAuthorizationPort` unless a specific use case needs non-standard authorization semantics outside the canonical pipeline.

Therefore:

```text
standard protected request
→ request marker/descriptor
→ execution context
→ access facts
→ policy evaluator
→ handler
```

is the preferred current topology.

---

## 28. Governance producer fact hardening

Inspect the current `IAccessFactsProvider` implementation(s).

Classify each fact by semantic owner:

```text
Identity/Accounts fact
Workspace membership fact
resource-owner fact
Governance role/policy fact
Billing/feature fact if currently included
```

Do not move all these semantics into Governance merely because access evaluation consumes them.

Desired split:

```text
producer owns fact meaning
access facts aggregation obtains narrow facts
Governance/policy evaluator owns permission policy meaning
pipeline owns enforcement
```

---

## 29. BND-M3-C — Entitlement / feature-gate seam

Current source has two related mechanisms:

```text
Application/Common/Entitlements/*
Application/Common/Requests/Gates/IRequireFeature
```

and the access policy path can evaluate feature facts.

Do not blindly replace them with handler-level `IWorkEntitlementPort`.

First classify each consumer.

#### Case 1 — Generic pipeline gate

Example:

```text
request requires stable product capability key
```

If this is truly a cross-cutting request declaration:

```text
keep marker/pipeline mechanism
```

but remove Billing tier/plan semantics from consumers.

#### Case 2 — Use-case-specific entitlement fact

If the handler/business orchestration needs a richer semantic decision not represented by generic access policy:

```text
Consumer Port
```

may be appropriate.

#### Case 3 — Billing mutation/usage accounting

If the operation must mutate Billing-owned usage state:

```text
target-owned Billing command/use case
```

or approved transactional design is required.

Do not disguise mutation as a boolean gate.

---

## 30. BND-M3 entitlement migration target

Current problematic APIs include:

```text
HasActiveSubscriptionAsync
HasSubscriptionTierAsync(string minimumTier)
```

Migration rule:

```text
new consumer code
→ no tier-name dependency

touched consumer
→ migrate to stable capability/decision semantics

untouched legacy consumer
→ may remain temporarily

pipeline marker
→ may retain generic capability code if owner-neutral and canonical

Billing
→ remains owner of plan/subscription/grant calculation
```

---

## 31. BND-M3 entitlement file sequence

For one touched WorkManagement capability:

```text
1. inspect request markers and current pipeline gate
2. inspect current FeatureCode / feature key
3. determine whether pipeline already solves gate
4. if yes:
     migrate consumer to stable capability declaration
     do not add handler port
5. if no:
     add Work-owned Port
6. add/confirm Billing Public semantic decision
7. add pure Work ACL if semantics differ
8. add Infrastructure in-process adapter
9. register runtime implementation
10. migrate touched handler/use case
11. tests
12. keep legacy checker for untouched consumers
```

No big-bang Common rewrite.

---

## 32. BND-M3-D — Consumer Port placement

When required:

```text
backend/src/Notrelix.Application/Features/{Consumer}/Ports/
```

Examples:

```text
WorkManagement/Ports/Entitlements/IWorkEntitlementPort.cs
Automation/Ports/Work/IWorkActionPort.cs
Collaboration/Ports/Targets/ICommentTargetPort.cs
```

Do not use:

```text
Application/Common/Services/IBillingService.cs
Application/Common/IContextService.cs
```

to avoid ownership.

---

## 33. BND-M3-E — ACL placement

When semantic translation exists:

```text
Application/Features/{Consumer}/CrossContext/{Producer}/
```

Example:

```text
WorkManagement/CrossContext/Billing/WorkEntitlementAcl.cs
```

ACL must:

```text
be pure
not access DB
not access network
not resolve DI service
not contain provider client
```

---

## 34. BND-M3-F — In-process adapter placement

When Consumer Port needs runtime implementation:

```text
Infrastructure/CrossContext/{Consumer}/{Producer}/
```

Example:

```text
Infrastructure/CrossContext/WorkManagement/Billing/
BillingEntitlementInProcessAdapter.cs
```

If the repo's current Infrastructure organization makes a context-specific existing folder cleaner for a first migration:

```text
ACCEPTED-CURRENT
```

may be used.

Do not mass-reorganize Infrastructure.

New adapters should converge toward the canonical shape unless doing so causes unnecessary churn.

---

## 35. BND-M3 DI sequence

For a new Consumer Port:

```text
Application defines Port
Application defines pure ACL if needed
Producer defines Public semantic surface
Infrastructure implements adapter
Infrastructure registration binds Port → adapter
API composition root consumes normal AddInfrastructure path
```

Forbidden:

```text
Application DependencyInjection.cs
→ new BillingEntitlementInProcessAdapter(...)
```

Forbidden:

```text
API Program.cs
→ business conditional choosing plan semantics
```

---

## 36. BND-M3 exit gate

PASS when high-priority dependency-spine consumers no longer need new code that:

```text
reads foreign persistence
uses producer Domain model
uses producer internal request
hard-codes Billing tiers
hard-codes Governance internals
duplicates frozen pipeline behavior
```

Not every legacy consumer must be migrated yet.

---

## 37. BND-M4 — Reference Slice A: CreateBoardInWorkspace

### Objective

Use a real WorkManagement mutation to prove:

```text
pipeline-owned cross-cutting concerns
+
context-owned mutation
+
optional use-case-specific foreign dependency
+
architecture gates
```

without fabricating extra abstractions.

---

## 38. Current source truth for CreateBoardInWorkspace

Current request:

```text
CreateBoardInWorkspaceCommand
```

declares:

```text
IWriteRequest
IRequirePermission
IAuthenticatedRequest
IWorkspaceRequest
IIdempotentRequest
```

Current permission resource:

```text
ResourceKind "workspaces.workspace"
WorkspaceId
PermissionAction.CreateBoard
```

Current handler:

```text
uses IWorkManagementDbContext
uses ICurrentRequestContext
uses IDateTimeProvider
creates WorkManagement Board + default fields
enrolls realtime/integration event through existing mechanism
```

This is already structurally stronger than the earlier generic example that assumed:

```text
CreateBoardHandler
→ IWorkspaceFacts
→ IWorkAuthorizationPort
```

Do not regress the source by adding redundant dependencies.

---

## 39. BND-M4 Step 1 — classify current Workspace dependency

Determine whether:

```text
IWorkspaceRequest
+
ExecutionContextBehavior/access facts
```

already prove all Workspace facts CreateBoard requires.

If yes:

```text
no Workspace Public query is added to handler
```

If product semantics additionally require:

```text
Workspace must not be archived
Workspace lifecycle state X
```

and current pipeline does not guarantee it:

```text
add the narrowest producer fact at the correct boundary
```

Do not query Workspace DbContext directly.

---

## 40. BND-M4 Step 2 — classify Governance dependency

Current:

```text
IRequirePermission
→ AccessControlBehavior
```

is canonical.

Do not add handler-local Governance port merely to duplicate:

```text
CreateBoard permission
```

If current access facts/policy are incomplete:

```text
fix the access-facts provider / producer fact seam
```

at the canonical pipeline boundary.

---

## 41. BND-M4 Step 3 — classify Billing dependency

Ask product/roadmap:

```text
Does CreateBoard currently require a Billing capability?
```

If no:

```text
do not add entitlement architecture "for future use"
```

If yes and generic pipeline feature gate is sufficient:

```text
request implements approved feature/capability marker
→ access pipeline evaluates it
```

If richer use-case-specific capability is required:

```text
Work-owned Consumer Port
→ Billing Public
```

Do not guess product entitlement.

---

## 42. BND-M4 Step 4 — preserve local transaction ownership

Expected mutation:

```text
WorkManagement Board
WorkManagement default BoardFields
Work-owned event/outbox enrollment
```

All remain one WorkManagement mutation authority.

Do not add:

```text
Billing usage row update
Workspace row update
Governance row update
```

to Work transaction.

If product requires usage consumption:

```text
STOP
→ classify Billing mutation and consistency
```

before implementation.

---

## 43. BND-M4 Step 5 — preserve frozen pipeline ownership

Do not move into handler:

```text
authentication
Workspace execution scope
permission evaluation
data-session transaction policy
idempotency mechanism
```

unless canonical pipeline architecture explicitly changes.

---

## 44. BND-M4 expected production diff

Depending on audit result, possible valid diff:

```text
zero architecture production changes
+
new architecture tests proving current slice
```

or:

```text
narrow Workspace Public fact
narrow Billing capability declaration/port
Infrastructure adapter
touched request/handler
```

The plan must allow the correct outcome to be:

```text
current source already follows boundary
```

Do not create code to make the milestone look substantive.

---

## 45. BND-M4 forbidden diff

Do not:

```text
replace IRequirePermission with handler permission code
add IWorkspaceDbContext to handler
add IBillingDbContext to handler
load Workspace aggregate into Work handler
add tier string to Work command
call IMediator for foreign internal query
add HttpClient/gRPC
move transaction to API
```

---

## 46. BND-M4 test handoff

TESTS must prove:

```text
request descriptor/markers enforce scope/auth
handler mutates only Work state
foreign persistence gate passes
foreign Domain gate passes
producer Internal gate passes
idempotency behavior remains valid
event/realtime enrollment behavior remains valid
```

If Billing gate is added:

```text
capability allowed
capability denied
technical dependency policy
```

according to chosen pipeline/port mechanism.

---

## 47. BND-M4 exit gate

PASS when CreateBoard is a reference demonstrating:

```text
do not duplicate pipeline
do not bypass context ownership
do not add abstraction without semantic need
do not prepare network prematurely
```

---

## 48. BND-M5 — Reference Slice B: Automation → Work target-owned mutation

### Objective

Prove the harder case:

```text
one BC wants another BC to mutate state
```

This slice validates:

```text
Consumer Port
Producer Public action
in-process adapter
producer mutation authority
future remote replaceability
```

---

## 49. BND-M5 Step 0 — inventory actual Automation→Work mutations

Search Automation for:

```text
WorkManagement Domain types
IWorkManagementDbContext
Work internal commands
IMediator.Send Work requests
board/item repository access
direct SQL/read-model writes
```

List exact use cases.

Choose one representative action with clear semantics.

Prefer:

```text
update BoardItem
move BoardItem
set field value
create Work item
```

only if product semantics already exist.

Do not invent an Automation feature solely for architecture demonstration.

---

## 50. BND-M5 Step 1 — define Automation consumer need

Example:

```text
Automation needs to request "update BoardItem fields"
```

Do not define:

```text
Automation needs Work DbContext
```

Create cohesive port:

```text
Features/Automation/Ports/Work/IWorkActionPort.cs
```

only when a real action exists.

---

## 51. BND-M5 Step 2 — define Work Public action

Target:

```text
Features/WorkManagement/Public/Commands/IWorkActions.cs
```

or a more semantically focused facade if source supports it.

Public input must contain:

```text
stable IDs
semantic command data
actor/system execution context information required by canonical auth model
idempotency/operation identity where relevant
```

Public input must not expose:

```text
Work aggregate
EF entity
DbContext
internal MediatR request type
```

---

## 52. BND-M5 Step 3 — Work internal delegation

Producer Public implementation may:

```text
directly invoke owned application service
or
dispatch owned internal MediatR request
```

inside WorkManagement.

The boundary is:

```text
external context stops at Public
```

MediatR remains internal.

---

## 53. BND-M5 Step 4 — authorization identity

Automation background execution does not bypass authorization.

Classify actor:

```text
originating user
system actor
service principal
automation execution actor
```

Use canonical security/tenant model.

The port/public action must carry or reconstruct sufficient scope without leaking HTTP primitives.

---

## 54. BND-M5 Step 5 — idempotency

Because this mutation may become remote later, define operation identity now if retries are possible.

Candidate identity:

```text
AutomationExecutionId
AutomationActionExecutionId
BusinessOperationId
```

Do not generate a new random ID on each retry.

Define:

```text
same key + same action
same key + conflicting action
retry after unknown outcome
retention
```

as applicable.

---

## 55. BND-M5 Step 6 — in-process adapter

Target:

```text
Infrastructure/CrossContext/Automation/WorkManagement/
WorkActionInProcessAdapter.cs
```

It implements:

```text
IWorkActionPort
```

It calls:

```text
Work Public action
```

It may map technical/application contracts.

It must not implement Work business rules.

---

## 56. BND-M5 Step 7 — transaction semantics

Automation local transaction and Work local transaction are conceptually separate mutation authorities even if same physical DB currently exists.

Do not create semantic dependency on:

```text
one EF transaction rolling both back
```

Define outcomes:

```text
Work succeeds, Automation outcome persistence fails
Work rejects
Automation retries
duplicate action
```

If exact atomicity is product-required:

```text
STOP
```

and redesign workflow.

---

## 57. BND-M5 expected diff

Typical:

```text
Application/Features/Automation/Ports/Work/IWorkActionPort.cs
Application/Features/WorkManagement/Public/Commands/... public action contract
Infrastructure/CrossContext/Automation/WorkManagement/WorkActionInProcessAdapter.cs
Infrastructure DI registration
touched Automation action handler/process
Work Public implementation/delegation
Application tests
architecture tests
integration tests
```

Only create files actually required.

---

## 58. BND-M5 forbidden diff

```text
Automation injects IWorkManagementDbContext
Automation references Work Domain aggregate
Automation sends Work internal MediatR command
Automation writes work.* tables
Work Public action accepts EF entity
Infrastructure adapter decides Work transition
API coordinates Automation + Work transaction
```

---

## 59. BND-M5 exit gate

PASS when:

```text
Automation owns orchestration
Work owns Work mutation
port/facade boundary is explicit
current call is in-process
future remote replacement requires adapter/runtime changes only
idempotency/unknown outcome semantics are explicit
```

---

## 60. BND-M6 — Event boundary hardening

### Objective

Separate:

```text
Domain Event
Integration Event
consumer reaction
delivery mechanism
```

without wholesale event relocation.

---

## 61. BND-M6 event inventory

For each prioritized producer:

```text
WorkManagement
Workspaces
Billing
Automation
Documents
Collaboration
Integrations
```

list:

```text
Domain event
current mapper
current outward event
contract version
outbox path
consumers
consumer idempotency
```

Classify each outward event:

```text
PRODUCER_FACT
CONSUMER_INSTRUCTION
INTERNAL_EVENT
AMBIGUOUS
```

---

## 62. BND-M6 migration rule

For new event:

```text
producer-owned completed fact
versioned outward contract
outbox/delivery
```

For existing ambiguous event:

```text
MIGRATE-ON-TOUCH
```

unless it is an R0/R1 correctness problem.

Do not rename every event merely to satisfy naming style.

---

## 63. BND-M6 event contract placement

Current top-level:

```text
Application/EventMappers
Application/Events
```

is accepted current source.

For new context-owned outward contracts, prefer:

```text
Features/{Producer}/Public/Events
```

only when compatible with current messaging architecture.

Do not duplicate the same contract in both places.

If current serialization/discovery relies on top-level placement:

```text
preserve mechanism
document semantic owner
```

until a deliberate migration is implemented.

---

## 64. BND-M6 producer test requirements

Producer proves:

```text
owned mutation
→ owned Domain event if applicable
→ outward integration mapping
→ outbox enrollment
→ local atomic commit
```

Do not test consumer reaction in producer unit tests.

---

## 65. BND-M6 consumer test requirements

Consumer proves:

```text
message accepted
duplicate delivery safe
semantic reaction correct
consumer local state commit
retry/poison policy correct
```

Platform generic retry tests remain Platform-owned.

---

## 66. BND-M6 exit gate

PASS when new/touched events no longer:

```text
expose producer internals
command a specific consumer
reuse Domain Event as public wire contract accidentally
lack version/owner
```

---

## 67. BND-M6 — ResourceRef hardening

### Objective

Remove entity-graph coupling for polymorphic/cross-resource relationships.

Prioritize:

```text
Collaboration targets
Automation targets
audit/activity subjects
notifications
attachments
integration mappings
```

---

## 68. Resource relation decision

For each relationship ask:

```text
Is only identity needed?
→ Stable ID / ResourceRef

Is authoritative existence required now?
→ semantic query

Is hot display metadata needed?
→ projection/read model

Is foreign mutation required?
→ target-owned command
```

Do not solve all with FK navigation.

---

## 69. ResourceRef migration sequence

For one touched relationship:

```text
1. identify source owner
2. identify consumer owner
3. define stable target kind/id
4. remove need for foreign Domain object
5. preserve persistence integrity if physical FK still temporarily exists
6. remove cross-BC cascade
7. add explicit lifecycle behavior
8. tests
```

Physical FK may remain as reviewed integrity debt.

ORM navigation/cascade may not remain as semantic dependency.

---

## 70. BND-M6 — Projection hardening

Use only when actual read-frequency/failure/latency need exists.

Do not create projections solely for future microservices.

Candidate consumers:

```text
Analytics
Search
Work entitlement hot path
authorization hot path
UI composition hot path
```

---

## 71. Projection implementation sequence

```text
1. identify source authority
2. identify consumer query need
3. define freshness/staleness tolerance
4. define event/read-feed source
5. define consumer projection model
6. define revision/order behavior
7. define rebuild
8. define storage implementation
9. define fallback/failure
10. tests
```

Security projection additionally requires fail-closed semantics.

---

## 72. BND-M7 — Rolling team adoption

### Objective

Make boundary rules normal feature engineering rather than one architecture project.

Every material cross-context feature follows the Use-Case Boundary Card.

---

## 73. Team adoption — Identity & Accounts

Focus:

```text
stable actor/account identity
session/authentication semantics
Account authority
```

Avoid:

```text
turning Identity claims into Governance authority
consumer use of Identity persistence
```

Public references should be narrow stable identities/facts.

---

## 74. Team adoption — Workspace & Governance

Preserve:

```text
Workspace owns lifecycle/membership
Governance owns permission/role/policy meaning
pipeline owns enforcement mechanism
```

Do not merge BCs because one team owns both.

Harden producer facts feeding access pipeline.

---

## 75. Team adoption — WorkManagement

For every protected mutation:

```text
request markers/descriptor
→ pipeline scope/auth
→ owned handler
→ owned Domain/persistence
```

Cross-context facts only when not already supplied by canonical pipeline.

No plan/tier vocabulary.

---

## 76. Team adoption — Documents & Collaboration

Prefer:

```text
ResourceRef
producer Public fact
target-owned command
```

No foreign resource entity graphs.

Collaboration owns comments/reactions, not target aggregate.

---

## 77. Team adoption — Automation & Integrations

Automation:

```text
owns trigger interpretation
owns execution lifecycle
owns workflow/retry business semantics
```

Integrations:

```text
owns provider connection/provider action semantics
```

Provider SDK remains Infrastructure.

Automation must use target-owned commands.

---

## 78. Team adoption — Billing & Entitlements

Billing owns:

```text
plan
subscription
commercial grant
usage accounting
entitlement source truth
```

Product consumers should ask:

```text
capability/business decision
```

not interpret tier ordering.

---

## 79. Team adoption — Analytics & Reporting

Prefer:

```text
integration events
read feeds
consumer projections
reporting contracts
```

Do not make Analytics architecture depend on private transactional tables as its permanent contract.

Existing direct reporting reads may be classified and migrated based on performance/ownership evidence.

---

## 80. Team adoption — Platform & Foundation

Platform owns mechanism:

```text
delivery
retry engine
ordering primitive
idempotency mechanism
observability
```

Platform never absorbs context-specific semantic branching.

---

## 81. BND-M7 Definition of Ready per feature

Before coding a cross-context slice, resolve:

```text
OwningBC
WorkflowOwner
MutationAuthorities
Owned state
Foreign semantic need
ProducerBC
Mechanism
Pipeline-owned or use-case-owned?
Producer Public vs Consumer Port
ACL need
Adapter need
Transaction
Business failures
Technical dependency policy
Concurrency
Idempotency
Events/projections
Migration classification
Tests
```

---

## 82. BND-M7 PR scope rule

Preferred PR:

```text
one complete boundary correction
+
one real feature slice
+
its tests/gates
```

Avoid:

```text
boundary framework PR
then semantic PR later
```

unless compatibility/migration requires staging.

---

## 83. BND-M7 touched-code rule

When touching legacy code:

```text
fix boundary debt that is directly on the changed use-case path
```

Do not expand scope to neighboring unrelated modules unless:

```text
shared contract migration requires it
or
architecture test cannot be enabled otherwise
```

---

## 84. BND-M7 no-empty-scaffolding rule

Do not create:

```text
Public/
Ports/
CrossContext/
Processes/
Projections/
```

without concrete types.

Do not create:

```text
IService
IManager
IGateway
```

as placeholders.

---

## 85. BND-M8 — Architecture enforcement Wave 2

Activate after Wave 1 is stable.

```text
ARCH-BC-004 — cross-context EF navigation/cascade
ARCH-BC-005 — Public contract purity
ARCH-BC-006 — Application transport/provider purity
```

---

## 86. ARCH-BC-004 implementation direction

Inspect:

```text
Infrastructure/Data configurations
Domain navigation properties
EF delete behavior
```

Detect known cross-context relationships.

Machine enforcement may combine:

```text
namespace/type analysis
configuration inspection
explicit ownership metadata
```

Do not write a brittle regex that assumes every `Guid` FK is cross-context.

---

## 87. ARCH-BC-005 implementation direction

Target Public roots:

```text
Application/Features/*/Public/**
```

Reject references to:

```text
DbContext
EF
provider SDK
generated gRPC
ASP.NET transport types
producer Domain model
internal handler
```

Be careful:

```text
stable shared primitives
```

may be allowed if already canonical.

---

## 88. ARCH-BC-006 implementation direction

Scan Domain/Application for:

```text
HttpClient
HttpRequestMessage
GrpcChannel
generated client namespaces
provider SDK namespaces
MassTransit producer client
Npgsql concrete client
Redis client
```

Apply canonical exceptions explicitly.

Do not block approved Application EF compatibility exception by naive package-level rule; target direct use in boundary/business code according to canonical architecture.

---

## 89. BND-M8 — Architecture enforcement Wave 3

Later activate:

```text
ARCH-BC-007 — Integration Event ownership/versioning
ARCH-BC-008 — Common semantic leakage
ARCH-BC-009 — optional dependency catalog
```

Wave 3 is not prerequisite for normal feature delivery if Wave 1/2 sufficiently protect critical paths.

---

## 90. ARCH-BC-007 direction

Enforce where feasible:

```text
versioned outward event
producer owner identifiable
consumer-instruction smell detection/review
```

Avoid a test that infers semantic truth from event class name alone.

Use structural rules plus review/evidence.

---

## 91. ARCH-BC-008 direction

`Common` semantic leakage is partly semantic, not fully machine-detectable.

Use:

```text
known forbidden type/namespace lists
ownership heuristics
review gate
```

Examples worth flagging:

```text
PlanTier
SubscriptionTier
WorkspaceRole
context-specific FeatureCode enum
```

Do not ban all enums from Common.

---

## 92. ARCH-BC-009 admission gate

Only create a machine-readable boundary catalog if Wave 1/2 tests become duplicated/brittle without one.

Allowed catalog data:

```text
context ID
Domain namespace root
Application feature root
owned DbContext abstraction
Public namespace root
known reviewed exceptions
```

Forbidden catalog data:

```text
future service grouping
future transport
team priority
speculative database split
```

---

## 93. BND-M8 exit gate

PASS when:

```text
critical structural boundary rules are machine-protected
baseline cannot grow silently
tests remain actionable
CI runtime remains reasonable
false positives are controlled
```

---

## 94. BND-M9 — Extraction readiness

This milestone remains dormant unless:

```text
a concrete service/runtime extraction is proposed
+
architecture decision process accepts evaluation
```

Do not execute BND-M9 merely because semantic boundaries are healthy.

---

## 95. BND-M9 candidate evidence

Before deciding topology, measure:

### Co-host affinity

```text
sync interaction density
atomic consistency pressure
latency sensitivity
hot-path coupling
failure coupling
change coupling
```

### Extraction pressure

```text
independent scaling
runtime specialization
security isolation
provider/network isolation
SLO isolation
deployment cadence
cost
data residency
```

---

## 96. BND-M9 possible outcomes

All are valid:

```text
remain modular monolith
extract worker only
extract one BC
co-host multiple BCs in one service
split one BC into multiple deployables
defer
```

Do not assume:

```text
11 BCs
→ 11 services
```

---

## 97. BND-M9 contract packaging step

Only after extraction is approved, classify contracts:

```text
Semantic
Integration
Transport
```

Possible future shape:

```text
Billing.Contracts.Semantic
Billing.Contracts.Integration
Billing.Contracts.Transport.Grpc
```

Exact projects require extraction design.

Do not pre-create them today.

---

## 98. BND-M9 remote adapter cutover

Current:

```text
Consumer Port
→ InProcessAdapter
→ Producer Public
```

Future:

```text
Consumer Port
→ RemoteAdapter
→ Transport contract
→ network
→ Producer inbound adapter
→ Producer Application
```

Consumer business handler remains unchanged.

---

## 99. BND-M9 writer cutover

Mandatory invariant:

```text
exactly one authoritative writer
```

Safe Stage A:

```text
new service shadow/read-only
old runtime sole writer
```

Safe Stage B:

```text
traffic cutover
old writer disabled
new writer sole writer
```

Never:

```text
old + new active writer
```

---

## 100. BND-M9 network failure design

For each remote sync dependency define:

```text
deadline
retry eligibility
idempotency
unknown outcome
circuit/load-shed behavior if required
service identity
actor/tenant propagation
correlation/tracing
error translation
```

These are Infrastructure/runtime mechanics.

Product fallback remains Application-owned.

---

## 101. BND-M9 data extraction

Do not split DB first.

Preferred:

```text
semantic ownership clean
→ runtime cutover
→ sole logical writer
→ remove foreign reads/joins
→ physical DB move only if operationally justified
```

Physical shared DB may remain during intermediate extraction.

---

## 102. BND-M9 rollback

Plan must answer:

```text
who is writer before cutover?
who is writer after cutover?
how is old writer disabled?
can traffic revert?
what happens to writes during rollback?
how are message versions handled?
how is partial deployment detected?
```

No rollback/roll-forward story means not extraction-ready.

---

## 103. Exact migration playbook — Common/Entitlements

This section is deliberately detailed because it is the strongest current semantic hotspot.

---

### 103.1 Do not begin with deletion

Do not start:

```text
delete IEntitlementChecker
delete FeatureCode
fix compile errors
```

That forces accidental semantics.

Start with consumer inventory.

---

### 103.2 Inventory consumers

Find every usage of:

```text
IEntitlementChecker
IFeatureGateChecker
ISubscriptionChecker
FeatureCode
IRequireFeature
HasSubscriptionTierAsync
HasActiveSubscriptionAsync
```

Classify each:

```text
PIPELINE_GATE
USE_CASE_FACT
BILLING_MUTATION
UI/API_ONLY
LEGACY_UNUSED
```

---

### 103.3 PIPELINE_GATE migration

If request gating is correctly pipeline-owned:

```text
request declares stable capability key
→ access facts obtains Billing-owned grant
→ policy evaluates
→ handler runs only if allowed
```

Preserve pipeline.

Migrate only semantic key ownership/leakage.

Do not inject entitlement checker into handler.

---

### 103.4 USE_CASE_FACT migration

If handler genuinely needs richer entitlement semantics:

```text
Consumer-owned Port
→ Billing Public semantic contract
→ optional ACL
→ Infrastructure adapter
```

Remove legacy direct checker from touched handler.

---

### 103.5 BILLING_MUTATION migration

If feature consumes quota/usage:

```text
boolean gate
```

is insufficient if authoritative Billing usage must change.

Classify:

```text
reservation
consume command
usage event
post-commit accounting
```

based on product invariant.

If atomicity with product mutation is required:

```text
STOP
```

for consistency design.

Do not silently update Billing tables inside product transaction.

---

### 103.6 Legacy preservation

After migrating one consumer:

```text
do not delete old interface
```

until all consumers are migrated or an explicit compatibility adapter maintains them.

Keep migration incremental.

---

## 104. Exact migration playbook — cross-context direct persistence

For each detected foreign DbContext/repository:

```text
1. identify read vs mutation
2. identify producer owner
3. identify semantic need
4. choose mechanism
5. add producer contract if required
6. add consumer port if semantics differ
7. add adapter
8. migrate consumer
9. tests
10. remove foreign persistence dependency
11. remove baseline entry
```

For a pure foreign read:

```text
sync fact
projection
read model
```

are candidates.

For mutation:

```text
target-owned command
```

is default.

---

## 105. Exact migration playbook — foreign Domain model

For each consumer reference:

```text
Notrelix.Domain.{Producer}.*
```

inside another context:

```text
1. identify fields/behavior actually needed
2. do not copy whole entity to DTO
3. define smallest stable Producer.Public Fact/Reference
4. map producer model internally
5. migrate consumer
6. remove foreign Domain import
7. add architecture gate/baseline removal
```

If consumer invokes foreign Domain behavior:

```text
target-owned command
```

is likely required.

---

## 106. Exact migration playbook — foreign internal MediatR

For:

```text
Consumer → IMediator → Producer internal request
```

migrate:

```text
1. identify producer-owned behavior
2. define Producer.Public action/query
3. if consumer language differs, define Consumer Port
4. add in-process adapter
5. producer Public implementation delegates internally
6. remove consumer dependency on producer internal request
7. architecture test
```

Do not expose the existing internal request merely by moving it into `Public`.

Public contract must be reviewed semantically.

---

## 107. Exact migration playbook — cross-context EF navigation/cascade

For each relationship:

```text
1. identify both context owners
2. determine whether reference is identity-only
3. replace object graph dependency with stable ID/ResourceRef
4. preserve producer existence validation through semantic contract if needed
5. remove cascade semantics
6. explicitly model delete/archive reaction
7. keep physical FK only if reviewed
8. test lifecycle
```

---

## 108. Exact migration playbook — consumer-instruction event

For event such as:

```text
RunAutomation
RefreshAnalytics
ReindexSearch
```

ask:

```text
what producer fact caused this?
```

Replace/introduce:

```text
producer completed fact
```

Then consumer owns reaction.

If an immediate command is actually intended:

```text
model as target-owned command
```

instead of pretending it is an event.

---

## 109. Exact migration playbook — Application transport leakage

For handler/service using:

```text
HttpClient
GrpcClient
provider SDK
```

migrate:

```text
1. identify semantic capability
2. define Application Port
3. define semantic input/output
4. Infrastructure implements client adapter
5. technical failure mapping
6. DI registration
7. migrate Application caller
8. tests
```

Provider DTO stays in Infrastructure.

---

## 110. Exact folder creation matrix

| Artifact | Target |
|---|---|
| aggregate/entity/value object | `Domain/{Context}/{Module}` |
| Domain event | owned Domain module `Events/` |
| use-case command | `Application/Features/{Context}/{Module}/Commands/{UseCase}` |
| use-case query | `Application/Features/{Context}/{Module}/Queries/{UseCase}` |
| context-owned DbContext abstraction | `Application/Features/{Context}/Abstractions` |
| consumer semantic port | `Application/Features/{Consumer}/Ports/{Concern}` |
| producer public semantic query | `Application/Features/{Producer}/Public/Queries` |
| producer public semantic fact | `Application/Features/{Producer}/Public/Facts` |
| producer public action | `Application/Features/{Producer}/Public/Commands` |
| producer integration contract | context Public event surface or canonical existing event location |
| pure consumer ACL | `Application/Features/{Consumer}/CrossContext/{Producer}` |
| process manager | `Application/Features/{Owner}/Processes/{Workflow}` |
| projection contract/model | `Application/Features/{Consumer}/Projections/{Projection}` |
| in-process cross-context adapter | `Infrastructure/CrossContext/{Consumer}/{Producer}` |
| remote cross-context adapter | same Infrastructure boundary after extraction |
| provider adapter | existing Infrastructure provider/context folder |
| generic messaging mechanism | `Platform/Messaging` |
| HTTP contract/binding | `API/Contracts`, `API/Endpoints` |
| architecture enforcement | `tests/Notrelix.Architecture.Tests/{RelevantArea}` |

Do not create an artifact when the corresponding semantic need does not exist.

---

## 111. Exact change-order template for Coding Agent

For every boundary implementation:

```text
Phase A — Read
1. product context docs
2. boundary SPEC
3. owning use-case source
4. producer source
5. pipeline/DI source
6. tests

Phase B — Classify
7. owner
8. mechanism
9. pipeline vs handler
10. Public vs Port
11. ACL
12. adapter
13. transaction
14. failures
15. idempotency

Phase C — Contracts first
16. smallest semantic contract/port
17. contract tests
18. no implementation yet if producer contract unresolved

Phase D — Implementation
19. producer semantic implementation
20. ACL if needed
21. Infrastructure adapter
22. DI
23. consumer migration

Phase E — Proof
24. focused Application tests
25. Infrastructure/integration tests
26. architecture tests
27. API contract tests if affected
28. full relevant gate

Phase F — Cleanup
29. remove old dependency on touched path
30. shrink baseline
31. remove dead compatibility code only if no consumers
32. report remaining debt
```

---

## 112. Expected diff discipline

Every implementation should explicitly list:

```text
EXPECTED CREATE
EXPECTED MODIFY
EXPECTED DELETE
FORBIDDEN
```

Example Automation→Work:

```text
EXPECTED CREATE
- Automation/Ports/Work/IWorkActionPort.cs
- WorkManagement/Public/... action contract
- Infrastructure/CrossContext/... adapter
- tests

EXPECTED MODIFY
- one Automation execution path
- Infrastructure DI
- Work public implementation/delegation

EXPECTED DELETE
- direct foreign dependency on touched path

FORBIDDEN
- new project
- gRPC
- foreign DbContext
- Work internal command import from Automation
```

---

## 113. Deletion discipline

Do not delete a legacy abstraction until:

```text
all consumers enumerated
all consumers migrated
DI no longer binds it
tests no longer use it
generated/API/event consumers unaffected
```

This is especially important for:

```text
Common/Entitlements
event contracts
authorization markers
```

---

## 114. Migration/DB discipline

Boundary refactor alone should not require DB migration.

If it does, classify why:

```text
removing cross-context FK/cascade
introducing projection storage
moving authoritative data
changing stable reference representation
```

Then follow canonical migration/data-change process.

Do not add schema merely to represent an interface boundary.

---

## 115. Security discipline

Every boundary change re-checks:

```text
Actor
Account
Workspace
ResourceRef
authorization action
background execution scope
service/system actor
RLS impact
```

Do not allow an in-process adapter to bypass a producer authorization requirement simply because caller is trusted code.

The producer-owned use case remains responsible for producer-side invariants/authorization contract according to canonical architecture.

---

## 116. Error discipline

Cross-context contract should return/use stable semantic outcomes.

Infrastructure translates technical failures.

API translates Application/public failures to HTTP.

Never leak:

```text
EF exception
SQLSTATE
RpcException
HttpRequestException
provider error object
```

as product semantics.

---

## 117. Observability discipline

For current in-process boundary:

```text
do not add distributed tracing complexity solely for future extraction
```

Use existing tracing/correlation infrastructure.

After extraction, remote adapter adds dependency spans/metrics according to runtime architecture.

Semantic contract should not carry observability fields unless canonical envelope/context requires them.

---

## 118. Performance discipline

Do not replace a cheap in-process path with:

```text
multiple interfaces
multiple allocations
serialization
network simulation
```

for hypothetical future extraction.

Current boundary should remain low-cost.

If direct Producer.Public is semantically valid, use it.

---

## 119. Architecture debt burn-down rule

A baseline entry leaves only when:

```text
violating dependency removed
tests prove target boundary
no compatibility path still requires violation
```

Do not remove baseline entry because file was renamed.

---

## 120. CI integration principle

Boundary architecture gates should join existing backend architecture lane.

Do not create a parallel CI universe.

Fast Wave-1/2 tests should run on ordinary backend PRs.

Expensive:

```text
real network failure
mixed-version service tests
service cutover tests
```

remain absent until extraction exists.

---

## 121. Evidence required per milestone

### BND-M0

```text
candidate source inventory
ownership classification
```

### BND-M1

```text
concrete debt baseline
risk classification
```

### BND-M2

```text
Wave-1 architecture tests
CI pass
baseline cannot grow
```

### BND-M3

```text
real producer/consumer semantic surfaces
pipeline ownership preserved
```

### BND-M4

```text
CreateBoard reference proof
```

### BND-M5

```text
target-owned cross-context mutation proof
```

### BND-M6

```text
event/reference/projection proof as applicable
```

### BND-M7

```text
team adoption cards + feature-level proof
```

### BND-M8

```text
Wave-2/3 fitness tests
```

### BND-M9

```text
ADR + operational/data/runtime extraction proof
```

---

## 122. What does not block feature teams

The following do not globally block roadmap work:

```text
all contexts having Public folder
all Common debt removed
all events migrated
all projections defined
all architecture Wave-3 gates complete
service extraction plan
gRPC contracts
independent databases
```

Only the current affected slice stops if it hits a STOP condition.

---

## 123. Global completion criteria for this execution plan

Boundary execution is considered operationally established when:

```text
BND-M0 complete
BND-M1 concrete baseline complete
BND-M2 Wave-1 gates active
BND-M3 dependency-spine patterns proven
BND-M4 pipeline-integrated reference slice proven
BND-M5 target-owned mutation slice proven
BND-M7 rolling adoption in team execution
```

`BND-M6` and `BND-M8` continue based on touched paths/priority.

`BND-M9` remains conditional.

---

## 124. Coding Agent final checklist before declaring a boundary task complete

The agent must be able to answer:

```text
1. What source SHA/state did I inspect?
2. Which canonical docs govern this change?
3. Which BC owns the use case?
4. Which BC owns each mutation?
5. Is there a workflow owner?
6. Which foreign semantic facts/actions are needed?
7. Is each concern already pipeline-owned?
8. Why is each interaction sync query / target command / event / ref / projection / process?
9. Why direct Producer.Public or why Consumer Port?
10. Is ACL required? What semantic mismatch does it solve?
11. Is adapter required? What mechanism/topology does it adapt?
12. What is the transaction boundary?
13. What are business failures?
14. What is technical dependency policy?
15. What is concurrency behavior?
16. What is idempotency behavior?
17. What happens on retry/duplicate?
18. Is any foreign persistence used?
19. Is any foreign Domain model used?
20. Is any foreign Internal/MediatR request used?
21. Does API/Infrastructure own any business rule accidentally?
22. Which tests prove the boundary?
23. Which architecture Rule IDs are enforced?
24. Which baseline entries were added/removed?
25. If producer became remote tomorrow, which business-handler lines must change?
```

Desired answer to 25:

```text
none, except possibly contract-package namespace/reference movement;
runtime adapter/composition changes instead.
```

If the answer is:

```text
rewrite handler logic around HTTP/gRPC
```

the boundary is not complete.

---

## 125. Final execution principle

The implementation sequence is intentionally:

```text
source truth
→ semantic ownership
→ smallest contract
→ current in-process mechanism
→ machine proof
→ rolling migration
→ operational evidence
→ optional distribution
```

Never reverse it into:

```text
future network topology
→ adapter framework
→ generic service contracts
→ forced domain design
```

The success condition is a modular monolith whose bounded contexts are already semantically clean enough that later runtime extraction is a topology change rather than a business rewrite.
