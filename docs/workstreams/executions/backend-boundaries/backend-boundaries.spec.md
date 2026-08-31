---
document_id: WRK-SPEC-BACKEND-BOUNDARIES
document_type: workstream-spec
status: active
owner: backend-architecture
applies_to:
  - backend
  - bounded-contexts
  - cross-context-dependencies
  - backend-parallel-delivery
  - future-service-extraction
canonical_sources:
  - docs/architecture/bounded-context-map.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/architecture/capability-extraction-strategy.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/cross-team-dependencies.md
  - backend/docs/architecture/backend-overview.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/testing-and-quality-gates.md
review_on:
  - bounded-context-dependency-change
  - cross-context-contract-change
  - data-ownership-change
  - architecture-gate-change
  - service-extraction-proposal
---

# SPEC — Backend Boundary Execution

## 1. Purpose

This execution package operationalizes the existing Notrelix architecture for cross-bounded-context delivery.

It does not redefine bounded contexts, product semantics, service topology, or extraction policy.

Its purpose is to ensure every backend team implements features through the same interaction model so that:

```text
business semantics
        ↓
bounded-context ownership
        ↓
explicit semantic contract
        ↓
adapter / delivery mechanism
        ↓
current or future runtime topology
```

A consumer use case must not need semantic redesign merely because a provider bounded context later moves from the current modular monolith into another deployable.

## 2. Non-goals

This execution MUST NOT:

- create one production project per bounded context;
- declare one service per bounded context;
- freeze candidate service groupings such as Trust, Work, Ecosystem, Commercial, or Insights as current architecture authority;
- introduce HTTP/gRPC between contexts that currently share a process merely to imitate microservices;
- require a complete repository-wide refactor before feature work continues;
- create empty Public/Ports folders for contexts that have no real cross-context consumer;
- move physical databases merely to prepare for hypothetical extraction;
- introduce a global saga service, global business service locator, or generic cross-context manager.

## 3. Core invariant

Application/business code MUST NOT depend on where another bounded context is deployed.

Consumer code must not directly depend on:

```text
foreign DbContext
foreign repository
foreign mutable aggregate/entity
foreign private/internal namespace
HTTP client
gRPC generated client
message-broker SDK
provider SDK
foreign database table as an integration API
```

Cross-context interaction must be expressed through an approved semantic boundary.

## 4. Ownership-first rule

Every use case starts by identifying:

```text
Owning bounded context
Mutation owner
Authoritative fact owners
Owned aggregate(s)
Owned persistence boundary
```

Implementation mechanism is chosen only after ownership and consistency are understood.

A database table, route group, team, namespace, or current process is not sufficient evidence of business ownership.

## 5. Unified cross-context interaction taxonomy

Every material cross-context edge MUST be classified as one or more of the following.

| Need | Approved mechanism |
|---|---|
| Authoritative answer is required now | Synchronous query |
| Target context owns a mutation and caller needs its immediate outcome | Target-owned synchronous command/use-case contract |
| Producer has committed a fact and consumers may react later | Integration event |
| Consumer only needs durable identity/reference | Stable ID / ResourceRef |
| UI/read endpoint needs data from several contexts | API/BFF composition |
| Repeated hot read can tolerate explicit staleness | Consumer-owned local projection/read model |
| Workflow spans several owners over time | Process manager / saga-style coordination |
| Consumer language differs from producer language | Consumer port + anti-corruption layer |
| Independently deployed units later need one stable logical entry point | Domain gateway, only after runtime topology justifies it |

No team may use direct persistence access as a substitute for choosing one of these mechanisms.

## 6. Producer Public Surface

A bounded context may expose a Public surface only when another context has a real dependency.

A Public surface may contain stable semantic artifacts such as:

```text
queries
commands/use-case contracts
facts/decision DTOs
integration-event contracts
stable identifiers/resource references
```

It MUST NOT expose:

```text
Domain aggregate implementation
EF entity
DbContext
repository implementation
provider SDK DTO
internal handler type
persistence-only enum/state
```

Public means cross-context semantic surface, not necessarily network/public internet API.

## 7. Producer contract versus consumer port

Use a producer-owned Public contract directly when the producer's vocabulary is already the correct vocabulary for the consumer.

Example:

```text
WorkManagement
    ↓
Workspaces.Public.IWorkspaceFacts
```

Use a consumer-owned port when the consumer has a distinct semantic need or must translate the producer model.

Example:

```text
WorkManagement
    ↓
IWorkCapabilityPort
    ↓
ACL
    ↓
Billing.Public entitlement contract
```

A new interface MUST NOT be created merely because a dependency crosses a bounded-context folder.

