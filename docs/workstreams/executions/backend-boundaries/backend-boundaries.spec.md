---
document_id: WRK-SPEC-BACKEND-BOUNDARIES-V3
document_type: execution-spec
status: draft
owner: backend-architecture
version: 3
audit_snapshot:
  repository: Nqv1208/Notrelix
  branch: main
  commit: 6030d06051e8bbb4844746150be5c1d5d4c53bbd
applies_to:
  - backend/src/Notrelix.Domain
  - backend/src/Notrelix.Application
  - backend/src/Notrelix.Infrastructure
  - backend/src/Notrelix.Platform
  - backend/src/Notrelix.API
  - backend/tests
  - cross-bounded-context feature delivery
  - future backend service extraction
authority_model:
  higher_authorities:
    - RULE.md
    - AGENTS.md
    - backend/AGENTS.md
    - docs/architecture/bounded-context-map.md
    - docs/architecture/contract-boundaries.md
    - docs/architecture/data-ownership-and-consistency.md
    - docs/architecture/capability-extraction-strategy.md
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
  this_document_owns:
    - executable cross-bounded-context coding rules
    - canonical target folder topology for boundary-related code
    - placement rules for Public contracts, Consumer Ports, ACLs and adapters
    - current-monolith to future-service replacement invariants
    - boundary migration rules for existing source
    - mandatory boundary stop conditions
  this_document_does_not_own:
    - product semantics
    - bounded-context discovery
    - team priority
    - roadmap phase sequencing
    - provider selection
    - authorization policy meaning
    - service-extraction approval
---

# SPEC — Backend Boundary Execution V3

## 1. Purpose

This specification is the implementation contract for any backend change that crosses a bounded-context boundary.

It is written to remove architectural ambiguity for both human engineers and Coding Agents.

The target is not merely:

```text
code compiles
tests pass
feature works
```

The target is:

```text
feature works
+
business ownership remains explicit
+
cross-context interaction is semantic
+
data ownership remains enforceable
+
current modular-monolith implementation stays simple
+
future runtime/service topology can change without business-handler redesign
```

The highest invariant is:

> Business/Application code must not know whether a depended-on bounded context is in-process, hosted in another worker, or reached over a network.

This document does **not** authorize microservices.

This document does **not** authorize new production projects.

This document does **not** require an adapter for every cross-context dependency.

This document does **not** require repository-wide folder migration before feature work.

---

## 2. Audit baseline

### 2.1 Audited production topology

The current backend production topology is exactly:

```text
Notrelix.Domain
Notrelix.Application
Notrelix.Infrastructure
Notrelix.Platform
Notrelix.API
```

This topology remains authoritative for normal execution.

A Coding Agent MUST NOT create:

```text
Notrelix.WorkManagement.Service
Notrelix.Billing.Service
Notrelix.Workspaces.Service
Notrelix.Automation.Worker
```

or any other new production project unless a separately accepted architecture/extraction decision explicitly authorizes it.

---

### 2.2 Audited Application structure

Current Application is already context/module oriented:

```text
Notrelix.Application/
├── Common/
├── EventMappers/
├── Events/
├── Features/
│   ├── Accounts/
│   ├── Analytics/
│   ├── Automation/
│   ├── Billing/
│   ├── Collaboration/
│   ├── Documents/
│   ├── Governance/
│   ├── Identity/
│   ├── Integrations/
│   ├── Notifications/
│   ├── Operations/
│   ├── Search/
│   ├── WorkManagement/
│   └── Workspaces/
└── DependencyInjection.cs
```

The canonical Application architecture already defines module-first use-case placement:

```text
Features/{BoundedContext}/{Module}/Commands/{UseCase}/
Features/{BoundedContext}/{Module}/Queries/{UseCase}/
```

This specification extends that existing convention.

It does not replace it.

---

### 2.3 Audited context-local persistence abstractions

Examples already exist:

```text
Features/WorkManagement/Abstractions/IWorkManagementDbContext.cs
Features/Workspaces/Abstractions/IWorkspaceDbContext.cs
```

The existence of context-local DbContext abstractions is good evidence for logical ownership.

However:

```text
IWorkManagementDbContext
```

is a WorkManagement-local persistence capability.

It is not a public WorkManagement integration contract.

---

### 2.4 Audited semantic hotspot

Current source includes:

```text
Application/Common/Entitlements/
├── FeatureCode.cs
├── IEntitlementChecker.cs
├── IFeatureGateChecker.cs
└── ISubscriptionChecker.cs
```

This is a known architecture hotspot.

It may contain historically useful pipeline/application abstractions, but it must not become permanent authority for context-owned Billing/product capability semantics.

New code MUST NOT expand plan/tier/subscription semantic coupling in `Common`.

Migration is incremental and defined later in this specification.

---

### 2.5 Audited Infrastructure structure

Current Infrastructure contains mechanism/context folders such as:

```text
Auditing
Auth
BackgroundJobs
Billing
Caching
Configuration
Data
DependencyInjection
Email
Events
Identity
Integrations
Messaging
...
```

This is current implementation evidence.

Infrastructure folder names do not define bounded contexts.

Infrastructure may implement context-specific adapters, but business ownership remains inward.

---

### 2.6 Audited API structure

Current API contains:

```text
Contracts
Endpoints
ErrorHandling
Extensions
Idempotency
Middleware
OpenApi
Options
DependencyInjection.cs
Program.cs
```

API remains transport/composition.

Cross-context business coordination MUST NOT be moved into API merely because multiple endpoints or contracts are involved.

---

### 2.7 Audited Platform structure

Current Platform is intentionally narrow, with Messaging as its primary source area.

Platform owns reusable runtime/delivery mechanics.

It MUST NOT become the dumping ground for:

```text
cross-context business orchestration
generic "service bus" business APIs
context-specific permission policy
context-specific entitlement rules
```

---

### 2.8 Audited architecture enforcement

Current `DbContextBoundaryArchitectureTests`:

```text
scans Features/**
filters *Handler.cs
maps module to expected DbContext
searches source text for forbidden DbContext interface names
```

This provides useful initial enforcement but does not prove:

```text
foreign Domain-model dependency
producer Internal/MediatR dependency
Public-contract purity
cross-context EF navigation
Application transport purity
cross-context event semantics
```

This specification defines the target rules.

The TESTS execution document defines how those rules become machine enforcement.

---

## 3. Architecture vocabulary

### BOUND-CORE-001 — Semantic topology, code topology and runtime topology are different

Always reason with:

```text
Semantic topology
≠
Code topology
≠
Runtime topology
≠
Transport topology
```

#### Semantic topology

Answers:

```text
Who owns meaning?
Who owns invariant?
Who owns authoritative state?
Who owns mutation?
```

