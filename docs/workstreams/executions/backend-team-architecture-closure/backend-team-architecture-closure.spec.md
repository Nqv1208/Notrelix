---
document_id: EXEC-BACKEND-TEAM-ARCHITECTURE-CLOSURE-SPEC
title: Backend Team Operating Architecture Bootstrap & Closure — Execution Specification
version: 2.6
status: execution-ready
supersedes: version 2.5
repository: Nqv1208/Notrelix
branch: architecture/backend-boundary-execution
audit_snapshot:
  commit: b94312b3e10e2212f18440003e775d415aa1b5d7
  pr: 113
audit_revision: semantic-authority-runtime-closure-v2.6-interaction-normalized-source-pattern-public-capability-topology
source_refresh:
  commit: fc3d98d6ce2c72d9e0fab3c76df0b032891b49eb
  scope: IA current-source drift + M3 blocker validation + source-operating-pattern audit; full 47-flow source revalidation remains PRE-M4
scope:
  - backend semantic architecture closure
  - team reference-flow bootstrap
  - cross-context executable examples
  - architecture blocker freeze
  - broker/event/platform boundary
  - structural topology
  - architecture fitness functions
  - team development guidance
  - cross-process interaction operating model
  - future runtime substitution readiness
depends_on:
  - backend-boundaries
  - backend-structural-topology
canonical_context_authority:
  - docs/architecture/bounded-context-map.md
---

# Backend Team Operating Architecture Bootstrap & Closure — Execution Specification

## 0. Revision notice

v2.6 retains the bootstrap requirement and source-normalization rule. The original execution assumption was:

```text
no real production flow
→ DEFER-NO-REAL-FLOW
```

with the required team-bootstrap model:

```text
no real production flow
→ implement a bounded, executable reference flow
→ use it to freeze architecture and source topology
→ future product features copy/adapt the proven pattern
```

This is intentional.

## 0.1 Source-normalized reference rule — retained from v2.1

The v2 audit found that several reference capabilities already exist in source
under older or non-canonical locations. Therefore the execution MUST NOT assume
that a missing canonical folder means a missing capability.

Every mandatory reference receives exactly one disposition before coding:

```text
REUSE-EXISTING
HARDEN-EXISTING
MIGRATE-EXISTING
IMPLEMENT-MISSING
FREEZE-EXACT-DEBT
BLOCKED-DECISION
```

The order is mandatory:

```text
inventory current semantic capability
→ reuse if already correct
→ harden if correct but weakly enforced
→ migrate if semantics are correct but ownership/location is wrong
→ implement new source only if the capability is genuinely absent
```

`IMPLEMENT-MISSING` is not the default.

This prevents the bootstrap package from creating a second event, second
authorization engine, second ResourceRef, second process state model, or second
provider abstraction merely because the canonical v2 taxonomy uses a cleaner
name.

## 0.1A Producer Public capability-first topology — normative

`Features/<BC>/Public` is the producer-owned **published semantic surface** for
other bounded contexts. It is not a second CQRS layer and MUST NOT be organized
primarily by technical artifact type.

Canonical topology:

```text
Application/Features/<Producer>/Public/<PublishedCapability>/
  [<SemanticSubCapability>/]
  <public contract files>
```

The first directory below `Public` MUST express producer-owned business
capability/language. New top-level technical buckets are non-canonical:

```text
Public/Commands
Public/Queries
Public/Facts
Public/Actions
Public/Contracts
Public/DTOs
Public/Services
Public/Common
```

This rule does **not** prohibit internal CQRS structure such as
`Features/<BC>/<Module>/Commands|Queries/<UseCase>/`. Contract type names may
still communicate interaction semantics (`...Facts`, `...Actions`, `...Fact`,
`...Request`, `...Result`) without turning those semantics into mandatory
top-level folders.

Existing reviewed technical-bucket paths in bounded contexts not yet normalized
remain exact migration debt only. They are no-growth precedent and MUST shrink
when the owning milestone touches that Public surface. No big-bang cross-BC
refactor is implied.

## 0.2 Coding-agent autonomy contract — v2.6

For the audited PR #113 snapshot `b94312b3e10e2212f18440003e775d415aa1b5d7`, mandatory architecture references are
**pinned**. A coding agent does not choose an alternative reference merely
because another flow looks cleaner.

Rule:

```text
pinned source exists and semantics still match
→ implement exactly the pinned reference

pinned source moved/renamed but semantic identity is provably unchanged
→ follow the rename/move and record SOURCE-RELOCATION

pinned source is absent or semantics materially changed
→ STOP-SOURCE-DRIFT
→ re-audit the affected reference
→ DO NOT substitute another feature/aggregate/metric/provider on your own
```

Words such as `prefer`, `choose`, `select`, `if appropriate`, and
`or equivalent` are non-authoritative for mandatory references unless a
deterministic decision table immediately follows them.

### 0.2.1 Pinned reference implementation map

| Reference | Pinned implementation at audited b94312b3 candidate |
|---|---|
| IA-REF-001 | existing `IIdentityUserFacts` |
| IA-REF-002 | existing `IAccountMembershipFacts` |
| IA-REF-003 | move `IAccountMembershipProvisioner` responsibility behind Accounts Public target action |
| WG-REF-001 | existing `AcceptInvitationCommand` |
| WG-REF-002 | existing canonical authorization pipeline/Common Security path; no second evaluator stack |
| WG-REF-003 | existing `WorkspaceMemberAddedIntegrationEvent`; existing Collaboration activity projection is the reference consumer |
| WM-REF-001 | existing `CreateBoardInWorkspace` pipeline-first slice |
| WM-REF-002 | existing `IWorkManagementCollaborationReadPort` + adapter |
| WM-REF-003 | existing `MoveBoardItemCommand` semantic exposed as WorkManagement Public target action |
| WM-REF-004 | migrate member-assigned outward fact to WorkManagement ownership; retain existing Work event catalog |
| DC-REF-001/002 | existing SharedKernel `ResourceRef` + `CreateCommentCommand.ForBoardItem` |
| DC-REF-003 | existing `IResourceScopedRequest`/authorization-resource path for CreateComment; no extra metadata Port unless source proves a missing fact |
| DC-REF-004 | classify existing Notifications implementation under Collaboration; no new BC |
| AI-REF-001 | migrated WorkManagement member-assigned event → existing Automation consumer/evaluator |
| AI-REF-002 | Automation `MoveItem` action → consumer Port → WorkManagement Public Move action |
| AI-REF-003 | existing `AutomationExecution` as sole durable process state |
| AI-REF-004 | normalized N8n provider path; Automation business process delegates through Integrations-owned semantic boundary |
| BI-REF-001 | replace Common entitlement authority with Billing-owned capability facts |
| BI-REF-002 | `CreateAutomationRuleCommand` is the mandatory first real Billing-capability consumer |
| BI-REF-003 | audited b94312b3 external Billing provider = NOT-REQUIRED; do not fabricate Stripe |
| AR-REF-001 | Work item current-placement projection driven by existing Work events and rebuilt from producer-owned Work snapshot facts |
| PF pack | existing outbox/envelope/inbox/dedup/retry mechanism |

This table is a build instruction, not an example list.

### 0.2.2 Current transaction decision is pinned

For `AcceptInvitation` at audited `b94312b3e10e2212f18440003e775d415aa1b5d7`, the execution does **not** ask the coding agent
to redesign product semantics.

Canonical v2.6 choice:

```text
TAC-FRZ-001 = B
BOUND-TX-002 = temporary reviewed shared-transaction exception
```

Reason:

```text
current source intentionally relies on one request transaction
+
current rollback behavior is already tested/evidence-bearing
+
there is no canonical product evidence in this audit that partial
Account-membership success is safe to expose independently
```

Therefore the agent must:

```text
preserve current atomic visibility/rollback
→ replace foreign private Accounts abstraction with Accounts Public action
→ record BOUND-TX-002 exactly
→ add extraction/removal trigger
```

The agent must not choose A or C during this execution.

### 0.2.3 Exact reference choices

#### Work target mutation

```text
MoveBoardItemCommand
```

is the target semantic because:

```text
existing product operation
IIdempotentRequest
resource-scoped authorization
clear WorkManagement mutation authority
AutomationActionType already contains MoveItem
```

#### Collaboration local mutation

```text
CreateCommentCommand.ForBoardItem(...)
```

is the target semantic.

It already demonstrates:

```text
Collaboration-owned mutation
SharedKernel ResourceRef
foreign Work item identity
resource-scoped authorization path
no Work aggregate navigation
```

#### Billing capability consumer

```text
CreateAutomationRuleCommand
```

must become the first real consumer of Billing capability:

```text
capability = AUTOMATION_RULE
amount = 1
scope = current Account + requested Workspace
```

No Governance consumer is selected for the bootstrap slice.

#### Analytics projection

Pinned semantic:

```text
WorkspaceWorkItemPlacementProjection
```

Purpose:

```text
Analytics-owned read state of current Work item placement
```

Minimum fields:

```text
WorkspaceId
ItemId
BoardId
GroupId
SourceVersion/Revision where available
LastOccurredAt
```

Live update sources:

```text
BoardItemMovedIntegrationEvent
BoardItemArchivedIntegrationEvent where removal/archival affects projection
BoardItemCreatedIntegrationEvent may trigger producer snapshot fetch because
its current event payload does not carry GroupId
```

Rebuild source:

```text
WorkManagement.Public projection-source query
→ minimal current item placement snapshot
```

Rebuild is **not** outbox replay because outbox is not an event store.

Existing `ReportingSnapshot` is not reused for this reference; its current
`LegacyGap` remains exact/no-growth with its own migration trigger.

## 0.2.4 Historical runtime-closure input — NON-NORMATIVE

The HEAD audit after v2.3 exposed implementation classes that can still pass
CI while violating the intended reference semantics.

These are mandatory closure requirements.

### A. N8n technical retry must match the real consumer transaction

Production Integration Event consumers run inside the current
`DeduplicationConsumeFilter` transaction, and that transaction commits only
when the consumer returns successfully.

Therefore this shape is invalid:

```text
use case mutates retry evidence
→ SaveChanges
→ consumer throws retryable exception
→ outer dedup transaction rolls back
```

because the durable retry evidence is rolled back with the delivery attempt.

For the bootstrap N8n reference, v2.4 pins:

```text
RETRY MODEL = delivery-owned retry
```

Meaning:

```text
transient provider/network failure
→ no durable Automation retry-attempt mutation is required
→ consumer throws retryable technical exception
→ Platform/MassTransit owns redelivery/retry
→ failed delivery transaction rolls back
```

`AutomationExecution.RecordRetryableDispatchFailure(...)` MUST NOT be relied on
by this technical delivery-retry path.

Success and terminal semantic failure remain Automation-owned durable outcomes
and commit when the consumer completes successfully.

Unknown provider outcome is not blindly retried. Without an explicit
provider reconciliation/idempotency mechanism:

```text
UnknownOutcome
→ durable terminal/manual-reconciliation Automation outcome
→ consumer returns successfully
→ transaction commits
```

### B. Automation → Work target action must enforce all public identity fields

The WorkManagement Public action carries:

```text
OperationId
WorkspaceId
ExecutorUserId
```

Those values MUST be consumed by the producer implementation.

Required:

```text
WorkspaceId
→ loaded item must belong to requested Workspace

ExecutorUserId
→ WorkManagement/Governance authorization for MoveItem must succeed

OperationId
→ target-owned mutation deduplication/idempotency
```

Forbidden:

```text
Public contract carries identity/scope/idempotency fields
→ WorkItemActions/MoveBoardItemUseCase ignores them
```

Same `OperationId` + same semantic request:

```text
one logical mutation
```

Same `OperationId` + conflicting request:

```text
deterministic conflict
```

### C. Automation → Work must exist as a production process path

A test that directly invokes `IWorkActionPort` does not complete AI-REF-002.

Required production flow:

```text
AutomationExecution
→ Automation Application action executor
→ AutomationActionType.MoveItem
→ IWorkActionPort.MoveItemAsync
→ WorkManagement.Public IWorkItemActions
→ producer-local MoveBoardItem use case
```

The Automation executor owns:

```text
action configuration validation
Automation principal construction
ExecutionId → OperationId
target business-failure mapping
technical-failure distinction
AutomationExecution outcome progression
```

It must be reachable from the real Automation execution path.

### D. Billing capability must honor entitlement scope and real usage lifecycle

`IBillingCapabilityFacts` receives:

```text
AccountId
WorkspaceId
CapabilityCode
RequestedAmount
```

The producer must honor entitlement target scope.

Account-scoped entitlement:

```text
AccountId matches
```

Workspace-scoped entitlement:

```text
AccountId matches
AND TargetWorkspaceId == requested WorkspaceId
```

A Workspace-A entitlement cannot authorize Workspace B.

For the pinned capability:

```text
AUTOMATION_RULE
```

the authoritative usage lifecycle is:

```text
successful AutomationRule creation
→ +1 Billing usage

capacity-releasing permanent rule deletion/retirement
→ -1 Billing usage
```

Disable/archive states that product semantics say still consume capacity do not
release usage.

Usage writes must be idempotent and tied to a source resource/action identity
so retries cannot double-count.

The consistency model between AutomationRule mutation and Billing usage
mutation must be explicit. A second hidden shared-transaction assumption is
forbidden.

### E. Analytics producer Public implementation must be producer-owned

Required:

```text
WorkManagement.Public IWorkItemProjectionSource
        ↓
WorkManagement Application implementation
        ↓
IWorkManagementDbContext
```

Analytics runtime may delegate through an adapter to that producer-owned Public
implementation.

Forbidden:

```text
Infrastructure/CrossContext/Analytics/WorkManagement
→ directly inject IWorkManagementDbContext
→ implement producer Public contract itself
```

The Analytics consumer must not own direct Work persistence reads.

## 0.2.5 False-green test rule

A certification test must execute the production mechanism that owns the
guarantee.

Required examples:

```text
N8n retry
→ real DeduplicationConsumeFilter transaction

Automation→Work
→ real Automation action executor + target Public action

Billing
→ rule lifecycle + capability/usage lifecycle

Analytics
→ producer Public implementation + consumer adapter
```

Manual `SaveChangesAsync()` or direct construction of a lower-level adapter is
supplementary evidence only when production runtime wraps that code
differently.

## 0.3 Execution-authority precedence

This package is an **architecture-bootstrap and cross-team closure overlay**.

It does not replace an existing team execution package.

```text
existing team execution package
    owns full team/domain/feature backlog and detailed product rollout

backend-team-architecture-closure
    owns mandatory architecture reference slices
    cross-team mechanism closure
    freeze blockers
    source normalization required by those references
    cross-pack integration proof
```

For example, existing `identity-accounts` and `workspace-governance` executions
remain the detailed team authorities where they already define source-first
feature/domain work. TAC references consume their decisions and add only the
architecture proof needed for the shared backend model.

The purpose of this execution is not merely to clean existing code.

The purpose is to ensure every backend team can start feature development with
an already-executable architectural model for:

- local vertical slices;
- authoritative reads;
- cross-context mutation;
- Public contracts;
- consumer Ports;
- ACLs;
- runtime adapters;
- Integration Events;
- Broker/outbox delivery;
- local projections;
- Process Managers;
- stable ResourceRef patterns;
- provider integration;
- transaction boundaries;
- idempotency;
- failure semantics;
- architecture tests.

Therefore:

```text
REFERENCE-IMPLEMENTATION-REQUIRED
```

is now a first-class execution state.

---

## 0.2.6 Historical Team Operating Architecture input — NON-NORMATIVE

The earlier runtime audit closed important execution gaps, but a broader team
audit found one remaining model defect:

```text
one team pack
→ one or several representative reference flows
```

does **not** guarantee that every bounded context owned by that team has an
executable architecture baseline.

That is insufficient for the requested operating model.

The Team Operating Architecture revision changed the unit of completeness to:

```text
Team
→ owned Bounded Context
→ Real Flow Catalog
→ Flow Card
→ canonical mechanism(s)
→ production runtime
→ tests/gates/evidence
```

A flow in one bounded context can no longer satisfy the local-baseline
requirement of another bounded context merely because the same team owns both.

Examples:

```text
CreateComment.ForBoardItem
≠ Documents local-flow proof

N8n provider adapter
≠ Integrations connection-lifecycle proof

AcceptInvitation
≠ Governance local mutation proof

Billing capability query
≠ Billing usage-mutation proof
```

Those references remain historical inputs to the v2.6 Team Operating Architecture Pack.
complete Team Operating Architecture Pack.

## 0.2.7 Semantic-authority and runtime-closure correction — v2.6

The Team/BC Real Flow model is retained and strengthened by v2.6.

The deeper audit found several places where a coding agent could implement the
correct architecture shape while selecting the wrong semantic authority.

v2.6 closes these distinctions:

```text
IntegrationConnection
≠ CalendarIntegration

Domain SecretRef property
≠ persisted secret-reference authority

outbound WebhookDelivery
≠ inbound provider-delivery intake

Workspace-scoped business event with AccountId == null
≠ valid tenant envelope

mentioned user
≠ mention actor

feature capacity/quota
≠ billable UsageMetric by default

idempotent quota mutation
≠ concurrency-safe hard quota

Common namespace/name purity
≠ Common public-signature purity

cross-BC owner Public action
≠ authenticated local-admin copy-model
```

### Normative authority

Only v2.6/current-candidate rules are normative.

Older pre-v2.6 material, where retained for provenance, is:

```text
HISTORICAL — NON-NORMATIVE
```

and cannot override current Flow Cards, freezes, tests or exact-SHA source.

### New mandatory freezes

```text
TAC-FRZ-017 Common public-signature semantic purity
TAC-FRZ-018 scoped Integration Event tenant envelope
TAC-FRZ-019 Calendar connection/binding/secret lifecycle
TAC-FRZ-020 Billing capacity semantics + hard-quota concurrency
```

## 0.3 Interaction Architecture normalization — current candidate

This revision closes the remaining operating-model ambiguity between:

```text
semantic bounded-context boundary
current same-process runtime binding
future different-process/service binding
```

It does **not** change the accepted rule:

```text
BC != project != service
```

and it does not introduce microservice extraction into this execution.

The current production topology remains the existing modular monolith. The new
requirement is that every real cross-BC interaction is explicit enough that a
team can answer, without inventing architecture:

```text
what business need crosses the boundary?
which TAC-XC-A..F mechanism owns that edge?
who owns the contract / Port / ACL / mutation / process / projection?
how is the edge bound in-process today?
what is the approved future distributed substitution shape?
what consistency and transaction semantics apply?
who owns retry, idempotency, unknown outcome and compensation?
is physical extraction currently blocked by an accepted shared transaction?
```

A Real Flow may contain multiple interaction edges. Therefore:

```text
Real Flow
!=
one cross-BC edge
```

Each actual edge is recorded under the Flow Card's `Interactions[]` field and
classified independently as exactly one of `TAC-XC-A..F`.

Supporting shapes such as Local Vertical Slice, ResourceRef, Composite Read,
BFF/read composition, provider Ports and Platform delivery mechanics do not
become seventh mechanisms.

This revision also normalizes the interpretation of `RemoteSubstitution`:

```text
legacy Flow Card summary only
```

The normative cross-process fields are now per-edge:

```text
RemoteReadiness
FutureDistributedBinding
ExtractionBlocker
RemovalTrigger
```

For later packs, detailed remote/failure semantics may remain
`DEFERRED-{MILESTONE}` until that pack's entry audit. The mechanism and semantic
owners may not remain unclassified.

---

# 1. Core objective

The execution target is:

```text
semantic ownership
        ↓
canonical interaction mechanism
        ↓
executable reference flow
        ↓
intentional source topology
        ↓
behavior + architecture tests
        ↓
team-copyable development pattern
        ↓
future runtime substitution without business redesign
```

A feature team should not have to invent architecture when implementing a new
feature.

After this execution, the team should be able to inspect source and answer:

```text
How do I build a local command?
How do I read an authoritative fact from another context?
How do I ask another context to mutate its state?
How do I consume an event?
How do I build a projection?
How do I model a long-running workflow?
How do I call an external provider?
Where does retry live?
Where does business failure live?
How do I preserve transaction ownership?
How is the same semantic edge bound in-process today?
What changes if the producer later runs in another process/service?
When is a clean boundary still extraction-blocked?
Where do tests go?
Which architecture gate will reject the wrong implementation?
```

The answer must exist in executable source, not only prose.

---

# 2. What “reference implementation” means

A reference implementation is:

> A minimal, executable, production-grade architectural slice whose purpose is
> to demonstrate an approved mechanism for future feature development.

It is not:

- pseudocode;
- empty interface;
- empty folder;
- `.gitkeep`;
- throw-only stub;
- fake HTTP service;
- fake microservice;
- unused DTO;
- interface without adapter/test;
- event without producer/consumer;
- projection without rebuild semantics;
- process without progression semantics.

A valid reference implementation has enough of the real chain to prove:

```text
ownership
contract
runtime mechanism
failure behavior
DI
tests
architecture enforcement
```

It may be:

```text
not externally routed yet
```

when the corresponding product endpoint is not ready.

But it must still:

```text
compile
execute in tests
resolve through DI when runtime-bound
obey the same architecture gates as product code
```

---

# 3. Reference implementation versus product commitment

This execution may introduce architectural seed flows before a user-facing
feature is shipped.

That does **not** mean the reference becomes a product commitment.

Use this distinction:

```text
Reference Architecture Commitment
    how a class of interaction is built

Product Commitment
    whether/when a user-facing capability ships
```

Example:

```text
Automation
→ consumes a Work Management fact
→ invokes a Work Management action
```

is already an accepted bounded-context relationship.

Implementing one reference path for that relationship freezes the architecture.

It does not commit the UI, pricing, trigger catalog, or complete automation
product roadmap.

---

# 4. Reference implementation semantic safety

Reference code MUST NOT invent a large product model merely to demonstrate a
pattern.

Allowed:

- use already accepted context ownership;
- use already accepted relationship semantics;
- define the smallest semantic contract needed to demonstrate the mechanism;
- create deterministic reference state where required;
- keep a reference non-user-facing until product behavior is finalized;
- explicitly label the slice as architecture bootstrap in tests/evidence.

Forbidden:

- invent a new bounded context;
- invent a new business lifecycle unrelated to product authority;
- invent a pricing plan;
- invent a permission hierarchy;
- invent a workflow whose ownership is unknown;
- create placeholder aggregate only because a folder is missing;
- expose reference endpoints to users merely to make code reachable;
- use “Reference” as a dumping ground.

The key rule is:

```text
implement architecture proactively
without inventing ownership retroactively
```

---

# 5. Canonical business contexts

System architecture recognizes eleven business bounded contexts:

```text
Accounts
Identity
Workspaces
Governance
Work Management
Documents
Collaboration
Automation
Integrations
Billing
Analytics / Reporting
```

These are semantic ownership boundaries.

They are not generated from source folders.

A source feature folder such as:

```text
Features/Notifications
```

does not automatically define a twelfth bounded context.

Because canonical architecture assigns notification semantics to Collaboration,
the execution must classify such implementation modules against the accepted
context map rather than treating folder names as semantic authority.

---

# 6. Production project topology

This execution preserves the current five production projects:

```text
Notrelix.Domain
Notrelix.Application
Notrelix.Infrastructure
Notrelix.Platform
Notrelix.API
```

It MUST NOT create:

```text
Notrelix.Identity
Notrelix.Workspaces
Notrelix.Billing
...
```

merely to represent bounded contexts.

The current model remains a modular monolith.

---

# 7. Semantic, code, runtime, and transport topology remain distinct

```text
Semantic topology
≠ Code topology
≠ Runtime topology
≠ Transport topology
```

Therefore:

```text
11 BCs
≠
11 projects
≠
11 services
≠
11 databases
≠
11 queues
```

Reference implementation code MUST preserve future extraction optionality.

It MUST NOT simulate distribution today.

---

# 8. Core architectural hierarchy

Every implementation decision follows:

```text
Business capability
↓
Bounded Context
↓
Semantic owner
↓
Use case
↓
Cross-context mechanism
↓
Application contract
↓
Runtime adapter
↓
Persistence/provider/transport
↓
Deployable
```

Do not reverse this hierarchy.

Wrong:

```text
we may use gRPC later
→ create service interface now
→ force business code around remote DTO
```

Correct:

```text
consumer needs an authoritative capability
→ define semantic boundary
→ use in-process implementation today
→ replace runtime adapter later if necessary
```

---

# 9. Ownership invariants

## TAC-INV-001 — One authoritative state, one semantic owner

The same business fact cannot have two mutation authorities.

## TAC-INV-002 — One authoritative mutation, one target owner

A consumer may request mutation.

It does not become mutation owner.

## TAC-INV-003 — Workflow owner is distinct from participant mutation owners

A Process Manager may coordinate multiple BCs.

It does not directly mutate participant persistence.

## TAC-INV-004 — Shared DB does not merge ownership

A shared EF Core DbContext implementation can serve multiple context-local
abstractions.

It does not authorize direct foreign persistence use.

## TAC-INV-005 — Team ownership does not merge BC ownership

Same team:

```text
Identity + Accounts
```

still means two semantic contexts.

Same rule for:

```text
Workspaces + Governance
Documents + Collaboration
Automation + Integrations
```

---

# 10. Pipeline-first invariant

Before defining a cross-context dependency, ask whether the need is already
owned by request pipeline behavior.

Typical pipeline-owned concerns:

```text
authentication
request identity
workspace/account scope
standard permission enforcement
idempotency
transaction/data session
correlation
```

Do not build:

```text
IAuthenticationPort
IWorkspaceScopePort
IStandardPermissionPort
ITransactionPort
```

merely to make the architecture look layered.

A reference flow must demonstrate pipeline-first behavior.

`CreateBoardInWorkspace` is the primary reference.

---

# 11. Canonical Application topology

The taxonomy remains:

```text
Features/{Context}/
├── Abstractions/
├── Public/
│   ├── Facts/
│   ├── Queries/
│   ├── Commands/
│   ├── Events/
│   └── References/
├── Ports/
│   └── {ProducerOrPurpose}/
├── CrossContext/
│   └── {ProducerOrPurpose}/
├── Processes/
│   └── {Process}/
├── Projections/
│   └── {Projection}/
└── {Module}/
    ├── Commands/{UseCase}/
    ├── Queries/{UseCase}/
    ├── DTOs/
    ├── ReadModels/
    ├── Mapping/
    ├── Permissions/
    └── Services/
```

v2.6 retains and tightens this policy:

The execution MAY create a canonical role folder before a user-facing feature
ships **only when it contains a complete executable reference implementation**.

Still forbidden:

```text
empty folder
marker file
empty interface
placeholder class
```

---

# 12. Abstractions

`Abstractions` contains inward context-local contracts.

Examples:

```text
IWorkManagementDbContext
IWorkspaceDbContext
IBillingDbContext
```

Other contexts MUST NOT use another context's Abstractions as their public API.

Reference flows must demonstrate this rule.

---

# 13. Public

`Public` is producer-owned.

It expresses stable producer semantics for cross-context consumption.

Allowed categories:

```text
Facts
Queries
Commands/Actions
Events
References
```

Public MUST NOT expose:

```text
aggregate/entity
DbContext
repository
producer internal handler
internal MediatR command/query
EF type
provider SDK
transport DTO
consumer-specific implementation object
```

Reference flows must create Public only where producer-owned semantics require
cross-context reuse.

---

# 14. Ports

`Ports` are consumer-owned semantic needs.

A Port is appropriate when:

- consumer language differs from producer language;
- runtime substitution matters;
- composite reads are required;
- ACL translation is required;
- target mutation needs a consumer-facing abstraction;
- multiple producers are composed.

A Port is not mandatory for every Public read.

Reference implementation packs must demonstrate both:

```text
Public Direct
and
Consumer Port + Adapter
```

A Port expresses a semantic capability, not a foreign repository facade.

Good shape:

```text
GetAccountAdmission(...)
MoveWorkItem(...)
ResolveCollaborationFacts(...)
```

Forbidden shape:

```text
GetForeignAggregate()
SaveForeignAggregate()
GetDbSet()
UpdateForeignEntity()
```

Port ownership is determined by the consumer use case:

```text
Consumer Application owns Port
→ runtime adapter implements Port
→ producer-owned Public/Application boundary owns producer semantics
```

A future remote adapter replaces the runtime binding, not the consumer business
handler contract.

---

# 15. CrossContext ACL

Application `CrossContext` contains consumer-owned, pure semantic translation.

ACL is optional. It exists only when producer and consumer vocabularies differ.

Request direction:

```text
Consumer semantic request
→ consumer ACL request mapping
→ producer Public semantic request
```

Response direction where needed:

```text
producer semantic result
→ consumer ACL response mapping
→ consumer semantic result
```

The pinned Automation → Work reference therefore teaches:

```text
Automation Application
→ IWorkActionPort
→ Automation-owned MoveItem ACL mapping
→ Infrastructure WorkItemActionAdapter
→ WorkManagement.Public action
```

The ACL may not contain:

```text
HttpClient
gRPC
MassTransit
EF Core
provider SDK
retry
timeout
transport serialization
authorization
target idempotency
producer business policy
```

These belong to runtime/Platform or the producer target Application as
appropriate.

This execution MUST implement at least one real executable ACL reference.

---

# 16. Infrastructure cross-context adapters

Canonical runtime location:

```text
Notrelix.Infrastructure/
└── CrossContext/
    └── {Consumer}/
        └── {ProducerOrPurpose}/
            ├── *InProcessAdapter.cs
            ├── *ProjectionAdapter.cs
            └── future *RemoteAdapter.cs
```

Reference adapters must:

- implement consumer-owned Ports where TAC-XC-B is selected;
- call producer Public or another producer-owned Application boundary;
- own runtime/transport translation only;
- translate technical failures without owning business policy;
- be replaceable without changing consumer business handler semantics.

An adapter MUST NOT become the place that:

```text
reads a foreign DbContext as a substitute for producer ownership
mutates foreign aggregates
performs producer authorization
owns target idempotency
recreates producer policy
```

Current legacy source that still does this is migration debt, not a copy-model.

Same-process binding:

```text
Consumer Handler
→ Consumer Port
→ optional consumer ACL mapping
→ InProcess Adapter
→ Producer.Public / producer-owned Application boundary
```

Future different-process binding:

```text
Consumer Handler
→ same Consumer Port
→ same consumer semantic mapping
→ Remote Adapter
→ transport
→ producer inbound adapter
→ Producer Application
```

No fake remote adapter is required before physical extraction.

---

# 17. Processes

`Processes` is a semantic role for workflow-owner long-running process
semantics.

v2.6 requires at least one executable Process/Process-Manager reference, with
Automation as the preferred owner because Automation owns durable execution
lifecycle and action progression.

However:

```text
semantic Process role
≠ mandatory /Processes folder
≠ mandatory second Process aggregate
```

Current `Automation/Executions` may satisfy the canonical Process role after it
is normalized and proven to own the workflow correctly.

Create/rehome a `Processes/` folder only if that improves the actual source
model without duplicating `AutomationExecution`.

Platform remains responsible for technical delivery/retry/timers.
# 18. Projections

`Projections` contains consumer-owned derived state.

v2.6 requires at least one executable projection reference.

Preferred owner:

```text
Analytics / Reporting
```

using a producer-owned Work Management fact/event.

The reference projection must define:

```text
source owner
event/version
scope
ordering/revision
idempotency
rebuild source
failure behavior
freshness semantics
```

It MUST NOT become Work Management source truth.

---

# 19. Integration Events

Reference execution MUST contain at least one complete producer→consumer event
chain.

Canonical chain:

```text
producer Domain mutation
→ Domain Event
→ explicit Integration Event mapping
→ producer-owned IntegrationEvent V1
→ outbox enrollment
→ producer transaction commit
→ Platform dispatcher
→ Broker
→ tenant restoration where scoped
→ Platform dedup claim
→ Infrastructure consumer inbound adapter
→ consumer Application use case
→ consumer-local transaction/projection/process
→ dedup success
```

An Integration Event means a committed producer fact.

Not:

```text
producer tells a particular consumer implementation what to do
```

A broker consumer is an inbound adapter. It should be conceptually equivalent
to an HTTP endpoint entering Application logic:

```text
Broker consumer
→ Application Command/UseCase
→ Handler/Application service
→ consumer-owned Domain/Persistence
```

It MUST NOT become a large Infrastructure business service or directly mutate
foreign persistence.

### 19.1 Outbox atomicity

Forbidden:

```text
SaveChanges
→ publish event
```

because producer state and outward fact can diverge on process failure.

Required:

```text
BEGIN producer transaction
  mutate producer business state
  enroll outbox record
COMMIT

Platform dispatcher publishes after commit
```

### 19.2 Durable accepted-request exception

A name containing `Requested` does **not** automatically make a contract an
imperative consumer command.

A producer may durably accept an intent/request as producer-owned state:

```text
producer accepts + persists intent
→ acceptance is itself a committed producer fact
→ producer-owned ...RequestedV1 Integration Event may be valid
```

Current example to preserve carefully:

```text
N8nDispatchRequestedV1
```

It may remain a valid Automation-owned outward fact because the dispatch intent
is persisted with an accepted durable `AutomationExecution`.

Do not rename/delete/reclassify it merely because the name contains
`Requested`.

The audit must instead prove:

```text
dispatch intent is durably accepted by Automation
event states that accepted producer fact
consumer is not addressed through a consumer-owned implementation contract
```

The ownership defect remains the Work member-assigned fact being
Automation-coupled, not all requested events.

### 19.3 Failure ownership

Platform owns:

```text
delivery
technical retry
dedup
poison/dead-letter
message identity
envelope
ordering mechanism
```

Business contexts own:

```text
event meaning
consumer reaction
business failure
workflow progression
business retry/reconciliation meaning
```

Producer success is not rolled back by an independent event consumer failure.
If producer success actually depends on participant outcomes, the flow requires
an explicit transaction decision or TAC-XC-F Process Manager semantics rather
than pretending event choreography is atomic.

---

# 20. Broker/Platform rule

Platform owns mechanisms:

```text
message id
envelope
outbox
consumer delivery state
dedup
technical retry
ordering
dead-letter
poison handling
broker client
observability
```

Business contexts own:

```text
event meaning
consumer reaction
workflow progression
business retry meaning
```

Reference flows MUST execute through the actual current Platform/outbox
mechanism where applicable.

Do not create an alternate teaching Broker.

---

# 21. ResourceRef

Reference execution MUST contain at least one stable cross-resource reference
pattern.

Preferred context pair:

```text
Collaboration
→ target resource owned by Documents or Work Management
```

ResourceRef means:

```text
ResourceType
ResourceId
```

It does not imply:

```text
exists
authorized
same workspace
same lifecycle
```

Those remain separate checks.

---

# 22. API boundary

API is:

```text
inbound transport
composition root
read/BFF composition where appropriate
```

API is not:

```text
multi-BC mutation workflow owner
```

Reference flows do not require fake endpoints.

If a reference is not user-facing yet:

```text
test through Application/runtime composition
```

rather than exposing a temporary API.

---

# 23. Six canonical interaction mechanisms

No seventh mechanism is introduced by this execution.

Every **actual cross-BC interaction edge** must classify as exactly one of
`TAC-XC-A..F`.

A Real Flow may contain several independently classified edges.

## 23.1 Interaction selection decision tree

```text
Does the use case need another BC?
|
+-- No
|   → Local Vertical Slice / another supporting shape
|
+-- Need an authoritative answer now?
|   |
|   +-- producer vocabulary already fits and no consumer runtime seam matters
|   |   → TAC-XC-A Producer Public Direct
|   |
|   +-- consumer needs vocabulary isolation, composition or runtime substitution
|       → TAC-XC-B Consumer Port + ACL + Adapter
|
+-- Need the target BC to mutate state it owns?
|   → TAC-XC-E Target-owned Command/Action
|
+-- A producer fact has committed and reactions are independently owned?
|   → TAC-XC-C Integration Event
|
+-- Consumer needs foreign-derived data frequently and staleness is acceptable?
|   → TAC-XC-D Local Projection
|
+-- Multi-step participant workflow needs durable progression/outcomes?
    → TAC-XC-F Process Manager
```

Before creating any cross-context Port, first apply the pipeline-first invariant
from section 10. Standard authentication, scope, permission enforcement,
idempotency, transaction/data-session and correlation do not become artificial
Ports.

## 23.2 TAC-XC-A — Producer Public Direct

Use when:

```text
consumer needs authoritative answer immediately
producer semantics already fit
no consumer-specific translation is required
no runtime abstraction is materially useful today
```

Current binding:

```text
Consumer Handler
→ Producer.Public query/fact
→ producer-owned implementation
→ producer persistence
```

`Producer.Public Direct` is first a **semantic boundary**, not an automatic
remote seam.

At future physical extraction, architecture review chooses one of:

```text
A. runtime proxy implements the stable Producer.Public contract
   → remote producer endpoint

or

B. consumer migrates the edge to TAC-XC-B
   → Consumer Port
   → remote query Adapter
   → producer endpoint
```

Therefore:

```text
TAC-XC-A = semantic-boundary ready
TAC-XC-A != automatically runtime-substitution ready
```

Do not pre-create a fake remote adapter.

## 23.3 TAC-XC-B — Consumer Port + ACL + Adapter

Use when:

```text
consumer vocabulary differs
consumer wants runtime isolation
composite read is required
future in-process → remote substitution matters
```

Canonical request path:

```text
Consumer Handler
→ Consumer Port
→ optional consumer ACL request mapping
→ Infrastructure InProcess Adapter
→ Producer.Public / producer-owned Application boundary
→ producer semantic result
→ optional consumer ACL response mapping
→ consumer result
```

Future binding:

```text
same Handler + Port + consumer semantic mapping
→ Remote Adapter
→ request-response transport
→ producer inbound adapter
→ Producer Application
```

Application business logic remains transport-free.

The Port is consumer-owned. Producer Public is producer-owned. The Adapter is
runtime-owned. ACL is consumer-owned and pure.

## 23.4 TAC-XC-C — Integration Event

Use when:

```text
producer fact has committed
producer success does not require consumer completion
zero/one/many consumers may react
consumer reaction is independently owned
eventual consistency is accepted
```

Canonical path is section 19:

```text
producer commit + outbox
→ Platform delivery
→ consumer dedup
→ consumer-local reaction
```

The event contract is producer-owned and versioned. Platform delivery is
business-neutral.

## 23.5 TAC-XC-D — Local Projection

Use when:

```text
consumer reads foreign-derived data frequently
live remote query on each read is undesirable
eventual consistency is acceptable
consumer owns derived read semantics
```

Canonical path:

```text
Producer Event/Fact
→ Platform delivery / accepted source adapter
→ Consumer Projection
→ consumer-local persistence
→ Consumer local read
```

Projection requirements:

```text
source authority
projection owner
event/version or snapshot source
scope
idempotency
ordering/revision guard
freshness semantics
rebuild source
failure recovery / drift repair
```

Outbox is not an event store. Rebuild must use an explicitly accepted
authoritative producer-owned source.

## 23.6 TAC-XC-E — Target-owned Command/Action

Use when one BC needs another BC to mutate state owned by the target and caller
cares about the semantic result.

Same-process shape:

```text
Consumer/workflow
→ optional Consumer Port + ACL + Adapter
→ Producer.Public action
→ Producer Application
→ Producer Domain
→ producer transaction
→ semantic result
```

The target owns:

```text
mutation
authorization for target operation
target scope enforcement
target idempotency/concurrency
business rejection
outward facts caused by target mutation
```

The caller MUST NOT load/save the target aggregate or use target DbContext.

Future remote shape may be:

```text
Consumer Port
→ Remote Target Adapter
→ request-response transport
→ target inbound adapter
→ target Application
```

but a clean target action does not erase transaction/product extraction
blockers. `BOUND-TX-002`, `BOUND-TX-003` when selected, and `BOUND-TX-004` must
retain their explicit extraction semantics.

## 23.7 TAC-XC-F — Process Manager

Do not use by default.

Simple event fan-out:

```text
CommittedFact
├→ independent consumer A
└→ independent consumer B
```

does not require a Saga/Process Manager merely because more than one consumer
exists.

Use TAC-XC-F when:

```text
workflow spans multiple participant steps
later decisions depend on earlier outcomes
progress must survive restart
business timeout/retry/compensation exists
workflow has explicit durable progression state
```

Canonical shape:

```text
Workflow-owner Process
→ durable process state
→ participant command/action
← participant outcome/event
→ state transition
→ next participant
...
→ Completed / Failed / ReconciliationRequired
```

Process Manager owns:

```text
workflow progression
process state
participant correlation
business timeout
business compensation decision
```

It does not own participant Domain state/DbContext or technical broker retry.

Automation/`AutomationExecution` is the pinned executable reference. A second
Process aggregate/folder is not created unless source normalization proves it is
actually needed.

## 23.8 Provider boundary — supporting runtime pattern, not TAC-XC-G

External provider interaction uses:

```text
Application semantic Port
→ Infrastructure provider Adapter
→ SDK/HTTP/provider transport
→ translated semantic result
```

Application must not depend on provider SDK/HTTP response types.

Provider results must distinguish the semantic categories required by the real
flow, for example:

```text
success
business/terminal rejection
transient technical failure
configuration/credential failure
unknown outcome
```

Unknown provider outcome is never assumed safe to blindly retry without an
explicit idempotency/reconciliation policy.

## 23.9 Interaction decision matrix

| Need | Primary mechanism | Caller waits? | Current same-process form | Future distributed form |
|---|---|---:|---|---|
| Authoritative answer; producer semantics fit | TAC-XC-A | yes | Producer.Public direct | runtime proxy or migrate to B after extraction review |
| Authoritative answer; consumer isolation/translation required | TAC-XC-B | yes | Port → ACL → in-process Adapter → Producer.Public | same Port → remote query Adapter |
| Target owner must mutate and return semantic result | TAC-XC-E | usually yes | Public action direct or through Port/Adapter | remote command Adapter, unless extraction-blocked |
| Completed producer fact; independent reaction | TAC-XC-C | no | Outbox → Platform → consumer | same broker-event semantics across services |
| Frequent foreign-derived read; staleness allowed | TAC-XC-D | local read | event/fact → local projection | projection remains consumer-local |
| Durable multi-step workflow | TAC-XC-F | process-driven | durable local Process | durable cross-service Process |

## 23.10 Cross-process readiness model

Do not collapse readiness into one boolean.

Track independently:

```text
SemanticBoundaryReady
RuntimeSubstitutionReady
ExtractionBlocked
ExtractionBlocker
```

### Semantic boundary ready

```text
semantic owner explicit
contract owner explicit
foreign private state inaccessible
interaction mechanism classified
```

### Runtime substitution ready

```text
stable consumer dependency exists where required
runtime adapter/proxy replacement path is understood
transport stays outside Application
remote failure/timeout/unknown-outcome semantics are sufficiently defined
```

General guidance:

```text
TAC-XC-B → adapter-ready when Port + thin adapter are real
TAC-XC-C → broker-native when real outbox/dedup runtime is proven
TAC-XC-D → projection-native when source/rebuild semantics are proven
TAC-XC-F → process-native when durable progression is proven
TAC-XC-A → may be semantic-only until extraction review
```

### Extraction blocked

A boundary can be semantically clean while physical extraction is blocked by
accepted product consistency.

Current examples include:

```text
BOUND-TX-002
BOUND-TX-004
BOUND-TX-003 when the selected Billing consistency model remains a shared local transaction
```

This is not an architecture failure. Hiding the blocker or pretending remote
HTTP preserves local atomicity would be the failure.

### Per-edge readiness mapping

Flow Cards use `RemoteReadiness` as a compact per-edge profile. Certification
derives the independent readiness dimensions from it plus `ExtractionBlocker`.

Normative mapping:

| RemoteReadiness | SemanticBoundaryReady | RuntimeSubstitutionReady | ExtractionBlocked |
|---|---|---|---|
| `semantic-only` | yes | deferred | false unless exact blocker says otherwise |
| `adapter-ready` | yes | yes for the selected Port/Adapter seam | false unless exact blocker says otherwise |
| `broker-native` | yes | yes for event deployment substitution | false unless exact blocker says otherwise |
| `projection-native` | yes | yes for the selected projection/source model | false unless exact blocker says otherwise |
| `process-native` | yes | yes for participant transport substitution | false unless exact blocker says otherwise |
| `deferred-by-design` | yes if owners/mechanism are frozen | deferred | value comes from `ExtractionBlocker` |
| `extraction-blocked` | yes | no/deferred | true |

Rules:

```text
RemoteReadiness
!= service-ready boolean

ExtractionBlocker
overrides any optimistic runtime-readiness interpretation

FutureDistributedBinding
is a target substitution shape, not evidence that runtime substitution is already certified
```

TESTS/CERTIFICATION may report the derived fields
`SemanticBoundaryReady`, `RuntimeSubstitutionReady`, and `ExtractionBlocked`;
they MUST use this mapping rather than inventing a second readiness model.

---

# 24. Supporting shapes

These support the six mechanisms.

They are not seventh mechanisms.

## TAC-SUP-001 — Local vertical slice

## TAC-SUP-002 — Composite read adapter

## TAC-SUP-003 — ResourceRef

## TAC-SUP-004 — BFF/read composition

Provider Port/Infrastructure Adapter and Platform delivery are also supporting
runtime roles used by Real Flows. This revision does not invent a new TAC-XC
mechanism or require a new TAC-SUP identifier merely to describe them.

---

# 25. Team Operating Architecture Pack model

The canonical execution unit is now:

```text
Team Operating Architecture Pack
```

A Team Pack contains one or more bounded contexts.

For each owned business bounded context, the pack MUST contain:

```text
1. one real local production flow
2. every mandatory cross-context case assigned to that BC
3. every runtime/provider/projection/process case that the BC actually owns
4. tests and architecture gates for those flows
5. a copy-model future feature work can follow
```

A local production flow means a real owner-side Application path that:

```text
accepts a real request/fact
executes real owner semantics
reads/writes real owner state where applicable
uses the real pipeline/runtime
has real failure behavior
is DI reachable
has behavior/integration evidence
```

It does NOT have to be publicly routed through HTTP.

A producer-owned service invoked through another context may satisfy the owner
BC's local implementation requirement when that service is itself a complete
owner-side mutation/query flow.

---

## 25.1 Real Flow Catalog rule

Every mandatory pack contains a `Real Flow Catalog`.

Each flow has an immutable execution ID:

```text
{TEAM}-FLOW-{NN}
```

Examples:

```text
IA-FLOW-01
WG-FLOW-04
DC-FLOW-06
```

Old `*-REF-*` identifiers remain compatibility aliases for architecture
mechanism references.

The flow ID becomes the primary unit for:

```text
PLAN tasking
TEST traceability
CERTIFICATION
team handoff
future copy guidance
```

---

## 25.2 Flow Card contract

Every mandatory Real Flow MUST have the existing owner/use-case fields plus an
explicit interaction model.

Flow-wide fields remain:

```text
FlowId:
Team:
BoundedContext:
Purpose:
Disposition:
EntryPoint:
ProductionReachability:
SemanticOwner:
WorkflowOwner:
MutationAuthority:
SourceAuthority:
TargetAuthority:
Mechanisms:
FlowKind: Local | CrossContext | Mixed | Platform | Supporting
SupportingShapes: zero or more approved supporting shapes/roles
RequestPipeline:
Actor:
Authorization:
AccountScope:
WorkspaceScope:
ResourceScope:
Idempotency:
Concurrency:
Transaction:
ApplicationPath:
DomainPath:
PublicContract:
ConsumerPort:
ACL:
InfrastructureAdapter:
ProviderBoundary:
IntegrationEvents:
Projection:
ProcessState:
FailureClasses:
RetryOwner:
Observability:
DI:
BehaviorTests:
IntegrationTests:
ArchitectureGates:
RemoteSubstitution:
KnownDebt:
STOPConditions:
CompletionEvidence:
```

`RemoteSubstitution` remains a compatibility summary only. It MUST NOT override
per-edge interaction fields.

`FlowKind` interpretation:

```text
Local
→ one BC-local owner flow; no actual cross-BC edge in this Flow Card

CrossContext
→ the Flow Card's primary purpose is one or more TAC-XC cross-BC edges

Mixed
→ local owner work plus one or more actual TAC-XC cross-BC edges in the same workflow

Platform
→ Platform/Foundation runtime mechanism flow

Supporting
→ reusable non-BC interaction/pipeline/supporting flow whose actual cross-BC business
  edges are recorded on the participating business Flow Cards
```

Therefore:

```text
FlowKind = Mixed
→ Interactions[] MUST NOT be empty

FlowKind = Supporting
→ Interactions[] may be empty when this Flow Card owns no business cross-BC edge
```

### 25.2.1 Real Flow != one interaction edge

A single workflow may contain several mechanisms.

Example:

```text
AcceptInvitation
→ Identity authoritative fact           TAC-XC-A
→ Accounts admission fact               TAC-XC-A
→ Accounts membership target mutation   TAC-XC-E
→ Workspaces local mutation              supporting Local Vertical Slice
→ WorkspaceMemberAdded outward fact      TAC-XC-C in its producer→consumer flow
```

Therefore a single flat field such as `InteractionClass` is forbidden.

### 25.2.2 Per-edge `Interactions[]`

Every actual cross-BC edge is recorded independently:

```text
Interactions:
  - EdgeId: E1
    Purpose:
    SourceBC:
    TargetBC:
    Mechanism: TAC-XC-A | TAC-XC-B | TAC-XC-C | TAC-XC-D | TAC-XC-E | TAC-XC-F
    DecisionStatus: FROZEN | DEFERRED-{MILESTONE}

    ContractOwner:
    PortOwner:
    WorkflowOwner:
    MutationOwner:
    ProjectionOwner:
    ProcessOwner:

    CallerWaitsForOutcome:
    ConsistencyModel:
    CurrentBinding:
    RemoteReadiness:
    FutureDistributedBinding:

    TransactionBoundary:
    OutboxRequired:
    ConsumerDedupRequired:
    IdempotencyKey:
    OrderingRequirement:

    RetryOwner:
    BusinessFailureOwner:
    TechnicalFailureOwner:
    UnknownOutcomePolicy:
    CompensationPolicy:

    ExtractionBlocker:
    RemovalTrigger:
```

Direction convention:

```text
TAC-XC-A/B/E:
  SourceBC = initiating consumer/workflow
  TargetBC = authoritative producer/target owner

TAC-XC-C/D:
  SourceBC = authoritative fact producer
  TargetBC = consumer/projection owner

TAC-XC-F:
  SourceBC = workflow/process owner
  TargetBC = participant owner for that process interaction
```

`EdgeId` is a stable sub-label inside the Flow Card:

```text
IA-FLOW-05:E1
AI-FLOW-03:E2
```

It is not a new Flow ID, milestone, certification family or architecture
mechanism.

### 25.2.3 Mechanism vs supporting shape

Only actual cross-BC edges use `TAC-XC-A..F`.

The following remain supporting shapes/roles, not alternative mechanism values:

```text
Local Vertical Slice
Composite Read
ResourceRef
BFF/read composition
provider Port/Adapter
Platform outbox/dedup/retry mechanics
```

A local-only Flow Card therefore uses:

```text
Interactions: []
```

rather than inventing a fake TAC-XC edge.

### 25.2.4 `NotApplicable` and `DEFERRED`

`NotApplicable` means the field truly does not apply to that exact edge.

It must never mean:

```text
not analyzed
choose later
coding agent decides
```

Later-pack deep runtime decisions may use an explicit state such as:

```text
DecisionStatus: DEFERRED-M10
FutureDistributedBinding: undecided-by-design
```

only after the mechanism and semantic owners are already classified.

No field that affects correctness may be left as:

```text
TBD
choose later
appropriate mechanism
or equivalent
```

for a mandatory current-pack flow.

---

## 25.3 What counts as a Real Flow

Counts:

```text
real command/query handler
real producer Public implementation
real consumer Port + runtime adapter
real Integration Event producer/consumer
real projection update/query/rebuild
real process progression
real provider connection/action
real background target mutation
```

Does NOT count:

```text
NotImplementedException handler
throw-only production stub
*StubConsumer*
log-only consumer with no owned reaction
empty interface
empty architecture folder
test-only fake feature
test that manually invokes only a lower-level seam
README/pseudocode
```

A log-only consumer may be useful observability, but it is not a business
reaction and cannot satisfy a mandatory event-consumer flow.

---

## 25.4 Missing-real-flow rule

When the canonical product/domain model clearly owns a capability but current
Application source is a stub or absent:

```text
accepted semantics exist
+
mandatory team architecture case exists
+
no real flow implementation exists
→ IMPLEMENT-MISSING
```

The coding agent must implement the smallest complete production flow from
accepted semantics.

It must NOT:

```text
invent a new product capability
invent a new bounded context
invent a fake provider
invent a lifecycle transition the Domain/product authority does not define
```

Example:

```text
Documents Page.Archive exists in Domain
ArchivePage handler is NotImplemented
→ valid IMPLEMENT-MISSING reference

Documents PublishPage handler is NotImplemented
but current Page aggregate has no Publish transition
→ NOT a valid bootstrap target without product decision
```

---

## 25.5 Variant coverage rule

One generic handler may represent several materially different architecture
cases.

When variants differ in any of:

```text
resource owner
ResourceKind
authorization action
scope resolution
foreign fact source
mutation authority
failure semantics
```

they are separate Flow Card scenarios.

Example:

```text
CreateComment.ForBoardItem
CreateComment.ForPage
```

share one Collaboration handler but must have separate authorization/resource
evidence because the target resource owner differs.

---

## 25.6 v2.6 Real Flow count

The v2.6 catalog contains:

```text
47 Flow IDs total
46 executable/verification-required flows
1 conditional classification = BI-FLOW-05 external Billing provider
```

New v2.6 IDs:

```text
IA-FLOW-06 Accounts RenameAccount local admin mutation
PF-FLOW-07 scoped Integration Event tenant envelope
```

A count-only check is insufficient; every ID must map to substantive
PLAN/TESTS/CERT evidence.

## 25.7 Source Operating Pattern — copy model for team Real Flows

The backend does **not** create a generic cross-context framework.

The common working pattern is:

```text
frozen Flow Card
→ select TAC-XC mechanism
→ use the matching proven source topology
→ implement owner semantics
→ production DI/runtime
→ behavior + integration + architecture proof
→ VERIFIED Real Flow
```

The pattern is a set of **verified production shapes**, not a base class,
generic gateway, shared service or seventh interaction mechanism.

### 25.7.1 Canonical source locations

Owner-local flow:

```text
Application/Features/<Owner>/<Capability>/<Commands|Queries>/<UseCase>/
→ owner bounded-context DbContext interface
→ owner Domain
```

Producer Public Direct / target action:

```text
Application/Features/<Producer>/Public/<PublishedCapability>/
  [<SemanticSubCapability>/]
  <producer-owned public contracts>
```

The capability directory is semantic. Do not mirror internal CQRS or artifact
types at the first level below `Public`. `Commands`, `Queries`, `Facts`,
`Actions`, `Contracts`, `DTOs`, `Services` and `Common` are not canonical
top-level Public buckets.

Consumer Port:

```text
Application/Features/<Consumer>/Ports/<Producer>/
```

Consumer semantic ACL/mapping, when needed:

```text
owned by Consumer Application semantics
→ pure mapping/policy translation
→ no transport/provider/DbContext dependency
```

Current runtime adapter:

```text
Infrastructure/CrossContext/<Consumer>/<Producer>/
```

Async inbound adapter:

```text
Infrastructure/Messaging/Consumers/<Consumer or event-family>/
→ restore execution/message context
→ dispatch consumer-owned Application command/use case
→ no foreign private mutation in the consumer
```

Provider boundary:

```text
Consumer/Owner Application provider Port
→ Infrastructure provider adapter
→ provider SDK/API
```

### 25.7.2 Cross-context DI composition

Cross-context bindings are runtime composition, not persistence ownership.

Target topology:

```text
Infrastructure/DependencyInjection/CrossContextRegistration.cs

AddCrossContextBindings()
  IWorkActionPort
    → WorkItemActionAdapter

  IWorkManagementCollaborationReadPort
    → WorkManagementCollaborationReadAdapter

  IIdentityBootstrapReadPort
    → IdentityBootstrapReadAdapter

  Analytics projection-source runtime binding
    → current adapter until M10 normalization
```

`PersistenceRegistration` remains responsible for:

```text
ApplicationDbContext
bounded-context DbContext interface mappings
persistence interceptors
RLS/data-session persistence
outbox persistence services
```

A runtime cross-context adapter MUST NOT live in persistence registration merely
because one implementation currently reads a database.

This source normalization changes composition ownership only. It does not
change TAC-XC classification, transaction semantics or product behavior.

### 25.7.3 Reference eligibility

Only a `VERIFIED` Real Flow may be declared a **canonical copy-model** for
future teams.

A production source may still be used as a **structural reference** for one
specific invariant before its owning milestone is fully certified.

Structural reference does NOT mean:

```text
the whole Flow is VERIFIED
the owning milestone is complete
all failure/process/product semantics are frozen
```

Current source-pattern baseline at source refresh
`fc3d98d6ce2c72d9e0fab3c76df0b032891b49eb`:

```text
Local Vertical Slice
  IA-FLOW-01 / IA-FLOW-06 source
  candidate for first canonical M4 local copy-model

TAC-XC-A Producer Public Direct
  Accounts.Public IAccountMembershipFacts
  candidate for first canonical M4 Public Direct copy-model

TAC-XC-B Port + ACL + Adapter
  Automation IWorkActionPort
  → consumer semantic mapping
  → Infrastructure WorkItemActionAdapter
  → WorkManagement.Public IWorkItemActions
  structural reference now
  full owning-flow certification remains M8

TAC-XC-C Integration Event
  IdentityRegistrationCompletedIntegrationEventV1
  → real outbox/Platform delivery
  → WorkspaceProvisioningConsumer
  → ProvisionPersonalWorkspaceCommand
  candidate for first canonical M4 event copy-model

TAC-XC-E Target-owned Action
  Accounts.Public IAccountMembershipActions exists
  Register→Accounts provisioning is NOT canonical until IA-FLOW-02 migrates from
  private IAccountProvisioningService to Accounts.Public provisioning action

TAC-XC-D Projection
  NOT canonical before M10

TAC-XC-F Process Manager
  AutomationExecution durable state is source groundwork
  NOT canonical before M8 process proof
```

### 25.7.4 Explicit non-reference source

The following current source MUST NOT be copied as the generic team pattern:

```text
WorkManagementCollaborationReadAdapter
→ directly reads ICollaborationDbContext
→ DEFERRED-M6-SOURCE-NORMALIZATION
→ non-reference until M6 closes producer semantic boundary

WorkItemProjectionSourceAdapter
→ Runtime Adapter only — delegates to WorkManagement Application implementation of
  IWorkItemProjectionSource; never reads Work persistence itself
→ DEFERRED-M10-PORT-OWNERSHIP-NORMALIZATION = CLOSED-FROZEN (TAC-XC-B, M10)
→ reference pattern for consumer-boundary source lookup

IdentityBootstrapReadAdapter
→ multi-context bootstrap/composite read
→ Supporting Composite Read only
→ not TAC-XC-B teaching reference

*StubConsumer* / log-only consumer
→ observability/stub only
→ never satisfies Real Flow reaction
```

### 25.7.5 No generic framework rule

Forbidden attempts to "standardize" team work:

```text
IUniversalPort
IContextGateway
ICrossContextService
GenericPublicService<T>
GenericCrossContextAdapter<T>
SharedDomainService
Common business capability gateway
one folder/template instantiated for every BC
```

A context creates `Public`, `Ports`, `CrossContext`, `Processes`, `Projections`
or provider seams only when a real Flow requires that mechanism.

### 25.7.6 Team copy algorithm

After a canonical reference exists, a team implements a new Real Flow as:

```text
1. read Flow Card
2. enumerate Interactions[]
3. choose the matching VERIFIED reference by TAC-XC mechanism
4. copy topology/invariants, not business vocabulary
5. keep producer/consumer/workflow ownership from the new Flow Card
6. implement business behavior
7. register production runtime binding
8. run behavior/integration/architecture proof
9. record exact evidence
10. mark VERIFIED only after TESTS/CERT criteria pass
```

If no matching canonical reference exists yet:

```text
do not invent a new framework
do not copy a known non-reference
use the owning milestone to complete the missing flagship reference
```

# 25A. M1 Flow Card Register — local execution authority

Temporary local execution artifact. This section materializes the 47 mandatory Real Flow Cards required by TAC-M1.
It is not a repository deliverable and must not be committed.

### IA-FLOW-01 — Identity local profile mutation

```text
FlowId: IA-FLOW-01
Team: TAC-RAP-IA
BoundedContext: Identity
Purpose: Pure Identity local vertical slice for authenticated user profile mutation
Disposition: REUSE-EXISTING
EntryPoint: UpdateProfileCommand
ProductionReachability: current production source
SemanticOwner: Identity
WorkflowOwner: Identity
MutationAuthority: User aggregate
SourceAuthority: Notrelix.Domain.Identity.Users.User
TargetAuthority: Notrelix.Domain.Identity.Users.User
Mechanisms: UpdateProfileCommandHandler; canonical MediatR request pipeline; EF Core request transaction; User.UpdateProfile aggregate mutation
FlowKind: Local
SupportingShapes: LocalVerticalSlice
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated user
Authorization: Canonical Application authorization pipeline
AccountScope: NotApplicable — user-scoped profile mutation
WorkspaceScope: NotApplicable — user-scoped profile mutation
ResourceScope: Current user
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Request transaction owns durable mutation and required outbox enrollment
ApplicationPath: backend/src/Notrelix.Application/Features/Identity/Profiles/Commands/UpdateProfile/UpdateProfile.cs
DomainPath: User.UpdateProfile
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: ApplicationDbContext persistence adapter
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: UpdateProfileTests; UserMutationContractTests
IntegrationTests: UpdateProfileCommandHandlerTests
ArchitectureGates: AuthPipelineArchitectureTests; CommandMarkerArchitectureTests; UseCaseSecurityClassificationTests
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: None beyond current milestone scope
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: UpdateProfileCommandHandler source and UpdateProfileTests/UpdateProfileCommandHandlerTests exist
```

### IA-FLOW-02 — Identity registration to Accounts personal provisioning

```text
FlowId: IA-FLOW-02
Team: TAC-RAP-IA
BoundedContext: Identity + Accounts
Purpose: Register or OAuth-create user and provision personal Account atomically under BOUND-TX-004
Disposition: MIGRATE-EXISTING
EntryPoint: RegisterCommand and CompleteOAuthLoginCommand
ProductionReachability: current production handlers with private Accounts provisioning
SemanticOwner: Identity registration workflow
WorkflowOwner: Identity registration
MutationAuthority: Accounts personal Account provisioning
SourceAuthority: Identity User
TargetAuthority: Accounts Account and AccountMember
Mechanisms: RegisterCommandHandler and CompleteOAuthLoginCommandHandler; AccountProvisioningService; BOUND-TX-004 shared request transaction; IdentityRegistrationCompleted outbox intent
FlowKind: Mixed
SupportingShapes: LocalVerticalSlice
Interactions:
  - EdgeId: E1
    Purpose: personal Account provisioning owned by Accounts
    SourceBC: Identity
    TargetBC: Accounts
    Mechanism: TAC-XC-E
    DecisionStatus: FROZEN
    ContractOwner: Accounts
    PortOwner: NotApplicable
    WorkflowOwner: Identity registration
    MutationOwner: Accounts
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: direct-in-process
    RemoteReadiness: extraction-blocked
    FutureDistributedBinding: undecided-by-design
    TransactionBoundary: BOUND-TX-004 shared current request transaction across Identity User + Accounts personal Account/member provisioning
    OutboxRequired: target-owned if outward fact is part of target mutation
    ConsumerDedupRequired: target-defined where operation identity exists
    IdempotencyKey: registration identity + personal Account uniqueness/idempotent provisioning
    OrderingRequirement: target aggregate/version/concurrency rules
    RetryOwner: caller/current request; target owns semantic idempotency
    BusinessFailureOwner: Accounts target mutation owner
    TechnicalFailureOwner: current in-process request runtime; future remote adapter/runtime
    UnknownOutcomePolicy: resolved inside current local request transaction; remote unknown outcome is not certified while BOUND-TX-004 exists
    CompensationPolicy: none unless workflow/product explicitly defines it
    ExtractionBlocker: true — BOUND-TX-004
    RemovalTrigger: Identity/Accounts physical extraction or approved product decision allowing asynchronous/partial provisioning + reconciliation
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated user
Authorization: Canonical Application authorization pipeline
AccountScope: Created personal Account
WorkspaceScope: NotApplicable — Account provisioning only
ResourceScope: New user and new Account
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: BOUND-TX-004 shared request transaction
ApplicationPath: backend/src/Notrelix.Application/Features/Identity/Registration/Commands/Register/RegisterCommandHandler.cs; backend/src/Notrelix.Application/Features/Identity/OAuth/Commands/CompleteOAuthLogin/CompleteOAuthLoginCommand.cs; backend/src/Notrelix.Application/Features/Accounts/Provisioning/AccountProvisioningService.cs
DomainPath: NotApplicable — no Domain mutation required
PublicContract: Accounts.Public.PersonalAccountProvisioning
ConsumerPort: NotApplicable — current reference calls producer-owned Accounts.Public provisioning action directly under TAC-XC-E
ACL: NotApplicable — no consumer vocabulary translation in the current direct Accounts.Public action
InfrastructureAdapter: current shared request data session; after M4 normalization Accounts owner implementation owns Account/AccountMember persistence behind Accounts.Public
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: IdentityRegistrationCompletedIntegrationEventV1 after successful Account provisioning
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: RegisterTests; AccountProvisioningServiceTests; IdentityRegistrationCompletedIntegrationEventTests
IntegrationTests: RegisterCommandHandlerTests; CompleteOAuthLoginCommandHandlerTests
ArchitectureGates: CommonPublicSignaturePurityArchitectureTests; ScopedEventTenantEnvelopeArchitectureTests; PipelineFreezeArchitectureTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: Source refresh must prove private provisioning removal and capability-first Accounts.Public.PersonalAccountProvisioning topology before canonical copy-model status
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Register and OAuth handlers emit IdentityRegistrationCompleted after AccountProvisioningService success; tests exist
```

### IA-FLOW-03 — Identity registration event to Workspace provisioning

```text
FlowId: IA-FLOW-03
Team: TAC-RAP-IA
BoundedContext: Identity + Workspaces
Purpose: Fan out completed registration to personal Workspace provisioning
Disposition: REUSE-EXISTING
EntryPoint: IdentityRegistrationCompletedIntegrationEventV1
ProductionReachability: current production source
SemanticOwner: Identity registration workflow
WorkflowOwner: Workspaces provisioning consumer
MutationAuthority: Workspaces ProvisionPersonalWorkspaceCommand
SourceAuthority: IdentityRegistrationCompleted event
TargetAuthority: Workspace and WorkspaceMember
Mechanisms: MassTransit receive pipeline; TenantContextConsumeFilter; DeduplicationConsumeFilter; WorkspaceProvisioningConsumer; ProvisionPersonalWorkspaceCommand
FlowKind: CrossContext
SupportingShapes: PlatformDelivery
Interactions:
  - EdgeId: E1
    Purpose: committed Identity registration fact triggers Workspaces-owned personal Workspace provisioning
    SourceBC: Identity
    TargetBC: Workspaces
    Mechanism: TAC-XC-C
    DecisionStatus: FROZEN
    ContractOwner: Identity
    PortOwner: NotApplicable
    WorkflowOwner: Workspaces personal Workspace provisioning
    MutationOwner: Workspaces
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: no
    ConsistencyModel: eventual
    CurrentBinding: platform-event-delivery
    RemoteReadiness: broker-native
    FutureDistributedBinding: broker-event
    TransactionBoundary: producer business state + outbox commit atomically; consumer-local transaction is independent
    OutboxRequired: yes
    ConsumerDedupRequired: yes
    IdempotencyKey: EventId/MessageId + consumer identity
    OrderingRequirement: Platform/event-stream rule plus producer revision where the event family requires ordering
    RetryOwner: Platform technical delivery retry
    BusinessFailureOwner: Workspaces provisioning semantics
    TechnicalFailureOwner: Platform delivery runtime
    UnknownOutcomePolicy: delivery state/retry/dead-letter is Platform-owned; business reconciliation remains consumer/workflow-owned
    CompensationPolicy: none in current choreography; registration success is independent of Workspace consumer completion
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
RequestPipeline: MassTransit receive pipeline -> TenantContextConsumeFilter -> DeduplicationConsumeFilter -> consumer
Actor: System consumer with event AccountId
Authorization: ISystemInternalRequest with explicit AccountId
AccountScope: Event AccountId required
WorkspaceScope: Created personal Workspace
ResourceScope: Personal Workspace per Account
Idempotency: MessageId plus WorkspaceProvisioningConsumer identity through DeduplicationConsumeFilter
Concurrency: Personal Workspace uniqueness and consumer dedup claim
Transaction: Consumer request transaction provisions Workspace atomically with dedup completion
ApplicationPath: backend/src/Notrelix.Application/Features/Workspaces/Provisioning/Commands/ProvisionPersonalWorkspace/ProvisionPersonalWorkspaceCommand.cs
DomainPath: NotApplicable — no Domain mutation required
PublicContract: IdentityRegistrationCompletedIntegrationEventV1
ConsumerPort: NotApplicable — WorkspaceProvisioningConsumer is an Infrastructure inbound message Adapter, not an Application Port
ACL: NotApplicable — local flow
InfrastructureAdapter: backend/src/Notrelix.Infrastructure/Messaging/Consumers/Identity/RegistrationCompleted/WorkspaceProvisioningConsumer.cs
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: workspace.created and membership facts as applicable
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: Platform/MassTransit delivery retry; deterministic tenant-envelope violations are non-retryable
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: WorkspaceProvisioningConsumerTests; IdentityRegistrationCompletedIntegrationEventTests
IntegrationTests: Target IA-FLOW-03 registration runtime-chain test; current scoped-chain pattern: WorkspaceMemberAddedScopedTenantRuntimeChainIntegrationTests
ArchitectureGates: ScopedEventTenantEnvelopeArchitectureTests; PlatformReferenceCompletenessTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: None beyond current milestone scope
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: WorkspaceProvisioningConsumer source and consumer tests exist; full registration runtime-chain proof remains IA-FLOW-03 team work
```