The goal is cohesive semantic surfaces, not interface proliferation.

## 8. ACL versus transport adapter

These are distinct responsibilities.

```text
Consumer
   ↓
Consumer Port
   ↓
ACL                  semantic translation
   ↓
Producer Contract
   ↓
Transport Adapter    in-process / HTTP / gRPC / projection
```

Today an adapter may be an in-process call.

A future extraction may replace only the transport implementation.

Semantic translation must not be hidden inside network-specific code.

## 9. Synchronous query rules

Use synchronous query only when the current use case requires an answer now.

Every cross-context synchronous fact/decision must define:

```text
semantic owner
input identity/scope
freshness requirement
validity/revision when relevant
not-found semantics
unavailable-dependency semantics
race tolerance
security/tenant scope
```

A synchronous query does not automatically provide cross-owner atomicity.

For security- or commercial-sensitive decisions, the design must explicitly consider revision/validity and races between decision and mutation.

## 10. Cross-context mutation rules

If Context A causes state owned by Context B to change:

```text
A requests B-owned behavior through B's contract
```

not:

```text
A mutates B's aggregate/table/repository
```

If A and B must succeed atomically, the team MUST stop and classify the invariant before implementation.

Possible outcomes are:

```text
move/redefine ownership so the invariant is local
accept explicit cross-context strong consistency as reviewed extraction debt
use process-manager/compensation semantics
```

A synchronous command alone is not a distributed-transaction solution.

## 11. Integration-event rules

Integration events represent completed, committed business facts.

Producer event naming describes what happened, for example:

```text
BoardItemChangedV1
MembershipChangedV1
SubscriptionChangedV1
```

Do not publish consumer instructions such as:

```text
RunAutomations
RefreshAnalytics
UpdateSearchIndex
```

A Domain Event is not automatically an Integration Event.

Expected flow:

```text
Domain mutation
  ↓
Domain Event
  ↓
Application mapping
  ↓
versioned Integration Event
  ↓
outbox/post-commit enrollment
```

## 12. Stable references

Cross-context durable relationships SHOULD use stable scalar identity or the repository-approved ResourceRef equivalent.

Suitable use cases include:

```text
comments
mentions
activity/audit subjects
automation targets
document links/embeds
integration mappings
```

Cross-context ORM navigation and cascade ownership are not approved integration mechanisms.

## 13. Data and DbContext ownership

The current shared PostgreSQL and physical ApplicationDbContext are compatible with this execution.

Logical ownership remains bounded-context-specific.

An Application handler may use its own context persistence abstraction, for example:

```text
WorkManagement handler
→ IWorkManagementDbContext
```

It must not solve foreign dependencies by injecting:

```text
IWorkspaceDbContext
IGovernanceDbContext
IBillingDbContext
```

A physical DbContext may implement multiple context persistence abstractions today without granting cross-context access permission.

## 14. Cross-context database relationships

Default policy:

```text
same-context FK
→ allowed

cross-context stable scalar ID / ResourceRef
→ preferred

cross-context ORM navigation
→ forbidden

cross-context cascade
→ forbidden

cross-context physical FK
→ reviewed transitional/integrity constraint, not semantic ownership
```

Any retained cross-context FK must be classified as extraction debt and must not authorize foreign mutation or object-graph ownership.

## 15. Query composition and projections

Cross-context transactional feature handlers SHOULD NOT make private shared-DB joins the permanent integration contract.

Classify reads as:

```text
authoritative small fact
→ synchronous query

multi-context client/read endpoint
→ API/BFF composition

frequent/hot dependency with controlled staleness
→ local projection

reporting/analytics
→ derived analytical model
```

A local projection must define:

```text
source owner
projection owner
freshness/lag
revision/version where relevant
rebuild/recovery
security/tenant scope
failure behavior
```

Projection ownership never replaces source authority.

## 16. Process-manager admission

Use a process manager when a workflow:

- spans multiple semantic owners;
- lasts beyond one local transaction;
- requires explicit progress/state/retry/compensation;
- cannot be represented safely as one synchronous call chain.

The process manager must have a semantic owner.

Do not create a generic GlobalSagaService.

## 17. Current runtime and future extraction

Current default remains the existing modular monolith.

This execution does not authorize new services.

When future operational evidence justifies extraction, the expected path is:

```text
semantic boundary already explicit
        ↓
consumer uses Public contract / port
        ↓
current in-process adapter
        ↓
remote/runtime adapter introduced
        ↓
authority cut over
        ↓
foreign reads/writes removed
        ↓
physical data moved only if justified
```