#### Code topology

Today:

```text
five production projects
+
bounded-context folders/namespaces
```

#### Runtime topology

Today may be:

```text
one API process
+
existing runtime workers/mechanisms
```

Future may be:

```text
API
Billing Service
Automation Worker
Search Indexer
...
```

#### Transport topology

May be:

```text
in-process
HTTP
gRPC
broker
queue
projection
```

No Coding Agent may infer one topology from another.

---

### BOUND-CORE-002 — Required conceptual hierarchy

```text
Business Domain
    ↓
Bounded Context
    ↓
Context-owned semantic surface
    ↓
Application use case / Consumer Port
    ↓
Runtime adapter
    ↓
Transport
    ↓
Deployable
```

Wrong:

```text
"We may want microservices"
→ introduce HTTP
→ introduce service interfaces
→ redesign domain around network
```

Correct:

```text
semantic ownership
→ explicit boundary
→ current in-process implementation
→ operational evidence
→ optional extraction
```

---

## 4. Bounded-context ownership

### BOUND-OWN-001 — One authoritative state has one semantic owner

Every authoritative business state must have one bounded-context owner.

Examples:

```text
Workspace lifecycle
→ Workspaces

role/policy/permission meaning
→ Governance

Board / BoardItem lifecycle
→ WorkManagement

subscription/plan/entitlement meaning
→ Billing

AutomationRule / AutomationExecution
→ Automation
```

Do not infer ownership from:

```text
team
database schema alone
folder alone
API endpoint
provider
who currently queries the table most
```

---

### BOUND-OWN-002 — Every authoritative mutation has one mutation authority

For each state change answer:

```text
Which BC is allowed to decide and commit this mutation?
```

Examples:

```text
CreateBoard
→ WorkManagement

ChangeWorkspaceMembership
→ Workspaces

ChangeRolePolicy
→ Governance

ConsumeBillingUsage
→ Billing
```

A consumer may request another BC to perform a mutation.

The consumer does not gain mutation authority.

---

### BOUND-OWN-003 — Workflow owner is not mutation owner

For a long-running cross-context workflow:

```text
one BC owns orchestration semantics
+
each participant owns its own mutations
```

Example:

```text
WorkspaceTerminationProcess
→ workflow owner = Workspaces

Work cleanup
→ mutation owner = WorkManagement

Document cleanup
→ mutation owner = Documents

Automation cleanup
→ mutation owner = Automation

Billing operation
→ mutation owner = Billing
```

Never write:

```text
WorkspaceTermination owns all tables involved
```

---

## 5. Canonical five-project target topology

This section defines where boundary-related code belongs.

It does not require immediate migration of every legacy file.

---

### 5.1 Domain target

Canonical shape:

```text
backend/src/Notrelix.Domain/
├── Accounts/
├── Identity/
├── Workspaces/
├── Governance/
├── WorkManagement/
├── Documents/
├── Collaboration/
├── Automation/
├── Integrations/
├── Billing/
└── Analytics/
```

Inside a context:

```text
{Context}/
├── {AggregateOrModule}/
│   ├── {Aggregate}.cs
│   ├── {Entity}.cs
│   ├── {ValueObject}.cs
│   ├── {DomainService}.cs              # only if genuinely domain-owned
│   └── Events/
│       └── {Fact}DomainEvent.cs
└── ...
```

Domain MUST NOT contain:

```text
Public/
Ports/
CrossContext/
Remote/
Grpc/
Http/
Infrastructure/
API DTOs
EF configuration
provider SDK models
```

Reason:

```text
Domain owns business truth
not integration topology
```

---

### 5.2 Application target

Canonical context shape:

```text
backend/src/Notrelix.Application/
└── Features/
    └── {Context}/
        ├── Abstractions/
        ├── Ports/
        ├── Public/
        ├── CrossContext/
        ├── Processes/
        ├── Projections/
        └── {Module}/
            ├── Commands/
            │   └── {UseCase}/
            ├── Queries/
            │   └── {UseCase}/
            ├── DTOs/
            ├── ReadModels/
            ├── Mapping/
            ├── Permissions/
            └── Services/
```

Not every folder must exist.

Create a folder only when at least one real type belongs there.

Empty architecture scaffolding is forbidden.

---

### 5.3 Meaning of Application folders

#### `Abstractions/`

Context-local inward technical/application abstractions.

Examples:

```text
IWorkManagementDbContext
IWorkspaceDbContext
```

These are not Public cross-context contracts.

Rules:

```text
consumer outside context must not inject them
they may expose owned Domain types if necessary for owned persistence
Infrastructure implements them
```

#### `Ports/`

Consumer-owned semantic needs from outside the context.

Example:

```text
WorkManagement/
└── Ports/
    ├── Authorization/
    │   └── IWorkAuthorizationPort.cs
    └── Entitlements/
        └── IWorkEntitlementPort.cs
```

A Port speaks consumer language.

#### `Public/`

Producer-owned semantic surface for other contexts.

Example:

```text
Workspaces/
└── Public/
    ├── Queries/
    ├── Commands/
    ├── Facts/
    ├── Events/
    └── References/
```

Do not create all subfolders by default.

#### `CrossContext/`

Application-level **pure semantic bridge logic** only.

Use for:

```text
ACL translator
consumer-side semantic mapper
cross-context result normalization
```

Example:

```text
WorkManagement/
└── CrossContext/
    └── Billing/
        └── WorkEntitlementAcl.cs
```

This folder MUST NOT contain:

```text
HttpClient
GrpcClient
EF query
provider SDK
network retry
```

Those belong to Infrastructure.

#### `Processes/`

Long-running workflow orchestration owned by this context.

Example:

```text
Workspaces/
└── Processes/
    └── WorkspaceTermination/
        ├── WorkspaceTerminationProcess.cs
        ├── WorkspaceTerminationState.cs
        └── ...
```

#### `Projections/`

Application contract/model for consumer-owned derived state.

Example:

```text
WorkManagement/
└── Projections/
    └── Entitlements/
        ├── WorkEntitlementProjection.cs
        └── IWorkEntitlementProjectionStore.cs
```

Infrastructure implements storage/feed mechanics.

---

## 6. Application use-case placement

### BOUND-APP-001 — Preserve canonical module-first placement

New use cases MUST follow:

```text
Features/{Context}/{Module}/Commands/{UseCase}/
Features/{Context}/{Module}/Queries/{UseCase}/
```

Example:

```text
Features/WorkManagement/Boards/Commands/CreateBoard/
├── CreateBoardCommand.cs
├── CreateBoardHandler.cs
├── CreateBoardValidator.cs
└── CreateBoardResult.cs
```