### IA-FLOW-04 — Accounts membership admission read

```text
FlowId: IA-FLOW-04
Team: TAC-RAP-IA
BoundedContext: Accounts
Purpose: Producer-owned Account admission fact for cross-context consumers
Disposition: REUSE-EXISTING
EntryPoint: IAccountMembershipFacts.GetAdmissionAsync
ProductionReachability: current production source
SemanticOwner: Accounts
WorkflowOwner: Consumer use case
MutationAuthority: NotApplicable — read flow
SourceAuthority: Account aggregate and AccountMember state
TargetAuthority: AccountMembershipAdmissionFact
Mechanisms: Accounts.Public IAccountMembershipFacts authoritative query contract; AccountMembershipFactsProvider; EF Core read query under consumer request transaction
FlowKind: CrossContext
SupportingShapes: None
Interactions:
  - EdgeId: E1
    Purpose: Workspaces obtains authoritative Account membership/admission fact
    SourceBC: Workspaces
    TargetBC: Accounts
    Mechanism: TAC-XC-A
    DecisionStatus: FROZEN
    ContractOwner: Accounts
    PortOwner: NotApplicable
    WorkflowOwner: Workspace invitation/admission
    MutationOwner: NotApplicable
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: direct-in-process
    RemoteReadiness: semantic-only
    FutureDistributedBinding: undecided-by-design
    TransactionBoundary: current request read boundary; no foreign mutation
    OutboxRequired: no
    ConsumerDedupRequired: no
    IdempotencyKey: NotApplicable — read
    OrderingRequirement: NotApplicable
    RetryOwner: caller/current request; no message retry
    BusinessFailureOwner: Accounts authoritative read semantics
    TechnicalFailureOwner: current in-process request runtime
    UnknownOutcomePolicy: no transport unknown outcome in current binding; define timeout/availability policy at extraction
    CompensationPolicy: NotApplicable — read
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated consumer request
Authorization: Consumer protected request plus Accounts.Public authoritative read contract
AccountScope: Required AccountId
WorkspaceScope: NotApplicable — Account read fact
ResourceScope: Account
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Request transaction owns durable mutation and required outbox enrollment
ApplicationPath: backend/src/Notrelix.Application/Features/Accounts/Members/Services/AccountMembershipFactsProvider.cs
DomainPath: NotApplicable — no Domain mutation required
PublicContract: Accounts.Public.Membership
ConsumerPort: NotApplicable — IAccountMembershipFacts is producer-owned Accounts.Public, not a consumer-owned Port
ACL: NotApplicable — producer semantics are consumed directly under TAC-XC-A
InfrastructureAdapter: NotApplicable for the cross-BC edge — Accounts producer implementation owns its persistence internally
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: AccountMembershipFactsProviderTests
IntegrationTests: Target Accounts membership fact persistence test; current proof: AccountMembershipFactsProviderTests
ArchitectureGates: PublicSemanticContractArchitectureTests; BoundedContextPortArchitectureTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: None beyond current milestone scope
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Accounts.Public fact provider and AccountMembershipFactsProviderTests exist
```

### IA-FLOW-05 — Accounts membership target mutation

```text
FlowId: IA-FLOW-05
Team: TAC-RAP-IA
BoundedContext: Accounts
Purpose: Producer-owned Account membership target action under BOUND-TX-002
Disposition: REUSE-EXISTING
EntryPoint: IAccountMembershipActions.EnsureAccountMembershipAsync
ProductionReachability: current production source
SemanticOwner: Accounts
WorkflowOwner: Workspace invitation acceptance
MutationAuthority: AccountMember aggregate
SourceAuthority: Workspace invitation request facts
TargetAuthority: AccountMember and access grant projection
Mechanisms: IAccountMembershipActions target action; AccountMembershipActions; AccountMember aggregate mutation; Workspace grant projection adapter; BOUND-TX-002 shared request transaction
FlowKind: CrossContext
SupportingShapes: None
Interactions:
  - EdgeId: E1
    Purpose: Workspaces invitation workflow requests Accounts-owned membership mutation
    SourceBC: Workspaces
    TargetBC: Accounts
    Mechanism: TAC-XC-E
    DecisionStatus: FROZEN
    ContractOwner: Accounts
    PortOwner: NotApplicable
    WorkflowOwner: Workspace invitation acceptance
    MutationOwner: Accounts
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: direct-in-process
    RemoteReadiness: extraction-blocked
    FutureDistributedBinding: undecided-by-design
    TransactionBoundary: BOUND-TX-002 shared current request transaction with Workspace mutation + security projection
    OutboxRequired: target-owned if outward fact is part of target mutation
    ConsumerDedupRequired: target-defined where operation identity exists
    IdempotencyKey: canonical membership uniqueness / idempotent current-request retry
    OrderingRequirement: target aggregate/version/concurrency rules
    RetryOwner: caller/current request; target owns semantic idempotency
    BusinessFailureOwner: Accounts target mutation owner
    TechnicalFailureOwner: current in-process request runtime; future remote adapter/runtime
    UnknownOutcomePolicy: resolved inside current local request transaction; remote partial success is not certified
    CompensationPolicy: none unless workflow/product explicitly defines it
    ExtractionBlocker: true — BOUND-TX-002
    RemovalTrigger: Accounts/Workspaces physical extraction or approved product decision allowing partial success/reconciliation
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated invitee or system consumer
Authorization: Workspace flow authorizes; Accounts owns membership admission
AccountScope: Required AccountId
WorkspaceScope: Triggered by Workspace invitation but mutation is Account-scoped
ResourceScope: AccountMember
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: BOUND-TX-002 shared request transaction with Workspace mutation
ApplicationPath: backend/src/Notrelix.Application/Features/Accounts/Members/Services/AccountMembershipActions.cs
DomainPath: NotApplicable — no Domain mutation required
PublicContract: Accounts.Public.Membership
ConsumerPort: NotApplicable in the current reference — IAccountMembershipActions is producer-owned Accounts.Public target action
ACL: NotApplicable — no consumer vocabulary translation in the current direct target action
InfrastructureAdapter: NotApplicable for the consumer edge — Accounts target implementation owns Account persistence/grant projection internally
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: Account membership facts as canonical
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: AccountMembershipActionsTests
IntegrationTests: AccountMembershipTransactionEvidenceTests
ArchitectureGates: CrossContextApplicationDependencyTests; HandlerDataPortGateTests; CommonPublicSignaturePurityArchitectureTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: None beyond current milestone scope
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Producer-owned action and AccountMembershipTransactionEvidenceTests exist
```

### IA-FLOW-06 — Accounts local admin mutation

```text
FlowId: IA-FLOW-06
Team: TAC-RAP-IA
BoundedContext: Accounts
Purpose: Authenticated Account-scoped local admin mutation reference for RenameAccount
Disposition: HARDEN-EXISTING
EntryPoint: RenameAccountCommand via Accounts API route
ProductionReachability: current production API/Application source at PR head; implementation introduced before interaction-normalized recertification
SemanticOwner: Accounts
WorkflowOwner: Accounts
MutationAuthority: Account aggregate
SourceAuthority: Account aggregate
TargetAuthority: Account name and lifecycle state
Mechanisms: RenameAccountCommandHandler; canonical Application request pipeline; PermissionAction.ManageAccount; Account.Rename aggregate mutation; request transaction
FlowKind: Local
SupportingShapes: LocalVerticalSlice
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated Account admin
Authorization: Governance Account-admin permission
AccountScope: Required AccountId
WorkspaceScope: NotApplicable — Account resource
ResourceScope: Account
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Account version or expected-version where required
Transaction: Request transaction owns durable mutation and required outbox enrollment
ApplicationPath: backend/src/Notrelix.Application/Features/Accounts/Accounts/Commands/RenameAccount/RenameAccountCommand.cs; backend/src/Notrelix.Application/Features/Accounts/Accounts/Commands/RenameAccount/RenameAccountCommandHandler.cs
DomainPath: Account.Rename
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: ApplicationDbContext persistence adapter
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: RenameAccountCommandHandlerTests; AccountTests
IntegrationTests: Target RenameAccount integration tests
ArchitectureGates: Target Accounts command marker and authorization gates; current: CommandMarkerArchitectureTests; AuthPipelineArchitectureTests
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: No authenticated Accounts Application command exists yet
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: M0 source audit confirms Account.Rename exists and Application command is missing
```

### WG-FLOW-01 — Workspaces local creation

```text
FlowId: WG-FLOW-01
Team: TAC-RAP-WG
BoundedContext: Workspaces
Purpose: Create Workspace and owner membership atomically with grant projection
Disposition: REUSE-EXISTING
EntryPoint: CreateWorkspaceCommand
ProductionReachability: current production source
SemanticOwner: Workspaces
WorkflowOwner: Workspaces
MutationAuthority: Workspace and WorkspaceMember aggregates
SourceAuthority: Authenticated Account scope
TargetAuthority: Workspace, WorkspaceMember, access_grants projection
Mechanisms: CreateWorkspaceCommandHandler; canonical Application request pipeline; WorkspaceFactory.CreateWithOwner; Workspace and WorkspaceMember aggregate mutation; grant projection; outbox enrollment
FlowKind: Local
SupportingShapes: LocalVerticalSlice
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated Account member
Authorization: PermissionAction.CreateWorkspace
AccountScope: Required AccountId
WorkspaceScope: Created Workspace
ResourceScope: Workspace
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Slug database uniqueness and request transaction
Transaction: Request transaction owns durable mutation and required outbox enrollment
ApplicationPath: backend/src/Notrelix.Application/Features/Workspaces/Workspaces/Commands/CreateWorkspace/CreateWorkspace.cs
DomainPath: WorkspaceFactory.CreateWithOwner
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: WorkspaceDbContext and Workspace grant projection adapter
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: workspace.created and workspace.member.added as canonical
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: Target CreateWorkspace Application behavior tests; current proof: WorkspaceTests; WorkspaceOwnerRulesTests
IntegrationTests: CreateWorkspaceCommandHandlerTests; WorkspaceCreationPipelineAuthorizationTests
ArchitectureGates: AuthPipelineArchitectureTests; WorkspaceScopedArchitectureTests; ScopedEventTenantEnvelopeArchitectureTests
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: None beyond current milestone scope
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: CreateWorkspace source and pipeline authorization tests exist
```

### WG-FLOW-02 — Invitation acceptance workflow

```text
FlowId: WG-FLOW-02
Team: TAC-RAP-WG
BoundedContext: Workspaces + Accounts
Purpose: Accept workspace invitation with Identity facts, Accounts admission, WorkspaceMember mutation, and grant projection
Disposition: REUSE-EXISTING
EntryPoint: AcceptInvitationCommand
ProductionReachability: current production source
SemanticOwner: Workspaces invitation lifecycle
WorkflowOwner: Workspaces invitation acceptance
MutationAuthority: WorkspaceInvitation and WorkspaceMember
SourceAuthority: Identity user facts and Accounts admission facts
TargetAuthority: WorkspaceMember and access_grants projection
Mechanisms: AcceptInvitationCommandHandler; canonical Application request pipeline; IAccountMembershipFacts and IAccountMembershipActions; WorkspaceInvitation and WorkspaceMember mutation; grant projection; BOUND-TX-002 shared request transaction
FlowKind: Mixed
SupportingShapes: LocalVerticalSlice
Interactions:
  - EdgeId: E1
    Purpose: Workspaces obtains authoritative Identity fact for invitee
    SourceBC: Workspaces
    TargetBC: Identity
    Mechanism: TAC-XC-A
    DecisionStatus: FROZEN
    ContractOwner: Identity
    PortOwner: NotApplicable
    WorkflowOwner: Workspace invitation acceptance
    MutationOwner: NotApplicable
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: direct-in-process
    RemoteReadiness: semantic-only
    FutureDistributedBinding: undecided-by-design
    TransactionBoundary: current request read boundary; no foreign mutation
    OutboxRequired: no
    ConsumerDedupRequired: no
    IdempotencyKey: NotApplicable — read
    OrderingRequirement: NotApplicable
    RetryOwner: caller/current request; no message retry
    BusinessFailureOwner: Identity authoritative read semantics
    TechnicalFailureOwner: current in-process request runtime
    UnknownOutcomePolicy: no transport unknown outcome in current binding; define timeout/availability policy at extraction
    CompensationPolicy: NotApplicable — read
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
  - EdgeId: E2
    Purpose: Workspaces obtains authoritative Accounts admission/membership fact
    SourceBC: Workspaces
    TargetBC: Accounts
    Mechanism: TAC-XC-A
    DecisionStatus: FROZEN
    ContractOwner: Accounts
    PortOwner: NotApplicable
    WorkflowOwner: Workspace invitation acceptance
    MutationOwner: NotApplicable
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: direct-in-process
    RemoteReadiness: semantic-only
    FutureDistributedBinding: undecided-by-design
    TransactionBoundary: current request read boundary; no foreign mutation
    OutboxRequired: no
    ConsumerDedupRequired: no
    IdempotencyKey: NotApplicable — read
    OrderingRequirement: NotApplicable
    RetryOwner: caller/current request; no message retry
    BusinessFailureOwner: Accounts authoritative read semantics
    TechnicalFailureOwner: current in-process request runtime
    UnknownOutcomePolicy: no transport unknown outcome in current binding; define timeout/availability policy at extraction
    CompensationPolicy: NotApplicable — read
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
  - EdgeId: E3
    Purpose: Workspaces requests Accounts-owned membership mutation
    SourceBC: Workspaces
    TargetBC: Accounts
    Mechanism: TAC-XC-E
    DecisionStatus: FROZEN
    ContractOwner: Accounts
    PortOwner: NotApplicable
    WorkflowOwner: Workspace invitation acceptance
    MutationOwner: Accounts
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: direct-in-process
    RemoteReadiness: extraction-blocked
    FutureDistributedBinding: undecided-by-design
    TransactionBoundary: BOUND-TX-002 shared current request transaction across Accounts membership + WorkspaceMember/grant projection
    OutboxRequired: target-owned if outward fact is part of target mutation
    ConsumerDedupRequired: target-defined where operation identity exists
    IdempotencyKey: membership/workspace invitation idempotency under current request transaction
    OrderingRequirement: target aggregate/version/concurrency rules
    RetryOwner: caller/current request; target owns semantic idempotency
    BusinessFailureOwner: Accounts target mutation owner
    TechnicalFailureOwner: current in-process request runtime; future remote adapter/runtime
    UnknownOutcomePolicy: remote partial success is not certified while BOUND-TX-002 exists
    CompensationPolicy: none unless workflow/product explicitly defines it
    ExtractionBlocker: true — BOUND-TX-002
    RemovalTrigger: Accounts/Workspaces physical extraction or approved product decision allowing partial success/reconciliation
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated invitee
Authorization: Token scope, verified email, Workspace membership admission
AccountScope: Workspace AccountId
WorkspaceScope: Invitation WorkspaceId
ResourceScope: WorkspaceInvitation and WorkspaceMember
Idempotency: Already-member path is semantic no-op for invitation acceptance
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: BOUND-TX-002 shared request transaction
ApplicationPath: backend/src/Notrelix.Application/Features/Workspaces/Invitations/Commands/AcceptInvitation/AcceptInvitation.cs
DomainPath: NotApplicable — no Domain mutation required
PublicContract: Accounts.Public.Membership (authoritative fact + target action)
ConsumerPort: NotApplicable in the current reference — both are producer-owned Accounts.Public.Membership contracts; introduce a Workspaces-owned Port only if TAC-XC-B is explicitly selected
ACL: NotApplicable — current Workspaces→Accounts direct Public facts/actions use accepted producer vocabulary
InfrastructureAdapter: current shared request data session composes Workspaces-owned persistence + Accounts-owned target implementation + Workspace grant projection under BOUND-TX-002; Workspaces does not own AccountDbContext
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: workspace.member.added as canonical
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: Target AcceptInvitation Application behavior tests; current public transport proof: InvitationEndpointTests
IntegrationTests: Target AcceptInvitation transaction rollback integration test; current related proof: AccountMembershipTransactionEvidenceTests
ArchitectureGates: CrossContextApplicationDependencyTests; HandlerDataPortGateTests; AuthPipelineArchitectureTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: None beyond current milestone scope
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: AcceptInvitation source uses producer-owned Accounts seams; endpoint tests exist; transaction rollback proof remains target
```

### WG-FLOW-03 — Workspace membership fact to Collaboration activity

```text
FlowId: WG-FLOW-03
Team: TAC-RAP-WG
BoundedContext: Workspaces + Collaboration
Purpose: Project committed WorkspaceMemberAdded fact into Collaboration activity projection
Disposition: REUSE-EXISTING
EntryPoint: WorkspaceMemberAddedIntegrationEvent
ProductionReachability: current production source
SemanticOwner: Workspaces membership fact
WorkflowOwner: Collaboration activity projection
MutationAuthority: Activity projection state
SourceAuthority: WorkspaceMemberAdded event
TargetAuthority: Activity projection
Mechanisms: WorkspaceMember mutation; DomainEventInterceptor; WorkspaceEventMapper; outbox; OutboxDispatcher; MassTransit receive pipeline; TenantContextConsumeFilter; DeduplicationConsumeFilter; WorkspaceMemberAddedActivityConsumer; activity projection
FlowKind: CrossContext
SupportingShapes: PlatformDelivery; LocalProjection
Interactions:
  - EdgeId: E1
    Purpose: committed WorkspaceMemberAdded fact projects Collaboration Activity
    SourceBC: Workspaces
    TargetBC: Collaboration
    Mechanism: TAC-XC-C
    DecisionStatus: FROZEN
    ContractOwner: Workspaces
    PortOwner: NotApplicable
    WorkflowOwner: Collaboration activity projection
    MutationOwner: Collaboration
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: no
    ConsistencyModel: eventual
    CurrentBinding: platform-event-delivery
    RemoteReadiness: broker-native
    FutureDistributedBinding: broker-event
    TransactionBoundary: producer business state + outbox commit atomically; consumer-local transaction is independent
    OutboxRequired: yes
    ConsumerDedupRequired: yes
    IdempotencyKey: EventId/MessageId + consumer identity
    OrderingRequirement: Platform/event-stream rule plus producer revision where the event family requires ordering
    RetryOwner: Platform technical delivery retry
    BusinessFailureOwner: Collaboration Activity projection semantics
    TechnicalFailureOwner: Platform delivery runtime
    UnknownOutcomePolicy: delivery state/retry/dead-letter is Platform-owned; business reconciliation remains consumer/workflow-owned
    CompensationPolicy: none unless an owning Process Manager explicitly defines business compensation
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
RequestPipeline: MassTransit receive pipeline -> TenantContextConsumeFilter -> DeduplicationConsumeFilter -> consumer
Actor: System consumer
Authorization: TenantContextConsumeFilter restores Workspace tenant
AccountScope: Event AccountId required
WorkspaceScope: Event WorkspaceId required
ResourceScope: Workspace member activity
Idempotency: MessageId plus WorkspaceMemberAddedActivityConsumer identity through DeduplicationConsumeFilter
Concurrency: Dedup unique claim plus activity projection insert
Transaction: DeduplicationConsumeFilter transaction wraps tenant restoration, consumer SaveChanges, and dedup success
ApplicationPath: NotApplicable — Infrastructure consumer projection flow; producer Application path is Workspace membership mutation
DomainPath: NotApplicable — no Domain mutation required
PublicContract: workspace.member.added v1
ConsumerPort: NotApplicable — WorkspaceMemberAddedActivityConsumer is an Infrastructure inbound message Adapter
ACL: NotApplicable — local flow
InfrastructureAdapter: backend/src/Notrelix.Infrastructure/Messaging/Consumers/Collaboration/ActivityProjectionConsumers.cs
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: Workspace member activity
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: Platform/MassTransit delivery retry; deterministic tenant-envelope violations are non-retryable
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: WorkspaceMembershipEventReferenceTests
IntegrationTests: WorkspaceMembershipOutboxEvidenceTests; WorkspaceMemberAddedScopedTenantRuntimeChainIntegrationTests
ArchitectureGates: ScopedEventTenantEnvelopeArchitectureTests; PlatformReferenceCompletenessTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: None beyond current milestone scope
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Outbox evidence and full scoped tenant runtime-chain test prove producer/outbox/filter/consumer/projection
```

### WG-FLOW-04 — Governance local permission mutation

```text
FlowId: WG-FLOW-04
Team: TAC-RAP-WG
BoundedContext: Governance
Purpose: Grant resource permission for board and page ResourceKind variants
Disposition: REUSE-EXISTING
EntryPoint: GrantResourcePermissionCommand; RevokeResourcePermissionCommand
ProductionReachability: current production source
SemanticOwner: Governance
WorkflowOwner: Governance
MutationAuthority: ResourcePermission aggregate
SourceAuthority: Authenticated granter and target resource scope
TargetAuthority: ResourcePermission state
Mechanisms: GrantResourcePermissionCommandHandler; RevokeResourcePermissionCommandHandler (soft-delete of the active row); canonical Application request pipeline; ResourcePermission aggregate mutation; outbox enrollment
FlowKind: Mixed
SupportingShapes: LocalVerticalSlice; ResourceRef
Interactions:
  - EdgeId: E1
    Purpose: Governance resolves authoritative Board target facts for board permission variant
    SourceBC: Governance
    TargetBC: WorkManagement
    Mechanism: TAC-XC-A
    DecisionStatus: FROZEN
    ContractOwner: WorkManagement
    PortOwner: NotApplicable
    WorkflowOwner: Governance GrantResourcePermission(board)
    MutationOwner: NotApplicable
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: direct-in-process
    RemoteReadiness: semantic-only
    FutureDistributedBinding: undecided-by-design
    TransactionBoundary: current request read boundary; no foreign mutation
    OutboxRequired: no
    ConsumerDedupRequired: no
    IdempotencyKey: NotApplicable — read
    OrderingRequirement: NotApplicable
    RetryOwner: caller/current request; no message retry
    BusinessFailureOwner: WorkManagement authoritative read semantics
    TechnicalFailureOwner: current in-process request runtime
    UnknownOutcomePolicy: no transport unknown outcome in current binding; define timeout/availability policy at extraction
    CompensationPolicy: NotApplicable — read
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
  - EdgeId: E2
    Purpose: Governance resolves authoritative Page target facts for page permission variant
    SourceBC: Governance
    TargetBC: Documents
    Mechanism: TAC-XC-A
    DecisionStatus: FROZEN
    ContractOwner: Documents
    PortOwner: NotApplicable
    WorkflowOwner: Governance GrantResourcePermission(page)
    MutationOwner: NotApplicable
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: direct-in-process
    RemoteReadiness: semantic-only
    FutureDistributedBinding: undecided-by-design
    TransactionBoundary: current request read boundary; no foreign mutation
    OutboxRequired: no
    ConsumerDedupRequired: no
    IdempotencyKey: NotApplicable — read
    OrderingRequirement: NotApplicable
    RetryOwner: caller/current request; no message retry
    BusinessFailureOwner: Documents authoritative read semantics
    TechnicalFailureOwner: current in-process request runtime
    UnknownOutcomePolicy: no transport unknown outcome in current binding; define timeout/availability policy at extraction
    CompensationPolicy: NotApplicable — read
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated granter
Authorization: Granter authority and target-specific PermissionAction
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: work-management.board or documents.page
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Governance aggregate version where required
Transaction: Request transaction owns durable mutation and required outbox enrollment
ApplicationPath: backend/src/Notrelix.Application/Features/Governance/ResourcePermissions/Commands/GrantResourcePermission/GrantResourcePermissionCommand.cs
DomainPath: ResourcePermission grant behavior
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: ApplicationDbContext persistence adapter
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: ResourcePermissionGranted event
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: CanonicalKindValidatorsTests; ResourcePermissionTests
IntegrationTests: Target GrantResourcePermission board/page variant integration tests; RevokeResourcePermission authorization/ceiling integration tests (41D2)
ArchitectureGates: AuthPipelineArchitectureTests; WorkspaceScopedArchitectureTests; UseCaseSecurityClassificationTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: None beyond current milestone scope
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: grant/revoke commands + validator tests; board/page variant runtime proof closed at M5 Wave 5 (TAC-WG-FLOW-04 41D/41D2, GovernanceResourcePermissionFlowTests on real PostgreSQL); Page ACL semantics frozen by the M5 decision below
```

### WG-FLOW-05 — Governance local authoritative query

```text
FlowId: WG-FLOW-05
Team: TAC-RAP-WG
BoundedContext: Governance
Purpose: Read Governance permission state without joining foreign target aggregates
Disposition: REUSE-EXISTING
EntryPoint: GetResourcePermissionsQuery
ProductionReachability: current production source
SemanticOwner: Governance
WorkflowOwner: Governance read
MutationAuthority: NotApplicable — read flow
SourceAuthority: Governance persistence
TargetAuthority: Permission read model
Mechanisms: GetResourcePermissionsQueryHandler; canonical Application query pipeline; Governance read model query
FlowKind: Local
SupportingShapes: LocalVerticalSlice
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated principal
Authorization: Protected permission-read authorization
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: Resource permissions
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Request transaction owns durable mutation and required outbox enrollment
ApplicationPath: backend/src/Notrelix.Application/Features/Governance/ResourcePermissions/Queries/GetResourcePermissions/GetResourcePermissionsQuery.cs
DomainPath: NotApplicable — query flow
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: ApplicationDbContext persistence adapter
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: CanonicalKindValidatorsTests
IntegrationTests: Target Governance permission read integration test
ArchitectureGates: DbContextBoundaryArchitectureTests; UseCaseSecurityClassificationTests
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: None beyond current milestone scope
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Governance query source and CanonicalKindValidatorsTests exist
```

### WG-FLOW-06 — Canonical authorization pipeline

```text
FlowId: WG-FLOW-06
Team: TAC-RAP-WG
BoundedContext: Governance + Application pipeline
Purpose: Prove one authorization engine denies protected mutations before effects for pinned cases
Disposition: REUSE-EXISTING
EntryPoint: AccessControlBehavior
ProductionReachability: current production source
SemanticOwner: Application authorization pipeline
WorkflowOwner: Each protected use case
MutationAuthority: Protected handler after authorization
SourceAuthority: ExecutionContext and AccessFacts
TargetAuthority: Authorization decision
Mechanisms: MediatR pipeline behaviors; AccessControlBehavior; ExecutionContextBehavior; DataSessionBehavior; IdempotencyBehavior
FlowKind: Supporting
SupportingShapes: CompositeRead
Interactions: [] — standard authorization pipeline is a supporting composite-read flow; exact TAC-XC-A source-fact edges are recorded in the pinned protected business Flow Cards
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated or system principal
Authorization: PermissionAction and ResourceRef policy
AccountScope: Resolved by request scope
WorkspaceScope: Resolved by request scope
ResourceScope: Request ResourceRef
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Request transaction owns durable mutation and required outbox enrollment
ApplicationPath: backend/src/Notrelix.Application/Common/Behaviors/AccessControlBehavior.cs
DomainPath: NotApplicable — pipeline flow
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: ApplicationDbContext persistence adapter
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: AccessControlBehaviorTests; PipelineOrderTests
IntegrationTests: WorkspaceCreationPipelineAuthorizationTests; PipelineTelemetryIntegrationTests
ArchitectureGates: AuthPipelineArchitectureTests; HandlerAuthorizationBypassArchitectureTests; PipelineFreezeArchitectureTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: None beyond current milestone scope
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Pipeline authorization tests pass on current branch
```

### WM-FLOW-01 — Local pipeline-first mutation

```text
FlowId: WM-FLOW-01
Team: TAC-RAP-WM
BoundedContext: WorkManagement
Purpose: Create Board in Workspace through canonical pipeline and Domain aggregate
Disposition: REUSE-EXISTING
EntryPoint: CreateBoardInWorkspaceCommand
ProductionReachability: current production source
SemanticOwner: WorkManagement
WorkflowOwner: WorkManagement
MutationAuthority: Board aggregate
SourceAuthority: Authenticated Workspace scope
TargetAuthority: Board state
Mechanisms: CreateBoardInWorkspaceCommandHandler; canonical Application request pipeline; Board.Create aggregate mutation; outbox enrollment
FlowKind: Local
SupportingShapes: LocalVerticalSlice
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated Workspace member
Authorization: Workspace permission
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: Board
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Request transaction owns durable mutation and required outbox enrollment
ApplicationPath: backend/src/Notrelix.Application/Features/WorkManagement/Boards/Commands/CreateBoardInWorkspace/CreateBoardInWorkspace.cs
DomainPath: Board.Create
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: ApplicationDbContext persistence adapter
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: board.created
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: CreateBoardInWorkspaceTests; BoardTests
IntegrationTests: CreateBoardInWorkspaceCommandHandlerTests
ArchitectureGates: AuthPipelineArchitectureTests; WorkspaceScopedArchitectureTests; PublicEventContractArchitectureTests
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: None beyond current milestone scope
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: CreateBoardInWorkspace source and handler tests exist
```

### WM-FLOW-02 — Consumer-owned Collaboration read

```text
FlowId: WM-FLOW-02
Team: TAC-RAP-WM
BoundedContext: WorkManagement + Collaboration
Purpose: WorkManagement consumes Collaboration through consumer-owned read port
Disposition: REUSE-EXISTING
EntryPoint: IWorkManagementCollaborationReadPort
ProductionReachability: current production source
SemanticOwner: Collaboration data
WorkflowOwner: WorkManagement query
MutationAuthority: NotApplicable — read flow
SourceAuthority: Collaboration persistence
TargetAuthority: WorkManagement read model
Mechanisms: WorkManagement query/use case; IWorkManagementCollaborationReadPort; WorkManagementCollaborationReadAdapter; Collaboration DbContext read
FlowKind: CrossContext
SupportingShapes: CompositeRead
Interactions:
  - EdgeId: E1
    Purpose: WorkManagement consumes Collaboration data through a consumer-owned read Port
    SourceBC: WorkManagement
    TargetBC: Collaboration
    Mechanism: TAC-XC-B
    DecisionStatus: CLOSED-FROZEN — Collaboration/Public/ResourceSummary is the producer boundary; remote binding deferred-by-design
    ContractOwner: Collaboration producer-owned semantic boundary
    PortOwner: WorkManagement
    WorkflowOwner: WorkManagement query
    MutationOwner: NotApplicable
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: in-process-adapter
    RemoteReadiness: deferred-by-design
    FutureDistributedBinding: remote-query-adapter
    TransactionBoundary: current request boundary; adapter owns no transaction
    OutboxRequired: no
    ConsumerDedupRequired: no
    IdempotencyKey: NotApplicable — read unless target operation declares it
    OrderingRequirement: NotApplicable
    RetryOwner: caller/current request; remote retry only after explicit policy
    BusinessFailureOwner: Collaboration producer semantics; consumer maps result
    TechnicalFailureOwner: WorkManagement Infrastructure adapter; current direct Collaboration persistence read is migration debt and not the copy-model
    UnknownOutcomePolicy: remote adapter policy is defined at extraction; current source must first stop depending on foreign DbContext
    CompensationPolicy: NotApplicable unless target mutation is separately classified TAC-XC-E
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated Workspace member
Authorization: WorkManagement protected query
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: BoardItem comments or related collaboration data
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Request transaction owns durable mutation and required outbox enrollment
ApplicationPath: backend/src/Notrelix.Application/Features/WorkManagement/Ports/Collaboration/IWorkManagementCollaborationReadPort.cs
DomainPath: NotApplicable — no Domain mutation required
PublicContract: Collaboration producer-owned semantic read boundary required by M6 normalization; the consumer Port is not Public
ConsumerPort: IWorkManagementCollaborationReadPort
ACL: NotApplicable unless a pure WorkManagement-owned semantic mapper is required; WorkManagementCollaborationReadAdapter is the runtime Adapter, not ACL
InfrastructureAdapter: backend/src/Notrelix.Infrastructure/CrossContext/WorkManagement/Collaboration/WorkManagementCollaborationReadAdapter.cs
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: Target WorkManagement Collaboration read port behavior tests; current adapter proof: WorkManagementCollaborationReadAdapterTests
IntegrationTests: WorkManagementCollaborationReadAdapterTests
ArchitectureGates: CrossContextApplicationDependencyTests; BoundedContextPortArchitectureTests; DbContextBoundaryArchitectureTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: Current WorkManagementCollaborationReadAdapter reads Collaboration persistence directly; M6 must normalize to producer-owned semantic source behind the consumer Port before this becomes the copy-model
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Read port and adapter exist with WorkManagementCollaborationReadAdapterTests
```

### WM-FLOW-03 — Producer target action