Service grouping remains a future operational decision driven by measured coupling and extraction pressure.

## 18. Use-Case Boundary Card

Every feature PLAN that crosses a bounded-context boundary MUST record at least:

```text
UseCase
OwningBC
MutationOwner
OwnedAggregates
OwnedPersistence

ForeignDependencies:
  ProviderBC
  Need
  Authority
  Freshness
  Consistency
  Mechanism
  ProducerContract
  ConsumerPort
  ACL
  FailureSemantics

LocalTransaction
Concurrency
Idempotency
EventsProduced
EventsConsumed
StableReferences
ProjectionCandidate
ProcessManagerOwner
CurrentAdapter
FutureRemoteImpact
```

The card may be embedded in the team's existing PLAN; it does not require a separate file per use case.

## 19. Cross-team handshake

When Team A needs a capability owned by Team B:

1. Consumer describes the semantic need, not a table/class it wants to access.
2. Producer and consumer confirm authoritative owner.
3. Dependency is classified using the unified interaction taxonomy.
4. Producer defines/accepts the stable semantic contract.
5. Consumer decides whether direct producer contract use is sufficient or an owned port/ACL is needed.
6. Current adapter is implemented without exposing topology to business code.
7. Producer/consumer integration evidence is added.
8. Dependency readiness is updated through the existing D0-D5 model.

## 20. Required readiness behavior

This execution uses the repository's existing readiness model.

```text
D0/D1
→ no irreversible consumer implementation

D2
→ consumer may scaffold port/adapter/mock and local behavior

D3
→ integration preparation may begin

D4
→ producer/consumer integration may be relied on

D5
→ broad downstream parallelization is allowed
```

The execution does not create a second readiness scale.

## 21. Architecture STOP conditions

Implementation MUST stop for boundary review when a feature appears to require:

- foreign DbContext/repository;
- foreign mutable aggregate/entity;
- foreign private/internal namespace;
- one local transaction spanning multiple bounded contexts without an explicit reviewed invariant;
- cross-context FK cascade;
- shared mutable domain model;
- provider DTO/SDK semantics leaking into another business context;
- consumer dependence on producer private plan/role/internal enum semantics;
- a synchronous chain that would become a high-coupling A→B→C→D path after extraction;
- a new Common abstraction containing unowned business semantics;
- BFF/API transport layer orchestrating business mutation;
- ambiguous authoritative ownership.

STOP means design the boundary before continuing; it does not imply a repository-wide architecture rewrite.

## 22. Rolling-adoption rule

This execution is intentionally incremental.

Do NOT wait for a perfect audit of every bounded context before feature delivery continues.

Adoption model:

```text
global hotspot baseline
      ↓
freeze new violations
      ↓
for each touched use case
    audit touched cross-context edges
    classify them
    formalize only required contracts
    add proof
```

Legacy debt may be baselined, but new violations are not allowed.

## 23. Initial high-priority seams

The first boundary work should focus on dependency-spine hotspots with real consumers:

```text
Workspace facts
Governance authorization
Billing entitlement/capability
```

Then extend only when feature work reaches additional edges such as:

```text
WorkManagement ↔ Collaboration
WorkManagement/Documents → Automation
Automation ↔ Integrations
source contexts → Analytics/Search projections
```

## 24. Reference slices

Initial architecture proof should be small.

Preferred early reference slices:

```text
CreateBoard
→ synchronous Workspace/Governance/Billing dependencies

Automation action against WorkManagement
→ integration fact + target-owned command

Subscription/entitlement change
→ event-fed consumer projection where justified
```

Other patterns are proven when real features require them.

## 25. Definition of Ready

A cross-context feature is Ready when:

- owning bounded context is known;
- mutation owner is known;
- owned persistence is known;
- each foreign dependency is classified;
- required producer contract is at least D2;
- consistency/transaction model is explicit;
- authorization/entitlement implications are explicit;
- event/projection implications are classified;
- no unresolved STOP condition remains.

## 26. Definition of Done

A feature is Done only when:

- business behavior is verified;
- local transaction ownership is clear;
- no foreign persistence bypass was introduced;
- cross-context contract behavior is tested;
- tenant/security scope is preserved;
- authorization is proven where required;
- event/outbox/idempotency/concurrency behavior is proven where applicable;
- architecture fitness functions pass;
- dependency readiness/evidence is updated.

## 27. Success criterion

The architecture is considered extraction-ready for a dependency when changing the provider from in-process to remote requires changes primarily in:

```text
DI/composition
adapter/transport
runtime/deployment
observability/reliability mechanics
```

and does not require redesigning the consumer's business use case or the producer's domain semantics.