Do not create:

```text
Features/WorkManagement/Commands/Boards/CreateBoard/
```

for new code solely because legacy source may use older layout.

---

### BOUND-APP-002 — Handler remains visible orchestration

A handler may:

```text
obtain required semantic facts
invoke consumer ports
invoke owned Domain behavior
use owned persistence abstraction
return semantic result
```

A handler MUST NOT:

```text
directly call HttpClient
directly call gRPC generated client
directly use provider SDK
inject foreign DbContext
dispatch foreign internal MediatR request
mutate foreign aggregate
invent plan/role semantics
```

---

## 7. Public semantic surface

### BOUND-PUB-001 — Public exists only when a real cross-context consumer exists

Do not proactively create:

```text
Public/
```

for every context.

Create it when:

```text
a stable cross-context fact/query/action/reference/event is actually required
```

---

### BOUND-PUB-002 — Public is semantic, not implementation

Allowed Public types:

```text
stable query interfaces
stable action/facade interfaces
small immutable Fact records
stable Reference records
versioned Integration Event contracts
semantic result/status types
```

Forbidden:

```text
aggregate
entity
DbContext
repository
internal handler
MediatR internal request
EF entity/model
provider SDK DTO
generated gRPC type
ASP.NET request/response type
database row
Npgsql/Redis type
```

---

### BOUND-PUB-003 — Producer Public query placement

Example:

```text
Features/Workspaces/Public/Queries/
├── IWorkspaceFacts.cs
└── WorkspaceFactQuery.cs              # only if public facade implementation is useful
```

Fact:

```text
Features/Workspaces/Public/Facts/
└── WorkspaceScopeFact.cs
```

The public contract belongs to the producer because the producer owns the meaning.

---

### BOUND-PUB-004 — Producer Public action placement

When another BC must request producer-owned mutation:

```text
Features/WorkManagement/Public/Commands/
└── IWorkActions.cs
```

or an equivalent explicit public application facade.

The public action contract may internally delegate to:

```text
internal Application command/use-case
Domain behavior
owned persistence
```

External contexts MUST NOT dispatch the producer's internal command directly.

---

## 8. Internal MediatR boundary

### BOUND-MEDIATOR-001 — MediatR is not a cross-context API by default

Forbidden:

```csharp
await mediator.Send(
    new UpdateBoardItemCommand(...));
```

from Automation if `UpdateBoardItemCommand` is WorkManagement internal use-case implementation.

Allowed:

```text
Automation
→ IWorkActionPort
→ WorkManagement.Public/IWorkActions
→ WorkManagement internal Application implementation
```

The producer may use MediatR internally behind its Public facade.

---

### BOUND-MEDIATOR-002 — Public request types must be explicit if intentionally shared

Do not infer:

```text
public CLR visibility
=
public architecture contract
```

Only types under approved Public contract surface or explicitly governed shared contract count as cross-context contracts.

---

## 9. Producer Public vs Consumer Port

### BOUND-PORT-001 — Same semantic language

If consumer needs exactly the producer-owned fact:

```text
WorkManagement
→ Workspaces.Public.IWorkspaceFacts
```

Use Producer.Public directly.

Do not create:

```text
IWorkWorkspaceFactsPort
```

that merely renames every Workspace field without semantic reason.

---

### BOUND-PORT-002 — Different semantic language

If consumer should not understand producer vocabulary:

```text
WorkManagement
→ IWorkEntitlementPort
```

is appropriate.

Example:

Wrong:

```text
WorkManagement
→ Billing.HasSubscriptionTier("Pro")
```

Correct:

```text
WorkManagement
→ IWorkEntitlementPort.CanCreateBoard(...)
```

---

### BOUND-PORT-003 — Port granularity

Avoid both extremes.

Wrong generic port:

```text
IBillingService
IWorkspaceService
IGovernanceManager
```

Wrong interface explosion:

```text
ICanCreateBoardPort
ICanCreateItemPort
ICanCreateFieldPort
ICanMoveItemPort
```

for every single method with no cohesive consumer semantic.

Prefer a cohesive consumer capability boundary:

```text
IWorkEntitlementPort
IWorkAuthorizationPort
ICommentTargetPort
IWorkActionPort
```

Split only when:

```text
ownership differs
failure semantics differ materially
runtime mechanism differs materially
cohesion becomes poor
```

---

## 10. ACL placement and meaning

### BOUND-ACL-001 — ACL is pure semantic translation

Canonical placement:

```text
Notrelix.Application/
└── Features/
    └── {Consumer}/
        └── CrossContext/
            └── {Producer}/
                └── {Semantic}Acl.cs
```

Example:

```text
Features/WorkManagement/CrossContext/Billing/WorkEntitlementAcl.cs
```

ACL may translate:

```text
producer semantic result
→ consumer semantic result
```

ACL must remain provider/transport independent.

---

### BOUND-ACL-002 — Producer business policy cannot migrate into ACL

Example:

If Billing owns:

```text
Plan
Subscription
Entitlement grant calculation
```

then Work ACL must not implement:

```csharp
if (plan == Plan.Pro)
{
    return WorkCapability.Allowed;
}
```

Billing must first expose its own stable semantic decision/grant.

Work ACL may then map that producer meaning into Work language.

---

## 11. Infrastructure adapter topology

### BOUND-INFRA-001 — Runtime adapters live in Infrastructure

Canonical target when an adapter is required:

```text
Notrelix.Infrastructure/
└── CrossContext/
    └── {Consumer}/
        └── {Producer}/
            ├── {Name}InProcessAdapter.cs
            ├── {Name}ProjectionAdapter.cs
            └── future:
                {Name}GrpcAdapter.cs
```

Do not create `CrossContext/` globally until first real adapter requires it.

Current context-specific Infrastructure folders may remain during migration.

---

### BOUND-INFRA-002 — Adapter is optional, not ceremonial

Adapter required when at least one exists:

```text
Consumer Port must be implemented
runtime topology must be hidden
transport mapping exists
Infrastructure persistence/query mechanism exists
projection storage lookup exists
scope/correlation adaptation exists
```

Adapter not required when:

```text
consumer uses Producer.Public directly
+
same process
+
no semantic translation
+
no mechanism adaptation
```

---

### BOUND-INFRA-003 — In-process consumer-port implementation

Recommended full chain when Consumer Port + ACL exists:

```text
WorkManagement Handler
    ↓
IWorkEntitlementPort
    ↓
Infrastructure/CrossContext/WorkManagement/Billing/
BillingEntitlementInProcessAdapter
    ↓
Billing.Public semantic contract
    ↓
WorkEntitlementAcl
    ↓
Work semantic result
```

The actual invocation/mapping order may be implemented cleanly as:

```text
adapter calls producer
→ receives producer result
→ invokes pure ACL
→ returns consumer result
```

Responsibilities stay separate.

---

### BOUND-INFRA-004 — Infrastructure adapter must not own business policy

Adapter may own:

```text
DI implementation
runtime invocation
transport client
transport DTO mapping
scope propagation
correlation/trace propagation
technical timeout/retry policy
technical failure translation
projection storage
```

Adapter may not own:

```text
aggregate decisions
permission policy
plan entitlement calculation
workflow orchestration
consumer product fallback policy
```

---

## 12. Dependency injection ownership

### BOUND-DI-001 — Application DI registers Application mechanics

`Notrelix.Application/DependencyInjection.cs` may register:

```text
MediatR
validators
pipeline behaviors
pure Application services
pure ACL translators where registration is useful
```

It MUST NOT instantiate Infrastructure concrete adapters.

---

### BOUND-DI-002 — Infrastructure DI registers runtime implementations

Current Infrastructure already has:

```text
DependencyInjection.cs
DependencyInjection/
```

Use Infrastructure registration for:

```text
DbContext ports
cross-context runtime adapters
provider clients
remote clients
projection stores
cache/storage implementations
```

Context-specific registration is preferred when existing conventions support it.

Example:

```text
DependencyInjection/
├── BillingRegistration.cs
├── WorkManagementRegistration.cs
└── CrossContextRegistration.cs          # only if central grouping is actually useful
```

Do not create a giant boundary registry merely to centralize all business topology.

---

### BOUND-DI-003 — API is composition root, not semantic owner

API may compose:

```text
AddApplication(...)
AddInfrastructure(...)
AddPlatform(...)
```

API may configure environment/runtime choices.

API MUST NOT decide:

```text
which Billing plan grants Work capability
which Governance role grants action
how workflow compensates
```

---

## 13. Business failure vs technical dependency failure

### BOUND-FAIL-001 — Business failure belongs to semantic use case

Examples:

```text
WorkspaceNotFound
WorkspaceInactive
PermissionDenied
CapabilityDenied
LimitExceeded
InvalidBoardState
```

These may be represented by existing application result/error conventions.

---

### BOUND-FAIL-002 — Technical failure belongs to runtime/dependency boundary

Examples:

```text
timeout
connection unavailable
protocol failure
broker unavailable
remote host unavailable
```

Do not add future network vocabulary to producer semantic contracts today.

Wrong:

```text
WorkspaceGrpcTimeout
BillingHttpUnavailable
```

inside Product/Application semantic contract.

---

### BOUND-FAIL-003 — Application may define technical dependency policy

Application may define what the use case does when a dependency cannot be obtained.

Possible policy:

```text
fail request
fail closed
use an approved projection
defer
```

But the low-level transport exception remains Infrastructure-owned.

Example:

```text
RpcException
→ Infrastructure maps to dependency failure
→ Application/API follows approved policy
```

---

## 14. Synchronous Query

### BOUND-SYNCQ-001 — Use sync query for authoritative answer required before continuation

Examples:

```text
workspace active?
resource exists?
current authorization decision?
```

Each query dependency must define:

```text
producer authority
input identity
tenant/account/workspace scope
freshness
not-found behavior
race tolerance
business failure
technical dependency policy
```

---

### BOUND-SYNCQ-002 — Synchronous does not mean atomically consistent with caller

Example:

```text
Billing says allowed
↓
Billing changes
↓
Work commits
```

If the product cannot tolerate this race:

```text
revision/validity protocol
projection/version model
ownership redesign
or long-running consistency
```

must be considered.

Do not assume RPC solves the race.

---

## 15. Target-owned synchronous command

### BOUND-CMD-001 — Producer owns mutation

Example:

```text
Automation
→ IWorkActionPort
→ Work Public action
→ Work internal use case
→ Work Domain
→ Work persistence
```

Automation owns:

```text
why it wants the action
how its execution reacts
```

Work owns:

```text
whether Work mutation is valid
how Work state changes
Work commit
```

---

### BOUND-CMD-002 — Distributed atomicity check

Before foreign mutation answer:

```text
Does caller require:
local caller mutation
AND
target mutation
to roll back together?
```

If yes:

```text
STOP
```

Then evaluate:

```text
mutation ownership
Process Manager
compensation
reservation/commit protocol
different consistency model
```

Do not hide distributed atomicity behind the current shared database.

---

### BOUND-CMD-003 — Remote command idempotency classification

Before a target-owned command can become remote, classify:

```text
naturally idempotent
or
idempotency key required
or
automatic retry forbidden
```

Critical future failure:

```text
remote commits
↓
response is lost
↓
consumer retries
```

The design must not duplicate the mutation.

---

## 16. Domain Event and Integration Event

### BOUND-EVT-001 — Domain Event is internal Domain fact

Example:

```text
BoardItemChangedDomainEvent
```

It is not automatically a broker/public contract.

---

### BOUND-EVT-002 — Integration Event is versioned outward fact

Preferred chain:

```text
Domain mutation
→ Domain Event
→ Application mapper
→ Integration Event V1
→ transactional outbox
→ commit
→ Platform/Infrastructure delivery
```

Current top-level Application `EventMappers/` and `Events/` are accepted current layout.

New context-specific integration contracts SHOULD prefer producer context ownership under:

```text
Features/{Producer}/Public/Events/
```

when doing so does not conflict with canonical existing messaging conventions.

Do not mass-move existing events merely for folder aesthetics.

---

### BOUND-EVT-003 — Event describes producer fact, not consumer instruction

Good:

```text
BoardItemChangedV1
SubscriptionChangedV1
WorkspaceArchivedV1
```

Bad:

```text
RunAutomationsV1
RefreshAnalyticsV1
UpdateSearchIndexV1
```

unless the producer truly owns the commanded action.

---

### BOUND-EVT-004 — Consumer assumes at-least-once unless stronger guarantee is explicit

Durable consumer must handle duplicate delivery safely.

Platform owns generic delivery mechanics.

Consumer BC owns business idempotency/reaction.

---

## 17. Platform boundary

### BOUND-PLATFORM-001 — Platform owns reusable delivery mechanism

Platform may own:

```text
message envelopes
dispatch mechanics
consumer runtime
ordering primitive
retry mechanism
poison mechanism
delivery observability
```

Platform MUST NOT own:

```text
BoardItem semantics
Billing entitlement meaning
Automation trigger interpretation
Workspace lifecycle
```

---

### BOUND-PLATFORM-002 — Message contract ownership remains producer context

A Platform message abstraction may carry:

```text
MessageId
ContractName
Version
OccurredAt
CorrelationId
CausationId
AccountId
WorkspaceId
trace metadata
```