```text
FlowId: WM-FLOW-03
Team: TAC-RAP-WM
BoundedContext: WorkManagement
Purpose: Expose WorkManagement MoveItem target action for Automation with scope, auth, and dedup
Disposition: HARDEN-EXISTING
EntryPoint: IWorkItemActions or Work target action
ProductionReachability: current production source
SemanticOwner: WorkManagement
WorkflowOwner: Automation execution
MutationAuthority: BoardItem aggregate
SourceAuthority: Automation operation identity
TargetAuthority: BoardItem placement and events
Mechanisms: IWorkItemActions target action; WorkItemActions; MoveBoardItemUseCase; WorkItemActionAdapter; target authorization/scope/idempotency
FlowKind: CrossContext
SupportingShapes: None
Interactions:
  - EdgeId: E1
    Purpose: Automation requests WorkManagement-owned MoveItem mutation
    SourceBC: Automation
    TargetBC: WorkManagement
    Mechanism: TAC-XC-E
    DecisionStatus: FROZEN
    ContractOwner: WorkManagement
    PortOwner: NotApplicable
    WorkflowOwner: Automation action execution
    MutationOwner: WorkManagement
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: in-process-adapter
    RemoteReadiness: adapter-ready
    FutureDistributedBinding: remote-command-adapter
    TransactionBoundary: WorkManagement owns target mutation transaction
    OutboxRequired: target-owned if outward fact is part of target mutation
    ConsumerDedupRequired: target-defined where operation identity exists
    IdempotencyKey: OperationId enforced/deduplicated by WorkManagement target
    OrderingRequirement: target aggregate/version/concurrency rules
    RetryOwner: caller/current request; target owns semantic idempotency
    BusinessFailureOwner: WorkManagement target mutation owner
    TechnicalFailureOwner: current in-process request runtime; future remote adapter/runtime
    UnknownOutcomePolicy: future remote retry uses Work target OperationId; conflicting duplicate returns deterministic conflict
    CompensationPolicy: none unless workflow/product explicitly defines it
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Automation executor principal
Authorization: Target Workspace permission and scope
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: BoardItem
Idempotency: LogicalOperationId required for retryable target action
Concurrency: Expected version or placement ordering guard
Transaction: Request transaction owns durable mutation and required outbox enrollment
ApplicationPath: backend/src/Notrelix.Application/Features/WorkManagement/BoardItems/Services/WorkItemActions.cs; backend/src/Notrelix.Application/Features/WorkManagement/BoardItems/Services/MoveBoardItemUseCase.cs
DomainPath: NotApplicable — no Domain mutation required
PublicContract: WorkManagement.Public target action
ConsumerPort: Automation-owned IWorkActionPort — consumer-side Port referenced by this producer target-action teaching flow
ACL: Automation-owned pure MoveItem semantic mapper required by TAC-AI-011; WorkItemActionAdapter is runtime Adapter, not ACL
InfrastructureAdapter: Automation WorkItemActionAdapter for the cross-context runtime; WorkManagement persistence remains target-owned behind producer Application
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: board_item.moved and placement facts
Projection: NotApplicable — no projection required
ProcessState: AutomationExecution
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: WorkItemActionsTests; MoveBoardItemTests
IntegrationTests: AutomationWorkActionChainIntegrationTests
ArchitectureGates: IdempotencyOperationArchitectureTests; AuthPipelineArchitectureTests; CrossContextApplicationDependencyTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: Scope, auth, and dedup hardening is tracked by M6
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Current source has target action and AutomationWorkActionChainIntegrationTests; hardening remains M6
```

### WM-FLOW-04 — Producer Integration Event

```text
FlowId: WM-FLOW-04
Team: TAC-RAP-WM
BoundedContext: WorkManagement
Purpose: Emit WorkManagement outward facts with authoritative tenant envelope
Disposition: REUSE-EXISTING
EntryPoint: Board and BoardItem Domain events mapped to IntegrationEvents
ProductionReachability: current production source
SemanticOwner: WorkManagement
WorkflowOwner: WorkManagement producer
MutationAuthority: NotApplicable — event production
SourceAuthority: WorkManagement Domain events
TargetAuthority: Integration event contracts
Mechanisms: WorkManagement Domain events; BoardEventMapper; DomainEventInterceptor; MessagingOutboxMessage; IntegrationEventCatalog; OutboxDispatcher
FlowKind: CrossContext
SupportingShapes: PlatformDelivery
Interactions:
  - EdgeId: E1
    Purpose: committed WorkManagement fact triggers Automation reaction
    SourceBC: WorkManagement
    TargetBC: Automation
    Mechanism: TAC-XC-C
    DecisionStatus: FROZEN
    ContractOwner: WorkManagement
    PortOwner: NotApplicable
    WorkflowOwner: Automation process
    MutationOwner: Automation
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: no
    ConsistencyModel: eventual
    CurrentBinding: platform-event-delivery
    RemoteReadiness: broker-native
    FutureDistributedBinding: broker-event
    TransactionBoundary: producer business state + outbox commit atomically; consumer-local transaction is independent
    OutboxRequired: yes
    ConsumerDedupRequired: yes
    IdempotencyKey: EventId/MessageId + consumer identity
    OrderingRequirement: Platform/event-stream rule plus producer revision where the event family requires ordering
    RetryOwner: Platform technical delivery retry
    BusinessFailureOwner: Automation consumer reaction owner
    TechnicalFailureOwner: Platform delivery runtime
    UnknownOutcomePolicy: delivery state/retry/dead-letter is Platform-owned; business reconciliation remains consumer/workflow-owned
    CompensationPolicy: none unless an owning Process Manager explicitly defines business compensation
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
  - EdgeId: E2
    Purpose: committed WorkManagement fact updates Analytics projection
    SourceBC: WorkManagement
    TargetBC: Analytics/Reporting
    Mechanism: TAC-XC-C
    DecisionStatus: FROZEN
    ContractOwner: WorkManagement
    PortOwner: NotApplicable
    WorkflowOwner: Analytics projection update
    MutationOwner: Analytics/Reporting
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: no
    ConsistencyModel: eventual
    CurrentBinding: platform-event-delivery
    RemoteReadiness: broker-native
    FutureDistributedBinding: broker-event
    TransactionBoundary: producer business state + outbox commit atomically; consumer-local transaction is independent
    OutboxRequired: yes
    ConsumerDedupRequired: yes
    IdempotencyKey: EventId/MessageId + consumer identity
    OrderingRequirement: Platform/event-stream rule plus producer revision where the event family requires ordering
    RetryOwner: Platform technical delivery retry
    BusinessFailureOwner: Analytics/Reporting consumer reaction owner
    TechnicalFailureOwner: Platform delivery runtime
    UnknownOutcomePolicy: delivery state/retry/dead-letter is Platform-owned; business reconciliation remains consumer/workflow-owned
    CompensationPolicy: none unless an owning Process Manager explicitly defines business compensation
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
RequestPipeline: producer SaveChanges -> DomainEventInterceptor -> outbox commit -> OutboxDispatcher -> IntegrationEventBus -> MassTransit receive pipeline
Actor: System producer
Authorization: NotApplicable — producer emits committed facts
AccountScope: Event AccountId required
WorkspaceScope: Event WorkspaceId required
ResourceScope: Board or BoardItem
Idempotency: Outbox EventId plus consumer identity; dispatcher dedup by OutboxDispatcher consumer name
Concurrency: Aggregate version for source mutation; outbox claim lease for dispatch
Transaction: Outbox enrollment commits atomically with WorkManagement Domain mutation
ApplicationPath: backend/src/Notrelix.Application/EventMappers/WorkManagement/BoardEventMapper.cs
DomainPath: NotApplicable — no Domain mutation required
PublicContract: WorkManagement integration event contracts
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: backend/src/Notrelix.Infrastructure/Data/Interceptors/DomainEventInterceptor.cs
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: board.*, board_item.*, field, label, checklist, view events
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: OutboxDispatcher retry for dispatch failure; MassTransit retry for consumer delivery failure
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: BoardEventTests; BoardItemEventTests
IntegrationTests: OutboxAtomicityTests; DomainEventInterceptorTests
ArchitectureGates: ScopedEventTenantEnvelopeArchitectureTests; PublicEventContractArchitectureTests; IntegrationEventOwnershipArchitectureTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: None beyond current milestone scope
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: WorkManagement events classified Workspace and mappers preserve AccountId
```

### WM-FLOW-05 — Producer projection-source query

```text
FlowId: WM-FLOW-05
Team: TAC-RAP-WM
BoundedContext: WorkManagement + Analytics
Purpose: Expose producer-owned Work projection source for Analytics rebuild
Disposition: MIGRATE-EXISTING
EntryPoint: IWorkItemProjectionSource
ProductionReachability: current production source
SemanticOwner: WorkManagement
WorkflowOwner: Analytics rebuild
MutationAuthority: NotApplicable — read flow
SourceAuthority: WorkManagement persistence
TargetAuthority: Projection snapshot
Mechanisms: IWorkItemProjectionSource (workspace snapshot + item lookup); WorkItemProjectionSourceAdapter (runtime Adapter only); WorkManagement query owner implementation; Analytics rebuild consumer
FlowKind: CrossContext
SupportingShapes: CompositeRead; LocalProjection
Interactions:
  - EdgeId: E1
    Purpose: Analytics rebuild obtains producer-owned Work placement snapshot through a consumer runtime seam
    SourceBC: Analytics/Reporting
    TargetBC: WorkManagement
    Mechanism: TAC-XC-B
    DecisionStatus: CLOSED-FROZEN — M10-PORT-OWNERSHIP-NORMALIZATION resolved: Analytics-owned consumer Port → Infrastructure adapter → WorkManagement.Public IWorkItemProjectionSource → WorkManagement Application implementation → IWorkManagementDbContext
    ContractOwner: WorkManagement.Public projection-source semantic
    PortOwner: Analytics/Reporting Application source/rebuild Port
    WorkflowOwner: Analytics projection rebuild
    MutationOwner: NotApplicable
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: in-process-adapter
    RemoteReadiness: deferred-by-design
    FutureDistributedBinding: remote-query-adapter
    TransactionBoundary: current request boundary; adapter owns no transaction
    OutboxRequired: no
    ConsumerDedupRequired: no
    IdempotencyKey: NotApplicable — read unless target operation declares it
    OrderingRequirement: NotApplicable
    RetryOwner: caller/current request; remote retry only after explicit policy
    BusinessFailureOwner: WorkManagement producer semantics; consumer maps result
    TechnicalFailureOwner: Analytics Infrastructure adapter delegates to producer-owned WorkManagement Application/Public implementation; direct Work DbContext read is forbidden
    UnknownOutcomePolicy: rebuild fails/retries according to Analytics recovery semantics; remote source timeout policy frozen in M10
    CompensationPolicy: NotApplicable unless target mutation is separately classified TAC-XC-E
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: System rebuild consumer
Authorization: System internal query with explicit Workspace scope
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: Work items
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Request transaction owns durable mutation and required outbox enrollment
ApplicationPath: backend/src/Notrelix.Application/Features/WorkManagement/Public/ItemPlacement/IWorkItemProjectionSource.cs (contract); WorkManagement Application implementation reads IWorkManagementDbContext
DomainPath: NotApplicable — no Domain mutation required
PublicContract: WorkManagement.Public queries
ConsumerPort: Analytics/Reporting Application source Port — IWorkItemProjectionSourceAdapter (Application-owned port role) reaches the producer Public contract via the Infrastructure adapter
ACL: NotApplicable unless M10 introduces a pure Analytics semantic mapper; WorkItemProjectionSourceAdapter is runtime Adapter, not ACL
InfrastructureAdapter: backend/src/Notrelix.Infrastructure/CrossContext/Analytics/WorkManagement/WorkItemProjectionSourceAdapter.cs
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: Workspace work item placement
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: Target projection source behavior tests; current proof: WorkspacePlacementProjectionIntegrationTests
IntegrationTests: WorkspacePlacementProjectionIntegrationTests
ArchitectureGates: CrossContextPersistenceBoundaryTests; DbContextBoundaryArchitectureTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: Resolved in M10 — WorkManagement Application implements IWorkItemProjectionSource (workspace snapshot + item lookup) over IWorkManagementDbContext; Analytics adapter delegates; Analytics direct Work DbContext access remains forbidden
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Projection source port and adapter exist with WorkspacePlacementProjectionIntegrationTests
```

### DC-FLOW-01 — Documents local creation

```text
FlowId: DC-FLOW-01
Team: TAC-RAP-DC
BoundedContext: Documents
Purpose: Create Page through Documents local mutation
Disposition: HARDEN-EXISTING
EntryPoint: CreatePageCommand
ProductionReachability: current production source
SemanticOwner: Documents
WorkflowOwner: Documents
MutationAuthority: Page aggregate
SourceAuthority: Authenticated Workspace scope
TargetAuthority: Page state
Mechanisms: CreatePageCommandHandler; canonical Application request pipeline; Page.Create aggregate mutation; outbox enrollment
FlowKind: Local
SupportingShapes: LocalVerticalSlice; ResourceRef
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated Workspace member
Authorization: M2G target PermissionAction.CreatePage on workspace resource
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: Workspace page creation
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Request transaction owns durable mutation and required outbox enrollment
ApplicationPath: backend/src/Notrelix.Application/Features/Documents/Pages/Commands/CreatePage/CreatePage.cs
DomainPath: Page.Create
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: ApplicationDbContext persistence adapter
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: page.created
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: PageTests
IntegrationTests: Target CreatePage integration tests
ArchitectureGates: AuthPipelineArchitectureTests; UseCaseSecurityClassificationTests; ScopedEventTenantEnvelopeArchitectureTests
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: Current command still uses ManageBoard action and needs M2G policy change
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: CreatePage handler exists; permission action hardening remains M7
```

### DC-FLOW-02 — Documents lifecycle mutation

```text
FlowId: DC-FLOW-02
Team: TAC-RAP-DC
BoundedContext: Documents
Purpose: Archive Page through Documents lifecycle mutation
Disposition: IMPLEMENT-MISSING
EntryPoint: ArchivePageCommand target handler
ProductionReachability: command exists but handler throws NotImplementedException
SemanticOwner: Documents
WorkflowOwner: Documents
MutationAuthority: Page aggregate
SourceAuthority: Authenticated Page scope
TargetAuthority: Page archived state
Mechanisms: ArchivePageCommand target handler; canonical Application request pipeline; Page.Archive aggregate mutation; outbox enrollment
FlowKind: Local
SupportingShapes: LocalVerticalSlice; ResourceRef
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated Page manager
Authorization: Page lifecycle permission
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: Page
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Request transaction owns durable mutation and required outbox enrollment
ApplicationPath: backend/src/Notrelix.Application/Features/Documents/Pages/Commands/ArchivePage/ArchivePage.cs
DomainPath: Page.Archive
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: ApplicationDbContext persistence adapter
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: page.archived
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: PageTests
IntegrationTests: Target ArchivePage integration tests
ArchitectureGates: CommandMarkerArchitectureTests; AuthPipelineArchitectureTests
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: Handler is stub while Domain Page.Archive exists
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: M0 source audit confirms stub handler and Domain Page.Archive
```

### DC-FLOW-03 — Collaboration comment on Work item

```text
FlowId: DC-FLOW-03
Team: TAC-RAP-DC
BoundedContext: Collaboration + WorkManagement
Purpose: Create comment targeting BoardItem with target-specific authorization
Disposition: HARDEN-EXISTING
EntryPoint: CreateCommentCommand.ForBoardItem
ProductionReachability: current production source
SemanticOwner: Collaboration
WorkflowOwner: Collaboration
MutationAuthority: Comment aggregate
SourceAuthority: BoardItem target reference
TargetAuthority: Comment state
Mechanisms: CreateCommentCommandHandler.ForBoardItem; canonical Application request pipeline; Comment.Create aggregate mutation; outbox enrollment
FlowKind: Mixed
SupportingShapes: LocalVerticalSlice; ResourceRef
Interactions:
  - EdgeId: E1
    Purpose: Collaboration resolves authoritative BoardItem target facts/authorization source without navigating Work aggregate
    SourceBC: Collaboration
    TargetBC: WorkManagement
    Mechanism: TAC-XC-A
    DecisionStatus: FROZEN
    ContractOwner: WorkManagement
    PortOwner: NotApplicable
    WorkflowOwner: CreateComment.ForBoardItem
    MutationOwner: NotApplicable
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: direct-in-process
    RemoteReadiness: semantic-only
    FutureDistributedBinding: undecided-by-design
    TransactionBoundary: current request read boundary; no foreign mutation
    OutboxRequired: no
    ConsumerDedupRequired: no
    IdempotencyKey: NotApplicable — read
    OrderingRequirement: NotApplicable
    RetryOwner: caller/current request; no message retry
    BusinessFailureOwner: WorkManagement authoritative read semantics
    TechnicalFailureOwner: current in-process request runtime
    UnknownOutcomePolicy: no transport unknown outcome in current binding; define timeout/availability policy at extraction
    CompensationPolicy: NotApplicable — read
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated Workspace member
Authorization: M2G target PermissionAction.CreateComment with BoardItem ResourceKind
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: BoardItem comment
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Request transaction owns durable mutation and required outbox enrollment
ApplicationPath: backend/src/Notrelix.Application/Features/Collaboration/Comments/Commands/CreateComment/CreateComment.cs
DomainPath: Comment.Create
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: ApplicationDbContext persistence adapter
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: comment.created
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: CreateCommentResourceRefTests; ResourceKindFactoriesTests; CommentTests
IntegrationTests: Target CreateComment.ForBoardItem integration tests
ArchitectureGates: AuthPipelineArchitectureTests; UseCaseSecurityClassificationTests; ScopedEventTenantEnvelopeArchitectureTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: Current action uses ManageBoard and needs intended policy matrix
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: CreateComment handler exists; M2G permission hardening remains M7
```

### DC-FLOW-04 — Collaboration comment on Document Page

```text
FlowId: DC-FLOW-04
Team: TAC-RAP-DC
BoundedContext: Collaboration + Documents
Purpose: Create comment targeting Page with target-specific authorization
Disposition: HARDEN-EXISTING
EntryPoint: CreateCommentCommand.ForPage
ProductionReachability: current production source
SemanticOwner: Collaboration
WorkflowOwner: Collaboration
MutationAuthority: Comment aggregate
SourceAuthority: Page target reference
TargetAuthority: Comment state
Mechanisms: CreateCommentCommandHandler.ForPage; canonical Application request pipeline; Comment.Create aggregate mutation; outbox enrollment
FlowKind: Mixed
SupportingShapes: LocalVerticalSlice; ResourceRef
Interactions:
  - EdgeId: E1
    Purpose: Collaboration resolves authoritative Page target facts/authorization source without navigating Document aggregate
    SourceBC: Collaboration
    TargetBC: Documents
    Mechanism: TAC-XC-A
    DecisionStatus: FROZEN
    ContractOwner: Documents
    PortOwner: NotApplicable
    WorkflowOwner: CreateComment.ForPage
    MutationOwner: NotApplicable
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: direct-in-process
    RemoteReadiness: semantic-only
    FutureDistributedBinding: undecided-by-design
    TransactionBoundary: current request read boundary; no foreign mutation
    OutboxRequired: no
    ConsumerDedupRequired: no
    IdempotencyKey: NotApplicable — read
    OrderingRequirement: NotApplicable
    RetryOwner: caller/current request; no message retry
    BusinessFailureOwner: Documents authoritative read semantics
    TechnicalFailureOwner: current in-process request runtime
    UnknownOutcomePolicy: no transport unknown outcome in current binding; define timeout/availability policy at extraction
    CompensationPolicy: NotApplicable — read
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated Workspace member
Authorization: M2G target PermissionAction.CreateComment with Page ResourceKind
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: Page comment
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Request transaction owns durable mutation and required outbox enrollment
ApplicationPath: backend/src/Notrelix.Application/Features/Collaboration/Comments/Commands/CreateComment/CreateComment.cs
DomainPath: Comment.Create
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: ApplicationDbContext persistence adapter
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: comment.created
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: CreateCommentResourceRefTests; ResourceKindFactoriesTests
IntegrationTests: Target CreateComment.ForPage integration tests
ArchitectureGates: AuthPipelineArchitectureTests; UseCaseSecurityClassificationTests; ScopedEventTenantEnvelopeArchitectureTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: Page variant must not reuse Board policy blindly
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: ForPage factory exists; policy hardening remains M7
```

### DC-FLOW-05 — Collaboration Activity projection

```text
FlowId: DC-FLOW-05
Team: TAC-RAP-DC
BoundedContext: Collaboration
Purpose: Project comment and mention facts into Activity projection
Disposition: HARDEN-EXISTING
EntryPoint: CommentCreated and MentionCreated activity consumers
ProductionReachability: current production source
SemanticOwner: Collaboration facts
WorkflowOwner: Activity projection
MutationAuthority: Activity projection state
SourceAuthority: Collaboration integration events
TargetAuthority: Activity projection
Mechanisms: Collaboration integration events; MassTransit receive pipeline; TenantContextConsumeFilter; DeduplicationConsumeFilter; CommentCreatedActivityConsumer and MentionCreatedActivityConsumer; activity projection
FlowKind: Local
SupportingShapes: LocalProjection; PlatformDelivery
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: MassTransit receive pipeline -> TenantContextConsumeFilter -> DeduplicationConsumeFilter -> consumer
Actor: System consumer
Authorization: TenantContextConsumeFilter restores Workspace tenant
AccountScope: Event AccountId required
WorkspaceScope: Event WorkspaceId required
ResourceScope: Activity stream
Idempotency: MessageId plus activity consumer identity through DeduplicationConsumeFilter
Concurrency: Dedup unique claim plus activity projection insert
Transaction: DeduplicationConsumeFilter transaction wraps consumer projection SaveChanges and dedup success
ApplicationPath: NotApplicable — Infrastructure activity projection consumers
DomainPath: NotApplicable — no Domain mutation required
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — Activity consumers are Infrastructure inbound message Adapters, not Application Ports
ACL: NotApplicable — local flow
InfrastructureAdapter: backend/src/Notrelix.Infrastructure/Messaging/Consumers/Collaboration/ActivityProjectionConsumers.cs
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: Activity
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: Platform/MassTransit delivery retry; deterministic tenant-envelope violations are non-retryable
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: Target activity consumer tests; current proof: WorkspaceMembershipEventReferenceTests
IntegrationTests: WorkspaceMemberAddedScopedTenantRuntimeChainIntegrationTests
ArchitectureGates: ScopedEventTenantEnvelopeArchitectureTests; PlatformReferenceCompletenessTests
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: Mention actor and tenant envelope hardening remains for M7
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Activity consumers exist; Mention actor hardening remains M7
```

### DC-FLOW-06 — Collaboration Mention to Notification

```text
FlowId: DC-FLOW-06
Team: TAC-RAP-DC
BoundedContext: Collaboration
Purpose: Create authoritative Mention fact and project Collaboration-owned notification feature/projection without confusing mentioned user with mentioner
Disposition: IMPLEMENT-MISSING
EntryPoint: MentionCreatedDomainEvent and MentionCreatedNotificationConsumer
ProductionReachability: current Mention mapper has actor defect and notification consumer materializes empty tenant
SemanticOwner: Collaboration Mention; Notifications is a Collaboration-owned feature/projection here, not a canonical BC
WorkflowOwner: Notification projection
MutationAuthority: Mention aggregate and notification projection
SourceAuthority: Trusted request actor at mention creation
TargetAuthority: Notification state
Mechanisms: MentionCreatedDomainEvent; CommentEventMapper; MassTransit receive pipeline; MentionCreatedNotificationConsumer; notification projection
FlowKind: Local
SupportingShapes: LocalProjection; PlatformDelivery
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: MassTransit receive pipeline -> TenantContextConsumeFilter -> DeduplicationConsumeFilter -> consumer
Actor: Mentioning user
Authorization: Comment or mention creation permission
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: Mention target
Idempotency: MessageId plus MentionCreatedNotificationConsumer identity through DeduplicationConsumeFilter
Concurrency: Dedup unique claim plus notification projection insert
Transaction: DeduplicationConsumeFilter transaction wraps notification projection SaveChanges and dedup success
ApplicationPath: backend/src/Notrelix.Application/Features/Collaboration/Mentions
DomainPath: Mention creation with MentionedByUserId fact
PublicContract: mention.created v1
ConsumerPort: NotApplicable — MentionCreatedNotificationConsumer is an Infrastructure inbound message Adapter
ACL: NotApplicable — local flow
InfrastructureAdapter: backend/src/Notrelix.Infrastructure/Messaging/Consumers/Collaboration/MentionCreatedNotificationConsumer.cs
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: mention.created
Projection: NotApplicable — no projection required
ProcessState: Notification delivery state
FailureClasses: validation; authorization; not-found; target unavailable; notification provider unknown outcome
RetryOwner: Platform/MassTransit delivery retry; notification provider unknown outcome requires reconciliation
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: MentionTests; Target Mention actor mapper tests
IntegrationTests: Target MentionCreated notification runtime-chain test
ArchitectureGates: ScopedEventTenantEnvelopeArchitectureTests; LegacyCollabNotificationWriteBanTests
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: MentionedByUserId currently maps MentionedId and ActorUserId is null
STOPConditions: None — M2F freezes authoritative Mention actor
CompletionEvidence: M0 source audit confirms mapper defect
```

### DC-FLOW-07 — Documents outward event producer

```text
FlowId: DC-FLOW-07
Team: TAC-RAP-DC
BoundedContext: Documents
Purpose: Classify Documents outward event producers and stub consumers without treating stubs as business flows
Disposition: HARDEN-EXISTING
EntryPoint: PageCreated and PageArchived integration events
ProductionReachability: current production source
SemanticOwner: Documents
WorkflowOwner: Documents producer
MutationAuthority: NotApplicable — event production
SourceAuthority: Documents Domain events
TargetAuthority: Integration event contracts
Mechanisms: Page Domain events; PageEventMapper; DomainEventInterceptor; MessagingOutboxMessage; IntegrationEventCatalog
FlowKind: Local
SupportingShapes: PlatformDelivery
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: producer SaveChanges -> DomainEventInterceptor -> outbox commit -> OutboxDispatcher -> IntegrationEventBus -> MassTransit receive pipeline
Actor: System producer
Authorization: NotApplicable — producer emits committed facts
AccountScope: Event AccountId required
WorkspaceScope: Event WorkspaceId required
ResourceScope: Page
Idempotency: Outbox EventId plus consumer identity; dispatcher dedup by OutboxDispatcher consumer name
Concurrency: Aggregate version for Page mutation; outbox claim lease for dispatch
Transaction: Outbox enrollment commits atomically with Page mutation
ApplicationPath: backend/src/Notrelix.Application/EventMappers/Documents/PageEventMapper.cs
DomainPath: NotApplicable — no Domain mutation required
PublicContract: page.created and page.archived
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: backend/src/Notrelix.Infrastructure/Data/Interceptors/DomainEventInterceptor.cs
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: page.created and page.archived
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: OutboxDispatcher retry for dispatch failure; MassTransit retry for consumer delivery failure
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: PageTests; ScopedEventTenantEnvelopeReplayCompatibilityTests
IntegrationTests: OutboxAtomicityTests; DomainEventInterceptorTests
ArchitectureGates: ScopedEventTenantEnvelopeArchitectureTests; PublicEventContractArchitectureTests; ConsumerRegistryCompletenessTests
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: Stub consumers must remain classified as stubs
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Page events classified Workspace and manifest generated
```

### AI-FLOW-01 — Automation local rule creation

```text
FlowId: AI-FLOW-01
Team: TAC-RAP-AI
BoundedContext: Automation
Purpose: Create AutomationRule with Billing capacity dependency and Workspace scope
Disposition: HARDEN-EXISTING
EntryPoint: CreateAutomationRuleCommand
ProductionReachability: current production source
SemanticOwner: Automation
WorkflowOwner: Automation rule lifecycle
MutationAuthority: AutomationRule aggregate
SourceAuthority: Authenticated Workspace scope and Billing capacity decision
TargetAuthority: AutomationRule state
Mechanisms: CreateAutomationRuleCommandHandler; canonical Application request pipeline; Billing capacity decision; AutomationRule.Create; BOUND-TX-003 request transaction
FlowKind: Mixed
SupportingShapes: LocalVerticalSlice
Interactions:
  - EdgeId: E1
    Purpose: Automation rule creation requests Billing-owned hard-capacity decision/mutation
    SourceBC: Automation
    TargetBC: Billing
    Mechanism: TAC-XC-E
    DecisionStatus: FROZEN
    ContractOwner: Billing
    PortOwner: Automation consumer capacity Port role
    WorkflowOwner: Automation rule lifecycle
    MutationOwner: Billing
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: in-process-adapter
    RemoteReadiness: extraction-blocked
    FutureDistributedBinding: undecided-by-design
    TransactionBoundary: BOUND-TX-003 shared current request transaction while that consistency model is selected
    OutboxRequired: target-owned if outward fact is part of target mutation
    ConsumerDedupRequired: target-defined where operation identity exists
    IdempotencyKey: LogicalOperationId owned/enforced by Billing capacity mutation
    OrderingRequirement: target aggregate/version/concurrency rules
    RetryOwner: caller/current request; target owns semantic idempotency
    BusinessFailureOwner: Billing target mutation owner
    TechnicalFailureOwner: current in-process request runtime; future remote adapter/runtime
    UnknownOutcomePolicy: remote partial success is not certified while BOUND-TX-003 remains selected
    CompensationPolicy: none unless workflow/product explicitly defines it
    ExtractionBlocker: true while BOUND-TX-003 is selected
    RemovalTrigger: Automation/Billing physical extraction or approved IDEMPOTENT-CAPACITY-PROTOCOL with explicit partial-success/reconciliation semantics
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated Workspace member
Authorization: Automation rule permission
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: AutomationRule
Idempotency: LogicalOperationId required for capacity mutation
Concurrency: Last-slot race protection required
Transaction: M2C BOUND-TX-003 request transaction for capacity reserve, rule create, finalize, commit
ApplicationPath: backend/src/Notrelix.Application/Features/Automation/Rules/Commands/CreateAutomationRule/CreateAutomationRule.cs
DomainPath: AutomationRule.Create
PublicContract: Billing-owned capability/capacity semantic boundary; exact target action/fact contract normalized under M9
ConsumerPort: Automation-owned Billing capacity Port role only if the final M9 edge uses consumer runtime isolation; producer-owned Billing contract must remain distinct
ACL: NotApplicable unless Automation and Billing vocabularies require a pure consumer-owned mapper
InfrastructureAdapter: current in-process Billing capacity binding under BOUND-TX-003; must not expose Billing persistence as Automation ownership
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: automation rule facts as canonical
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: CreateAutomationRuleBillingGateTests; AutomationRuleTests
IntegrationTests: Target AutomationRule capacity race integration tests
ArchitectureGates: IdempotencyOperationArchitectureTests; CommonEntitlementsAntiRegressionTests; AuthPipelineArchitectureTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: Billing capacity protocol remains FRZ-020 work
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: CreateAutomationRule source exists; capacity hardening tracked M9
```

### AI-FLOW-02 — Work fact to Automation process

```text
FlowId: AI-FLOW-02
Team: TAC-RAP-AI
BoundedContext: Automation + WorkManagement
Purpose: Consume WorkManagement fact and start Automation process with tenant restoration and dedup
Disposition: REUSE-EXISTING
EntryPoint: WorkManagement integration events
ProductionReachability: current production source
SemanticOwner: WorkManagement fact
WorkflowOwner: Automation process
MutationAuthority: AutomationExecution process state
SourceAuthority: WorkManagement event envelope
TargetAuthority: AutomationExecution state
Mechanisms: BoardItemMemberAssignedIntegrationEvent; MassTransit receive pipeline; TenantContextConsumeFilter; DeduplicationConsumeFilter; BoardItemMemberAssignedAutomationConsumer; N8nAutomationRuleEvaluator; AutomationExecution and durable dispatch intent
FlowKind: CrossContext
SupportingShapes: ProcessManager; PlatformDelivery
Interactions:
  - EdgeId: E1
    Purpose: committed WorkManagement fact starts/advances Automation-owned durable execution
    SourceBC: WorkManagement
    TargetBC: Automation
    Mechanism: TAC-XC-C
    DecisionStatus: FROZEN
    ContractOwner: WorkManagement
    PortOwner: NotApplicable
    WorkflowOwner: Automation process
    MutationOwner: Automation
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: no
    ConsistencyModel: eventual
    CurrentBinding: platform-event-delivery
    RemoteReadiness: broker-native
    FutureDistributedBinding: broker-event
    TransactionBoundary: producer business state + outbox commit atomically; consumer-local transaction is independent
    OutboxRequired: yes
    ConsumerDedupRequired: yes
    IdempotencyKey: EventId/MessageId + consumer identity
    OrderingRequirement: Platform/event-stream rule plus producer revision where the event family requires ordering
    RetryOwner: Platform technical delivery retry
    BusinessFailureOwner: Automation process start/evaluation semantics
    TechnicalFailureOwner: Platform delivery runtime
    UnknownOutcomePolicy: delivery state/retry/dead-letter is Platform-owned; business reconciliation remains consumer/workflow-owned
    CompensationPolicy: none unless an owning Process Manager explicitly defines business compensation
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
RequestPipeline: MassTransit receive pipeline -> TenantContextConsumeFilter -> DeduplicationConsumeFilter -> consumer
Actor: System consumer
Authorization: TenantContextConsumeFilter restores Workspace tenant
AccountScope: Event AccountId required
WorkspaceScope: Event WorkspaceId required
ResourceScope: Automation rule execution
Idempotency: MessageId plus automation consumer identity; AutomationExecution trigger uniqueness
Concurrency: AutomationExecution trigger uniqueness and consumer dedup claim
Transaction: Consumer transaction creates AutomationExecution and stages N8nDispatchRequestedV1 outbox intent atomically
ApplicationPath: backend/src/Notrelix.Application/Features/Automation/Events/N8nAutomationRuleEvaluator.cs
DomainPath: NotApplicable — no Domain mutation required
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — Work event consumers are Infrastructure inbound message Adapters; AutomationExecution/process semantics remain Application-owned
ACL: NotApplicable — local flow
InfrastructureAdapter: backend/src/Notrelix.Infrastructure/Messaging/Consumers/Automation/BoardItemMemberAssignedForAutomationConsumer.cs
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: AutomationExecution
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: Platform/MassTransit delivery retry; deterministic tenant-envelope violations are non-retryable
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: AutomationExecutionTests; N8nAutomationTests
IntegrationTests: N8nAutomationTests; AutomationN8nDurabilityIntegrationTests
ArchitectureGates: AutomationProcessReferenceArchitectureTests; ScopedEventTenantEnvelopeArchitectureTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: None beyond current milestone scope
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Automation process consumers and durability tests exist
```

### AI-FLOW-03 — Automation Process to Work target action