The business event payload remains producer-owned.

---

## 18. ResourceRef and stable cross-context identity

### BOUND-REF-001 — Prefer stable references over entity graphs

Concept:

```text
ResourceRef
├── ResourceType
└── ResourceId
```

Use for:

```text
comment target
automation target
audit subject
activity subject
notification subject
attachment relation
integration mapping
```

---

### BOUND-REF-002 — ResourceRef does not grant validity or authorization

A reference only answers:

```text
which resource?
```

It does not prove:

```text
resource exists
actor may access it
resource belongs to claimed workspace
```

Application resolves required authoritative facts.

---

## 19. Projection topology

### BOUND-PROJ-001 — Projection is consumer-owned derived state

Application shape:

```text
Features/{Consumer}/Projections/{Projection}/
├── {ProjectionModel}.cs
└── I{Projection}Store.cs
```

Infrastructure shape:

```text
Infrastructure/
├── ReadModels/
│   └── {Consumer}/{Projection}/
or
└── CrossContext/
    └── {Consumer}/{Producer}/
        └── {Projection}Adapter.cs
```

Use the existing Infrastructure organization that best matches mechanism ownership.

Do not create duplicate storage layers.

---

### BOUND-PROJ-002 — Source authority never moves to projection

Projection must define:

```text
source owner
consumer owner
freshness
revision/order
rebuild path
failure/stale behavior
tenant scope
```

---

### BOUND-PROJ-003 — Security projection requires stronger semantics

Authorization/security projection must define:

```text
fail closed
revocation propagation
revision/invalidation
tenant/resource isolation
stale-state policy
```

---

## 20. Process Manager topology

### BOUND-PM-001 — Application owns workflow semantics

Canonical:

```text
Features/{WorkflowOwner}/Processes/{WorkflowName}/
├── {WorkflowName}Process.cs
├── {WorkflowName}State.cs
├── Commands/
├── EventHandlers/
└── ...
```

Infrastructure/Platform may persist/deliver workflow state/messages as mechanisms.

Do not place business Process Manager in Platform.

---

### BOUND-PM-002 — Participants remain autonomous mutation owners

Each participant:

```text
receive public command/event
→ validate locally
→ local transaction
→ emit outcome
```

Process Manager tracks progression.

---

## 21. Database ownership

### BOUND-DATA-001 — Shared physical database is allowed

Current physical `ApplicationDbContext` may implement multiple context-specific interfaces.

This does not merge contexts.

Conceptually:

```text
ApplicationDbContext
implements
  IAccountDbContext
  IIdentityDbContext
  IWorkspaceDbContext
  IGovernanceDbContext
  IWorkManagementDbContext
  ...
```

---

### BOUND-DATA-002 — Handler uses owned persistence only

Correct:

```text
CreateBoardHandler
→ IWorkManagementDbContext
```

Forbidden:

```text
CreateBoardHandler
→ IWorkspaceDbContext
→ IBillingDbContext
```

Use semantic cross-context contracts instead.

---

### BOUND-DATA-003 — Cross-context relationship rules

| Relationship | Policy |
|---|---|
| same-BC FK | allowed |
| stable scalar ID across BC | preferred |
| ResourceRef | preferred when polymorphic/resource-oriented |
| cross-BC ORM navigation | forbidden |
| cross-BC cascade | forbidden |
| cross-BC physical FK | reviewed integrity constraint / extraction debt |
| cross-BC write via foreign persistence | forbidden |
| broad transactional join as integration contract | forbidden |

---

### BOUND-DATA-004 — Read optimization does not create write authority

Approved read model/projection may:

```text
join
copy
denormalize
index
```

for query purposes.

All authoritative writes return to source owner.

---

## 22. Domain-model boundary

### BOUND-DOMAIN-001 — Producer Domain model is never cross-context integration API

Another context MUST NOT depend on producer:

```text
aggregate
entity
internal value object
domain enum
domain service
```

even if:

```text
consumer only reads it
type is public C#
same assembly makes import easy
```

Allowed:

```text
Producer.Public Fact
Producer.Public Reference
stable scalar ID
ResourceRef
approved technical primitive
```

---

## 23. Common boundary

### BOUND-COMMON-001 — Common owns mechanism, not arbitrary shared business meaning

Good candidates:

```text
execution context
technical Result primitives
correlation
generic idempotency mechanism
time abstraction
request marker infrastructure
technical messaging abstractions
```

Bad default candidates:

```text
PlanTier
SubscriptionTier
WorkspaceRole business semantics
global entitlement vocabulary
global Work permission meaning
global Automation trigger vocabulary
```

---

### BOUND-COMMON-002 — Existing Common/Entitlements is migration debt, not precedent

Current:

```text
Common/Entitlements/FeatureCode
Common/Entitlements/IEntitlementChecker
Common/Entitlements/IFeatureGateChecker
Common/Entitlements/ISubscriptionChecker
```

Migration rule:

```text
new product feature
→ do not add new plan/tier coupling

touched consumer feature
→ migrate toward consumer capability semantics where appropriate

pipeline-owned generic feature gate
→ preserve only if semantics are truly cross-cutting and owner-neutral

Billing-owned plan/subscription semantics
→ move/consume through Billing-owned semantic contract
```

No big-bang rewrite is required.

---

## 24. Authorization boundary

### BOUND-AUTH-001 — Product context owns action/resource meaning

Example:

```text
WorkManagement owns:
Board action names / resource meaning

Governance owns:
policy / role / permission semantics

Application pipeline owns:
enforcement mechanism

Infrastructure owns:
mechanical fact retrieval where needed
```

Do not merge these merely because authorization runs in a shared pipeline.

---

### BOUND-AUTH-002 — Feature handler does not hard-code Governance internals

Forbidden:

```text
if role == "Admin"
if permissionRow.Code == ...
```

unless that exact semantic is explicitly owned/public.

Prefer:

```text
resource/action authorization declaration
```

through the canonical pipeline/port.

---

## 25. API boundary

### BOUND-API-001 — API endpoint is inbound transport adapter

Endpoint owns:

```text
binding
authentication integration
Application request construction
HTTP result mapping
OpenAPI metadata
```

Endpoint does not own:

```text
Domain rule
cross-context workflow
DbContext query/write
Billing/Governance policy
```

---

### BOUND-API-002 — BFF/API composition is read/presentation composition

Allowed:

```text
Board view
→ Work read
→ Collaboration summary
→ entitlement UI state
```

Forbidden:

```text
API endpoint manually coordinates three BC mutations
```

Use owned Application orchestration/Process Manager.

---

## 26. Current in-process interaction patterns

Coding Agent must choose exactly one of these patterns.

---