```text
FlowId: AI-FLOW-03
Team: TAC-RAP-AI
BoundedContext: Automation + WorkManagement
Purpose: Execute real Automation action through WorkManagement target action with auth, scope, and idempotency
Disposition: IMPLEMENT-MISSING
EntryPoint: Automation action executor to IWorkActionPort
ProductionReachability: chain tests exist but real executor ownership still requires hardening
SemanticOwner: Automation execution
WorkflowOwner: Automation process
MutationAuthority: WorkManagement target aggregate
SourceAuthority: AutomationExecution and operation identity
TargetAuthority: BoardItem placement
Mechanisms: Automation action executor; IWorkActionPort; WorkItemActionAdapter; IWorkItemActions; MoveBoardItemUseCase; target authorization/scope/idempotency
FlowKind: CrossContext
SupportingShapes: ProcessManager
Interactions:
  - EdgeId: E1
    Purpose: Automation application isolates Work target capability behind consumer Port + ACL + runtime Adapter
    SourceBC: Automation
    TargetBC: WorkManagement
    Mechanism: TAC-XC-B
    DecisionStatus: FROZEN
    ContractOwner: WorkManagement.Public MoveItem semantic
    PortOwner: Automation
    WorkflowOwner: Automation process
    MutationOwner: NotApplicable
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: in-process-adapter
    RemoteReadiness: adapter-ready
    FutureDistributedBinding: remote-command-adapter
    TransactionBoundary: current request boundary; adapter owns no transaction
    OutboxRequired: no
    ConsumerDedupRequired: no
    IdempotencyKey: Automation ExecutionId maps to Work OperationId
    OrderingRequirement: NotApplicable
    RetryOwner: caller/current request; remote retry only after explicit policy
    BusinessFailureOwner: WorkManagement producer semantics; consumer maps result
    TechnicalFailureOwner: runtime adapter
    UnknownOutcomePolicy: current in-process binding has no transport unknown outcome; remote adapter must classify timeout/unavailable
    CompensationPolicy: NotApplicable unless target mutation is separately classified TAC-XC-E
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
  - EdgeId: E2
    Purpose: Automation requests WorkManagement-owned MoveItem mutation
    SourceBC: Automation
    TargetBC: WorkManagement
    Mechanism: TAC-XC-E
    DecisionStatus: FROZEN
    ContractOwner: WorkManagement
    PortOwner: Automation
    WorkflowOwner: Automation process
    MutationOwner: WorkManagement
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: in-process-adapter
    RemoteReadiness: adapter-ready
    FutureDistributedBinding: remote-command-adapter
    TransactionBoundary: WorkManagement owns target mutation transaction
    OutboxRequired: target-owned if outward fact is part of target mutation
    ConsumerDedupRequired: target-defined where operation identity exists
    IdempotencyKey: WorkManagement OperationId
    OrderingRequirement: target aggregate/version/concurrency rules
    RetryOwner: caller/current request; target owns semantic idempotency
    BusinessFailureOwner: WorkManagement target mutation owner
    TechnicalFailureOwner: current in-process request runtime; future remote adapter/runtime
    UnknownOutcomePolicy: unknown remote outcome must be retried/reconciled through the same OperationId; never duplicate semantic mutation
    CompensationPolicy: none unless workflow/product explicitly defines it
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
  - EdgeId: E3
    Purpose: Automation durable Process coordinates participant action and records participant outcome
    SourceBC: Automation
    TargetBC: WorkManagement
    Mechanism: TAC-XC-F
    DecisionStatus: FROZEN
    ContractOwner: Automation process + WorkManagement participant contract
    PortOwner: Automation
    WorkflowOwner: Automation process
    MutationOwner: NotApplicable
    ProjectionOwner: NotApplicable
    ProcessOwner: Automation
    CallerWaitsForOutcome: conditional/process-driven
    ConsistencyModel: process-coordinated
    CurrentBinding: process-runtime
    RemoteReadiness: process-native
    FutureDistributedBinding: durable-process
    TransactionBoundary: Automation process state transition and Work target mutation commit in participant-owned transactions; no distributed ACID assumption
    OutboxRequired: yes where participant commands/outcomes are message-driven
    ConsumerDedupRequired: yes for participant outcomes
    IdempotencyKey: ProcessId/LogicalOperationId + participant operation identity
    OrderingRequirement: process version/state-transition guard plus participant correlation
    RetryOwner: Platform technical retry; workflow owner owns business progression/retry meaning
    BusinessFailureOwner: Automation workflow/process owner
    TechnicalFailureOwner: Platform delivery/runtime
    UnknownOutcomePolicy: persist Pending/ReconciliationRequired or another product-authoritative durable process state
    CompensationPolicy: Automation workflow owner only if a product-authoritative compensation step exists
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Automation executor principal
Authorization: Target Workspace permission and scope
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: BoardItem
Idempotency: LogicalOperationId required
Concurrency: Expected version or placement ordering guard
Transaction: Target action request transaction with operation identity
ApplicationPath: backend/src/Notrelix.Application/Features/WorkManagement/BoardItems/Services/MoveBoardItemUseCase.cs; backend/src/Notrelix.Application/Features/Automation/Ports/WorkManagement/IWorkActionPort.cs
DomainPath: NotApplicable — no Domain mutation required
PublicContract: WorkManagement.Public target action
ConsumerPort: Automation-owned IWorkActionPort
ACL: Automation-owned pure MoveItem semantic mapper required by TAC-AI-011; WorkItemActionAdapter is runtime Adapter
InfrastructureAdapter: WorkItemActionAdapter → WorkManagement.Public; WorkManagement persistence remains behind target Application
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: board_item.moved and placement facts
Projection: NotApplicable — no projection required
ProcessState: AutomationExecution
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: WorkItemActionsTests; MoveBoardItemTests
IntegrationTests: AutomationWorkActionChainIntegrationTests
ArchitectureGates: AutomationProcessReferenceArchitectureTests; IdempotencyOperationArchitectureTests; CrossContextApplicationDependencyTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: Production executor path must not be replaced by direct port invocation
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: M0 and PLAN pin this as AI-FLOW-03 implementation target
```

### AI-FLOW-04 — Automation to N8n provider operation

```text
FlowId: AI-FLOW-04
Team: TAC-RAP-AI
BoundedContext: Automation + Integrations
Purpose: Dispatch accepted automation execution to N8n with retry, dedup, and provider outcome classification
Disposition: HARDEN-EXISTING
EntryPoint: N8nDispatchRequestedV1
ProductionReachability: current production source
SemanticOwner: Automation execution
WorkflowOwner: N8n provider adapter
MutationAuthority: AutomationExecution provider state
SourceAuthority: Automation execution and N8n request event
TargetAuthority: N8n provider result
Mechanisms: N8nDispatchRequestedV1; MassTransit receive pipeline; TenantContextConsumeFilter; DeduplicationConsumeFilter; N8nDispatchConsumer; N8nDispatchUseCase; IN8nWebhookActions; N8nClient provider boundary
FlowKind: Mixed
SupportingShapes: ProcessManager; ProviderPort; PlatformDelivery
Interactions:
  - EdgeId: E1
    Purpose: Automation accepted dispatch workflow invokes Integrations-owned N8n action/provider boundary
    SourceBC: Automation
    TargetBC: Integrations
    Mechanism: TAC-XC-E
    DecisionStatus: FROZEN
    ContractOwner: Integrations
    PortOwner: NotApplicable
    WorkflowOwner: Automation N8n dispatch
    MutationOwner: Integrations/provider-operation semantic
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: direct-in-process
    RemoteReadiness: semantic-only
    FutureDistributedBinding: remote-command-adapter
    TransactionBoundary: Integrations owns target mutation transaction
    OutboxRequired: target-owned if outward fact is part of target mutation
    ConsumerDedupRequired: target-defined where operation identity exists
    IdempotencyKey: Automation ExecutionId / accepted dispatch identity plus provider idempotency where supported
    OrderingRequirement: target aggregate/version/concurrency rules
    RetryOwner: Platform owns technical redelivery for transient provider/network failure; Automation owns durable terminal/unknown semantic outcome
    BusinessFailureOwner: Integrations target mutation owner
    TechnicalFailureOwner: Platform delivery + Integrations provider adapter
    UnknownOutcomePolicy: durable terminal/manual-reconciliation Automation outcome; no blind redelivery without provider reconciliation/idempotency
    CompensationPolicy: none unless workflow/product explicitly defines it
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
RequestPipeline: MassTransit receive pipeline -> TenantContextConsumeFilter -> DeduplicationConsumeFilter -> consumer
Actor: System consumer
Authorization: TenantContextConsumeFilter restores Workspace tenant
AccountScope: Event AccountId required
WorkspaceScope: Event WorkspaceId required
ResourceScope: AutomationExecution
Idempotency: ExecutionId is stable external idempotency identity; consumer dedup uses MessageId plus consumer identity
Concurrency: AutomationExecution status transition plus provider unknown outcome reconciliation
Transaction: Durable request event commits before provider call; provider result updates AutomationExecution state
ApplicationPath: backend/src/Notrelix.Application/Features/Automation/Executions/Services/N8nDispatchUseCase.cs
DomainPath: NotApplicable — no Domain mutation required
PublicContract: Automation-owned N8nDispatchRequestedV1 accepted-fact contract + Integrations.Public N8n target/provider action
ConsumerPort: NotApplicable for provider transport — N8nDispatchConsumer is an inbound message Adapter, not an Application Port
ACL: NotApplicable — provider translation belongs to Integrations/Application semantic Port + Infrastructure provider Adapter, not CrossContext ACL
InfrastructureAdapter: backend/src/Notrelix.Infrastructure/Messaging/Consumers/Automation/N8nDispatchConsumer.cs; backend/src/Notrelix.Infrastructure/Integrations/Providers/N8nClient.cs
ProviderBoundary: N8n webhook or API
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: AutomationExecution provider status
FailureClasses: validation; authorization; provider transient; provider terminal; provider unknown
RetryOwner: Platform/MassTransit retry for technical retryable dispatch; Automation owns durable success/terminal outcome; UnknownOutcome requires reconciliation
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: AutomationExecutionRetryTests; AutomationExecutionTests
IntegrationTests: AutomationN8nDurabilityIntegrationTests; N8nAutomationTests
ArchitectureGates: AutomationProcessReferenceArchitectureTests; ApplicationTransportBoundaryTests; PlatformBoundaryTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: Retry semantics and filter proof remain AI-FLOW-04 hardening
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: N8n dispatch consumer/use case and durability tests exist; retry semantics are provider outcome classification, not generic HTTP retry
```

### AI-FLOW-05 — Integrations Calendar connection and binding creation

```text
FlowId: AI-FLOW-05
Team: TAC-RAP-AI
BoundedContext: Integrations
Purpose: Connect Calendar using IntegrationConnection, IntegrationSecretVersion, and CalendarIntegration binding
Disposition: IMPLEMENT-MISSING
EntryPoint: ConnectCalendarCommand target handler
ProductionReachability: command exists but handler throws NotImplementedException
SemanticOwner: Integrations
WorkflowOwner: Calendar connection workflow
MutationAuthority: IntegrationConnection, IntegrationSecretVersion, CalendarIntegration
SourceAuthority: Provider OAuth callback and Workspace scope
TargetAuthority: Calendar binding and secret reference
Mechanisms: Target ConnectCalendarCommandHandler; canonical Application request pipeline; IntegrationConnection, IntegrationSecretVersion, and CalendarIntegration aggregate mutations; secret provider port; request transaction
FlowKind: Local
SupportingShapes: LocalVerticalSlice; ProviderPort
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated Workspace member
Authorization: Workspace integration permission
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: CalendarIntegration
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Request transaction persists connection, secret version, and binding atomically
ApplicationPath: backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/ConnectCalendar/ConnectCalendar.cs
DomainPath: IntegrationConnection.Create, IntegrationSecretVersion.Create, CalendarIntegration.Create
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: IntegrationDbContext and secret provider adapter
ProviderBoundary: Calendar provider OAuth
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: Target Calendar connect behavior tests; current Domain proof: CalendarIntegrationTests; IntegrationConnectionTests
IntegrationTests: Target Calendar DB round-trip integration test
ArchitectureGates: Target TAC-GATE-024 Calendar semantic/persistence gate; current: IntegrationConnectionContractTests
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: FRZ-019 not started
STOPConditions: None — CAL-CONN-001 and Calendar lifecycle decisions are frozen
CompletionEvidence: M0 source audit confirms stub handlers and required concepts
```

### AI-FLOW-06 — Calendar binding disconnect and generic Connection policy

```text
FlowId: AI-FLOW-06
Team: TAC-RAP-AI
BoundedContext: Integrations
Purpose: Disconnect Calendar by deactivating CalendarIntegration first and revoke generic Connection only under CAL-CONN-001
Disposition: IMPLEMENT-MISSING
EntryPoint: DisconnectCalendarCommand target handler
ProductionReachability: command exists but handler throws NotImplementedException
SemanticOwner: Integrations
WorkflowOwner: Calendar disconnect workflow
MutationAuthority: CalendarIntegration and IntegrationConnection
SourceAuthority: CalendarIntegration binding and active binding inventory
TargetAuthority: Deactivated binding and optional revoked Connection
Mechanisms: Target DisconnectCalendarCommandHandler; CalendarIntegration.Deactivate first; CAL-CONN-001 generic Connection cleanup policy; provider cleanup outcome classification
FlowKind: Local
SupportingShapes: LocalVerticalSlice; ProviderPort
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated Workspace member
Authorization: Workspace integration permission
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: CalendarIntegration
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Local durable state changes commit atomically; provider cleanup is separate with success, retryable, terminal, unknown outcomes
ApplicationPath: backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/DisconnectCalendar/DisconnectCalendar.cs
DomainPath: CalendarIntegration.Deactivate and IntegrationConnection revoke policy
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: IntegrationDbContext and provider adapter
ProviderBoundary: Calendar provider secret cleanup
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: Target Calendar disconnect behavior tests; current Domain proof: CalendarIntegrationTests; IntegrationConnectionDeletionLifecycleTests
IntegrationTests: Target Calendar disconnect provider outcome tests
ArchitectureGates: Target TAC-GATE-024 Calendar semantic/persistence gate
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: FRZ-019 not started
STOPConditions: None — CAL-CONN-001 is frozen
CompletionEvidence: M0 source audit confirms stub handler and frozen policy
```

### AI-FLOW-07 — Integrations verified inbound Calendar webhook

```text
FlowId: AI-FLOW-07
Team: TAC-RAP-AI
BoundedContext: Integrations
Purpose: Receive provider webhook with signature verification, Infrastructure inbound receipt, dedup, and trusted tenant derivation
Disposition: IMPLEMENT-MISSING
EntryPoint: HandleCalendarWebhookCommand target handler
ProductionReachability: command exists but handler throws NotImplementedException
SemanticOwner: Integrations provider intake
WorkflowOwner: Calendar webhook intake
MutationAuthority: Infrastructure inbound receipt and dedup state
SourceAuthority: Provider signature, verified Connection, CalendarIntegration binding
TargetAuthority: Inbound receipt and downstream sync effect
Mechanisms: Target raw HTTP webhook endpoint; signature/timestamp/replay verification; trusted CalendarIntegration derivation; Infrastructure inbound receipt/dedup; provider-neutral Integrations Application input
FlowKind: Local
SupportingShapes: ProviderPort; PlatformDelivery
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: raw HTTP -> signature verification -> trusted binding derivation -> Infrastructure receipt/dedup -> Application input
Actor: Provider webhook caller
Authorization: Provider signature verification plus trusted tenant derivation
AccountScope: Derived from verified CalendarIntegration
WorkspaceScope: Derived from verified CalendarIntegration
ResourceScope: Calendar webhook
Idempotency: Provider event identity and Infrastructure inbound dedup
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Receipt commit before downstream processing
ApplicationPath: backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/HandleCalendarWebhook/HandleCalendarWebhook.cs
DomainPath: NotApplicable — inbound receipt is Infrastructure technical state
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: IntegrationDbContext inbound receipt and provider verifier
ProviderBoundary: Calendar provider webhook
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: Inbound receipt status
FailureClasses: signature invalid; unknown connection; revoked binding; transient provider processing; duplicate receipt
RetryOwner: Webhook delivery and provider retry policy
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: Target Calendar webhook intake tests; current Domain proof: WebhookSecretHashTests; InboundWebhookEventTests
IntegrationTests: Target verified Calendar webhook integration test
ArchitectureGates: Target TAC-GATE-024 Calendar semantic/persistence gate; current: WebhookRulesTests
RemoteSubstitution: Fake provider signatures for tests only
KnownDebt: FRZ-019 not started; outbound WebhookDelivery must not be reused as inbound receipt
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: M0 source audit confirms stub handler and required receipt model
```

### BI-FLOW-01 — Capability and capacity resolution

```text
FlowId: BI-FLOW-01
Team: TAC-RAP-BI
BoundedContext: Billing
Purpose: Resolve capability and capacity with BILL-LIMIT-001 zero-capacity semantics
Disposition: VERIFIED
EntryPoint: Billing capability decision service
ProductionReachability: BillingCapabilityFactsProvider resolves BILL-LIMIT-001 (zero = hard-zero; explicit IsUnlimited = unbounded), workspace-scoped ledger usage, and deterministic entitlement precedence (TAC-BI-001).  The M9 closure wave corrected precedence ordering, the Used-authority (ledger sum in every state), and the fail-closed fact shape (unavailable + Limit NULL).
SemanticOwner: Billing
WorkflowOwner: Capability resolution
MutationAuthority: NotApplicable — decision flow
SourceAuthority: Entitlement, WorkspaceFeatureUsage, and FeatureUsageLedger
TargetAuthority: BillingCapabilityFact
Mechanisms: BillingCapabilityFactsProvider; Billing.Public capability decision contract; Entitlement and WorkspaceFeatureUsage read; BILL-LIMIT-001 target semantics
FlowKind: CrossContext
SupportingShapes: None
Interactions:
  - EdgeId: E1
    Purpose: Automation obtains authoritative Billing capability/capacity fact
    SourceBC: Automation
    TargetBC: Billing
    Mechanism: TAC-XC-A
    DecisionStatus: FROZEN
    ContractOwner: Billing
    PortOwner: NotApplicable
    WorkflowOwner: Automation capability/capacity decision
    MutationOwner: NotApplicable
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: direct-in-process
    RemoteReadiness: semantic-only
    FutureDistributedBinding: undecided-by-design
    TransactionBoundary: current request read boundary; no foreign mutation
    OutboxRequired: no
    ConsumerDedupRequired: no
    IdempotencyKey: NotApplicable — read
    OrderingRequirement: NotApplicable
    RetryOwner: caller/current request; no message retry
    BusinessFailureOwner: Billing authoritative read semantics
    TechnicalFailureOwner: current in-process request runtime
    UnknownOutcomePolicy: no transport unknown outcome in current binding; define timeout/availability policy at extraction
    CompensationPolicy: NotApplicable — read
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Protected consumer request
Authorization: Billing.Public capability decision contract
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId for workspace capacity
ResourceScope: Feature capability
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Request transaction owns durable mutation and required outbox enrollment
ApplicationPath: backend/src/Notrelix.Application/Features/Billing/Entitlements/Services/BillingCapabilityFactsProvider.cs
DomainPath: Entitlement and WorkspaceFeatureUsage semantics
PublicContract: Billing.Public capability/capacity facts for approved consumers
ConsumerPort: NotApplicable for the producer reference; consumer may use TAC-XC-A direct Public or its own Port under TAC-XC-B
ACL: NotApplicable in Billing producer reference
InfrastructureAdapter: ApplicationDbContext persistence adapter
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: BillingCapabilityFactsProviderTests; EntitlementTests; WorkspaceFeatureUsageTests
IntegrationTests: Target Billing capability capacity integration tests
ArchitectureGates: CommonEntitlementsAntiRegressionTests; Target TAC-GATE-025 Billing capacity gate
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: None for this flow; M9-CAPACITY-RELEASE-LIFECYCLE tracked separately
STOPConditions: None — BILL-LIMIT-001 is frozen
CompletionEvidence: M9-CLOSURE closure evidence (FROZEN) — BillingCapabilityFactsProviderTests 18; real capability-after-mutation integration proof; exact-head Backend CI run green at the closure candidate SHA
```

### BI-FLOW-02 — Billing hard-capacity mutation

```text
FlowId: BI-FLOW-02
Team: TAC-RAP-BI
BoundedContext: Billing
Purpose: Consume or reserve hard capacity atomically with last-slot concurrency protection
Disposition: VERIFIED
EntryPoint: Billing capacity owner action target
ProductionReachability: BillingCapacityActions is the Billing-owned hard-capacity action with global LogicalOperationId dedup, first-use atomic seed from the ledger sum, reconcile-on-every-op (ReconcileAtOpenCheck), and a real PostgreSQL last-slot concurrency proof (exactly one winner in the direct race).
SemanticOwner: Billing
WorkflowOwner: Capacity mutation
MutationAuthority: WorkspaceFeatureUsage
SourceAuthority: LogicalOperationId and requested amount
TargetAuthority: CurrentUsage and quota events
Mechanisms: Target Billing capacity owner action; WorkspaceFeatureUsage.Consume; conditional database update; FeatureUsageLedger; LogicalOperationId
FlowKind: CrossContext
SupportingShapes: None
Interactions:
  - EdgeId: E1
    Purpose: Automation or another approved consumer requests Billing-owned hard-capacity mutation
    SourceBC: Automation
    TargetBC: Billing
    Mechanism: TAC-XC-E
    DecisionStatus: FROZEN
    ContractOwner: Billing
    PortOwner: NotApplicable
    WorkflowOwner: Automation rule lifecycle / Billing capacity protocol
    MutationOwner: Billing
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: direct-in-process
    RemoteReadiness: semantic-only
    FutureDistributedBinding: remote-command-adapter
    TransactionBoundary: Billing owns target mutation transaction
    OutboxRequired: target-owned if outward fact is part of target mutation
    ConsumerDedupRequired: target-defined where operation identity exists
    IdempotencyKey: LogicalOperationId
    OrderingRequirement: target aggregate/version/concurrency rules
    RetryOwner: caller/current request; target owns semantic idempotency
    BusinessFailureOwner: Billing target mutation owner
    TechnicalFailureOwner: current in-process request runtime; future remote adapter/runtime
    UnknownOutcomePolicy: current local target transaction; remote protocol requires idempotent reserve/consume/release/reconcile semantics before extraction
    CompensationPolicy: none unless workflow/product explicitly defines it
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Protected workflow or system capacity consumer
Authorization: Billing capacity authority
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: Workspace feature capacity
Idempotency: LogicalOperationId required
Concurrency: Aggregate version plus conditional database update required
Transaction: Capacity mutation commits atomically with ledger or reservation state
ApplicationPath: NotApplicable — Domain capacity aggregate; target Application capacity action
DomainPath: backend/src/Notrelix.Domain/Billing/Usage/WorkspaceFeatureUsage.cs
PublicContract: Billing.Public hard-capacity target action/protocol
ConsumerPort: NotApplicable for the Billing producer reference; consumer Port, if used, is consumer-owned
ACL: NotApplicable in Billing producer reference
InfrastructureAdapter: ApplicationDbContext persistence adapter
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: QuotaExceededDomainEvent as canonical
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: WorkspaceFeatureUsageTests; UsageRulesTests
IntegrationTests: Target last-slot race integration tests
ArchitectureGates: Target TAC-GATE-025 Billing capacity/concurrency gate; IdempotencyOperationArchitectureTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: None for this flow
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: M9-CLOSURE closure evidence (FROZEN) — BillingCapacityActionsTests 20; BillingCapacityFlowIntegrationTests last-slot race with coherent ledger and exactly-one-winner op binding; exact-head Backend CI run green
```

### BI-FLOW-03 — AutomationRule lifecycle to Billing capacity

```text
FlowId: BI-FLOW-03
Team: TAC-RAP-BI
BoundedContext: Billing + Automation
Purpose: Create or delete AutomationRule through capacity reserve, consume, release, or reconcile protocol
Disposition: VERIFIED
EntryPoint: CreateAutomationRule capacity chain
ProductionReachability: CreateAutomationRule consumes one AUTOMATION_RULE slot inside the request transaction (BOUND-TX-003) via the Billing-owned action; retries never double-consume (global LogicalOperationId dedup), a lost slot rolls back the whole mutation, and a disabled rule still occupies its slot — release deferred under M9-CAPACITY-RELEASE-LIFECYCLE until a delete/archive lifecycle exists.
SemanticOwner: Billing capacity
WorkflowOwner: Automation rule lifecycle
MutationAuthority: AutomationRule and WorkspaceFeatureUsage
SourceAuthority: LogicalOperationId and AutomationRule request
TargetAuthority: Rule state and capacity usage
Mechanisms: CreateAutomationRuleCommandHandler; Billing capacity reserve/consume/release protocol; AutomationRule aggregate; BOUND-TX-003 request transaction
FlowKind: CrossContext
SupportingShapes: None
Interactions:
  - EdgeId: E1
    Purpose: Automation isolates Billing capacity capability behind consumer-facing capacity seam
    SourceBC: Automation
    TargetBC: Billing
    Mechanism: TAC-XC-B
    DecisionStatus: FROZEN
    ContractOwner: Billing capacity semantic
    PortOwner: Automation
    WorkflowOwner: Automation rule lifecycle
    MutationOwner: NotApplicable
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: in-process-adapter
    RemoteReadiness: extraction-blocked
    FutureDistributedBinding: undecided-by-design
    TransactionBoundary: BOUND-TX-003 current request transaction while selected
    OutboxRequired: no
    ConsumerDedupRequired: no
    IdempotencyKey: LogicalOperationId
    OrderingRequirement: NotApplicable
    RetryOwner: caller/current request; remote retry only after explicit policy
    BusinessFailureOwner: Billing producer semantics; consumer maps result
    TechnicalFailureOwner: runtime adapter
    UnknownOutcomePolicy: current in-process binding has no transport unknown outcome; remote adapter must classify timeout/unavailable
    CompensationPolicy: NotApplicable unless target mutation is separately classified TAC-XC-E
    ExtractionBlocker: true while BOUND-TX-003 is selected
    RemovalTrigger: approved IDEMPOTENT-CAPACITY-PROTOCOL or Automation/Billing extraction decision
  - EdgeId: E2
    Purpose: Automation requests Billing-owned reserve/consume/release mutation
    SourceBC: Automation
    TargetBC: Billing
    Mechanism: TAC-XC-E
    DecisionStatus: FROZEN
    ContractOwner: Billing
    PortOwner: Automation
    WorkflowOwner: Automation rule lifecycle
    MutationOwner: Billing
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: in-process-adapter
    RemoteReadiness: extraction-blocked
    FutureDistributedBinding: undecided-by-design
    TransactionBoundary: BOUND-TX-003 current request transaction while selected
    OutboxRequired: target-owned if outward fact is part of target mutation
    ConsumerDedupRequired: target-defined where operation identity exists
    IdempotencyKey: LogicalOperationId
    OrderingRequirement: target aggregate/version/concurrency rules
    RetryOwner: caller/current request; target owns semantic idempotency
    BusinessFailureOwner: Billing target mutation owner
    TechnicalFailureOwner: current in-process request runtime; future remote adapter/runtime
    UnknownOutcomePolicy: remote partial success/reconciliation must be explicitly designed before removing BOUND-TX-003
    CompensationPolicy: none unless workflow/product explicitly defines it
    ExtractionBlocker: true while BOUND-TX-003 is selected
    RemovalTrigger: approved IDEMPOTENT-CAPACITY-PROTOCOL or physical extraction decision
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated Workspace member
Authorization: Automation permission plus Billing capacity decision
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: AutomationRule capacity
Idempotency: LogicalOperationId required
Concurrency: Last-slot race protection required
Transaction: M2C BOUND-TX-003 request transaction until extraction to idempotent capacity protocol
ApplicationPath: backend/src/Notrelix.Application/Features/Automation/Rules/Commands/CreateAutomationRule/CreateAutomationRule.cs
DomainPath: NotApplicable — no Domain mutation required
PublicContract: Billing-owned capacity semantic action/fact contract; exact producer contract finalized in M9
ConsumerPort: Automation-owned capacity Port role; must remain distinct from Billing producer Public contract
ACL: pure Automation-owned Billing-capacity semantic mapper only if vocabularies differ; runtime adapter must not be mislabeled ACL
InfrastructureAdapter: current in-process capacity binding under BOUND-TX-003; Billing and Automation persistence remain separately owned
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: CreateAutomationRuleBillingGateTests; AutomationRuleLifecycleTests
IntegrationTests: Target concurrent CreateAutomationRule capacity tests
ArchitectureGates: IdempotencyOperationArchitectureTests; Target TAC-GATE-025 Billing capacity gate
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: M9-CAPACITY-RELEASE-LIFECYCLE — no delete/archive AutomationRule lifecycle exists yet; ReleaseAsync is implemented and protected but remains uncallable until such a lifecycle lands
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: M9-CLOSURE closure evidence (FROZEN) — CreateAutomationRuleBillingGateTests 5; BillingCapacityFlowIntegrationTests (disabled-rule occupancy, retry same LogicalOperationId, account/workspace isolation, ReleaseAsync restoring headroom); exact-head Backend CI run green
```

### BI-FLOW-04 — Capability after real capacity mutation

```text
FlowId: BI-FLOW-04
Team: TAC-RAP-BI
BoundedContext: Billing
Purpose: Prove capability decision reflects real capacity mutation and zero-capacity semantics
Disposition: VERIFIED
EntryPoint: Billing capability query after capacity mutation
ProductionReachability: capability follows the real production lifecycle — each persisted rule consumes one ledgered slot, the real provider reports the exhausted fact (IsAvailable=false, finite Limit intact) at full capacity, and usage is workspace-isolated under a shared account grant.
SemanticOwner: Billing
WorkflowOwner: Capability read
MutationAuthority: NotApplicable — read flow
SourceAuthority: Entitlement and WorkspaceFeatureUsage state
TargetAuthority: BillingCapabilityFact
Mechanisms: BillingCapabilityFactsProvider read after capacity mutation; Entitlement and WorkspaceFeatureUsage state; BILL-LIMIT-001 target semantics
FlowKind: CrossContext
SupportingShapes: None
Interactions:
  - EdgeId: E1
    Purpose: Automation verifies authoritative Billing capability after real capacity mutation
    SourceBC: Automation
    TargetBC: Billing
    Mechanism: TAC-XC-A
    DecisionStatus: FROZEN
    ContractOwner: Billing
    PortOwner: NotApplicable
    WorkflowOwner: Billing capability-after-usage proof
    MutationOwner: NotApplicable
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: direct-in-process
    RemoteReadiness: semantic-only
    FutureDistributedBinding: undecided-by-design
    TransactionBoundary: current request read boundary; no foreign mutation
    OutboxRequired: no
    ConsumerDedupRequired: no
    IdempotencyKey: NotApplicable — read
    OrderingRequirement: NotApplicable
    RetryOwner: caller/current request; no message retry
    BusinessFailureOwner: Billing authoritative read semantics
    TechnicalFailureOwner: current in-process request runtime
    UnknownOutcomePolicy: no transport unknown outcome in current binding; define timeout/availability policy at extraction
    CompensationPolicy: NotApplicable — read
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Protected consumer request
Authorization: Billing.Public capability decision contract
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: Feature capability
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Request transaction owns durable mutation and required outbox enrollment
ApplicationPath: backend/src/Notrelix.Application/Features/Billing/Entitlements/Services/BillingCapabilityFactsProvider.cs
DomainPath: Entitlement and WorkspaceFeatureUsage
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: ApplicationDbContext persistence adapter
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: BillingCapabilityFactsProviderTests; WorkspaceFeatureUsageTests
IntegrationTests: Target capability-after-capacity integration tests
ArchitectureGates: CommonEntitlementsAntiRegressionTests; Target TAC-GATE-025 Billing capacity gate
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: None for this flow
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: M9-CLOSURE closure evidence (FROZEN) — BillingCapabilityFactsProviderTests 18; RealCapabilityLifecycle and release-at-full integration proofs; exact-head Backend CI run green
```

### BI-FLOW-05 — External Billing provider

```text
FlowId: BI-FLOW-05
Team: TAC-RAP-BI
BoundedContext: Billing
Purpose: Classify external Billing provider flow as conditional and not a mandatory local baseline
Disposition: NOT-APPLICABLE-AT-CANDIDATE (BI-REF-003)
EntryPoint: External Billing provider boundary
ProductionReachability: conditional external provider flow
SemanticOwner: Billing
WorkflowOwner: External provider integration
MutationAuthority: External provider state
SourceAuthority: Provider webhook or billing API
TargetAuthority: Billing provider subscription state
Mechanisms: Conditional external Billing provider boundary; provider webhook/API; no mandatory local production flow in bootstrap
FlowKind: Local
SupportingShapes: ProviderPort
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: provider webhook/API -> Billing provider boundary -> subscription state mapping
Actor: External provider
Authorization: Provider signature or API authentication
AccountScope: Provider account mapping
WorkspaceScope: NotApplicable — Billing provider is Account-scoped
ResourceScope: Billing provider subscription
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Request transaction owns durable mutation and required outbox enrollment
ApplicationPath: NotApplicable — conditional external provider flow
DomainPath: NotApplicable — no Domain mutation required
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: ApplicationDbContext persistence adapter
ProviderBoundary: External Billing provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: Target Billing provider boundary tests if product makes flow mandatory
IntegrationTests: Target provider sandbox integration tests if product makes flow mandatory
ArchitectureGates: ApplicationTransportBoundaryTests; PlatformBoundaryTests
RemoteSubstitution: Provider sandbox or fake only
KnownDebt: No local mandatory implementation required in bootstrap
STOPConditions: STOP if product decides provider flow becomes mandatory
CompletionEvidence: SPEC marks BI-FLOW-05 as conditional classification; the closure candidate has no external Billing provider path in bootstrap, so no provider sandbox/fake is required at candidate
```

### AR-FLOW-01 — Live Work placement projection

```text
FlowId: AR-FLOW-01
Team: TAC-RAP-AR
BoundedContext: Analytics + WorkManagement
Purpose: Maintain Analytics placement projection from WorkManagement facts through consumer-owned projection service
Disposition: MIGRATE-EXISTING
EntryPoint: WorkspaceWorkItemPlacementService
ProductionReachability: current production source
SemanticOwner: Analytics projection
WorkflowOwner: WorkManagement fact consumer
MutationAuthority: WorkspaceWorkItemPlacement state
SourceAuthority: BoardItemMoved and related Work facts
TargetAuthority: Placement projection
Mechanisms: WorkManagement integration events; MassTransit receive pipeline; TenantContextConsumeFilter; DeduplicationConsumeFilter; placement consumers; WorkspaceWorkItemPlacementService; projection persistence
FlowKind: CrossContext
SupportingShapes: LocalProjection; PlatformDelivery
Interactions:
  - EdgeId: E1
    Purpose: committed WorkManagement placement fact is delivered to Analytics
    SourceBC: WorkManagement
    TargetBC: Analytics/Reporting
    Mechanism: TAC-XC-C
    DecisionStatus: FROZEN
    ContractOwner: WorkManagement
    PortOwner: NotApplicable
    WorkflowOwner: Analytics live placement projection
    MutationOwner: Analytics/Reporting
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: no
    ConsistencyModel: eventual
    CurrentBinding: platform-event-delivery
    RemoteReadiness: broker-native
    FutureDistributedBinding: broker-event
    TransactionBoundary: producer business state + outbox commit atomically; consumer-local transaction is independent
    OutboxRequired: yes
    ConsumerDedupRequired: yes
    IdempotencyKey: EventId/MessageId + consumer identity
    OrderingRequirement: Platform/event-stream rule plus producer revision where the event family requires ordering
    RetryOwner: Platform technical delivery retry
    BusinessFailureOwner: Analytics/Reporting consumer reaction owner
    TechnicalFailureOwner: Platform delivery runtime
    UnknownOutcomePolicy: delivery state/retry/dead-letter is Platform-owned; business reconciliation remains consumer/workflow-owned
    CompensationPolicy: none unless an owning Process Manager explicitly defines business compensation
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
  - EdgeId: E2
    Purpose: Analytics maintains consumer-owned local placement projection from Work authority
    SourceBC: WorkManagement
    TargetBC: Analytics/Reporting
    Mechanism: TAC-XC-D
    DecisionStatus: FROZEN
    ContractOwner: WorkManagement source fact/rebuild snapshot
    PortOwner: NotApplicable
    WorkflowOwner: Analytics projection
    MutationOwner: NotApplicable
    ProjectionOwner: Analytics/Reporting
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: local-read
    ConsistencyModel: eventual
    CurrentBinding: local-projection
    RemoteReadiness: projection-native
    FutureDistributedBinding: local-projection
    TransactionBoundary: consumer-local projection transaction
    OutboxRequired: producer-side when event-fed
    ConsumerDedupRequired: yes when event-fed
    IdempotencyKey: message/consumer identity + source revision/checkpoint
    OrderingRequirement: Work source revision/version guard; stale event cannot regress projection
    RetryOwner: Platform delivery retry; projection owner owns rebuild/recovery
    BusinessFailureOwner: Analytics/Reporting projection owner
    TechnicalFailureOwner: Platform delivery + projection persistence runtime
    UnknownOutcomePolicy: projection remains recoverable; rebuild from accepted producer-owned source
    CompensationPolicy: NotApplicable — reconcile/rebuild derived state
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
RequestPipeline: MassTransit receive pipeline -> TenantContextConsumeFilter -> DeduplicationConsumeFilter -> consumer
Actor: System consumer
Authorization: TenantContextConsumeFilter restores Workspace tenant
AccountScope: Event AccountId required
WorkspaceScope: Event WorkspaceId required
ResourceScope: Work item placement
Idempotency: MessageId plus placement consumer identity; event revision guard
Concurrency: Placement revision last-write-wins guard
Transaction: DeduplicationConsumeFilter transaction wraps placement projection SaveChanges and dedup success
ApplicationPath: backend/src/Notrelix.Application/Features/Analytics/Placements/Services/WorkspaceWorkItemPlacementService.cs
DomainPath: NotApplicable — projection ownership moved to Application service
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — WorkManagement projection events are producer-owned contracts consumed through Infrastructure message Adapters
ACL: NotApplicable — local flow
InfrastructureAdapter: backend/src/Notrelix.Infrastructure/Messaging/Consumers/Analytics/WorkItemPlacementConsumers.cs
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: WorkspaceWorkItemPlacement (Application/Features/Analytics/Projections/WorkItemPlacement; relocated from Domain in M10)
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: Platform/MassTransit delivery retry; rebuild recovers from producer projection source
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: Target placement service behavior tests; current proof: WorkspacePlacementProjectionIntegrationTests
IntegrationTests: WorkspacePlacementProjectionIntegrationTests
ArchitectureGates: CrossContextPersistenceBoundaryTests; ScopedEventTenantEnvelopeArchitectureTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: Resolved in M10 (CLOSED-FROZEN TAC-XC-B) — missing-payload scope resolved via producer-owned item lookup on IWorkItemProjectionSource (not kept as a separate adapter method); projection relocated out of Domain
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Placement service and WorkspacePlacementProjectionIntegrationTests exist
```

### AR-FLOW-02 — Analytics local read

```text
FlowId: AR-FLOW-02
Team: TAC-RAP-AR
BoundedContext: Analytics
Purpose: Read Analytics projection locally without foreign aggregate mutation
Disposition: MIGRATE-EXISTING
EntryPoint: GetWorkspacePlacementsQuery
ProductionReachability: current production source
SemanticOwner: Analytics
WorkflowOwner: Analytics read
MutationAuthority: NotApplicable — read flow
SourceAuthority: Analytics projection
TargetAuthority: Analytics read model
Mechanisms: GetWorkspacePlacementsQueryHandler; canonical Application query pipeline; Analytics projection read
FlowKind: Local
SupportingShapes: LocalVerticalSlice; LocalProjection
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated Workspace member
Authorization: Analytics protected query
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: Workspace placements
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Request transaction owns durable mutation and required outbox enrollment
ApplicationPath: backend/src/Notrelix.Application/Features/Analytics/Placements/Queries/GetWorkspacePlacements/GetWorkspacePlacements.cs
DomainPath: NotApplicable — query flow
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: ReportingDbContext query
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: WorkspaceWorkItemPlacement (Application/Features/Analytics/Projections/WorkItemPlacement; relocated from Domain in M10)
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: Target Analytics read behavior tests; current proof: WorkspacePlacementProjectionIntegrationTests
IntegrationTests: WorkspacePlacementProjectionIntegrationTests
ArchitectureGates: WorkspaceScopedArchitectureTests; DbContextBoundaryArchitectureTests
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: Resolved in M10 — GetWorkspacePlacementsQuery reads only the Analytics local placement projection; it never calls WorkManagement.Public IWorkItemProjectionSource or IWorkManagementDbContext (TAC-AR-008 proof; AR-FLOW-02 carries its own local-read proof)
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Analytics query source exists
```

### AR-FLOW-03 — Projection rebuild

```text
FlowId: AR-FLOW-03
Team: TAC-RAP-AR
BoundedContext: Analytics + WorkManagement
Purpose: Rebuild Analytics placement projection from producer-owned Work projection source
Disposition: MIGRATE-EXISTING
EntryPoint: RebuildWorkspacePlacementsCommand
ProductionReachability: current production source
SemanticOwner: Analytics rebuild
WorkflowOwner: Analytics rebuild
MutationAuthority: Placement projection state
SourceAuthority: IWorkItemProjectionSource
TargetAuthority: Placement projection
Mechanisms: RebuildWorkspacePlacementsCommandHandler; IWorkItemProjectionSource; WorkItemProjectionSourceAdapter; WorkspaceWorkItemPlacementService; checkpoint state
FlowKind: CrossContext
SupportingShapes: LocalProjection; CompositeRead
Interactions:
  - EdgeId: E1
    Purpose: Analytics rebuild obtains authoritative Work placement snapshot through consumer runtime boundary
    SourceBC: Analytics/Reporting
    TargetBC: WorkManagement
    Mechanism: TAC-XC-B
    DecisionStatus: CLOSED-FROZEN — M10-PORT-OWNERSHIP-NORMALIZATION resolved: Analytics-owned consumer Port → Infrastructure adapter → WorkManagement.Public IWorkItemProjectionSource → WorkManagement Application implementation → IWorkManagementDbContext
    ContractOwner: WorkManagement.Public projection-source semantic
    PortOwner: Analytics/Reporting Application source/rebuild Port
    WorkflowOwner: Analytics projection rebuild
    MutationOwner: NotApplicable
    ProjectionOwner: NotApplicable
    ProcessOwner: NotApplicable
    CallerWaitsForOutcome: yes
    ConsistencyModel: immediate
    CurrentBinding: in-process-adapter
    RemoteReadiness: deferred-by-design
    FutureDistributedBinding: remote-query-adapter
    TransactionBoundary: current request boundary; adapter owns no transaction
    OutboxRequired: no
    ConsumerDedupRequired: no
    IdempotencyKey: NotApplicable — read unless target operation declares it
    OrderingRequirement: NotApplicable
    RetryOwner: caller/current request; remote retry only after explicit policy
    BusinessFailureOwner: WorkManagement producer semantics; consumer maps result
    TechnicalFailureOwner: Analytics runtime adapter delegates to WorkManagement producer-owned Application implementation; direct Work DbContext access forbidden
    UnknownOutcomePolicy: missing/unavailable source leaves rebuild recoverable and checkpointed; remote timeout semantics frozen in M10
    CompensationPolicy: NotApplicable unless target mutation is separately classified TAC-XC-E
    ExtractionBlocker: false
    RemovalTrigger: NotApplicable
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: System operation
Authorization: ISystemOperation with explicit Workspace scope
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: Workspace placements
Idempotency: Rebuild operation identity and checkpoint state
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Rebuild commits projection state with checkpoint
ApplicationPath: backend/src/Notrelix.Application/Features/Analytics/Placements/Commands/RebuildWorkspacePlacements/RebuildWorkspacePlacements.cs
DomainPath: NotApplicable — projection service
PublicContract: WorkManagement.Public IWorkItemProjectionSource producer-owned projection-source semantic (workspace snapshot + item lookup)
ConsumerPort: Analytics/Reporting Application source Port — IWorkItemProjectionSourceAdapter (Application-owned port role) reaches the producer Public contract via the Infrastructure adapter
ACL: NotApplicable — M10 keeps WorkItemProjectionSourceAdapter as runtime Adapter; no pure Analytics semantic mapper introduced
InfrastructureAdapter: backend/src/Notrelix.Infrastructure/CrossContext/Analytics/WorkManagement/WorkItemProjectionSourceAdapter.cs
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: WorkspaceWorkItemPlacement (Application/Features/Analytics/Projections/WorkItemPlacement; relocated from Domain in M10)
ProcessState: Rebuild checkpoint
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: Target rebuild behavior tests; current proof: WorkspacePlacementProjectionIntegrationTests
IntegrationTests: WorkspacePlacementProjectionIntegrationTests
ArchitectureGates: CrossContextPersistenceBoundaryTests; BoundedContextPortArchitectureTests
RemoteSubstitution: Legacy summary only — normative runtime/extraction semantics are in Interactions[]
KnownDebt: Normalized in M10 (CLOSED-FROZEN) — Analytics-owned Application source Port → Infrastructure delegate adapter → WorkManagement Application IWorkItemProjectionSource implementation; projection relocated to Application/Features/Analytics/Projections/WorkItemPlacement; direct Work DbContext access forbidden
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Rebuild command and projection source adapter exist
```

### AR-FLOW-04 — Projection failure and recovery case

```text
FlowId: AR-FLOW-04
Team: TAC-RAP-AR
BoundedContext: Analytics
Purpose: Prove projection failure leaves recoverable state and rebuild can reconcile
Disposition: MIGRATE-EXISTING
EntryPoint: Projection consumer failure path
ProductionReachability: current production source
SemanticOwner: Analytics projection
WorkflowOwner: Projection recovery
MutationAuthority: Projection state
SourceAuthority: Projection events and rebuild source
TargetAuthority: Recovered projection
Mechanisms: placement consumer failure path; revision guard; Platform delivery retry; rebuild recovery from producer projection source
FlowKind: Local
SupportingShapes: LocalProjection
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: MassTransit receive pipeline -> TenantContextConsumeFilter -> DeduplicationConsumeFilter -> consumer
Actor: System consumer
Authorization: TenantContextConsumeFilter restores Workspace tenant
AccountScope: Required AccountId
WorkspaceScope: Required WorkspaceId
ResourceScope: Projection stream
Idempotency: MessageId plus placement consumer identity; revision guard prevents stale regression
Concurrency: Placement revision last-write-wins guard
Transaction: Failed projection rolls back with DeduplicationConsumeFilter transaction; rebuild recovers from authoritative source
ApplicationPath: backend/src/Notrelix.Application/Features/Analytics/Placements/Services/WorkspaceWorkItemPlacementService.cs
DomainPath: NotApplicable — projection service
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: backend/src/Notrelix.Infrastructure/Messaging/Consumers/Analytics/WorkItemPlacementConsumers.cs
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: WorkspaceWorkItemPlacement
ProcessState: NotApplicable — no process state required
FailureClasses: stale revision; duplicate event; persistence failure; source unavailable
RetryOwner: Platform/MassTransit delivery retry and rebuild operation
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: Target projection failure tests; current proof: WorkspacePlacementProjectionIntegrationTests
IntegrationTests: WorkspacePlacementProjectionIntegrationTests
ArchitectureGates: PlatformReferenceCompletenessTests; CrossContextPersistenceBoundaryTests
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: Recovery proof remains M10 work
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Placement service has stale and duplicate guards
```

### PF-FLOW-01 — Request execution pipeline

```text
FlowId: PF-FLOW-01
Team: TAC-RAP-PF
BoundedContext: Platform
Purpose: Prove canonical request pipeline order and execution context propagation
Disposition: REUSE-EXISTING
EntryPoint: MediatR pipeline behaviors
ProductionReachability: current production source
SemanticOwner: Application pipeline
WorkflowOwner: Request execution
MutationAuthority: NotApplicable — mechanism flow
SourceAuthority: Request markers and ExecutionContext
TargetAuthority: Pipeline behavior order
Mechanisms: canonical MediatR request pipeline; ExceptionMapping, Tracing, RequestContract, ExecutionContext, DataSession, AccessControl, and Idempotency behaviors
FlowKind: Platform
SupportingShapes: RequestPipeline
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: ExceptionMapping -> Tracing -> RequestContract -> ExecutionContext -> DataSession -> AccessControl -> Idempotency
Actor: Authenticated or system request
Authorization: Pipeline markers decide behavior activation
AccountScope: Resolved by ExecutionContextBehavior
WorkspaceScope: Resolved by ExecutionContextBehavior
ResourceScope: Request
Idempotency: NotApplicable — local request transaction owns mutation; message flows use message identity
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Request transaction owns durable mutation and required outbox enrollment
ApplicationPath: backend/src/Notrelix.Application/Common/Behaviors
DomainPath: NotApplicable — mechanism flow
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: ApplicationDbContext persistence adapter
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: NotApplicable — HTTP request path is not message-retried
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: PipelineOrderTests; AccessControlBehaviorTests; ExecutionContextBehaviorTests
IntegrationTests: PipelineTelemetryIntegrationTests
ArchitectureGates: PipelineRuntimeOrderTests; PipelineFreezeArchitectureTests
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: None beyond current milestone scope
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Pipeline tests pass on current branch
```

### PF-FLOW-02 — Domain Event to Integration Event to Outbox

```text
FlowId: PF-FLOW-02
Team: TAC-RAP-PF
BoundedContext: Platform
Purpose: Prove committed Domain facts map to public IntegrationEvents and durable outbox rows atomically
Disposition: REUSE-EXISTING
EntryPoint: DomainEventInterceptor
ProductionReachability: current production source
SemanticOwner: Platform messaging
WorkflowOwner: Persistence interceptor
MutationAuthority: Outbox message state
SourceAuthority: Domain events and integration mappers
TargetAuthority: Outbox row
Mechanisms: DomainEventInterceptor; event mappers; MessagingOutboxMessage; OutboxDispatcher; IntegrationEventCatalog
FlowKind: Platform
SupportingShapes: PlatformDelivery
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: producer SaveChanges -> DomainEventInterceptor -> outbox commit -> OutboxDispatcher -> IntegrationEventBus -> MassTransit receive pipeline
Actor: System producer
Authorization: NotApplicable — producer mechanism
AccountScope: Outbox AccountId from event envelope
WorkspaceScope: Outbox WorkspaceId from event envelope
ResourceScope: Aggregate event stream
Idempotency: Outbox EventId plus dispatcher consumer identity
Concurrency: Outbox claim lease and stream ordering guard
Transaction: Outbox enrollment commits with SaveChanges; dispatcher publishes after commit
ApplicationPath: NotApplicable — Infrastructure persistence interceptor mechanism
DomainPath: NotApplicable — no Domain mutation required
PublicContract: IntegrationEvent contracts
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: backend/src/Notrelix.Infrastructure/Data/Interceptors/DomainEventInterceptor.cs; backend/src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: all registered public events
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: OutboxDispatcher retry for dispatch failure
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: DomainEventInterceptorTests; EnvelopeBuilderTests
IntegrationTests: OutboxAtomicityTests; OutboxDispatchContractTests
ArchitectureGates: PublicEventContractArchitectureTests; PlatformReferenceCompletenessTests
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: None beyond current milestone scope
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Outbox evidence tests pass
```

### PF-FLOW-03 — Broker delivery, tenant restoration, and dedup

```text
FlowId: PF-FLOW-03
Team: TAC-RAP-PF
BoundedContext: Platform
Purpose: Prove broker consumer restores tenant and deduplicates by message and consumer identity
Disposition: HARDEN-EXISTING
EntryPoint: TenantContextConsumeFilter and DeduplicationConsumeFilter
ProductionReachability: current production source
SemanticOwner: Platform delivery
WorkflowOwner: Consumer runtime
MutationAuthority: Dedup state and consumer effect
SourceAuthority: Integration event envelope
TargetAuthority: Tenant context and dedup record
Mechanisms: MassTransit receive pipeline; TenantContextConsumeFilter; DeduplicationConsumeFilter; MessageDeduplicationStore; RlsSessionContext
FlowKind: Platform
SupportingShapes: PlatformDelivery
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: MassTransit receive pipeline -> TenantContextConsumeFilter -> DeduplicationConsumeFilter -> consumer
Actor: System consumer
Authorization: Declared IntegrationEventTenantScope
AccountScope: Declared Account or Workspace scope
WorkspaceScope: Declared Workspace scope when applicable
ResourceScope: Message stream
Idempotency: MessageId plus ConsumerName
Concurrency: Dedup claim row and consumer transaction
Transaction: DeduplicationConsumeFilter transaction wraps RLS session setup, consumer effect, and dedup success
ApplicationPath: NotApplicable — Infrastructure consume filters
DomainPath: NotApplicable — no Domain mutation required
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: backend/src/Notrelix.Infrastructure/Messaging/TenantContextConsumeFilter.cs; backend/src/Notrelix.Infrastructure/Messaging/DeduplicationConsumeFilter.cs
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: Platform/MassTransit delivery retry; deterministic tenant-envelope violations are non-retryable
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: TenantContextConsumeFilterScopedEventTests
IntegrationTests: DeduplicationConsumeFilterFullIntegrationTests; WorkspaceMemberAddedScopedTenantRuntimeChainIntegrationTests
ArchitectureGates: ScopedEventTenantEnvelopeArchitectureTests; PlatformReferenceCompletenessTests
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: None — declared tenant scope is enforced before consumer delivery
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Filter tests, dedup integration tests, and full scoped tenant chain test exist
```

### PF-FLOW-04 — Delivery retry and failure

```text
FlowId: PF-FLOW-04
Team: TAC-RAP-PF
BoundedContext: Platform
Purpose: Prove transient retry, terminal failure, poison classification, and dead-letter behavior
Disposition: REUSE-EXISTING
EntryPoint: OutboxDispatcher and MassTransit retry filters
ProductionReachability: current production source
SemanticOwner: Platform delivery
WorkflowOwner: Delivery runtime
MutationAuthority: Delivery state
SourceAuthority: Message identity and consumer failure
TargetAuthority: Retry, dead-letter, or completion state
Mechanisms: OutboxDispatcher retry state; MassTransit endpoint retry; RabbitMQ retry policy; poison/dead-letter state
FlowKind: Platform
SupportingShapes: PlatformDelivery
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: outbox claim -> IntegrationEventCatalog resolve -> IntegrationEventBus publish -> consumer retry/failure -> completion state
Actor: System dispatcher or consumer
Authorization: NotApplicable — mechanism flow
AccountScope: Message envelope scope
WorkspaceScope: Message envelope scope
ResourceScope: Message stream
Idempotency: Message and consumer identity
Concurrency: Outbox claim lease and retry_count/max_retries guard
Transaction: Delivery state transitions commit independently of provider effects
ApplicationPath: NotApplicable — Infrastructure delivery mechanism
DomainPath: NotApplicable — no Domain mutation required
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: backend/src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/MessagingRegistration.cs
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: transient; deterministic contract; poison; unknown provider outcome
RetryOwner: Platform delivery policy
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: PoisonDetectorTests; RetryPolicyTests
IntegrationTests: OutboxClaimReclaimTests; OutboxDispatchContractTests
ArchitectureGates: PlatformReferenceCompletenessTests; PlatformBoundaryTests
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: None beyond current milestone scope
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Platform delivery tests pass
```

### PF-FLOW-05 — Contract evolution and capability-based recovery

```text
FlowId: PF-FLOW-05
Team: TAC-RAP-PF
BoundedContext: Platform
Purpose: Prove compound-key catalog resolution, declared compatibility, and the recovery capability of each changed event family
Disposition: HARDEN-EXISTING
EntryPoint: IntegrationEventCatalog and EventContractKey
ProductionReachability: current production source
SemanticOwner: Platform contracts
WorkflowOwner: Dispatch and replay
MutationAuthority: NotApplicable — resolution flow
SourceAuthority: Event name, version, and serialized payload
TargetAuthority: Resolved CLR contract plus the declared evolution/recovery policy
Mechanisms: IntegrationEventCatalog compound key resolution; EventContractKey; EventEvolutionPolicyRegistry; generated event manifest; capability-specific recovery
FlowKind: Platform
SupportingShapes: PlatformDelivery
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: outbox row name/version -> IntegrationEventCatalog resolve -> deserialize -> MassTransit receive pipeline
Actor: System dispatcher
Authorization: NotApplicable — mechanism flow
AccountScope: Envelope AccountId when declared
WorkspaceScope: Envelope WorkspaceId when declared
ResourceScope: Event contract
Idempotency: MessageId and versioned contract identity
Concurrency: NotApplicable — resolution flow
Transaction: Dispatch resolves before publish
ApplicationPath: backend/src/Notrelix.Application/Common/Events/EventContractKey.cs
DomainPath: NotApplicable — no Domain mutation required
PublicContract: backend/contracts/events/notrelix.events.json
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: backend/src/Notrelix.Infrastructure/Messaging/IntegrationEventCatalog.cs
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: Unknown contract resolution fails deterministically; transient dispatch failure retries
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: IntegrationEventCatalogResolutionTests; ScopedEventTenantEnvelopeReplayCompatibilityTests
IntegrationTests: OutboxDispatchContractTests
ArchitectureGates: PublicEventContractArchitectureTests; ContractRegistryCompletenessTests
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: Generic replay strategies have no retained event source and are not closure evidence. Replay is permitted only for an event family with an authoritative retained payload source.
STOPConditions: A ReplayableSameSchema or Upcastable policy without an executable source, checkpoint, resume, and dedup proof stops certification
CompletionEvidence: Catalog and manifest gates pass
```

### PF-FLOW-06 — Background actor and security context

```text
FlowId: PF-FLOW-06
Team: TAC-RAP-PF
BoundedContext: Platform
Purpose: Prove background consumers and system operations carry explicit tenant or system context
Disposition: REUSE-EXISTING
EntryPoint: ExecutionContext and system request markers
ProductionReachability: current production source
SemanticOwner: Platform security
WorkflowOwner: Background execution
MutationAuthority: Protected consumer effect
SourceAuthority: Message envelope and system operation reason
TargetAuthority: Execution context
Mechanisms: ExecutionContextBehavior; CurrentTenantContext; ISystemInternalRequest and ISystemOperation markers; consumer tenant restoration
FlowKind: Platform
SupportingShapes: PlatformDelivery
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: message envelope -> TenantContextConsumeFilter -> ExecutionContext/consumer authorization
Actor: System principal or explicit executor
Authorization: ISystemInternalRequest or ISystemOperation
AccountScope: Required for tenant-scoped background work
WorkspaceScope: Required for Workspace-scoped background work
ResourceScope: Background operation
Idempotency: Message identity for consumers; operation identity for system operations
Concurrency: Aggregate version or database constraint where mutable state is versioned
Transaction: Consumer request transaction
ApplicationPath: backend/src/Notrelix.Application/Common/Behaviors/ExecutionContextBehavior.cs
DomainPath: NotApplicable — no Domain mutation required
PublicContract: NotApplicable — local flow
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: backend/src/Notrelix.Infrastructure/Identity/Services/CurrentTenantContext.cs
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: validation; authorization; not-found; business-rule; concurrency; persistence
RetryOwner: Platform delivery retry for consumer failures
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: ExecutionContextBehaviorTests
IntegrationTests: RlsRuntimeEnforcementTests; WorkspaceMemberAddedScopedTenantRuntimeChainIntegrationTests
ArchitectureGates: SystemContextUsageTests; TenantContextArchitectureTests
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: None beyond current milestone scope
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Background context tests pass
```

### PF-FLOW-07 — Scoped Integration Event tenant envelope

```text
FlowId: PF-FLOW-07
Team: TAC-RAP-PF
BoundedContext: Platform + all producers
Purpose: Enforce declared Workspace, Account, and None tenant scope at runtime and prevent scoped events from falling back to System
Disposition: HARDEN-EXISTING
EntryPoint: TenantContextConsumeFilter
ProductionReachability: current production source
SemanticOwner: Producer event contracts
WorkflowOwner: Consumer runtime
MutationAuthority: Tenant context restoration
SourceAuthority: IntegrationEventTenantScopeAttribute and event envelope
TargetAuthority: CurrentTenantContext
Mechanisms: IntegrationEventTenantScopeAttribute; TenantContextConsumeFilter; IntegrationEventTenantEnvelopeException; MassTransit receive pipeline
FlowKind: Platform
SupportingShapes: PlatformDelivery
Interactions: [] — no actual cross-BC edge in this Flow Card
RequestPipeline: Integration Event -> MassTransit receive pipeline -> TenantContextConsumeFilter -> declared IntegrationEventTenantScope -> exact Account/Workspace/System restoration -> consumer
Actor: System consumer
Authorization: Declared tenant scope
AccountScope: Workspace and Account scopes require AccountId
WorkspaceScope: Workspace scope requires WorkspaceId
ResourceScope: Integration event message
Idempotency: Message identity remains separate from tenant validation
Concurrency: NotApplicable — tenant restoration mechanism
Transaction: Filter runs before consumer transaction and clears tenant state in finally
ApplicationPath: backend/src/Notrelix.Application/Common/Events/IntegrationEventTenantScopeAttribute.cs
DomainPath: NotApplicable — no Domain mutation required
PublicContract: IntegrationEvent contracts
ConsumerPort: NotApplicable — local flow
ACL: NotApplicable — local flow
InfrastructureAdapter: backend/src/Notrelix.Infrastructure/Messaging/TenantContextConsumeFilter.cs
ProviderBoundary: NotApplicable — no external provider
IntegrationEvents: NotApplicable — no outward fact required
Projection: NotApplicable — no projection required
ProcessState: NotApplicable — no process state required
FailureClasses: missing classification; missing AccountId; missing WorkspaceId; empty Guid; valid restoration
RetryOwner: IntegrationEventTenantEnvelopeException derives from ArgumentException and is classified non-retryable by RabbitMQ retry policy
Observability: request correlation id; structured logs without secrets
DI: Production API composition
BehaviorTests: TenantContextConsumeFilterScopedEventTests; ScopedEventTenantEnvelopeReplayCompatibilityTests
IntegrationTests: WorkspaceMemberAddedScopedTenantRuntimeChainIntegrationTests
ArchitectureGates: ScopedEventTenantEnvelopeArchitectureTests; PlatformReferenceCompletenessTests
RemoteSubstitution: NotApplicable — no remote provider
KnownDebt: None — declared Workspace and Account scopes fail closed before consumer execution
STOPConditions: None — accepted semantics and source authority exist
CompletionEvidence: Filter negative, None-explicit, missing-classification, replay compatibility, and full production-chain tests pass
```

# 26. Mandatory Team Operating Packs

Required:

```text
TAC-RAP-IA   Identity & Accounts
TAC-RAP-WG   Workspace & Governance
TAC-RAP-WM   Work Management
TAC-RAP-DC   Documents & Collaboration
TAC-RAP-AI   Automation & Integrations
TAC-RAP-BI   Billing & Entitlements
TAC-RAP-AR   Analytics & Reporting
TAC-RAP-PF   Platform & Foundation
```

All eight packs are mandatory.

Business packs must cover every BC assigned to that team.

There is no:

```text
DEFER-NO-REAL-FLOW
```

for the BC-local baseline.

---

# 27. TAC-RAP-IA — Identity & Accounts Real Flow Catalog

Owned BCs:

```text
Identity
Accounts
```

## IA-FLOW-01 — Identity local profile mutation

Pinned source:

```text
UpdateProfileCommand
```

Disposition:

```text
REUSE/HARDEN-EXISTING
```

Purpose:

```text
pure Identity local vertical slice
```

Required proof:

```text
authenticated principal
Identity DbContext only
User aggregate mutation
request transaction
no cross-BC mutation
behavior + persistence tests
```

This is the local Identity copy-model.

## IA-FLOW-06 — Accounts local admin mutation

Accounts needs an authenticated Account-scoped local mutation reference in
addition to cross-context Public provisioning.

Pinned bootstrap operation:

```text
RenameAccount
```

Current Domain authority already owns Account rename behavior.

Current disposition after repository implementation:

```text
HARDEN-EXISTING
```

The Application/API path now exists. The remaining work is to prove/harden the
same frozen flow rather than create a second RenameAccount implementation.

Required flow:

```text
authenticated request
→ trusted Account scope
→ Governance Account-admin authorization
→ Accounts Application handler
→ load Account
→ Account.Rename(...)
→ Accounts transaction
→ Account-owned Domain Event
→ outward event/outbox if canonical mapper exists
```

Required proof:

```text
actor explicit
AccountId explicit
owner/Admin authorization
same-name/no-op semantics follow Domain
version/concurrency semantics
no Identity/Workspaces private-state dependency
```

This is the Accounts local-admin copy-model.

`IA-FLOW-02` remains the cross-BC Public-action + shared-transaction reference.

## IA-FLOW-02 — Identity registration → Accounts personal provisioning

Pinned current source:

```text
RegisterCommandHandler
→ IAccountProvisioningService
→ AccountProvisioningService
```

Current source directly imports a private Accounts Application service.

Target:

```text
Identity Register
→ Accounts.Public IAccountProvisioningActions
→ Accounts-owned provisioning implementation
→ Account + owner AccountMember
```

Disposition:

```text
MIGRATE/HARDEN-EXISTING
```

Accounts Public request/result must expose only stable provisioning semantics.

Identity must not depend on:

```text
Features.Accounts.Provisioning private interface
IAccountDbContext
Account aggregate
AccountMember aggregate
```

### Transaction

Current source explicitly relies on one request transaction committing Identity
User and Accounts personal Account atomically.

v2.6 freezes current behavior as:

```text
BOUND-TX-004
Identity registration + personal Account provisioning
temporary reviewed shared local transaction
```

Record:

```text
WorkflowOwner = Identity Registration
IdentityMutationOwner = Identity
AccountMutationOwner = Accounts
Atomicity = current shared request transaction
ExtractionBlocker = true
RemovalTrigger =
  Identity/Accounts physical extraction
  or approved product workflow allowing asynchronous Account provisioning
```

The agent does not silently redesign this flow as eventual consistency.

## IA-FLOW-03 — Identity registration event → Workspace provisioning

Pinned source fact:

```text
IdentityRegistrationCompletedIntegrationEventV1
```

Pinned real consumer:

```text
WorkspaceProvisioningConsumer
```

Required production chain:

```text
Register succeeds
→ Identity-owned Integration Event enrolled in outbox
→ Platform delivery
→ WorkspaceProvisioningConsumer
→ Workspaces-owned provisioning mutation/reaction
```

The event carries only committed Identity/registration facts.

Required:

```text
dedup
tenant/account scope
failure/retry ownership
no direct Identity persistence in consumer
```

## IA-FLOW-04 — Accounts membership admission read

Compatibility aliases:

```text
IA-REF-002
Accounts Public membership/admission facts
```

Required:

```text
Workspaces
→ Accounts.Public IAccountMembershipFacts
→ Accounts owner implementation
```

This is the producer-Public authoritative-read reference.

## IA-FLOW-05 — Accounts membership target mutation

Compatibility alias:

```text
IA-REF-003
```

Pinned use:

```text
AcceptInvitation
→ Accounts.Public IAccountMembershipActions
```

Accounts owns the mutation.

`BOUND-TX-002` remains the reviewed Accounts↔Workspaces shared-transaction
exception.

## IA pack completion

Both are required:

```text
Identity local flow FROZEN
Accounts owner mutation/read flows FROZEN
```

One cannot substitute for the other.

---

# 28. TAC-RAP-WG — Workspace & Governance Real Flow Catalog

Owned BCs:

```text
Workspaces
Governance
```

## WG-FLOW-01 — Workspaces local creation

Pinned source:

```text
CreateWorkspaceCommand
```

Disposition:

```text
REUSE/HARDEN-EXISTING
```

Required production proof:

```text
Account scope pipeline
CreateWorkspace permission
Workspace + owner WorkspaceMember mutation
grant-projection synchronization
slug uniqueness/concurrency
transaction
```

This is the pure Workspaces local vertical-slice reference.

## WG-FLOW-02 — Invitation acceptance workflow

Compatibility alias:

```text
WG-REF-001
```

Pinned:

```text
AcceptInvitation
→ Identity.Public facts
→ Accounts.Public admission facts/action
→ Invitation.Accept
→ WorkspaceMember
→ grant projection
```

Transaction:

```text
BOUND-TX-002
```

This is the cross-BC workflow/target-mutation reference.

## WG-FLOW-03 — Workspace membership fact → Collaboration activity

Pinned producer:

```text
WorkspaceMemberAddedIntegrationEvent
```

Pinned real consumer:

```text
WorkspaceMemberAddedActivityConsumer
```

Disposition:

```text
REUSE/HARDEN-EXISTING
```

Required:

```text
Workspaces owns membership fact
Collaboration owns Activity projection
Platform owns delivery
duplicate event does not duplicate logical activity
Account/Workspace scope propagated correctly
```

Log-only/stub consumers do not count.

## WG-FLOW-04 — Governance local permission mutation

Pinned source:

```text
GrantResourcePermissionCommand
RevokeResourcePermissionCommand
```

Disposition:

```text
REUSE/HARDEN-EXISTING
```

This is the Governance local mutation reference.

Required separate scenarios:

```text
work-management.board
documents.page
```

Each scenario proves:

```text
ResourceKind
required PermissionAction
subject semantics
workspace/account scope
granter authority
audit record
duplicate/replace behavior
```

M5 Wave 5 frozen Page ACL semantics (reviewed source decision — the
mechanism already in production; freeze records the authority, it does not
change behavior):

```text
ACL management authority (grant / read / revoke on documents.page
resource-permission rows) = workspace Owner OR active page
ResourcePermission rank >= Manager; lower ranks and plain members hold no
management authority; applicable explicit Allow/Deny rules evaluate
deny-first through the single canonical evaluator.

Grant ceiling  = max(requested rank, target's existing active rank);
actor authority must meet it. Same subject+level grant is a semantic upsert
(one active row); DB unique active-subject index arbitrates the race.

Revoke ceiling = the target's existing active rank; revoke soft-deletes the
active row; a denied revoke leaves the row untouched; being the subject of a
row is not management authority.

Page visibility/audience is neither ACL-management authority nor lifecycle
authority. ArchivePage keeps its own PermissionAction and its own Manager-rank
ladder frozen at M7 (M2G extension): ManagePagePermission never archives and
ArchivePage never manages the ACL.

Cross-account addressing of a permission target is NotFound (hidden) on every
path including revoke; same-account cross-workspace follows RESOURCE-SCOPE-A.
```