### Pattern A — Producer Public direct

Use when consumer and producer semantics match.

```text
Consumer Handler
→ Producer.Public query/fact contract
→ producer implementation
```

No adapter is required if no adaptation exists.

Example:

```text
CreateBoardHandler
→ IWorkspaceFacts
```

---

### Pattern B — Consumer Port + ACL + in-process adapter

Use when semantic translation is required.

```text
Consumer Handler
→ Consumer Port
→ Infrastructure in-process adapter
→ Producer.Public
→ pure consumer ACL
→ Consumer result
```

Example:

```text
Work
→ IWorkEntitlementPort
→ BillingEntitlementInProcessAdapter
→ Billing.Public
→ WorkEntitlementAcl
```

---

### Pattern C — Integration Event

```text
Producer local commit
→ Integration Event
→ outbox
→ delivery
→ Consumer event handler
→ consumer local transaction
```

---

### Pattern D — Local Projection

```text
Producer event/fact
→ consumer projection updater
→ consumer-owned read model
→ consumer query/use case
```

---

### Pattern E — Target-owned Command

```text
Consumer
→ Consumer Port
→ Producer.Public action
→ producer internal use case
→ producer local transaction
```

---

### Pattern F — Process Manager

```text
Workflow Owner
→ durable process state
→ participant command/event
→ participant local transaction
→ participant outcome
→ workflow progression
```

No other cross-context mechanism should be invented without architecture review.

---

## 27. Canonical current folder examples

### 27.1 WorkManagement consumer of Workspaces/Governance/Billing

```text
Notrelix.Application/
└── Features/
    └── WorkManagement/
        ├── Abstractions/
        │   └── IWorkManagementDbContext.cs
        ├── Ports/
        │   ├── Authorization/
        │   │   └── IWorkAuthorizationPort.cs
        │   └── Entitlements/
        │       └── IWorkEntitlementPort.cs
        ├── CrossContext/
        │   └── Billing/
        │       └── WorkEntitlementAcl.cs
        └── Boards/
            └── Commands/
                └── CreateBoard/
                    ├── CreateBoardCommand.cs
                    ├── CreateBoardHandler.cs
                    └── CreateBoardValidator.cs

Features/
├── Workspaces/
│   └── Public/
│       ├── Queries/
│       │   └── IWorkspaceFacts.cs
│       └── Facts/
│           └── WorkspaceScopeFact.cs
├── Governance/
│   └── Public/
│       └── ...
└── Billing/
    └── Public/
        └── ...

Notrelix.Infrastructure/
└── CrossContext/
    └── WorkManagement/
        ├── Governance/
        │   └── GovernanceAuthorizationInProcessAdapter.cs
        └── Billing/
            └── BillingEntitlementInProcessAdapter.cs
```

Do not create Workspace adapter if direct Producer.Public use is sufficient.

---

### 27.2 Automation requests Work mutation

```text
Notrelix.Application/
└── Features/
    ├── Automation/
    │   └── Ports/
    │       └── Work/
    │           └── IWorkActionPort.cs
    └── WorkManagement/
        └── Public/
            └── Commands/
                └── IWorkActions.cs

Notrelix.Infrastructure/
└── CrossContext/
    └── Automation/
        └── WorkManagement/
            └── WorkActionInProcessAdapter.cs
```

Automation MUST NOT reference Work internal command classes.

---

### 27.3 Collaboration target reference

```text
Notrelix.Application/
└── Features/
    └── Collaboration/
        ├── Ports/
        │   └── Targets/
        │       └── ICommentTargetPort.cs
        └── Comments/
            └── Commands/
                └── CreateComment/
```

Target is stored as:

```text
ResourceRef
```

or another approved stable reference.

No:

```text
Comment.BoardItem
Comment.Document
```

cross-context ORM navigation.

---

## 28. Future service topology

### BOUND-SVC-001 — Service means independently deployable runtime

A future extracted service may conceptually become:

```text
services/Billing/
├── Billing.Domain
├── Billing.Application
├── Billing.Infrastructure
├── Billing.Contracts
├── Billing.API
├── Billing.Worker
└── Billing.Scheduler
```

This is an example only.

Exact project topology requires separate extraction decision.

---

### BOUND-SVC-002 — Service is not synonymous with public REST API

A service may expose:

```text
external HTTP ingress
internal gRPC ingress
internal HTTP ingress
message consumer ingress
worker
scheduler
```

These are adapters.

Business semantics remain in Application/Domain.

---

### BOUND-SVC-003 — 1 BC != 1 service

Valid future shapes:

```text
one service hosts multiple BCs
one BC has multiple deployables
one BC stays in modular monolith
one runtime-specialized worker is extracted
```

Do not freeze service grouping today.

---

## 29. Semantic, integration and transport contracts after extraction

### BOUND-CONTRACT-001 — Semantic contract

Purpose:

```text
Application/business-facing
transport-neutral
```

Today may live:

```text
Features/{Producer}/Public/
```

Future may be packaged into:

```text
{Producer}.Contracts.Semantic
```

Exact project name is not fixed.

---

### BOUND-CONTRACT-002 — Integration contract

Purpose:

```text
durable asynchronous public fact
```

Example:

```text
BoardItemChangedV1
```

Future may be packaged:

```text
{Producer}.Contracts.Integration
```

---

### BOUND-CONTRACT-003 — Transport contract

Purpose:

```text
wire schema
```

Examples:

```text
.proto
generated gRPC request/response
HTTP transport DTO
OpenAPI transport schema
```

Future may be packaged:

```text
{Producer}.Contracts.Transport
```

---

### BOUND-CONTRACT-004 — Contract categories cannot collapse

Forbidden:

```text
generated protobuf DTO
used directly as Application semantic fact
```

Forbidden:

```text
Domain Event class
used directly as external wire contract
```

Forbidden:

```text
REST DTO
becomes Domain model
```

Packaging may change during extraction.

Semantic meaning must remain stable.

---

## 30. Future remote adapter

### BOUND-REMOTE-001 — Replace runtime adapter, not business handler

Today:

```text
IWorkEntitlementPort
→ BillingEntitlementInProcessAdapter
```

Future:

```text
IWorkEntitlementPort
→ BillingEntitlementGrpcAdapter
```

Business handler remains:

```text
CreateBoardHandler
→ IWorkEntitlementPort
```

---

### BOUND-REMOTE-002 — Remote adapter responsibilities

May own:

```text
client selection
request mapping
deadline
safe retry
service identity
scope propagation
correlation/tracing
transport response mapping
transport error translation
```

Must not own:

```text
Billing plan rules
Work capability semantics
Work business fallback
```

---

### BOUND-REMOTE-003 — Remote inbound endpoint is adapter into producer Application

```text
gRPC endpoint
→ producer Application semantic use case
→ producer Domain/persistence
```

Do not duplicate business logic in gRPC endpoint.

---

## 31. Service extraction stages

### BOUND-EXT-001 — Boundary must be healthy before extraction

No service extraction should be used as a technique to discover/fix semantic ownership.

Required first:

```text
Public semantic boundary
owned persistence
no foreign Domain model dependency
no foreign Internal request dependency
explicit failure/consistency behavior
```

---

### BOUND-EXT-002 — Exactly one authoritative writer

At every point:

```text
one authoritative state
→ exactly one active writer
```

Forbidden temporary state:

```text
old monolith writes Billing
+
new Billing Service writes same Billing state
```

---

### BOUND-EXT-003 — Safe runtime introduction

Mode A:

```text
new service shadow/read-only
old runtime remains sole writer
```

Mode B:

```text
explicit traffic cutover
old writer disabled
new service sole writer
```

---

### BOUND-EXT-004 — Shared physical DB may remain temporarily

Safe progression:

```text
E0 semantic boundary healthy
E1 new runtime introduced
E2 exactly-one-writer enforced
E3 foreign reads/joins removed
E4 physical data moved only if justified
```

Do not split DB first.

---

### BOUND-EXT-005 — Extraction requires operational evidence

Valid drivers:

```text
independent scaling
runtime specialization
provider isolation
security isolation
SLO/reliability isolation
deployment cadence
cost isolation
data residency
```

Invalid drivers:

```text
BC exists
folder is large
microservices look mature
team wants service
```

---

## 32. Migration classification for existing source

Every existing pattern encountered during implementation is classified:

```text
TARGET
ACCEPTED-CURRENT
MIGRATE-ON-TOUCH
BOUNDARY-DEBT
STOP
```

---

### 32.1 TARGET

Already matches this specification.

Preserve unless feature change requires evolution.

---

### 32.2 ACCEPTED-CURRENT

Current layout/mechanism is valid but not the preferred new folder shape.

Do not refactor solely for aesthetics.

Example:

```text
current context-specific Infrastructure adapter location
```

may remain if ownership is correct.

---

### 32.3 MIGRATE-ON-TOUCH

Known legacy shape that should migrate when feature is materially changed.

Examples:

```text
legacy Application command/query placement
Common semantic coupling
cross-context consumer vocabulary that leaks producer details
```

Migration must stay within affected slice unless broader cleanup is explicitly scheduled.

---

### 32.4 BOUNDARY-DEBT

Current violation not immediately blocking unrelated features.

Record concrete:

```text
file/type
violated Rule ID
consumer
producer
owner
removal trigger
```

---

### 32.5 STOP

Implementation cannot safely proceed without architecture decision.

Examples listed in Section 36.

---

## 33. Risk classification

Use:

```text
R0 — BLOCKER
R1 — HIGH
R2 — MIGRATE-WHEN-TOUCHED
R3 — ACCEPTED-CURRENT
```

Do not use `P0/P1/...` because backend roadmap already owns `P*` priority notation.

---

## 34. Use-Case Boundary Card

Every material cross-context use case must be captured in team execution/plan.

Do not create a file per card by default.

Template:

```text
UseCase:
OwningBC:
WorkflowOwner:

MutationAuthorities:
  - State:
    OwnerBC:

OwnedAggregates:
OwnedPersistence:

ForeignDependencies:
  - ProviderBC:
    SemanticNeed:
    Mechanism:
    ProducerPublicContract:
    ConsumerPort:
    ACL:
    RuntimeAdapter:
    Authority:
    Freshness:
    RaceTolerance:
    BusinessFailures:
    TechnicalFailurePolicy:
    Idempotency:
    CurrentTopology:
    FutureRemoteImpact:

TransactionBoundary:
Concurrency:
Idempotency:

Authorization:
Entitlement:

EventsProduced:
EventsConsumed:

ResourceReferences:

Projection:
ProcessManager:

MigrationClassification:
KnownBoundaryDebt:
```

---

## 35. Coding Agent file-placement decision tree

Before creating a new class:

```text
Does it own business invariant/state?
→ Domain/{Context}/{Module}

Does it implement a command/query use case?
→ Application/Features/{Context}/{Module}/{Commands|Queries}/{UseCase}

Is it owned persistence abstraction?
→ Application/Features/{Context}/Abstractions

Is it a consumer-owned external semantic need?
→ Application/Features/{Consumer}/Ports

Is it producer-owned stable cross-context semantic contract?
→ Application/Features/{Producer}/Public

Is it pure producer→consumer semantic translation?
→ Application/Features/{Consumer}/CrossContext/{Producer}

Is it runtime/topology/transport/persistence implementation?
→ Infrastructure

Is it reusable messaging/delivery runtime mechanism?
→ Platform

Is it HTTP/OpenAPI/binding/composition transport?
→ API

Is it a long-running business workflow?
→ Application/Features/{WorkflowOwner}/Processes

Is it consumer-owned derived state contract/model?
→ Application/Features/{Consumer}/Projections

None match?
→ STOP and classify ownership before inventing folder
```

---

## 36. Mandatory STOP conditions

Stop only the affected slice when implementation appears to require:

```text
foreign DbContext injection
foreign repository access
foreign Domain aggregate/entity/value-object dependency
foreign Internal namespace dependency
cross-context internal MediatR request
cross-context ORM navigation
cross-context cascade
shared mutable business model
two mutation authorities for same state
one transaction spanning multiple BC mutation authorities
hard-coded Billing plan/tier vocabulary in consumer product logic
hard-coded Governance role/private permission vocabulary in consumer product logic
provider SDK type in Domain/Application
HttpClient/gRPC generated client in Domain/Application
BFF/API coordinating business mutation
producer Integration Event instructing consumer-specific action
normal synchronous chain A → B → C → D without explicit latency/failure design
dual authoritative writer during extraction
unclear source of truth
unclear workflow owner
unclear transaction owner
```

STOP means:

```text
resolve boundary design
```

not:

```text
freeze entire backend roadmap
```

---

## 37. Architecture enforcement target mapping

The detailed implementation belongs to TESTS, but Coding Agents must understand the intended coverage.

```text
ARCH-BC-001
→ foreign persistence dependency

ARCH-BC-002
→ foreign Domain model dependency

ARCH-BC-003
→ producer Internal/MediatR dependency

ARCH-BC-004
→ cross-context EF navigation/cascade

ARCH-BC-005
→ Public contract purity

ARCH-BC-006
→ Application transport/provider purity

ARCH-BC-007
→ Integration Event ownership/version semantics

ARCH-BC-008
→ Common semantic leakage

ARCH-BC-009
→ optional machine-readable dependency catalog
```