## WG-FLOW-05 — Governance local authoritative query

Pinned source:

```text
GetResourcePermissionsQuery
```

Required:

```text
reads Governance-owned permission state
does not query protected resource aggregate
scope is enforced before returning sensitive permission state
```

## WG-FLOW-06 — Canonical authorization pipeline

Compatibility alias:

```text
WG-REF-002
```

Pinned production examples:

```text
MoveBoardItem
CreateComment
CreatePage
```

At least one Work and one Documents/Collaboration protected resource path must
execute:

```text
request
→ canonical AuthorizationBehavior
→ permission evaluator/decision store
→ owner facts
→ allow/deny
→ handler only after allow
```

No second authorization engine.

---

# 29. TAC-RAP-WM — Work Management Real Flow Catalog

Owned BC:

```text
Work Management
```

## WM-FLOW-01 — Local pipeline-first mutation

Compatibility alias:

```text
WM-REF-001
```

Pinned:

```text
CreateBoardInWorkspace
```

Required proof:

```text
auth/account/workspace/idempotency pipeline
WorkManagement local DbContext
Domain mutation
no foreign BC Port for pipeline concerns
```

## WM-FLOW-02 — Consumer-owned Collaboration read

Compatibility alias:

```text
WM-REF-002
```

Pinned:

```text
IWorkManagementCollaborationReadPort
→ Infrastructure adapter
→ approved Collaboration read boundary
```

This demonstrates consumer Port + runtime adapter.

## WM-FLOW-03 — Producer target action

Compatibility aliases:

```text
WM-REF-003
AI-REF-002 target side
```

Pinned:

```text
IWorkItemActions.MoveItem
→ producer-local MoveBoardItem use case
```

carried-forward runtime requirements remain mandatory:

```text
WorkspaceId enforced
ExecutorUserId authorized
OperationId deduplicated
target mutation atomic with target idempotency
```

## WM-FLOW-04 — Producer Integration Event

Compatibility alias:

```text
WM-REF-004
```

Required event roles:

```text
BoardItemMemberAssignedIntegrationEvent
→ Automation trigger

BoardItemMovedIntegrationEvent
→ Analytics placement update
```

WorkManagement owns both facts.

## WM-FLOW-05 — Producer projection-source query

Pinned:

```text
IWorkItemProjectionSource
```

Required owner implementation:

```text
WorkManagement Application
→ IWorkManagementDbContext
→ WorkItemPlacementSnapshot
```

Analytics may consume the Public contract but cannot own its implementation.

---

# 30. TAC-RAP-DC — Documents & Collaboration Real Flow Catalog

Owned BCs:

```text
Documents
Collaboration
```

## DC-FLOW-01 — Documents local creation

Pinned source:

```text
CreatePageCommand
```

Disposition:

```text
REUSE/HARDEN-EXISTING
```

Required:

```text
workspace/account scope
authorization
Page aggregate creation
PageCreatedDomainEvent
persistence
transaction
```

This is the Documents local vertical-slice baseline.

## DC-FLOW-02 — Documents lifecycle mutation

Pinned target:

```text
ArchivePageCommand
```

Current Application handler is `NotImplementedException`, while the `Page`
aggregate already owns an `Archive(...)` transition and
`PageArchivedDomainEvent`.

Disposition:

```text
IMPLEMENT-MISSING
```

Required implementation:

```text
resource-scoped authorization
→ load Page in Documents
→ Page.Archive(...)
→ save through request transaction
→ PageArchivedDomainEvent
→ producer integration-event mapping if current mapper defines it
```

Do not use `PublishPage` as the bootstrap lifecycle flow because current Page
Domain semantics do not expose a Publish transition.

## DC-FLOW-03 — Collaboration comment on Work item

Compatibility aliases:

```text
DC-REF-001
DC-REF-002
```

Pinned:

```text
CreateCommentCommand.ForBoardItem
```

Required:

```text
canonical SharedKernel ResourceRef
target owner = WorkManagement
comment owner = Collaboration
target existence/auth resolved separately
no Work aggregate navigation
```

## DC-FLOW-04 — Collaboration comment on Document Page

Pinned:

```text
CreateCommentCommand.ForPage
```

This is a separate mandatory resource variant.

Required:

```text
ResourceKind = documents.page
target owner = Documents
comment owner = Collaboration
Page-specific authorization/resource lookup path
no Documents aggregate mutation
```

The test must not assume the BoardItem permission path automatically proves
the Page path.

If the current permission action for the Page variant is only a reused generic
`ManageBoard` semantic, the flow must either:

```text
prove that action is the canonical governed permission for Page comments
or
STOP-AUTHZ-SEMANTIC and correct the permission contract
```

No guess.

## DC-FLOW-05 — Collaboration Activity projection

Pinned real consumer:

```text
CommentCreatedActivityConsumer
```

Required chain:

```text
Comment mutation
→ Collaboration-owned CommentCreatedIntegrationEvent
→ Platform delivery
→ Collaboration Activity projection
→ local Activity query/read path
```

This is a real projection reaction, not a log-only consumer.

## DC-FLOW-06 — Collaboration Mention → Notification

Pinned real consumer:

```text
MentionCreatedNotificationConsumer
```

Notifications remain Collaboration semantics, not a 12th BC.

Required hardening:

```text
MentionCreatedIntegrationEvent
→ carries all authoritative Account/Workspace/actor/target facts needed by
  the notification materialization
→ NotificationItem/Recipient store correct AccountId
→ idempotent by source mention/event identity
```

Current `Guid.Empty` AccountId materialization is not certifiable.

If the producer event currently lacks AccountId, migrate the Collaboration
event contract rather than inventing account scope in Infrastructure.

## DC-FLOW-07 — Documents outward event producer

Pinned producer facts:

```text
PageCreatedIntegrationEvent
PageArchivedIntegrationEvent
```

Documents owns these facts.

Existing `DocumentsStubConsumers` / log-only consumers do not satisfy a
business-consumer requirement.

They must be:

```text
removed
or renamed/classified as observability-only
```

and excluded from Real Flow evidence.

---

# 31. TAC-RAP-AI — Automation & Integrations Real Flow Catalog

Owned BCs:

```text
Automation
Integrations
```

## AI-FLOW-01 — Automation local rule creation

Pinned:

```text
CreateAutomationRuleCommand
```

Required:

```text
Automation-owned Rule mutation
Governance authorization
Billing capability check
Billing usage consistency model
no private PlanTier branching
```

This is the Automation local vertical-slice reference.

## AI-FLOW-02 — Work fact → Automation process

Compatibility alias:

```text
AI-REF-001
```

Pinned:

```text
BoardItemMemberAssignedIntegrationEvent
→ Automation consumer/evaluator
→ AutomationExecution
```

Required:

```text
source-event idempotency
durable execution identity
scope/actor
outbox intent
```

## AI-FLOW-03 — Automation Process → Work target action

Compatibility aliases:

```text
AI-REF-002
AI-REF-003
```

Pinned:

```text
AutomationExecution
→ production MoveItem action executor
→ IWorkActionPort
→ ACL
→ WorkManagement Public MoveItem
```

All carried-forward target security/idempotency/runtime requirements remain mandatory.

## AI-FLOW-04 — Automation → N8n provider operation

Compatibility alias:

```text
AI-REF-004
```

Pinned:

```text
N8nDispatchRequestedV1
→ real dedup delivery transaction
→ N8nDispatchConsumer
→ N8nDispatchUseCase
→ Integrations.Public IN8nWebhookActions
→ Integrations provider Port
→ Infrastructure N8nClient
```

Retry model remains the v2.6 delivery-owned retry model.

## AI-FLOW-05 — Integrations Calendar connection + binding creation

Pinned entry:

```text
ConnectCalendarCommand
```

Current handler is `NotImplementedException`.

Disposition:

```text
IMPLEMENT-MISSING
```

### Distinct semantic authorities

Current source has:

```text
IntegrationConnection
→ generic provider relationship
→ stable provider account/tenant identity
→ connection status
→ credential reference/version

CalendarIntegration
→ Workspace-scoped Calendar binding
→ AccountId
→ WorkspaceId
→ ConnectionId
→ CalendarProvider
→ SyncDirection
→ IsActive
```

These MUST NOT be collapsed.

Generic Connection product authority allows explicit Account/Workspace/resource
scope. `CalendarIntegration` is explicitly Workspace-scoped.

Therefore v2.6 pins:

```text
CalendarIntegration = Workspace-scoped product binding
IntegrationConnection scope = explicit connection product semantics
```

not “all IntegrationConnections are Workspace-scoped”.

### Required target flow

```text
authenticated request
→ trusted Account + target Workspace
→ Governance manage-integration authorization
→ provider authorization material at inbound boundary
→ Integrations secret-store Port
→ Infrastructure stores raw secret
→ returns SecretRef + version
→ create/reuse IntegrationConnection
→ persist IntegrationSecretVersion(SecretReference)
→ advance IntegrationConnection current-secret version/pointer
→ create CalendarIntegration(
     AccountId,
     WorkspaceId,
     ConnectionId,
     CalendarProvider,
     SyncDirection)
→ commit Integrations-owned DB state
```

### Secret persistence authority

Current persistence does not make `IntegrationConnection.CurrentSecretRef`
itself the durable secret-reference source.

Pinned model:

```text
IntegrationSecretVersion
→ persisted SecretReference for connection/version

IntegrationConnection.CurrentSecretVersion
→ current version/pointer state

CalendarIntegration.ConnectionId
→ binding to the generic Connection
```

Do not certify a flow that only calls `RotateSecret()` in memory.

### Logical connection identity

Before creating a generic Connection, resolve the stable provider account/
tenant/subject identity where available.

Duplicate OAuth callback must not accidentally create a second logical
Connection.

### Failure/compensation

Define:

```text
secret stored but DB transaction fails
duplicate callback
same logical provider relationship already exists
Calendar binding already exists
```

No distributed atomicity claim with secret infrastructure.

Raw access/refresh tokens MUST NOT enter Domain, events, Activity, Analytics,
frontend state or ordinary logs.

Freeze:

```text
TAC-FRZ-019
```
## AI-FLOW-06 — Calendar binding disconnect + generic Connection policy

Pinned entry:

```text
DisconnectCalendarCommand
```

Its ResourceRef is:

```text
integrations.calendar-integration
```

So the selected product resource/mutation authority is `CalendarIntegration`,
not generic `IntegrationConnection` by default.

Disposition:

```text
IMPLEMENT-MISSING
```

Required first transition:

```text
resource-scoped authorization
→ load CalendarIntegration
→ verify Account/Workspace
→ CalendarIntegration.Deactivate(...)
→ persist Calendar binding
```

Generic Connection cleanup is a separate policy.

A Calendar disconnect MUST NOT automatically call
`IntegrationConnection.Disconnect(...)` unless product policy proves the
generic provider relationship should also be revoked.

Mandatory decision:

```text
CAL-CONN-001

when the last relevant Calendar binding is deactivated:
A. retain generic Connection
or
B. disconnect/revoke generic Connection
```

If no current authority selects A/B:

```text
STOP-CALENDAR-CONNECTION-LIFECYCLE
```

Provider/secret cleanup then follows the chosen generic Connection policy and
must distinguish success/retryable/terminal/unknown outcome.

No distributed atomicity claim.

Freeze:

```text
TAC-FRZ-019
```
## AI-FLOW-07 — Integrations verified inbound Calendar webhook

Pinned entry:

```text
HandleCalendarWebhookCommand
```

Current handler is `NotImplementedException`.

Disposition:

```text
IMPLEMENT-MISSING
```

The Application command is NOT the raw-provider authenticity boundary.

Canonical flow:

```text
API/Infrastructure raw HTTP request
→ size/content/type bounds
→ signature/secret verification
→ timestamp/replay verification
→ resolve trusted IntegrationConnection
→ resolve active CalendarIntegration
→ derive trusted AccountId/WorkspaceId
→ Infrastructure inbound provider-delivery receipt/dedup
→ enqueue/dispatch provider-neutral semantic input
→ Integrations Application translation
→ exact approved target-context action/event
→ target-owned semantic success
```

### Inbound-delivery state

Do NOT reuse outbound `WebhookDelivery` merely by name.

Existing `InboundWebhookEvent` is architecture debt for provider/ops intake and
must not become Domain authority automatically.

v2.6 pins:

```text
Infrastructure-owned inbound delivery/dedup record
```

unless a real user-facing Domain lifecycle for the existing type is first
proven and its LegacyGap classification removed.

Technical receipt identity includes at least:

```text
ConnectionId
provider
provider delivery/event identity
received-at
verification/processing status
payload hash/reference as technically needed
```

The exact target context, action/event contract, authorization, idempotency,
and success condition are a required Product/Integrations decision. Provider-
neutral processing alone cannot mark the receipt `Processed`.

### Authentication and tenant routing

Forbidden:

```text
IAuthenticatedRequest user session
as provider webhook authentication

AccountId/WorkspaceId trusted from provider JSON
```

Trusted tenant is derived from verified Connection + CalendarIntegration.

Provider delivery identity and product fact identity remain distinct.

Freeze:

```text
TAC-FRZ-019
```
# 32. TAC-RAP-BI — Billing & Entitlements Real Flow Catalog

Owned BC:

```text
Billing
```

## BI-FLOW-01 — Capability/capacity resolution

Pinned consumer:

```text
CreateAutomationRule
```

v2.6 distinguishes:

```text
feature capacity/quota
≠ billable commercial UsageMetric
```

`AUTOMATION_RULE` is a feature-capacity reference unless product authority
explicitly classifies it as a billable metric.

Required calculation:

```text
applicable Entitlement
→ target scope
→ effective status/time
→ capacity limit
→ reserved/consumed capacity
→ requested amount
→ availability/remaining
```

### Zero-limit semantic freeze

Decision ID:

```text
BILL-LIMIT-001
```

Current audited behavior treating numeric zero as unlimited is not sufficiently
governed.

Before certification:

```text
STOP-BILLING-LIMIT-SEMANTIC
```

must be resolved by product authority.

The decision must explicitly define:

```text
zero capacity meaning
unlimited representation
```

A coding agent MUST NOT infer unlimited from numeric zero.

Workspace-targeted entitlement applies only to that Workspace.

Account-scoped capacity must define whether consumption is Account-global or
partitioned by Workspace.

### Entitlement precedence freeze

The applicable entitlement set is filtered BEFORE ordering:

```text
Status = Active
AND (ExpiresAt IS NULL OR ExpiresAt > now)
AND (TargetScope = Account
     OR (TargetScope = Workspace AND TargetWorkspaceId = requested WorkspaceId))
```

Then deterministic ordering picks the winner:

```text
Workspace-targeted beats Account-scoped
same scope -> CreatedAt DESC, then Id DESC
LIMIT 1
```

A post-row expiry check that drops the selected winner instead of filtering
it before precedence is FORBIDDEN: an expired Workspace grant must never
shadow a valid Account grant.  TAC-BI-001 pins this precedence; every
productive resolve path (BillingCapabilityFactsProvider, AccessFactsQuery)
must follow it.

### Used / Remaining authority

```text
Used := SUM(feature_usage_ledger.delta)
        scoped (account_id, workspace_id, feature_code)
        in EVERY state (finite, zero, unlimited, missing)
```

```text
finite numeric limit -> Limit = N, Remaining = max(0, N - Used)
zero numeric limit   -> Limit = 0, Remaining = 0 (Used = actual ledger sum)
explicit unlimited   -> Limit = NULL, Used = actual ledger sum,
                        Remaining = NULL (NULL = unbounded; history retained)
missing entitlement  -> IsAvailable = false, Limit = NULL,
                        Used = actual ledger sum, Remaining = NULL
```

Freeze:

```text
TAC-FRZ-020
```
## BI-FLOW-02 — Billing hard-capacity mutation

This is the mandatory Billing-local mutation baseline for `AUTOMATION_RULE`.

Use capacity vocabulary:

```text
reserve
consume/commit
release
current capacity
```

Do not silently treat the flow as billable UsageMetric ingestion.

Required Billing-owned semantic action may be:

```text
IBillingCapacityActions
```

or an exact owner-equivalent.

Required identity:

```text
AccountId
WorkspaceId where scoped
CapabilityCode
Amount
LogicalOperationId
SourceResource
OccurredAt
```

### Hard-quota concurrency

Idempotency is not enough.

Pinned invariant:

```text
remaining capacity = 1

two concurrent reserve/create requests
→ at most one capacity reservation succeeds
```

The implementation must use an explicit repository-supported strategy:

```text
atomic conditional update
optimistic concurrency/version retry
serialized row/aggregate lock
reservation record + unique invariant
```

Forbidden:

```text
check count < limit
→ later increment
```

without concurrency protection.

### Reservation lifecycle

Hard capacity requires:

```text
reserve
→ feature mutation
→ commit/consume
```

or an equivalent atomic protocol.

Feature failure must lead to explicit:

```text
release
expiry
or compensation
```

Same LogicalOperationId + same request = one effect.

Same LogicalOperationId + conflicting request = deterministic conflict.

### Owner-action fail-closed rule

The owner action must decide capacity from the capability fact's availability,
not from its limit field alone.

```text
Unavailable fact (IsAvailable = false, any Limit) -> effective ceiling 0
Available unlimited (IsAvailable = true, Limit = NULL) -> no ceiling
Available finite (IsAvailable = true, Limit = N) -> ceiling N
Missing fact -> zero ceiling (fail closed)
```

A missing or unavailable entitlement MUST NOT map to a NULL ceiling
(unlimited) because its Limit happens to be NULL.

### First-use atomicity + materialization

Get-or-create of `WorkspaceFeatureUsage` must be atomic under concurrency:

```text
atomic INSERT ... ON CONFLICT DO NOTHING
-> reload the authoritative row
-> reconcile effective limit if stale
-> consume/release
```

The seed MUST materialize CurrentUsage from the SUM of the ledger for the
scope/capability, never a hard-coded zero, so the concurrency authority can
never drift below the committed-usage authority.  A first-use race must end
with both consumers served (both succeed) up to the limit — no false
unique-scope conflict.

### Effective-limit reconciliation

Entitlement is the limit authority; `WorkspaceFeatureUsage` is only the
usage/concurrency authority.  The effective limit is reconciled lazily on
every capacity op (via `WorkspaceFeatureUsage.ReconfigureLimits`):

```text
limit changed up   -> HardLimit/SoftLimit follow the fact
limit changed down -> usage retained (transient over-limit allowed)
                      Remaining = 0, new consumption denied, never delete
unlimited granted  -> HardLimit = NULL (unbounded)
```

### LogicalOperationId global identity

`LogicalOperationId` is a caller-supplied global dedup identity, not scoped
to an Account/Workspace pair:

```text
one partial unique index on logical_operation_id (WHERE NOT NULL)
semantic payload = (AccountId, WorkspaceId, CapabilityCode, signed Delta,
                    SourceResource)
identical payload -> replay (no second effect)
any difference against an executed id (including another scope)
                     -> deterministic conflict
```

If AUTOMATION_RULE becomes billable in future, add a separate metric Flow Card
with metric key/unit/aggregation/period/source/correction semantics.

Freeze:

```text
TAC-FRZ-020
```
## BI-FLOW-03 — AutomationRule lifecycle → Billing capacity

Pinned caller:

```text
CreateAutomationRule
```

Required hard-capacity behavior:

```text
Billing reserve capacity
→ AutomationRule create
→ Billing commit/consume capacity
```

or the exact frozen M2 consistency protocol providing the same invariant.

Mandatory race proof:

```text
remaining = 1

two concurrent CreateAutomationRule commands
→ exactly one successful capacity-consuming creation
```

No stale capability-check race.

If a real capacity-releasing Automation lifecycle exists:

```text
release/retire
→ Billing capacity release
```

If none exists, do not invent one.

### AUTOMATION_RULE existence-capacity semantic

Frozen at candidate (no capacity-releasing delete/archive lifecycle exists):

```text
AUTOMATION_RULE capacity = rule EXISTENCE capacity
CreateAutomationRule       -> consume one slot (already pinned above)
SetAutomationRuleEnabled   disable/enable -> NO release / NO re-reserve;
                              the created rule continues to hold its slot
Capability code stays AUTOMATION_RULE; never ACTIVE_AUTOMATION_RULE
```

If product authority later requires active-only accounting, that is a new
lifecycle (disable -> release; enable -> reserve with the same last-slot
race/idempotency protections) and requires a product decision before
implementation.

Consistency model:

```text
BOUND-TX-003
or
IDEMPOTENT-CAPACITY-PROTOCOL
```

must define reservation failure, feature failure, retry and compensation.

Freeze:

```text
TAC-FRZ-020
```
## BI-FLOW-04 — Capability after real capacity mutation

Required production proof:

```text
capability before
→ reserve/consume capacity through Billing owner path
→ capability after
```

Expected remaining capacity changes according to canonical limit semantics.

Also prove:

```text
concurrent last-slot consumption
duplicate LogicalOperationId
conflicting duplicate
reservation release/compensation
```

A test that manually seeds usage/current count is supplementary only.
## BI-FLOW-05 — External Billing provider

For the audited candidate:

```text
NOT-REQUIRED
formal disposition BI-REF-003 = NOT-APPLICABLE-AT-CANDIDATE
```

Do not fabricate Stripe.

If a real commercial provider appears:

```text
STOP-SOURCE-DRIFT
```

and add a provider-specific Flow Card.

---

# 33. TAC-RAP-AR — Analytics & Reporting Real Flow Catalog

Owned BC:

```text
Analytics / Reporting
```

## AR-FLOW-01 — Live Work placement projection

Compatibility alias:

```text
AR-REF-001
```

Pinned:

```text
Work Integration Events
→ Platform delivery
→ Analytics placement consumers
→ WorkspaceWorkItemPlacementProjection
```

Placement remains outside Domain.

Required:

```text
idempotency
ordering/revision
Workspace isolation
source authority separation
```

## AR-FLOW-02 — Analytics local read

Pinned:

```text
GetWorkspacePlacementsQuery
```

or the exact production local query after v2.6 normalization.

Required:

```text
query reads Analytics local projection only
no normal-path WorkManagement query/DbContext
freshness semantics exposed/documented where needed
```

This is the Analytics local-query copy-model.

## AR-FLOW-03 — Projection rebuild

Pinned:

```text
RebuildWorkspacePlacementsCommand
```

Required:

```text
Analytics rebuild
→ consumer source adapter
→ WorkManagement.Public IWorkItemProjectionSource
→ WorkManagement Application owner implementation
→ snapshot
→ local projection reconcile
```

No outbox-as-event-store.

No Analytics-owned direct Work DbContext access.

## AR-FLOW-04 — Projection failure/recovery case

Mandatory behavior case:

```text
duplicate event
stale event
missing producer snapshot
rebuild after drift
cross-workspace isolation
```

This is a separate flow scenario even when implemented by the same service.

---

# 34. TAC-RAP-PF — Platform & Foundation Real Flow Catalog

This pack owns technical mechanisms, not business truth.

## PF-FLOW-01 — Request execution pipeline

Pinned runtime:

```text
request
→ authentication
→ account/workspace/resource scoping
→ authorization
→ idempotency where declared
→ transaction/data session
→ handler
→ commit
```

Required production proof uses a real protected write flow.

## PF-FLOW-02 — Domain Event → Integration Event → Outbox

Pinned:

```text
Domain mutation
→ Domain Event
→ producer mapper
→ versioned Integration Event
→ outbox enrollment
→ same transaction commit
```

Required producer references include at least:

```text
WorkManagement
Workspaces
Identity
```

## PF-FLOW-03 — Broker delivery + tenant restoration + dedup

Pinned runtime:

```text
outbox dispatcher
→ event bus
→ TenantContextConsumeFilter
→ DeduplicationConsumeFilter
→ real consumer
```

Required:

```text
message identity
consumer identity
tenant scope
dedup claim
consumer mutation
dedup success
single transaction
```

## PF-FLOW-04 — Delivery retry/failure

Pinned:

```text
technical transient failure
→ delivery-owned retry
→ terminal/dead-letter policy
```

Must distinguish:

```text
business rejection
technical retryable
unknown outcome
poison/non-retryable
```

The N8n flow is the runtime proof.

## PF-FLOW-05 — Contract evolution and recovery capability

Current Platform source contains:

```text
event descriptors/version compatibility
upcasters
ReplayEngine/checkpoints
```

Required reference proof:

```text
versioned event compatibility decision
upcast where applicable
replay checkpoint/audit behavior
```

This remains technical and cannot mutate producer business truth arbitrarily.

## PF-FLOW-06 — Background actor/security context

Pinned target reference:

```text
Automation → Work MoveItem
```

Required:

```text
explicit executor identity
Account/Workspace scope
target authorization
correlation/causation
no system-context permission bypass
```

---

## PF-FLOW-07 — Scoped Integration Event tenant envelope

Platform owns the technical event envelope used to restore tenant context.

Required invariant:

```text
tenant-scoped business event has WorkspaceId
→ same event has authoritative AccountId
```

Required production proof:

```text
producer
→ outbox
→ broker
→ TenantContextConsumeFilter
→ Account/Workspace tenant
→ consumer
```

Global/System event classification must be explicit.

This is the reference for every future Integration Event family.

Freeze:

```text
TAC-FRZ-018
```

---

# 35. Cross-pack Real Flow Graph

The final framework is not eight isolated demos.

```text
Identity UpdateProfile
  → local Identity baseline

Identity Register
  → Accounts.Public personal-account provisioning
  → BOUND-TX-004
  → IdentityRegistrationCompletedV1
  → Platform
  → Workspaces provisioning

Workspaces CreateWorkspace
  → local Workspace baseline

AcceptInvitation
  → Identity Public fact
  → Accounts Public fact/action
  → BOUND-TX-002
  → WorkspaceMemberAdded
  → Platform
  → Collaboration Activity

Governance GrantResourcePermission
  → local Governance mutation

Protected Work/Documents/Collaboration requests
  → canonical Governance authorization pipeline

Work CreateBoard
  → local Work baseline

Automation MoveItem
  → Work Port/ACL/adapter
  → Work Public target mutation

Work member-assigned/moved facts
  → Platform
  ├→ AutomationExecution
  └→ Analytics placement projection

Documents CreatePage
  → local Documents baseline

Documents ArchivePage
  → real lifecycle mutation + event

Collaboration CreateComment.ForBoardItem
Collaboration CreateComment.ForPage
  → ResourceRef variants

CommentCreated
  → Collaboration Activity projection

MentionCreated
  → Collaboration Notification materialization

Automation CreateAutomationRule
  → Billing capability
  → Billing usage mutation

Integrations ConnectCalendar/DisconnectCalendar
  → IntegrationConnection + secret reference lifecycle

Provider webhook
  → verified Integrations inbound boundary

N8n dispatch
  → Integrations provider Port
  → Infrastructure N8n adapter

Analytics live/query/rebuild
  → local derived-state framework

Platform
  → request/outbox/delivery/dedup/retry/evolution/background context
```

This graph is the requested team copy-model.
# 36. Reference implementation reachability rule

A reference implementation must not be dead code in the sense of being
unexercised.

Every reference must be reachable by at least one:

```text
unit test
integration test
architecture reference test
```

For runtime-bound references:

```text
DI resolution test
```

is also required.

A public API route is not required.

---

# 37. Reference implementation discoverability

Each team pack must be discoverable without reading the execution document.

Use:

- intentional source placement;
- test names;
- concise XML/docs comments only where needed;
- existing architecture hard docs;
- team/agent routing.

Do not add `ReferenceExample.cs` files scattered around production.

The production shape itself should be the example.

---

# 38. No “example-only” business namespaces

Forbidden production namespace patterns:

```text
Examples
Samples
DemoArchitecture
Playground
FakeFeature
```

Reference implementation must inhabit the same canonical architecture that a
real feature will use.

If the feature is not user-facing yet, the lack of API exposure is sufficient.

---

# 39. Reference slice minimality

A reference slice should be the smallest complete path.

Example:

```text
Automation Process
```

does not require implementing:

- rule builder UI;
- all trigger types;
- all condition operators;
- scheduler product;
- provider catalog;
- audit dashboard.

It only needs enough semantic state to prove:

```text
event → process → target action → outcome
```

---

# 40. Reference flow quality bar

Every mandatory reference slice must be:

```text
compile-valid
DI-valid
behavior-tested
architecture-tested
failure-tested
idempotency-aware where required
transaction-aware
ownership-correct
remote-substitutable at adapter boundary
```

A stub returning fixed success fails this quality bar unless the fixed behavior
is a deliberate test fake outside production.

---

# 41. M4 Accounts↔Workspaces transaction remains a blocker

The prior blocker remains.

Required decision:

```text
A. separate mutation authorities / separate commit semantics

B. reviewed shared-transaction exception BOUND-TX-002

C. ownership/workflow redesign
```

v2.6 retains:

The selected outcome MUST be used as the team reference implementation for
target-owned mutation / transaction design.

There is no longer a path where this seam remains merely “deferred because
future feature absent”.

It is real source and must be frozen.

---

# 42. Public purity blocker remains

`ARCH-BC-005` must be hardened.

Target:

```text
System.*
exact approved technical Common primitives
producer's own Public
```

Forbidden by default:

```text
arbitrary Application.Common.*
foreign Producer.Public
Domain
DbContext/repository
Infrastructure
Platform
API
EF/provider/transport
internal Application
```

Reference implementations make this gate more important because they create
more Public surfaces.

---

# 43. Infrastructure/Services anti-regrowth remains

The baseline must be monotonic.

Target:

```text
actual == reviewed baseline
```

Reference adapters introduced by team packs MUST NOT use generic Services as a
shortcut.

---

# 44. Common semantic leakage + public-signature purity

Application/Common owns technical mechanisms.

It MUST NOT own or expose BC business vocabulary such as:

```text
PlanTier
WorkspaceRole
AccountRole
Entitlement
Permission policy
BC lifecycle enums
BC aggregate/value-object types
```

v2.6 closes a current loophole:

```text
namespace/name purity
is insufficient
```

Every public signature under:

```text
Notrelix.Application.Common.*
```

must be inspected.

Forbidden examples:

```text
Common interface method(AccountRole role)
Common result returns WorkspaceRole
Common DTO contains Billing Entitlement
Common generic constraint references BC business type
```

Current touched debt:

```text
Application/Common/Tenancy/IAccessGrantProjectionService
→ AccountRole
→ WorkspaceRole
```

Because IA/WG real flows use this seam, it cannot remain the canonical teaching
boundary.

Target options are owner-preserving only:

```text
Accounts/Workspaces map owner role semantics
→ stable technical grant descriptor
→ Common mechanism

or

move semantic projection contract into the owning BC
```

Architecture fitness MUST inspect:

```text
method parameters
return types
properties/fields
generic arguments
base/interface dependencies
```

Freeze:

```text
TAC-FRZ-017
```
# 45. Composite read adapter freeze remains

Identity Bootstrap remains the composite read reference.

A composite read is valid only when:

```text
presentation/read composition
no foreign mutation
no policy recreation
no cross-BC ORM navigation
```

This is a supporting reference in addition to team packs.

---

# 46. Public fact semantic scope remains frozen

Reference implementation proliferation makes broad booleans dangerous.

Current facts such as:

```text
CanParticipate
CanAdmitMember
```

must have exact inclusion/exclusion semantics.

A reference consumer MUST NOT assume:

```text
CanAdmitMember
```

also means:

```text
Billing seat available
Governance permits invitation
Identity is valid
Workspace is active
```

unless the contract explicitly owns those semantics.

---

# 47. N8n behavior freeze remains

N8n configuration parsing must retain behavior for:

```text
webhookPath
webhook_path
whitespace
leading slash
invalid JSON
missing property
non-string property
```

The Integrations reference pack must not break these while normalizing provider
architecture.

---

# 47A. Scoped Integration Event tenant-envelope invariant

Platform tenant restoration currently treats `AccountId == null` as System
context.

Therefore:

```text
WorkspaceId != null
→ AccountId != null
```

for every tenant-scoped business Integration Event.

Only explicitly Global/System events may omit tenant scope, and such events may
not perform tenant mutation without an approved authoritative scope-resolution
step.

Known event families requiring audit include:

```text
WorkManagement board/item events
Documents page events
Collaboration comment/mention events
Workspaces membership events
```

Producer mappers MUST preserve AccountId when owner Domain state/events already
have it.

Architecture fitness scans:

```text
Integration Event contracts
producer mappings
consumer runtime tenant restoration
```

Runtime proof goes through `TenantContextConsumeFilter`.

Freeze:

```text
TAC-FRZ-018
```

---

# 48. Broker/Event freeze

Broker architecture must be frozen before team event references are considered
complete.

Producer owns:

```text
event meaning
version
scope/resource identity
```

Platform owns:

```text
outbox
delivery
retry
order
dedup
dead-letter
broker adapter
```

Consumer owns:

```text
reaction
local mutation/projection/process
```

---

# 49. Reference event naming

Integration Events SHOULD be named as completed facts.

Good shape:

```text
WorkspaceMembershipChangedV1
BoardItemChangedV1
DocumentUpdatedV1
EntitlementChangedV1
```

Exact names must follow actual repository event conventions.

Bad:

```text
UpdateAnalyticsNow
ExecuteAutomationNow
InvalidateConsumerCache
```

unless the producer business fact is genuinely that such a request was created.

---

# 50. Reference process naming

A Process Manager name reflects workflow ownership.

Example shape:

```text
AutomationActionExecutionProcess
```

not:

```text
CrossContextOrchestrator
SharedWorkflowService
```

The process belongs to the workflow owner.

---

# 51. Reference projection naming

Projection names reflect consumer-owned derived meaning.

Example shape:

```text
WorkActivityProjection
```

under Analytics.

Not:

```text
WorkManagementReplica
```

unless the consumer genuinely maintains a replica with explicit semantics.

---

# 52. Reference Port naming

Port names use consumer language.

Example:

```text
Automation/Ports/WorkManagement/IWorkActionPort
```

not necessarily the same interface name as producer Public.

This allows:

```text
consumer semantics
→ adapter
→ producer semantics
```

---

# 53. Producer Public capability and action naming

Producer Public is organized by producer-owned published capability; the action
type inside that capability describes the producer-owned mutation semantic.

Canonical example shape:

```text
WorkManagement/Public/Items/IWorkItemActions
```

The directory must not be selected by coding-agent preference and must not be a
technical artifact bucket such as `Commands`, `Actions` or `Contracts`. If the
capability later develops independent semantic sub-capabilities, split by those
business meanings rather than by C# artifact kind.

It should not be named after the consumer:

```text
IAutomationWorkActions
```

because producer Public is not owned by Automation.

---

# 54. Reference ACL requirement

At least one mandatory pack must demonstrate an ACL.

Preferred:

```text
Automation consumer Work action Port
→ maps Automation action request
→ WorkManagement.Public action request
```

If the semantic shapes match exactly, do not create a useless mapper.

In that case, use a different real pack where translation is meaningful.

But the execution as a whole must include one non-ceremonial ACL reference.

---

# 55. Reference direct-Public requirement

At least one mandatory pack must demonstrate direct Producer.Public consumption
without a consumer Port.

Current candidate:

```text
Workspaces
→ Identity.Public fact
```

This prevents teams from learning:

```text
every cross-context read requires a Port
```

---

# 56. Reference target-mutation requirement

At least two paths must be covered conceptually:

```text
Workspaces → Accounts target mutation
Automation → WorkManagement target mutation
```

The first teaches transaction boundary.

The second teaches background/process-driven cross-context mutation.

Both preserve target ownership.

---

# 57. Reference Integration Event requirement

At least one Work Management event chain MUST be complete.

A second Workspace membership event chain is recommended and required if needed
to teach Governance/security reaction.

Do not create dozens of reference events.

One or two high-quality complete chains are better.

---

# 58. Reference Projection requirement

Analytics projection MUST be implemented even if no Analytics product screen
currently uses it.

This is an explicit change from version 1.0.

The purpose is to teach future Analytics work how to consume source facts
without table joins becoming semantic authority.

---

# 59. Reference Process requirement

Automation Process Manager MUST be implemented even if the broader Automation
feature set is incomplete.

This is an explicit change from version 1.0.

It provides the canonical long-running workflow example for future teams.

---

# 60. Reference Billing requirement

Billing Public entitlement/capability surface MUST be implemented even if no
current consumer has fully migrated.

At least one reference consumer must be wired/tested.

This prevents future features from hardcoding plan tiers.

---

# 61. Reference Governance requirement

Governance must have an executable authoritative permission reference.

This may use existing authorization infrastructure.

The reference must make the boundary visible:

```text
Identity = principal
Workspaces = membership
Billing = commercial capability
Governance = permission decision
Resource BC = resource state
```

Do not collapse these into one generic authorization object.

---

# 62. Reference Documents/Collaboration requirement

A comment/activity target reference must demonstrate:

```text
source resource stable ID
Collaboration local state
separate authorization/existence
```

This becomes the canonical resource-reference pattern for:

- comments;
- mentions;
- activities;
- attachments where applicable;
- Automation targets where appropriate.

---

# 63. Reference Integrations requirement

At least one provider-facing flow must demonstrate:

```text
Application semantic Port
→ Infrastructure provider adapter
→ provider transport/SDK
→ translated semantic result
```

Provider error mapping must distinguish:

```text
business/provider semantic failure
technical transient failure
configuration/credential failure
```

according to existing error model.

---

# 64. Team × BC × real-case minimum coverage

A team-level checkmark is no longer sufficient.

| Team | BC / technical owner | Local flow | Auth/pipeline | Public read | Port/ACL/Adapter | Target mutation | Event | Projection | Process | Provider/Webhook |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| IA | Identity | required | required where protected | producer fact | n/a | registration workflow caller | producer required | n/a | registration workflow | technical Identity providers only |
| IA | Accounts | owner mutation required | n/a | required | Public target action | required | optional | grant projection support | n/a | n/a |
| WG | Workspaces | required | required | producer fact | consumes IA Public | required | required | optional | invitation workflow | n/a |
| WG | Governance | required mutation + query | **required** | authoritative path | optional | permission mutation | optional | decision/audit derived state as existing | n/a | n/a |
| WM | Work Management | required | required | projection source required | required | producer required | required | producer source only | n/a | n/a |
| DC | Documents | required creation + lifecycle | required | optional | resource target source | local mutation | producer required | optional | n/a | n/a |
| DC | Collaboration | required comment | required per ResourceKind | optional | ResourceRef/target boundary | local mutation | producer/consumer required | Activity/Notification required | n/a | n/a |
| AI | Automation | required rule | required | consumes Billing | Work Port+ACL | required | consume + produce | optional | **required** | consumes Integrations |
| AI | Integrations | **required connection lifecycle** | Governance required | optional | provider Ports | local connection mutation | optional | sync state as applicable | provider operation lifecycle | **required inbound/outbound** |
| BI | Billing | required usage mutation + capability query | Governance separate | **required** | Public capability/usage | producer usage action | recommended source facts | derived totals optional | commercial lifecycle as product grows | only when real |
| AR | Analytics | local query required | read auth | consumes Work Public rebuild source | source adapter | source mutation forbidden | consume required | **required** | rebuild workflow | n/a |
| PF | Platform/Foundation | technical runtime | request/runtime | n/a | mechanism | n/a | delivery | projection delivery support | delivery/replay | broker/runtime |

For every cell marked `required`, at least one named `*-FLOW-*` must provide
production evidence.

A flow may satisfy multiple required cells only when the same production
execution genuinely performs those mechanisms.
# 65. Team pack completion state

Pack state:

```text
NOT-STARTED
INVENTORIED
FLOW-CATALOG-FROZEN
IMPLEMENTING
REAL-FLOWS-IMPLEMENTED
VERIFIED
FROZEN
BLOCKED-DECISION
BLOCKED-SOURCE-DRIFT
```

A business Team Pack cannot reach `FLOW-CATALOG-FROZEN` until:

```text
every owned BC has a local baseline flow
+
every required mechanism cell maps to a named Real Flow
+
every current stub selected as a target is dispositioned
```

It cannot reach `FROZEN` until every mandatory flow is `VERIFIED`.

`DEFER-NO-REAL-FLOW` is forbidden for BC-local baselines.
# 66. Real Flow production policy

Production reference code must satisfy:

```text
real owner semantics
real runtime reachability
no fake output
no throw NotImplementedException
no TODO-only handler
no test-only production branch
no marker-only architecture type
no hidden feature flag merely to make the flow exist
```

A current `NotImplementedException` command may be selected only as
`IMPLEMENT-MISSING` when accepted Domain/product semantics already define the
behavior.

A `*StubConsumer*` or logging-only consumer:

```text
does not count as Real Flow evidence
```

If retained for intentional observability:

```text
rename/classify it as observability-only
exclude it from business consumer registries/evidence
```

where repository runtime semantics require that distinction.
# 67. Reference data persistence policy

A reference flow may introduce persistence only if the mechanism genuinely
requires durable state.

Examples:

```text
Automation Process state
Analytics Projection state
```

Do not add tables merely because other patterns have tables.

If a reference process/projection can reuse an existing generic durable
mechanism without semantic ambiguity, prefer reuse.

Any migration must be intentional and tested.

---

# 68. Reference API exposure policy

No reference pack is required to expose a public HTTP endpoint.

Expose API only when:

```text
the product capability is already part of the intended API
```

Otherwise test the reference through:

```text
Application
Infrastructure
Platform
integration composition
```

This prevents temporary demo endpoints from becoming accidental contracts.

---

# 69. Reference background execution policy

Automation/Event reference consumers are allowed to execute as background
actors.

They MUST define:

```text
actor identity/principal model
workspace/account scope
authorization mode
correlation
idempotency
```

No background flow receives an implicit security bypass.

---

# 70. Transaction rules for reference flows

Local producer mutation:

```text
one BC
→ one transaction
```

Cross-BC target mutation:

```text
target BC owns mutation
```

If caller and target currently share atomic transaction:

```text
explicit exception decision
```

No reference flow may teach:

```text
because shared DbContext exists
→ cross-context atomicity is normal
```

---

# 71. Failure semantics

Each reference flow must distinguish:

```text
business failure
technical failure
unknown outcome
validation failure
authorization failure
not found
conflict/concurrency
```

as applicable.

Adapters map technical failures.

Business semantics remain in Application/Domain owner.

---

# 72. Idempotency

Required reference coverage:

```text
target mutation
event consumer
Automation Process
Projection update
provider operation when replayable
```

Idempotency key ownership must be explicit.

---

# 73. Concurrency

Reference flows that mutate existing aggregates must preserve current
concurrency model.

Do not introduce new optimistic concurrency strategy merely for the demo.

If current aggregate uses revision/version:

```text
reference uses it
```

---

# 74. Projection ordering

Analytics reference must define one of:

```text
strict revision/order
last-write by producer revision
commutative aggregation
rebuild-on-gap
```

It cannot leave event order undefined.

---

# 75. Projection rebuild

The Analytics reference projection must have an explicit rebuild story.

Possible current-stage implementation:

```text
replay Integration Events
or
read producer snapshot/fact through an approved rebuild adapter
```

The rebuild source must not become arbitrary foreign table access.

---

# 76. Process progression

Automation reference process must define:

```text
initial
running
completed
failed
```

and at least:

```text
duplicate outcome
technical retry
business failure
```

If compensation is not required by the selected reference action:

```text
state explicitly why
```

---

# 77. Broker delivery

The event reference should use the repository's actual outbox/delivery model.

Do not build a separate `ReferenceEventBus`.

The same mechanism used by future production events should be proven.

---

# 78. Contract versioning

All outward Integration Event reference contracts must follow current repository
versioning policy.

For the pinned v2.6 outward reference contracts, use the repository's explicit
versioned contract convention:

```text
...V1
```

Do not invent a different versioning convention for these references.

Do not version local internal commands merely for symmetry.

---

# 79. Public contract versioning

Public synchronous facts/actions do not automatically need transport-style
version suffixes.

Version according to repository Public contract policy.

Remote transport versioning is a future adapter concern.

---

# 80. Reference test requirements

Each mandatory reference flow requires:

```text
positive behavior
business failure
invalid scope/auth where relevant
idempotency/replay where relevant
DI resolution
architecture boundary
migration/persistence where relevant
```

Process/projection/event references require integration-level proof.

---

# 81. Architecture fitness functions

Existing gates remain owners:

```text
ARCH-BC-001 foreign persistence
ARCH-BC-002 foreign Domain
ARCH-BC-003 producer internal Application
ARCH-BC-004 cross-BC ORM navigation/cascade
ARCH-BC-005 Public purity
ARCH-BC-006 Application transport/provider purity
ARCH-BC-007 Integration Event ownership/version
ARCH-BC-008 Common semantic leakage
```

Reference code MUST pass all.

---

# 82. Structural fitness functions

Existing structural gates remain owners:

```text
STN-ARCH-001 no placeholder
STN-ARCH-002 real Port placement
STN-ARCH-005 no Data/ReadPorts regrowth
STN-ARCH-006 Services no-growth
STN-ARCH-007 no marker-only canonical folders
STN-ARCH-008 old seam disappearance
```

v2.6 reference folders are allowed only when they contain real code.

---

# 83. Required gate hardening

Before final freeze:

## ARCH-BC-005

Must deny:

```text
all Common wildcard
all foreign Public wildcard
```

## STN-ARCH-006

Must use monotonic exact baseline semantics.

## ARCH-BC-008

Must prevent reference packs from pushing business vocabulary into Common.

## ARCH-BC-006

Must keep Broker/provider clients outside Application business code.

---

# 84. Team adoption rule

Once a Reference Architecture Pack is FROZEN, new features in that team should:

```text
copy mechanism
not copy business names
```

Example:

Automation future action:

```text
use AI-REF-002 shape
```

but define its own:

```text
consumer semantic Port
target action request
failure mapping
```

as required.

---

# 85. Real Flow anti-patterns

Forbidden as completion evidence:

```text
interface + no production caller
event + no producer
event + only StubConsumer/log-only consumer claimed as business reaction
process class + no production entry/progression
projection class + no real source/query/rebuild
Public DTO that mirrors aggregate
Port that is a ceremonial alias
adapter that owns producer persistence
provider SDK in Application/Domain
fake controller only for reference
Common business enum for convenience
NotImplementedException handler counted as implemented
test directly calls Port while production never does
manual SaveChanges test that bypasses production transaction owner
one BC flow used to claim another BC's local baseline
generic ResourceRef test used to claim all target-resource authorization variants
```

Existing stub/log-only source must be classified explicitly rather than used to
inflate pack coverage.
# 86. Team Operating Pack acceptance criteria

A mandatory Flow is `REAL-FLOW-IMPLEMENTED` when:

```text
production source exists
entry/runtime reachability exists
owner semantics execute
DI resolves
behavior tests execute
```

A BC is `BASELINE-COMPLETE` when:

```text
its local baseline flow is verified
+
all required BC-specific architecture cases are verified
```

A Team Pack is `FROZEN` only when:

```text
every owned BC is BASELINE-COMPLETE
every required team×BC×case cell has a verified Real Flow
semantic ownership proven
scope/actor/auth proven
transaction/idempotency proven where relevant
failure/retry semantics proven
runtime-owner integration tests pass
architecture gates pass
team-copy rules documented
all blockers resolved
```

A paired team cannot be frozen when only one of its BCs is complete.
# 87. Team implementation independence

The reference packs should allow teams to develop in parallel.

Do not create one giant `SharedContracts` package that all packs depend on.

Preferred dependency graph:

```text
producer Public
consumer Port
consumer ACL
runtime adapter
Platform mechanism
```

Parallel teams coordinate contract changes, not internal implementations.

---

# 88. Contract ownership in parallel work

For a producer-owned Public contract:

```text
producer team approves semantic meaning
consumer team owns its Port/ACL
```

For an Integration Event:

```text
producer owns schema/version
consumer owns handler/reaction
Platform owns delivery
```

For target mutation:

```text
target team owns action semantics
caller team owns consumer Port if needed
```

---

# 89. Team handoff contract

Every team receives:

```text
1. BC Ownership Card
2. Real Flow Catalog
3. Flow Cards
4. mechanism decision table
5. STOP rules
6. copy-model
```

Minimum BC Ownership Card:

```text
Team:
BoundedContext:
AuthoritativeState:
MutationAuthorities:
LocalBaselineFlow:
ProducedPublicContracts:
ConsumedPublicContracts:
ProducedEvents:
ConsumedEvents:
OwnedProjections:
OwnedProcesses:
ProviderBoundaries:
KnownDebt:
```

Each Flow Card uses the mandatory fields from section 25.2.

A future coding agent starts from:

```text
identify BC
→ identify case/mechanism
→ find verified Real Flow in same BC/team
→ copy/adapt structure
```

For every new cross-BC edge, the coding agent MUST additionally record:

```text
closest verified FlowId:EdgeId
mechanism TAC-XC-A..F
semantic/contract/mutation/workflow owners
current binding
remote readiness
transaction/consistency model
failure/retry/idempotency ownership
```

If no verified case is sufficiently close, stop for architecture review rather
than inventing a new interaction style.

not:

```text
search repo randomly
→ infer architecture from nearby folders
```
# 90. STOP conditions

Implementation MUST STOP the affected flow when:

- semantic owner unclear;
- mutation owner unclear;
- workflow owner unclear;
- the team pack has no local baseline for the affected BC;
- coding requires treating another BC's flow as the local baseline;
- selected current handler is `NotImplementedException` and accepted product/
  Domain semantics do not define the missing behavior;
- a `StubConsumer`/log-only consumer is being claimed as a business reaction;
- shared transaction required but unjustified/unrecorded;
- Public contract would expose Domain/internal/provider types;
- consumer needs foreign DbContext to make the flow work;
- event cannot be stated as producer fact;
- event lacks scope/actor facts required by its real consumer and no approved
  authoritative resolution path exists;
- projection has no rebuild source;
- Process has no durable progression semantics;
- background actor security is undefined;
- provider secrets would enter Domain/event/log/activity/analytics;
- provider webhook authentication is being replaced by user-session auth;
- Billing plan tier would leak into consumer;
- Billing usage has no logical dedup identity;
- Governance private role/policy would leak into consumer;
- ResourceRef variant has no target-specific authorization mapping;
- Broker routing is becoming product semantics.
- any actual cross-BC edge remains unclassified under TAC-XC-A..F;
- a consumer Port becomes a CRUD/foreign-repository facade;
- an Integration Event is actually an ephemeral instruction to a consumer implementation;
- a current shared transaction is changed to eventual/remote interaction without reopening its BOUND-TX decision;
- a remote adapter requires Application business code to depend on HTTP/gRPC/provider transport DTOs;
- a target action is called remotely without an explicit unknown-outcome/idempotency policy where duplicate mutation is possible;
- a provider unknown outcome is treated as safe blind retry without accepted reconciliation/idempotency semantics;

STOP means:

```text
do not invent the missing semantic decision
record exact blocker
return to product/architecture authority
```

It does not mean replacing the flow with a fake demonstration.
# 91. Flow disposition model

Every mandatory flow receives one pre-code disposition:

```text
REUSE-EXISTING
HARDEN-EXISTING
MIGRATE-EXISTING
IMPLEMENT-MISSING
FREEZE-EXACT-DEBT
BLOCKED-DECISION
BLOCKED-SOURCE-DRIFT
NOT-APPLICABLE
```

`IMPLEMENT-MISSING` is valid only when:

```text
required architecture case
+
accepted product/Domain semantics
+
no real current implementation
```

It is invalid when the agent would need to invent new product semantics.

After implementation:

```text
REUSED
HARDENED
MIGRATED
IMPLEMENTED-MISSING
```

then:

```text
VERIFIED
FROZEN
```

`NOT-APPLICABLE` can apply to an optional mechanism, never to a mandatory
BC-local baseline.
# 92. Freeze registry

Mandatory blockers/subjects remain:

```text
TAC-FRZ-001 Accounts↔Workspaces transaction
TAC-FRZ-002 Public purity
TAC-FRZ-003 Infrastructure/Services no-regrowth
TAC-FRZ-004 Composite read adapter
TAC-FRZ-005 Public fact semantic scope
TAC-FRZ-006 N8n behavior
TAC-FRZ-007 Broker/Event boundary
TAC-FRZ-008 Common semantic leakage
```

v2.6 retains the bootstrap freezes:

```text
TAC-FRZ-009 Mandatory Reference Architecture Packs
TAC-FRZ-010 Automation target-mutation + Process reference
TAC-FRZ-011 Analytics Projection reference
TAC-FRZ-012 Billing entitlement reference
TAC-FRZ-013 Governance authorization reference
TAC-FRZ-014 Collaboration ResourceRef reference
TAC-FRZ-015 Integration provider Port/adapter reference
```

---

# 92A. TAC-FRZ-016 — Team/BC Real Flow Catalog

FROZEN only when all eight Team Packs have a complete BC-level Real Flow
Catalog.

Required BC local baselines:

```text
Identity
Accounts
Workspaces
Governance
Work Management
Documents
Collaboration
Automation
Integrations
Billing
Analytics / Reporting
```

Required technical baseline:

```text
Platform / Foundation
```

For paired teams:

```text
BC A flow cannot satisfy BC B local baseline
```

Required source classifications at the audited candidate include:

```text
Identity UpdateProfile                    → REUSE/HARDEN
Identity Register→Accounts provisioning    → MIGRATE/HARDEN
Identity RegistrationCompleted event       → REUSE/HARDEN

Workspaces CreateWorkspace                 → REUSE/HARDEN
Governance GrantResourcePermission          → REUSE/HARDEN

Documents CreatePage                        → REUSE/HARDEN
Documents ArchivePage                       → IMPLEMENT-MISSING
Collaboration BoardItem comment             → REUSE/HARDEN
Collaboration Page comment                  → REUSE/HARDEN
Collaboration Activity projection           → REUSE/HARDEN
Collaboration Mention notification          → HARDEN scope

Automation CreateAutomationRule             → REUSE/HARDEN
Automation process/MoveItem                  → IMPLEMENT/HARDEN
Integrations N8n provider operation          → HARDEN
Integrations ConnectCalendar                 → IMPLEMENT-MISSING
Integrations DisconnectCalendar              → IMPLEMENT-MISSING
Integrations Calendar webhook                → IMPLEMENT-MISSING

Billing capability                           → HARDEN
Billing usage ingestion                      → IMPLEMENT-MISSING/HARDEN

Analytics live/read/rebuild                  → MIGRATE/HARDEN

Platform request/outbox/delivery/retry       → REUSE/HARDEN
```

A flow classified `IMPLEMENT-MISSING` must become executable production source
before pack freeze.

# 93. TAC-FRZ-009 — Mandatory team packs

FROZEN only when all:

```text
TAC-RAP-IA
TAC-RAP-WG
TAC-RAP-WM
TAC-RAP-DC
TAC-RAP-AI
TAC-RAP-BI
TAC-RAP-AR
TAC-RAP-PF
```

are at least:

```text
VERIFIED
```

and architecture closure certifies them.

---

# 94. TAC-FRZ-010 — Automation target mutation + Process

FROZEN only when:

```text
Work event consumer exists
Automation execution state exists
Process progression exists
Automation Work Port exists
Infrastructure adapter exists
Work Public action exists
target Domain mutation executes
idempotency/failure tests exist
```

No fake success stub.

---

# 95. TAC-FRZ-011 — Analytics Projection

FROZEN only when:

```text
producer event selected/reused
Analytics consumer exists
projection state exists
query/read exists
idempotency exists
ordering semantics exist
rebuild path exists
tests pass
```

Known c409 classification debt:

```text
ReportingSnapshot
→ DomainHardeningArchitectureTests
→ ProjectionClassificationAllowlist
→ LegacyGap
```

The reference implementation must resolve that classification if
`ReportingSnapshot` participates in AR-REF-001:

```text
A. migrate it to the reporting projection model/storage role
or
B. prove/document a genuine user-managed Domain lifecycle and update the
   architecture classification intentionally
```

Do not simply reuse `ReportingSnapshot` while leaving an unexplained
projection-like Domain `LegacyGap`.

If another Analytics-owned derived model is selected, the existing
`ReportingSnapshot` debt remains exact/no-growth and receives its own migration
trigger; AR-REF-001 must not broaden it.
# 96. TAC-FRZ-012 — Billing entitlement

FROZEN only when:

```text
Billing Public capability/entitlement contract exists
producer semantics are Billing-owned
at least one consumer uses it
consumer does not inspect private plan tier
tests pass
```

---

# 97. TAC-FRZ-013 — Governance authorization

FROZEN only when:

```text
Governance authoritative semantic contract exists
standard pipeline vs use-case-specific query boundary is clear
at least one reference consumer/pipeline path exercises it
private Governance internals do not leak
tests pass
```

---

# 98. TAC-FRZ-014 — Collaboration ResourceRef

FROZEN only when:

```text
real Collaboration state references foreign resource by stable ID/ref
no foreign aggregate navigation
authorization/existence remain separate
tests pass
```

---

# 99. TAC-FRZ-015 — Integration provider boundary

FROZEN only when:

```text
Application semantic provider Port
Infrastructure provider adapter
provider error mapping
DI
tests
no provider SDK in Application
```

---

# 99A. TAC-FRZ-017 — Common public-signature semantic purity

FROZEN only when:

```text
Common public signatures contain no BC business types
current AccountRole/WorkspaceRole leakage is removed/owner-relocated
architecture test inspects signature dependency graph
```

# 99B. TAC-FRZ-018 — Scoped Integration Event tenant envelope

FROZEN only when:

```text
Workspace-scoped business events carry authoritative AccountId + WorkspaceId
TenantContextConsumeFilter restores tenant rather than System
producer mappers preserve owner scope
negative gates prevent regression
```

# 99C. TAC-FRZ-019 — Calendar connection/binding/secret lifecycle

FROZEN only when:

```text
IntegrationConnection and CalendarIntegration remain distinct
Calendar binding uses ConnectionId
secret reference persists through authoritative secret-version storage
CAL-CONN-001 generic-Connection cleanup policy is explicit
inbound webhook receipt is technical intake, not outbound WebhookDelivery
provider auth + trusted tenant derivation proven
```

# 99D. TAC-FRZ-020 — Billing capacity semantics + concurrency

FROZEN only when:

```text
zero-limit meaning explicit
unlimited representation explicit
AUTOMATION_RULE classified as capacity unless product says billable
hard-quota last-slot race proven safe
reservation/consume/release or equivalent protocol frozen
duplicate/conflicting operation semantics proven
```

# 100. Reference implementation and source cleanliness

Because v2 intentionally adds reference slices, structural cleanliness is not:

```text
fewest files possible
```

It is:

```text
every file has a semantic role
every reference has an executable purpose
no placeholders
no duplicated architecture
```

A complete reference pack is valuable code.

A dead placeholder is not.

---

# 100A. Historical c409 source anchors — NON-NORMATIVE

Earlier c409 findings are retained only as provenance.

```text
HISTORICAL — NON-NORMATIVE
```

They cannot override the v2.6 audited candidate:

```text
b94312b3e10e2212f18440003e775d415aa1b5d7
```

Current implementation authority comes from v2.6 Flow Cards and the source
anchors below.
# 100B.1 v2.6 current source-audit anchors

At audited PR #113 HEAD `b94312b3e10e2212f18440003e775d415aa1b5d7`:

```text
Identity Register still requires Accounts Public boundary normalization

Accounts lacks an authenticated Account-admin Application command baseline;
Account Domain already owns rename/lifecycle semantics

Application/Common/Tenancy IAccessGrantProjectionService leaks AccountRole/
WorkspaceRole through a Common signature

Documents CreatePage is real
Documents ArchivePage is NotImplemented while Page.Archive exists
Documents PublishPage remains outside bootstrap without Domain Publish semantics

Comment has BoardItem and Page variants

Mention event/mapper lacks authoritative actor semantics
multiple Workspace-scoped outward events require AccountId envelope audit
because Platform treats AccountId == null as System

Automation/Work/N8n carried-forward runtime blockers remain mandatory

Calendar Connect/Disconnect/Webhook handlers are NotImplemented

Calendar source has both:
  IntegrationConnection
  CalendarIntegration(ConnectionId, WorkspaceId, SyncDirection)

secret-reference persistence uses IntegrationSecretVersion/connection version
rather than relying only on IntegrationConnection.CurrentSecretRef

inbound webhook intake must not reuse outbound WebhookDelivery
existing InboundWebhookEvent remains provider/ops LegacyGap unless lifecycle is
proven

Billing numeric zero-limit meaning is not sufficiently product-governed
Billing hard capacity requires concurrency-safe reservation/consume/release
AUTOMATION_RULE must not be assumed billable UsageMetric

Platform contains request/outbox/delivery/dedup/retry/version/replay mechanisms
```

These anchors constrain v2.6 implementation and certification.
## 100B.2 Mention actor-authority correction — v2.6

Current audited source has:

```text
MentionCreatedDomainEvent
→ MentionedId
→ no authoritative creator/mentioner actor

current outward mapping
→ MentionedByUserId derived incorrectly from MentionedId
→ ActorUserId absent
```

Pinned invariant:

```text
mentioned user
≠ actor who created the mention
```

If Activity/Notification requires actor identity, Collaboration must capture it
at mention creation.

Required:

```text
trusted request actor
→ Mention owner state and/or Domain Event actor fact
→ exact Integration Event mapping
→ Activity/Notification consumer
```

Forbidden:

```text
mapper copies MentionedId into MentionedByUserId
consumer reconstructs actor
Infrastructure guesses actor
```

DC-FLOW-06 cannot certify until producer actor authority and mapping exactness
are proven.

# 100C. Carried-forward runtime closure gate — v2.6

The following are mandatory before pack freeze:

```text
AI:
  production MoveItem executor exists
  WorkspaceId is enforced by Work target
  ExecutorUserId is authorized by Work/Governance
  OperationId is actually deduplicated by Work
  N8n retry test runs through real dedup transaction
  transient retry does not pretend to persist state that is rolled back

BI:
  entitlement workspace scope is enforced
  AUTOMATION_RULE usage advances with real rule lifecycle
  usage writes are idempotent
  rule+usage consistency model is explicit

AR:
  projection remains outside Domain
  Work projection source has producer Application implementation
  Analytics adapter does not directly read IWorkManagementDbContext
```

Green CI without these conditions is not architecture closure.

# 101. Completion definition

Complete only when:

```text
11 business BCs each have a verified local operating baseline
+
8 Team Operating Packs FROZEN
+
all mandatory Real Flow Cards VERIFIED
+
all actual cross-BC Flow Card edges classified under TAC-XC-A..F
+
current-pack interaction contracts have explicit current binding, consistency, failure/retry ownership and remote readiness
+
all accepted shared-transaction edges record ExtractionBlocker + RemovalTrigger
+
Accounts local-admin + Accounts Public-action cases both exist
+
carried-forward runtime blockers closed
+
TAC-FRZ-017 Common signature purity FROZEN
+
TAC-FRZ-018 scoped-event tenant envelope FROZEN
+
TAC-FRZ-019 Calendar connection/binding/secret lifecycle FROZEN
+
TAC-FRZ-020 Billing capacity/concurrency semantics FROZEN
+
source topology canonical
+
architecture/runtime-owner tests green
+
stub/log-only evidence excluded
+
team copy-model durable
+
exact candidate SHA certified
```

The closure target is that every team can select a same-case verified flow
without inventing ownership, policy, scope, concurrency or provider semantics.
# 102. Final required Real Flow inventory

At closure production source/tests must contain at least:

```text
IA
1. Identity UpdateProfile
2. Accounts RenameAccount local admin mutation
3. Register → Accounts personal provisioning
4. IdentityRegistrationCompleted → Workspace provisioning
5. Accounts membership authoritative read
6. Accounts membership target action

WG
7. Workspaces CreateWorkspace
8. AcceptInvitation
9. WorkspaceMemberAdded → Collaboration Activity
10. Governance GrantResourcePermission
11. Governance GetResourcePermissions
12. canonical authorization pipeline

WM
13. CreateBoardInWorkspace
14. Work→Collaboration read Port
15. Work Public MoveItem target action
16. Work producer Integration Events
17. Work projection-source Public query

DC
18. Documents CreatePage
19. Documents ArchivePage
20. Comment on BoardItem
21. Comment on Page
22. CommentCreated → Activity
23. MentionCreated → Notification with authoritative actor + tenant
24. Documents outward events

AI
25. Automation CreateAutomationRule
26. Work event → AutomationExecution
27. AutomationExecution → Work MoveItem
28. N8n provider dispatch
29. Calendar Connection + CalendarIntegration binding
30. CalendarIntegration disconnect lifecycle
31. verified Calendar webhook intake

BI
32. capability/capacity resolution
33. hard-capacity reservation/mutation
34. AutomationRule → Billing capacity
35. capability-after-capacity

AR
36. live Work placement projection
37. Analytics local query
38. projection rebuild
39. duplicate/stale/drift recovery

PF
40. request execution pipeline
41. DomainEvent→IntegrationEvent→outbox
42. tenant restoration + dedup
43. delivery retry/failure
44. event version compatibility/replay
45. background actor/security
46. scoped-event tenant-envelope enforcement
```

External Billing provider remains conditional and must not be fabricated.

The count may grow for a genuinely new architecture case and may not shrink by
merging semantically distinct cases.
# 103. Required Real Flow reuse

Future features should reuse the closest verified flow by **case**, not merely
by folder.

Examples:

```text
new Identity local mutation
→ IA-FLOW-01

new cross-BC owner mutation requiring current shared local transaction
→ IA-FLOW-02 / WG-FLOW-02 transaction pattern
→ only after ownership/atomicity review

new protected Governance resource mutation
→ WG-FLOW-04

new Work producer target action
→ WM-FLOW-03

new commentable resource
→ DC-FLOW-03/04
→ add a new variant test if owner/auth semantics differ

new event-driven local projection
→ DC-FLOW-05 or AR-FLOW-01

new Automation target action
→ AI-FLOW-03

new external provider connection
→ AI-FLOW-05/06

new inbound provider webhook
→ AI-FLOW-07

new feature-capacity quota
→ BI-FLOW-02/03

new billable/commercial UsageMetric
→ separate Billing Flow Card; do not copy capacity semantics blindly

new derived Analytics read
→ AR-FLOW-01/02/03

new broker consumer
→ PF-FLOW-03/04

new Workspace-scoped Integration Event
→ PF-FLOW-07
```

Copy the mechanism and invariants.

Do not copy unrelated business semantics.
# 104. What future teams should copy

A coding agent/team follows:

```text
1. identify business BC
2. identify owner of state/mutation
3. classify case:
   local
   authoritative read
   target mutation
   event reaction
   projection
   process
   provider outbound
   provider inbound
   resource reference
4. open the same BC/team Real Flow Catalog
5. select the verified flow with the same case
6. copy:
   pipeline declaration
   owner boundary
   Port/Public/ACL shape
   scope/actor/auth pattern
   transaction/idempotency pattern
   failure/retry ownership
   runtime adapter
   tests/gates
7. substitute only feature-specific semantics
```

If no matching verified flow exists:

```text
STOP
→ classify the new architecture case
→ extend the Team Pack intentionally
```

Do not improvise a seventh interaction mechanism.
# 105. Service extraction rule

Reference implementation must make extraction easier.

It must not make extraction mandatory.

Today:

```text
Consumer Port
→ InProcessAdapter
→ Producer Public/Application
```

Future:

```text
Consumer Port
→ RemoteAdapter
→ transport
→ Producer inbound adapter
→ same Producer Application
```

Consumer business handler remains stable.

---

# 106. Extraction readiness stages

Still:

```text
E0 semantic healthy
E1 runtime introduced
E2 exactly-one-writer
E3 foreign reads/joins removed
E4 physical data move if justified
```

Reference code should establish E0/E1-style seams where appropriate.

Do not split storage merely to demonstrate architecture.

---

# 107. Final architecture principle

The final backend operating model is:

```text
Business semantics
        ↓
Bounded Context ownership
        ↓
Team Operating Architecture Pack
        ↓
BC Real Flow Catalog
        ↓
verified production Flow Card
        ↓
canonical mechanism A–F
        ↓
Application implementation
        ↓
Infrastructure/runtime adapter
        ↓
tests + architecture gates
```

A team should not need to infer how to build its next feature from unrelated
source.

It should be able to answer:

```text
Which BC owns this?
Which real flow is the same architectural case?
What does that flow require for scope/auth/tx/idempotency/failure/runtime?
Which parts do I copy?
Which parts are feature semantics I replace?
```

That is the closure target.

The execution is not complete while the answer is:

```text
“look around the repo and decide.”
```

# 108. Current candidate authority override — PR #158 re-audit

Historical flow rows in this SPEC remain historical evidence. For the current
candidate, the active interpretation is:

```text
PF-FLOW-05 = capability-based contract evolution and recovery.
Work V1 -> V2 = SchemaCompatibility.None + DrainBeforeCutover
                + RebuildFromAuthority.
Generic replay requires a declared retained event source; Platform replay
skeletons without that source are not closure evidence.

AI-FLOW-06 = local lifecycle and generic CAL-CONN-001 are in scope; provider
or secret cleanup is a separate outcome-classified post-commit mechanism.
Provider-specific Google/Microsoft subscription lifecycle is outside TAC-XC
closure.

AI-FLOW-07 = provider-neutral intake is not semantic completion. Exact
downstream target context, action/event, authorization/idempotency, and
target-owned success must be decided by the Product/Integrations authority
before the flow can be certified.
```

No percentage score or architecture-closure claim may be derived from source
inspection alone; affected flows require exact-SHA recertification.