Existing `DbContextBoundaryArchitectureTests` is only partial evidence for `ARCH-BC-001`.

Do not claim full enforcement until TESTS exit gates are met.

---

## 38. Reference use case — CreateBoard

### Ownership

```text
Owning BC
→ WorkManagement

Mutation authority
→ WorkManagement

Owned persistence
→ IWorkManagementDbContext
```

Dependencies:

```text
Workspaces
→ workspace scope/active fact

Governance
→ authorization decision

Billing
→ Work capability decision
```

Target:

```text
CreateBoardHandler
├── IWorkspaceFacts
├── IWorkAuthorizationPort
├── IWorkEntitlementPort
└── IWorkManagementDbContext
```

Workspace may be direct Producer.Public:

```text
IWorkspaceFacts
→ Workspaces implementation
```

Governance/Billing may require Consumer Ports because consumer semantics differ.

Mutation:

```text
Board.Create(...)
→ Work local transaction
→ Work-owned outbox enrollment
→ commit
```

Forbidden:

```text
query workspace table directly
query billing table directly
load Workspace aggregate
hard-code Plan.Pro
hard-code Governance role
```

---

## 39. Reference use case — Automation updates BoardItem

```text
Automation owns:
rule evaluation
execution lifecycle
reaction semantics

WorkManagement owns:
BoardItem mutation
```

Target:

```text
Automation handler/process
→ IWorkActionPort
→ WorkActionInProcessAdapter
→ WorkManagement.Public/IWorkActions
→ Work internal use case
→ Work Domain
→ Work transaction
```

Forbidden:

```text
Automation → IWorkManagementDbContext
Automation → Work internal UpdateBoardItemCommand via MediatR
```

Future:

```text
IWorkActionPort
→ WorkGrpcAdapter
→ network
→ Work internal RPC endpoint
→ same Work Application semantics
```

Automation business logic does not change.

---

## 40. Reference use case — Work event triggers Automation

```text
Work mutation
→ BoardItemChangedDomainEvent
→ BoardItemChangedV1
→ outbox
→ commit
→ delivery
→ Automation consumer
→ Automation reaction
```

Work emits:

```text
BoardItemChangedV1
```

not:

```text
RunMatchingAutomationsV1
```

Automation owns trigger interpretation.

---

## 41. Reference use case — Collaboration comment target

```text
Collaboration owns Comment
Work/Documents own target resource
```

Store:

```text
ResourceRef
```

Validate target through approved semantic contract/port when required.

No cross-context navigation.

No cross-context cascade.

Lifecycle behavior such as target deletion must be explicit.

---

## 42. Reference use case — Entitlement migration

Current hotspot:

```text
Application.Common.Entitlements
```

Target consumer example:

```text
WorkManagement
→ IWorkEntitlementPort
→ Billing semantic contract
```

Migration order:

```text
1. Do not add new tier-name checks.
2. For touched Work feature, define Work capability.
3. Reuse Billing semantic decision/grant.
4. Add consumer Port only if semantic translation exists.
5. Introduce pure ACL where required.
6. Implement runtime adapter.
7. Migrate touched handler.
8. Preserve old checker for untouched consumers.
9. Burn down remaining consumers later.
```

No big-bang entitlement rewrite.

---

## 43. Folder creation rules

Coding Agent MUST NOT create a new folder because:

```text
diagram showed it
future architecture may need it
another context has it
symmetry looks cleaner
```

Create folder only when:

```text
at least one real type belongs there
ownership is established
placement follows this specification
```

Do not create:

```text
Public/.gitkeep
Ports/.gitkeep
CrossContext/.gitkeep
Processes/.gitkeep
```

as architecture scaffolding.

---

## 44. Rename/move rules

Do not mass-move existing source to match target structure unless:

```text
move is required for active boundary correction
or
dedicated cleanup milestone explicitly authorizes it
```

A feature PR should optimize for:

```text
smallest complete semantic change
+
required boundary correction
```

not directory perfection.

---

## 45. New-project rule

Any proposed new production project is outside normal execution.

Required before code creation:

```text
accepted architecture decision
operational evidence
contract inventory
data ownership/cutover
deployment topology
security model
rollback/forward plan
```

Without that:

```text
remain inside five-project modular monolith
```

---

## 46. Definition of Ready for cross-context implementation

A material cross-context slice is ready when:

```text
Owning BC known
workflow owner known if applicable
every mutation authority known
foreign semantic needs listed
interaction mechanism selected
Producer.Public vs Consumer Port decided
ACL need decided
runtime adapter need decided
business failures known
technical dependency policy known
transaction boundary known
authorization/tenant scope known
idempotency/concurrency considered
no unresolved STOP condition
```

---

## 47. Definition of Done for cross-context implementation

Done requires:

```text
business behavior correct
Domain ownership preserved
Application orchestration explicit
no foreign persistence
no foreign Domain model
no producer Internal dependency
Public contract semantic and narrow
Consumer Port semantic if used
ACL pure if used
adapter mechanism-only if used
transaction authority clear
business/technical failures separated
events facts not consumer instructions
idempotency proven where required
architecture gates pass
tests pass
migration debt explicit
future remote replacement does not require business-handler redesign
```

---

## 48. Coding Agent non-negotiable reasoning sequence

Before modifying code, the agent MUST internally resolve in this order:

```text
1. Product semantic owner
2. Bounded Context
3. Workflow owner
4. Mutation authority
5. Aggregate/resource
6. Account/Workspace/resource scope
7. Authorization action/policy boundary
8. Foreign semantic facts
9. Cross-context mechanism
10. Public Contract vs Consumer Port
11. ACL need
12. Runtime adapter need
13. Transaction
14. Business failure
15. Technical dependency policy
16. Concurrency
17. Idempotency
18. Event/projection/process impact
19. Persistence/migration
20. API/runtime impact
21. Tests/gates
22. Migration classification
```

The agent MUST NOT choose folders/classes before materially resolving items 1–10.

---

## 49. Final architecture invariant

The backend architecture intentionally optimizes for:

```text
semantic cohesion inside a bounded context
+
replaceability across bounded contexts
```

The desired property is:

```text
TODAY

Consumer Application
→ semantic contract/port
→ optional ACL
→ in-process runtime implementation
→ Producer Application


FUTURE

Consumer Application
→ same semantic contract/port
→ same semantic ACL
→ remote runtime adapter
→ HTTP/gRPC/broker
→ Producer inbound adapter
→ same Producer Application semantics
```

The runtime changes.

The business ownership does not.

The consumer business use case does not become transport-aware.

That is the standard by which every boundary decision in this execution package is judged.
