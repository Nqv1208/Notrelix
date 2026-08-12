---
document_id: BE-DOMAIN-MODELING
document_type: architecture
status: active
owner: backend-architecture
applies_to:
  - backend/src/Notrelix.Domain
  - backend/tests/Notrelix.Domain.Tests
evidence:
  - backend/src/Notrelix.Domain/Notrelix.Domain.csproj
  - backend/src/Notrelix.Domain/Common/
  - backend/src/Notrelix.Domain/SharedKernel/
  - backend/src/Notrelix.Domain/Accounts/
  - backend/src/Notrelix.Domain/Identity/
  - backend/src/Notrelix.Domain/Workspaces/
  - backend/src/Notrelix.Domain/Governance/
  - backend/src/Notrelix.Domain/WorkManagement/
  - backend/src/Notrelix.Domain/Documents/
  - backend/src/Notrelix.Domain/Collaboration/
  - backend/src/Notrelix.Domain/Automation/
  - backend/src/Notrelix.Domain/Integrations/
  - backend/src/Notrelix.Domain/Billing/
  - backend/src/Notrelix.Domain/Analytics/
  - backend/tests/Notrelix.Domain.Tests/
  - backend/tests/Notrelix.Architecture.Tests/
review_on:
  - domain-base-model-change
  - aggregate-boundary-change
  - domain-event-contract-change
  - deletion-lifecycle-model-change
  - shared-kernel-admission-change
  - domain-identity-strategy-change
---

# Domain Modeling

> **Domain models encode owned business meaning and deterministic state transitions. They do not discover infrastructure facts, authorize users, talk to providers, or mirror the database merely because those concerns are convenient to access.**
>
> Aggregate boundaries follow consistency and lifecycle—not table count. Identity strategy follows semantic risk—not a rule that every database table must receive a custom `TId` wrapper.

This document is the canonical backend owner for Domain modeling mechanics.

Product rules themselves remain owned by:

```text
../../../docs/product/contexts/
```

This document defines **how** those rules are represented safely in `Notrelix.Domain`.

---

# 1. Domain scope

Domain owns deterministic local business semantics including:

```text
aggregate/entity state
value semantics
owned invariants
state transitions
lifecycle rules
semantic no-op behavior
Domain events
rule-code failures
owned audit/version behavior
```

Domain does not own:

```text
persistence
authorization orchestration
current user discovery
current time discovery
provider calls
cache
HTTP
background delivery
message retry
RLS
OpenAPI
frontend state
```

---

# 2. BE-DOM-001 — Domain remains framework/provider independent

`Notrelix.Domain` MUST NOT depend on outer runtime/provider frameworks to enforce business rules.

Current project evidence intentionally has no package references.

Forbidden examples:

```text
EF Core
DbContext
MediatR handlers
ASP.NET Core
Redis
MassTransit
HTTP clients
provider SDKs
search/storage SDKs
configuration providers
```

---

# 3. Determinism

A Domain operation should produce the same business result from the same:

```text
current Domain state
+
supplied input
+
supplied external facts
```

It should not secretly depend on ambient machine/runtime state.

---

# 4. BE-DOM-002 — Ambient time/random/user/provider state stays outside Domain

Do not read:

```csharp
DateTime.UtcNow
DateTimeOffset.UtcNow
Random.Shared
current HTTP user
environment variable
database query
provider client
```

inside business mutations.

Application/outer layer supplies the fact when needed.

---

# 5. External facts

A Domain rule may legitimately need facts it does not own.

Examples:

```text
actor ID
current timestamp
authorization outcome/fact
parent path
current count
external uniqueness result
quota/entitlement result
provider status
generated random/token input
```

The caller obtains and supplies those facts.

---

# 6. BE-DOM-003 — External fact supply does not transfer fact ownership to Domain

Example:

```text
Billing owns Entitlement
Application asks Billing
Application supplies "feature allowed" fact
Work Management Domain enforces its local transition
```

Work Management does not become the owner of Billing entitlement semantics.

---

# 7. Aggregate root

An aggregate root is a local consistency boundary with meaningful:

```text
identity
lifecycle
invariants
concurrency/version needs
loading requirements
business operations
events
```

Do not create aggregate roots because:

```text
there is a table
there is an API endpoint
there is a class
there is a collection
```

---

# 8. BE-DOM-004 — Aggregate boundary follows invariant ownership

Choose one root when state must be kept consistent through one local business transaction.

Split roots when lifecycle/concurrency/scale/ownership can be independent.

Do not make a giant aggregate to avoid orchestration.

Do not split one invariant across independently mutable roots without an explicit coordination model.

---

# 9. Child entity

A child entity has identity within the aggregate lifecycle and is controlled by the root for root-owned invariants.

Current `Entity` base provides:

```text
Guid identity
Domain event collection
identity equality
Guid v7 creation for default construction
```

A child may own purely local behavior, but it must not bypass root invariants.

---

# 10. BE-DOM-005 — Public child mutation cannot bypass root consistency

If changing a child can affect:

```text
cross-child uniqueness
ordering
aggregate limits
parent lifecycle
aggregate version
root event
```

the aggregate root coordinates the change.

Do not expose a public setter/mutator that lets callers evade that coordination.

---

# 11. Entity identity

Current base identity strategy is intentionally simple:

```text
Entity.Id : Guid
default generation : Guid.CreateVersion7()
```

An explicit constructor rejects `Guid.Empty`.

This is a current source baseline.

It is not a requirement to create one custom identity class per entity/table.

---

# 12. BE-DOM-006 — Do not create typed IDs mechanically per table

Typed IDs MAY be introduced when they provide concrete semantic protection, for example:

```text
public contract ambiguity
cross-context identity confusion
aggregate correctness
high-risk parameter mix-up
stable shared-kernel reference semantics
```

Do **not** require:

```text
UserId
SessionId
BoardItemId
BoardFieldId
BlockId
CommentId
...
```

for every persisted type solely because DDD supports typed IDs.

The cost must buy a real boundary.

---

# 13. Typed ID admission test

Before introducing a typed ID, answer:

```text
Which wrong operation does this prevent?
Does this identity cross a public/context boundary?
Will the type remain stable across persistence/provider changes?
Is the conversion/serialization/codegen cost justified?
Would Guid already be unambiguous inside the aggregate?
```

If no meaningful risk is reduced, keep the simpler identity.

---

# 14. BE-DOM-007 — Identity type is not persistence ownership

Whether identity is:

```text
Guid
typed wrapper
provider external ID
```

does not determine which context owns the entity.

Ownership comes from product semantics.

External provider IDs usually belong in integration/provider mapping rather than replacing Domain identity.

---

# 15. Aggregate root version

Current `AggregateRoot` maintains:

```text
Version : long
```

starting at `1`, incremented through protected checked logic.

Version is part of optimistic concurrency/business mutation semantics where the aggregate uses it.

---

# 16. BE-DOM-008 — Version changes exactly with accepted semantic mutation

A mutation contract should define whether it increments aggregate version.

Default expectation for a meaningful accepted aggregate state change:

```text
increment once
```

Rejected mutation:

```text
increment zero
```

Semantic no-op:

```text
increment zero
```

unless a specific product contract deliberately defines otherwise.

---

# 17. Version overflow

Current version increment uses checked arithmetic.

Do not replace with unchecked rollover.

Version overflow is extraordinary failure, not a normal lifecycle wrap.

---

# 18. Entity equality

Current base entity equality is:

```text
same runtime entity type
+
same Guid Id
```

This is identity equality.

Do not compare entities by all mutable properties as value objects.

---

# 19. Value object

A value object represents immutable semantic value without independent entity lifecycle.

Examples currently shared include concepts such as:

```text
Email
Money
DateRange
Slug
Url
Color
ResourceKind / ResourceRef
```

subject to each type's actual contract.

---

# 20. BE-DOM-009 — Value object equality is structural semantic equality

Current `ValueObject` base compares declared equality components.

A value object should be:

```text
immutable from caller perspective
valid when constructed
deterministic
free of provider/runtime dependencies
```

Do not give it independent persistence identity just because EF maps it.

---

# 21. Value object validation

Invalid semantic value should be rejected at creation/factory boundary.

Avoid creating:

```text
temporarily invalid Email
temporarily invalid DateRange
temporarily invalid Money
```

then expecting every caller to remember to validate later.

---

# 22. BE-DOM-010 — Value object normalization is part of its semantic contract

If semantically equivalent inputs should normalize:

```text
trim
case normalization
canonical format
```

do so deterministically before equality/value use.

Do not hide culture/environment-specific normalization.

---

# 23. Primitive versus value object

Do not wrap every primitive.

Use a value object when it provides meaningful:

```text
validation
normalization
equality
units
domain operation
ambiguity reduction
```

A simple `string` or `Guid` is acceptable when no stronger semantic type is needed.

---

# 24. Enum versus richer lifecycle

Use an enum when:

```text
state set is closed/stable
transition behavior remains readable elsewhere
```

Use richer modeling when:

```text
state carries data
transition rules are complex
enum leads to scattered switch statements
```

Do not choose based on “enterprise style”.

---

# 25. Invariant placement

Place an invariant with the semantic owner that has enough state to enforce it.

Examples:

```text
one Board Item internal value rule
→ BoardItem/Board aggregate as product design defines

last Workspace owner
→ owning Workspace membership aggregate boundary

Page hierarchy cycle rule
→ owning Documents model
```

Do not move an invariant to Application merely because it requires several method calls if Domain owns the underlying state.

---

# 26. BE-DOM-011 — One invariant has one enforcing semantic owner

Other layers can:

```text
prevalidate
optimize
fail early
```

but the authoritative owned invariant remains with its Domain owner when it is a Domain invariant.

Do not duplicate divergent rule implementations across handlers/controllers/providers.

---

# 27. Cross-aggregate rule

A rule can depend on facts from another aggregate.

Do not inject repositories into Domain.

Application obtains immutable facts and calls a pure Domain method.

---

# 28. BE-DOM-012 — Cross-aggregate Domain rule consumes facts, not repository callbacks

Preferred:

```csharp
aggregate.ChangeSomething(otherAggregateFact, occurredAt);
```

Not:

```csharp
aggregate.ChangeSomething(() => repository.LoadOther(...));
```

Repository callback makes Domain depend on persistence timing/failure semantics.

---

# 29. Prepare then apply mutation

For a mutation with several failure conditions:

```text
validate lifecycle
validate IDs/actor
validate business rules
normalize inputs
compute prospective state
prepare audit metadata
        ↓
mutate state once
        ↓
increment version
        ↓
raise completed event
```

This pattern avoids partial mutation.

---

# 30. BE-DOM-013 — Rejected mutation is failure-atomic

If a rule rejects the operation:

```text
owned state unchanged
audit unchanged
version unchanged
pending Domain events unchanged
```

unless the product explicitly defines a separate failure-state mutation.

Do not mutate first and throw later.

---

# 31. Semantic no-op

A no-op means the requested semantic end state is already true and the product contract says no business change occurs.

Example:

```text
set name to already-normalized current name
```

may be a no-op.

---

# 32. BE-DOM-014 — No-op is decided after semantic normalization, before mutation

Do not:

```text
update audit timestamp
increment version
raise event
```

then notice the semantic value was unchanged.

No-op semantics must be explicit per operation where relevant.

---

# 33. No-op versus repeated intent

Not every repeated request is a Domain no-op.

Examples:

```text
"send reminder again"
"record another payment attempt"
"add another comment with same text"
```

can be distinct operations despite similar values.

No-op is product semantics, not value-equality optimization.

---

# 34. Audit fields

Current base model includes shared auditing mechanics.

Audit metadata may track:

```text
created/updated actor
created/updated time
deletion metadata where mechanism applies
```

These are Domain/persistence support facts.

Do not conflate them automatically with:

```text
Governance security Audit
Activity feed
version history
business history
```

---

# 35. BE-DOM-015 — Audit metadata changes only with the mutation contract

For an accepted mutation, prepare/apply audit consistently.

For rejection/no-op, audit should remain unchanged unless a specific product rule says the attempted operation itself is a durable business event—which would normally be modeled separately.

---

# 36. Deletion is product lifecycle first

Notrelix does not define one universal business meaning named “soft delete”.

Different contexts may need:

```text
archive
revoke
remove
cancel
tombstone
disable
resolve
restore
```

The product context determines the lifecycle.

---

# 37. Current soft-deletion mechanism

Current Domain contains reusable types such as:

```text
SoftDeletableAggregateRoot
SoftDeletableEntity
```

The current aggregate base mechanism tracks fields such as:

```text
IsDeleted
DeletedAt
DeletedBy
DeleteReason
```

and supports prepare/apply deletion/restore plus:

```text
EnsureNotDeleted()
EnsureDeleted()
```

This is a **mechanism**.

It is not a mandate to use generic deletion semantics for every aggregate.

---

# 38. BE-DOM-016 — Shared deletion mechanism requires context-specific admission

Before deriving from a soft-deletable base, the owning product context must actually require:

```text
restorable logical deletion/tombstone semantics
```

Do not use it merely because rows should remain physically present.

Persistence retention and product lifecycle are different questions.

---

# 39. Deleted-state mutation

If an aggregate uses deletion/tombstone semantics, operations should state whether they are allowed while deleted.

Use explicit guards such as the current:

```text
EnsureNotDeleted
EnsureDeleted
```

or equivalent context-specific lifecycle logic.

Do not rely only on query filters to protect deleted lifecycle rules.

---

# 40. Restore

Restore must be a product-supported operation.

Do not expose restore just because a base class mechanically supports it.

The context determines:

```text
who can restore
what state comes back
what conflicts can occur
what event/history is emitted
```

---

# 41. BE-DOM-017 — Deletion does not hide previous business state in a repair field by default

Avoid patterns like:

```text
_statusBeforeDeletion
Status.SoftDeleted
```

when deletion is an orthogonal lifecycle/tombstone mechanism.

If the product truly models deletion as a business status, that must be an explicit product decision, not a persistence workaround.

---

# 42. Domain event

Current `DomainEvent` base contains:

```text
EventId : Guid v7
OccurredAt : supplied DateTimeOffset
```

The timestamp must be supplied and valid.

This supports deterministic event occurrence time.

---

# 43. BE-DOM-018 — Domain event represents a completed owned fact

Name event in past-tense business language where appropriate:

```text
ItemMoved
PageArchived
MembershipRevoked
SubscriptionChanged
```

not imperative:

```text
MoveItem
ArchivePage
```

Commands request behavior.

Events state completed facts.

---

# 44. Event identity versus aggregate identity

`EventId` identifies the event occurrence.

Aggregate/resource IDs identify the business subject.

Do not use one as a substitute for the other.

A retry may reuse the same logical message/event identity depending on delivery mapping, while a new business occurrence gets a new event.

---

# 45. Event timestamp

Domain event time should reflect the supplied business occurrence time.

Do not call ambient clock inside the event constructor.

Current base requires `occurredAt`.

Application supplies it.

---

# 46. Event scope

Current shared event bases support:

```text
GlobalDomainEvent
AccountScopedDomainEvent
WorkspaceScopedDomainEvent
```

Account-scoped events require non-empty Account ID.

Workspace-scoped events require both non-empty Account and Workspace IDs.

---

# 47. BE-DOM-019 — Event scope matches the fact

A Workspace-scoped fact must not be emitted as global merely because global delivery is easier.

A global fact must not invent a Workspace ID.

Scope is part of security/routing/observability meaning.

---

# 48. Event name/version

Current `EventNameAttribute` supports:

```text
stable Name
Version (default 1)
```

and constrains name shape/length at attribute construction.

A stable logical event name can support mapping/contract identity.

---

# 49. BE-DOM-020 — CLR class name is not the durable public event identity by default

A refactor:

```text
Rename C# class
```

should not automatically break a public/replayed integration contract.

If a Domain event maps to a public integration event, use deliberate logical naming/versioning.

Detailed outward contract belongs to Platform/API/system event architecture.

---

# 50. Internal Domain event versus integration event

Domain event:

```text
owned fact inside backend Domain model
```

Integration event:

```text
cross-context/deployment/public delivery contract
```

They may be related by mapping.

They are not automatically the same type.

---

# 51. BE-DOM-021 — Provider/transport fields do not pollute Domain event by default

Avoid adding fields only because:

```text
RabbitMQ needs it
frontend websocket expects it
provider requires it
```

Map at the outer contract layer unless the field is a genuine owned fact.

---

# 52. Collection payloads

If an event captures a caller-owned mutable collection, snapshot/copy it.

Do not let later mutation change the meaning of an already-raised event.

---

# 53. BE-DOM-022 — Raised event payload is stable after raise

Event records should contain immutable values/snapshots.

Do not expose mutable collections/state references that can change after event creation.

---

# 54. Secrets in Domain events

Do not put:

```text
access token
OAuth secret
password
payment secret
private provider credential
```

into Domain events.

Secret reference metadata may be modeled if the business/domain genuinely needs a reference without revealing the secret.

---

# 55. Rule failures

Domain business-rule failure should use stable semantic rule/error representation owned by Domain.

Do not throw provider/database exceptions as business outcomes.

A stable rule code can support:

```text
tests
Application translation
diagnostics
```

without exposing implementation details.

---

# 56. BE-DOM-023 — Rule code is semantic, not source-location identity

A rule code should survive:

```text
method rename
folder move
refactor
```

if the business rule is unchanged.

Do not encode line number/class name as contract identity.

---

# 57. Exception versus result

Domain may use exceptions/result types according to established current model.

The architecture requirement is that failure is:

```text
deterministic
semantic
failure-atomic
translatable outward
```

Do not introduce a second competing Domain failure system per feature.

---

# 58. Guard helpers

Shared guard utilities can centralize generic argument checks.

They should not become a giant repository of context-specific product policy.

Generic:

```text
non-empty ID
required string
valid timestamp
```

can be shared.

Context policy:

```text
last Workspace owner cannot leave
```

belongs in the owning model.

---

# 59. SharedKernel admission

Current `SharedKernel` contains genuinely cross-cutting semantic value types and ordering utilities.

A SharedKernel type has a high stability bar because every context can become coupled to it.

---

# 60. BE-DOM-024 — SharedKernel requires stable cross-context meaning

Admission requires:

```text
same semantics in multiple contexts
no provider/persistence dependency
low likelihood of context-specific divergence
clear ownership/review
future extraction usefulness
```

Do not move a type into SharedKernel merely to remove duplicate code.

---

# 61. Common admission

Current `Common` contains Domain infrastructure primitives such as:

```text
Entity
AggregateRoot
DomainEvent
scope interfaces/base events
auditing primitives
exceptions/guards
ValueObject
deletion mechanism
```

Common is for Domain modeling mechanics, not product-specific feature models.

---

# 62. BE-DOM-025 — Common is not a business dumping ground

Do not place:

```text
generic Status
generic Permission
generic Resource
generic Settings
generic UserInfo
```

in Common solely because several contexts have something with the same English word.

Shared name is not shared semantics.

---

# 63. Resource references

A stable cross-context resource reference can be useful for Governance/Activity/Collaboration.

The reference should identify:

```text
resource kind
resource ID
scope as defined by contract
```

without importing the foreign aggregate object.

---

# 64. BE-DOM-026 — Aggregate references another root by immutable reference, not navigation ownership

Prefer:

```text
BoardItem stores BoardId
Comment stores target ResourceRef
```

over:

```text
Comment directly owns BoardItem object graph
```

for cross-root/context relationships.

Persistence navigation convenience must not redefine aggregate boundary.

---

# 65. ResourceKind

A shared resource-kind vocabulary is appropriate when several contexts need a stable cross-context reference contract.

Do not make `ResourceKind` the business hierarchy owner.

The owning context still defines:

```text
what the resource is
its lifecycle
its authorization semantics
```

---

# 66. Fractional ordering/shared ordering

Ordering primitives can be shared if the ordering semantics are truly reusable across Work Management/Documents/etc.

The primitive owns:

```text
key generation/comparison constraints
```

The product context owns:

```text
what is being ordered
who may reorder
what lifecycle/order invariant applies
```

---

# 67. BE-DOM-027 — Ordering primitive does not own product move semantics

Do not put Board-specific or Block-specific business policy into the shared ordering algorithm.

Contexts call the shared deterministic primitive inside their own mutation contract.

---

# 68. Money

A shared Money value can represent:

```text
amount
currency
value equality
```

when semantics are stable.

Billing owns commercial lifecycle and charge/invoice meaning.

Money does not make every pricing decision a SharedKernel concern.

---

# 69. Email

A shared Email value can own normalization/validation.

Identity/Accounts/Notifications can consume it.

It does not own:

```text
email verification lifecycle
notification delivery
provider configuration
```

---

# 70. DateRange

A shared date range can own:

```text
start/end validity
overlap/containment semantics
```

when stable.

A product context still decides what the range means for its feature.

---

# 71. Domain constructors/factories

Construction should create a valid initial object.

Use factories when creation requires:

```text
normalization
several invariants
event creation
clear intent
```

Avoid public setters that let callers assemble invalid aggregate state.

---

# 72. BE-DOM-028 — Constructor/factory does not discover external facts

If creation requires:

```text
current actor
current time
existing count
permission
provider value
```

Application supplies them.

The factory remains deterministic.

---

# 73. Persistence constructor

ORM-compatible protected/private constructors can exist as persistence mechanics.

They should not become a second public creation path.

Do not weaken invariant construction because EF requires materialization support.

---

# 74. Mutable collections

Expose child collections read-only from the aggregate boundary where direct caller mutation would bypass invariants.

Mutation goes through semantic methods.

---

# 75. BE-DOM-029 — Collection mutation goes through intent method

Prefer:

```text
board.AddField(...)
page.MoveBlock(...)
workspace.RemoveMember(...)
```

over:

```text
aggregate.Children.Add(...)
aggregate.Children.Remove(...)
```

when collection membership/order is business state.

---

# 76. Input normalization

Normalize before comparing/current-state no-op checks where normalization is part of semantics.

Examples:

```text
trim name
canonical URL
case-insensitive email
normalized delete reason
```

Do not emit events with pre-normalized stale input when the committed state is normalized.

---

# 77. BE-DOM-030 — Event payload reflects committed semantic state

If the aggregate stores normalized/canonical value, the event should normally describe that accepted value.

Do not publish a value different from the Domain state unless the event contract deliberately represents the original input.

---

# 78. IDs supplied by caller

If Application supplies a child/external ID, Domain validates:

```text
non-empty
not duplicate
appropriate scope/ownership where local facts are available
```

Do not silently regenerate and ignore a supplied identity if it is part of the use-case contract.

---

# 79. Actor ID

Actor ID is an external fact.

Domain can record/use it for owned audit/business semantics.

Domain does not authenticate the actor.

Application/Governance authorizes first.

---

# 80. BE-DOM-031 — Actor presence is not authorization

A non-empty `actorId` only identifies an actor.

It does not prove:

```text
permission
membership
ownership
entitlement
```

Do not encode security based solely on actor ID existence.

---

# 81. Tenant IDs

Account/Workspace IDs carried in aggregates/events identify scope.

They do not prove access.

Application authorizes.

Infrastructure/RLS constrains persistence.

Domain uses scope to protect local consistency where it owns the relationship.

---

# 82. BE-DOM-032 — Tenant scope is immutable where the product identity requires it

If an aggregate belongs permanently to one Account/Workspace, avoid arbitrary public setters that re-parent it.

If transfer is a valid product operation, model an explicit transition with its own invariants/events.

---

# 83. Cross-tenant references

A Domain object should not create a cross-tenant relationship casually.

If both sides' tenant facts are available and the relation must be same-tenant, enforce it.

If not locally available, Application validates/supplies the fact before Domain mutation.

---

# 84. Lifecycle precondition

Each mutation should state valid lifecycle states.

Examples:

```text
cannot modify deleted/tombstoned
cannot accept expired invitation
cannot execute canceled automation
cannot mutate archived resource unless product permits
```

Do not scatter lifecycle checks in controllers/queries only.

---

# 85. BE-DOM-033 — Lifecycle guard precedes mutation

Check invalid lifecycle before changing:

```text
children
status
audit
version
events
```

to preserve failure atomicity.

---

# 86. State transition method

Use business intent names:

```text
Archive
Restore
Revoke
Move
Rename
Complete
Cancel
```

instead of generic:

```text
Update
SetStatus
Patch
```

when the operation carries invariant/lifecycle meaning.

---

# 87. BE-DOM-034 — Generic update method is not a substitute for business transition

Avoid one:

```csharp
Update(dto)
```

that changes unrelated fields and lifecycle without expressing invariants.

Group changes by cohesive product intent.

---

# 88. Bulk mutation

Bulk operations usually orchestrate many aggregate operations in Application.

Do not create a giant Domain root solely to mutate hundreds of independent items atomically.

If the product truly requires atomic batch invariant, model and justify the consistency boundary explicitly.

---

# 89. Large collections

Aggregate design should not require loading unbounded child collections when only a small invariant subset is needed.

Possible options:

```text
separate aggregate
supplied count/fact
database uniqueness constraint reinforcing Domain rule
Application query
projection
```

Preserve semantic owner while controlling load.

---

# 90. BE-DOM-035 — Aggregate size follows consistency, not object-graph convenience

ORM navigations are not the reason to load/own an entire graph.

A large Workspace does not automatically mean Workspace aggregate should contain every Board, Page, Comment, Automation, and Member as child entities.

---

# 91. Database uniqueness

Some invariants require database support for race-free uniqueness.

Domain can validate intent locally.

Infrastructure can enforce unique constraint.

Application handles conflict translation.

This is layered defense, not duplicated semantic ownership.

---

# 92. BE-DOM-036 — Database constraint reinforces, not replaces, product invariant meaning

A unique index knows:

```text
two rows conflict
```

but Domain/Application still own:

```text
what uniqueness means
which scope it uses
what failure the user sees
```

---

# 93. Concurrency

Optimistic concurrency protects against stale writes.

Domain `Version` can be part of this model.

Application/API own expected-version transport/orchestration.

Infrastructure maps/enforces database concurrency.

---

# 94. BE-DOM-037 — Stale conflict does not silently reapply business mutation

Do not automatically reload and overwrite when a mutation depends on stale state.

Return conflict/re-evaluate according to product use-case semantics.

---

# 95. Event after version

For a mutation whose event represents the new aggregate state/version, mutate/apply version consistently before creating the final event payload as needed.

The exact order is part of the aggregate operation contract.

Tests should prove:

```text
state
version
event
```

are coherent.

---

# 96. Event count

One business mutation can raise zero, one, or several Domain events.

Do not enforce:

```text
exactly one event per method
```

as a generic rule.

Raise only facts with real consumers/meaning.

---

# 97. BE-DOM-038 — No-event mutation is allowed when no durable Domain fact contract is needed

Do not create event noise purely for consistency with neighboring aggregates.

Conversely, do not omit an event that is part of a required downstream committed-fact contract.

---

# 98. Event consumers

Domain should not know who consumes the event.

If an event is shaped around one specific consumer's DTO requirement, the boundary is likely wrong.

Map outward in Application/Platform as appropriate.

---

# 99. Domain services

A Domain service is justified only for pure domain logic that:

```text
does not naturally belong to one aggregate/value object
operates on Domain facts
has no infrastructure/provider dependency
```

Do not create a Domain service as a dependency-injection home for repositories.

---

# 100. BE-DOM-039 — Domain service remains pure

No:

```text
DbContext
HTTP
Redis
provider
current user
current time discovery
```

inside Domain service.

If those are needed, Application orchestrates and supplies facts.

---

# 101. Specifications/predicates

Pure reusable Domain predicates can be useful if they encode stable owned semantics.

Do not create generic Specification infrastructure solely to abstract every `if`.

Complexity should buy clarity/reuse.

---

# 102. Policies

A pure Domain policy can calculate/decide from supplied facts.

Authorization policy that needs user/resource/service orchestration belongs to Application/Governance boundary, not Domain solely because the word “policy” sounds domain-driven.

---

# 103. BE-DOM-040 — Domain does not discover authorization state

Domain may enforce an already-owned actor invariant if actor data is part of its state.

General resource/action authorization is resolved before mutation through Application.

---

# 104. Error messages

Domain error messages are diagnostic/user-translatable content, not stable public API unless explicitly specified.

Stable rule/error codes are more durable contract anchors.

Do not make API consumers parse English exception text.

---

# 105. Localization

Domain does not own UI localization.

Store semantic rule code/facts.

API/frontend can map to localized presentation.

Do not inject localization provider into Domain.

---

# 106. Validation layers

Different validation belongs at different seams:

```text
transport format
→ API

request/use-case validation
→ Application

owned invariant/value validity
→ Domain

provider protocol validation
→ Infrastructure adapter
```

Avoid duplicating the same rule with divergent logic.

---

# 107. BE-DOM-041 — Domain validates owned semantics even if outer layer prevalidates

Application validation can fail early for UX/performance.

It does not make Domain accept an invalid owned state when called from another path/test/background use case.

---

# 108. Serialization

Domain types should not be shaped primarily for JSON/provider serialization.

Use mapping/converters/adapters.

A stable Domain event name attribute is acceptable when it expresses logical event identity, not transport implementation detail.

---

# 109. EF mapping annotations

Prefer Infrastructure configuration for persistence-specific mapping when possible.

Do not introduce EF annotations into Domain merely for convenience if they make Domain depend on ORM semantics.

Current Domain purity strongly supports external mapping.

---

# 110. Lazy loading/navigation

Do not rely on ORM lazy loading in Domain behavior.

Domain operation inputs should make required state/facts explicit.

Hidden DB access from property traversal destroys determinism and testability.

---

# 111. BE-DOM-042 — Domain operation has explicit data requirements

A caller should be able to know:

```text
which aggregate state must be loaded
which external facts must be supplied
```

without triggering hidden persistence.

---

# 112. Factory-generated identity

Current base Entity creates Guid v7 by default.

If an external use case requires caller-defined deterministic ID, use the explicit ID constructor/factory path where appropriate.

Do not conflate:

```text
database-generated identity
```

with:

```text
business identity
```

without a deliberate design.

---

# 113. Guid v7 rationale boundary

Guid v7 provides sortable/time-ordered characteristics useful for many systems.

Do not build product behavior that depends on decoding creation time from a Guid unless that is an explicit contract.

Use explicit timestamps for business time.

---

# 114. BE-DOM-043 — Identity generation does not replace business timestamp

`Guid.CreateVersion7()` is identity generation.

`OccurredAt`, `CreatedAt`, etc. are explicit time facts.

Do not infer one from the other as product meaning.

---

# 115. Aggregate construction from persistence

Persistence rehydration must restore valid previously committed state.

Infrastructure mapping may use non-public constructors/setters.

Do not expose those persistence seams as public mutation APIs.

---

# 116. Historical invalid data

If legacy persisted data violates a newly formalized invariant:

```text
migration/backfill policy
```

must resolve it.

Do not weaken Domain invariant permanently solely because bad historical rows exist, unless Product actually wants to change the rule.

---

# 117. Domain model evolution

When a product rule changes:

```text
update product canonical owner
→ update Domain model
→ update Domain tests
→ evaluate persistence/event/contract migration
```

Do not edit Domain behavior first and let documentation/consumer semantics drift.

---

# 118. Event evolution

If an internal Domain event changes without public consumers, refactor may be local.

If mapped to a public/replayed contract, evaluate:

```text
logical event name
version
backlog
replay
consumer compatibility
```

before changing outward semantics.

---

# 119. BE-DOM-044 — Public event compatibility is not inferred from internal type compatibility

Even if C# code compiles, a renamed/removed serialized field can break:

```text
queued message
old worker
replay
external consumer
```

Route public event changes through contract-first delivery.

---

# 120. Domain test contract

Every changed mutation should have focused tests for applicable:

```text
success
rejection
failure atomicity
semantic no-op
audit
version
event
lifecycle
tenant/scope fact
normalization
```

Do not blindly write all categories if they are not part of that operation.

---

# 121. BE-DOM-045 — Test behavior, not implementation ceremony

Tests should assert:

```text
resulting state
failure
event
version
audit/no-op
```

not private helper call order.

Use:

```text
backend/tests/Notrelix.Domain.Tests
```

as primary proof.

---

# 122. Aggregate coverage

Do **not** maintain a manually authored canonical list of every aggregate in this document.

Reason:

```text
aggregate inventory is a changing source fact
```

and should be discovered/generated from source/tests if an inventory is needed.

Representative current Domain context folders are executable evidence.

This avoids turning architecture docs into a second source inventory.

---

# 123. Context-specific modeling

Each product context may choose aggregate boundaries appropriate to its invariants.

Examples of current/representative concepts include:

```text
Identity:
User and identity/security lifecycle concepts

Workspaces:
Workspace, membership, invitation lifecycle concepts

Work Management:
Board, Item, Field and view/work-data semantics

Documents:
Page and Block document semantics

Collaboration:
Comment/share/collaboration semantics

Governance:
resource permission/role/policy semantics

Billing:
Subscription/Entitlement commercial semantics
```

The exact aggregate inventory remains source evidence and can evolve.

---

# 124. Do not mirror context docs

This file should not restate:

```text
what a Board is
what a Workspace owner can do
how Billing quota is calculated
what a ShareLink means
```

unless needed to illustrate a modeling mechanic.

Read product context docs for those truths.

---

# 125. Performance within Domain boundary

Domain design should avoid requiring:

```text
load all tenant history
load all items in Workspace
load all comments in account
```

for one local invariant when a smaller fact can preserve correctness.

Application can supply counts/projections/facts.

Database can reinforce constraints.

---

# 126. BE-DOM-046 — Supplied aggregate facts have clear freshness requirement

If a rule consumes a count/existence/path fact from Application, the use case must define whether:

```text
transactionally current
optimistically checked
eventually consistent
```

is acceptable.

Domain cannot correct a stale external fact by querying infrastructure.

---

# 127. Cross-root reference snapshots

Sometimes a Domain needs an immutable snapshot of another context/root fact.

The snapshot should be:

```text
minimal
semantically named
immutable
owned by the consuming contract
```

Do not copy entire foreign aggregate models.

---

# 128. BE-DOM-047 — Snapshot duplication is intentional contract, not shared mutable model

The producer still owns source truth.

The consumer defines what copied fact it needs and how freshness is maintained.

---

# 129. SecretRef and sensitive references

A Domain-safe secret reference may indicate:

```text
there is a credential/reference
```

without holding secret material.

Outer Infrastructure resolves actual secret.

Do not put secret values in Domain objects/events unless the product explicitly treats the secret itself as protected Domain state with a reviewed design.

---

# 130. ResourceRef and cross-context security

A resource reference identifies a target.

It does not authorize access to it.

Application/Governance resolves:

```text
principal
resource
action
scope
```

before protected mutation/read.

---

# 131. Domain layer and query models

Read-only projection DTOs generally belong outside Domain if they do not enforce Domain behavior.

Do not force all query/report models to be aggregate entities.

CQRS read paths can use Infrastructure/Application projections while preserving source ownership.

---

# 132. BE-DOM-048 — Domain is not the universal data-shape layer

Use Domain for behavior/invariants.

Use read models for query needs.

Do not load aggregates solely to render a read-only dashboard if no Domain behavior is needed.

---

# 133. Analytics calculations

A source-context Domain owns business facts.

Analytics can calculate derived metrics externally.

Only calculations that are themselves source business invariants need to live in the source Domain.

Do not put all reporting formulas into every aggregate.

---

# 134. Billing money/precision

Money value semantics can be shared.

Billing/provider persistence must preserve currency/precision appropriately.

Do not use floating-point shortcuts for monetary invariant if current Money contract uses exact decimal semantics.

Specific Billing rules remain Billing product-owned.

---

# 135. Ordering concurrency

For reorder/move operations:

```text
normalize/validate requested neighbors
generate deterministic ordering key
validate lifecycle/scope
apply mutation
version
event
```

as the context contract defines.

Ordering algorithm should not mutate unrelated product state.

---

# 136. BE-DOM-049 — Ordering failure is failure-atomic

If key generation or neighbor validation fails:

```text
position unchanged
audit unchanged
version unchanged
events unchanged
```

unless product explicitly defines another result.

---

# 137. Restoration and references

Restoring a deleted/tombstoned aggregate may fail if current world state conflicts with the old state.

Examples:

```text
slug reused
parent deleted
entitlement removed
unique name taken
```

Application may need to supply current external facts; Domain enforces owned restore invariants.

Do not assume restore is always the inverse of delete.

---

# 138. BE-DOM-050 — Restore is a new transition against current facts

It is not a time machine.

Restored state must be valid under the current product model.

---

# 139. Event history versus current state

Domain events record facts at occurrence time.

Do not rewrite historical event payload to match current aggregate state later.

If replay/version migration is needed, handle it as event-contract migration.

---

# 140. Rule-code evolution

If a rule's meaning changes materially, decide whether:

```text
same semantic rule with updated condition
or
new rule identity
```

is correct.

Do not reuse one stable code for unrelated failures merely to avoid adding codes.

---

# 141. Public child identity

A child can have Guid identity for addressing without being an aggregate root.

Do not promote it to root solely because API routes can address it.

Ask whether it needs independent:

```text
consistency
loading
concurrency
lifecycle
```

outside parent.

---

# 142. BE-DOM-051 — Addressability does not imply aggregate-root status

An API can target a child resource while Application loads the owning aggregate to enforce invariants.

Transport shape does not define aggregate boundary.

---

# 143. Aggregate repository boundary

Repository interfaces, where needed, are Application/persistence contracts around aggregate loading/saving.

Domain does not own repository implementation.

Do not create repositories for every entity.

Aggregate/root/read use-case needs determine persistence contracts.

---

# 144. Domain purity and testability

Pure Domain architecture allows tests with:

```text
no DB
no container
no host
no provider
```

for owned invariants.

If testing a basic invariant requires booting PostgreSQL, inspect whether infrastructure concerns leaked inward.

---

# 145. BE-DOM-052 — Domain test setup is small relative to the invariant

A complex aggregate can still need fixtures/builders.

But a simple rule should not require full application host, tenant DB, broker, provider.

Use Domain test seam for fast feedback.

---

# 146. Domain architecture tests

Architecture tests should reject:

```text
forbidden package/reference
outer-layer dependencies
provider/framework types
production InternalsVisibleTo expansion
```

where machine-detectable.

Do not rely only on code review for foundational purity.

---

# 147. Change classification

Domain changes can be:

```text
C2 semantic behavior
C3 public event contract
C4 persisted-state migration
C5 aggregate/context boundary
C6 tenant/security invariant
C8 destructive/financial lifecycle
```

Obligations are cumulative.

Do not call every Domain change “internal refactor”.

---

# 148. Domain ADR trigger

An ADR may be required for durable consequential changes such as:

```text
aggregate-boundary strategy
shared identity strategy
major lifecycle/deletion foundation
new shared ordering foundation
cross-context shared-kernel policy
public event identity architecture
```

Routine addition of a context-owned invariant normally does not need an ADR.

---

# 149. Domain change review checklist

```text
[ ] owning product context identified
[ ] aggregate/root boundary correct
[ ] external facts explicit
[ ] no provider/persistence dependency
[ ] identity strategy justified
[ ] lifecycle preconditions explicit
[ ] normalization before no-op
[ ] rejection failure-atomic
[ ] accepted mutation version/audit coherent
[ ] event completed/scoped/immutable
[ ] cross-root refs by ID/snapshot
[ ] shared-kernel admission justified
[ ] focused Domain tests
[ ] public event/persistence migration assessed
```

---

# 150. Typed-ID review checklist

```text
[ ] concrete semantic mistake prevented
[ ] crosses meaningful boundary
[ ] serialization/codegen cost understood
[ ] not introduced per-table mechanically
[ ] does not expose provider identity as Domain ID
[ ] existing Guid baseline insufficient for stated reason
```

---

# 151. Aggregate review checklist

```text
[ ] independent identity
[ ] lifecycle
[ ] invariant boundary
[ ] concurrency need
[ ] load size reasonable
[ ] children cannot bypass root
[ ] external facts supplied
[ ] cross-root refs immutable
[ ] failure atomicity
[ ] no-op semantics
[ ] version/events
```

---

# 152. Value-object review checklist

```text
[ ] semantic value exists
[ ] immutable
[ ] validates construction
[ ] deterministic normalization
[ ] structural equality
[ ] no independent lifecycle
[ ] no provider/persistence dependency
[ ] not a wrapper with zero semantic value
```

---

# 153. Event review checklist

```text
[ ] completed fact
[ ] correct owner
[ ] correct global/account/workspace scope
[ ] occurredAt supplied
[ ] stable payload
[ ] mutable collections copied
[ ] no secret
[ ] logical public identity/version assessed
[ ] no event on rejection/no-op unless explicitly designed
```

---

# 154. Mutation review checklist

```text
[ ] lifecycle first
[ ] actor/IDs/facts valid
[ ] business rules before mutation
[ ] normalization
[ ] semantic no-op
[ ] prospective state prepared
[ ] one coherent mutation
[ ] audit applied
[ ] version exactly once
[ ] event after completed fact
[ ] failure leaves state unchanged
```

---

# 155. Stop conditions

Stop Domain implementation if:

- the owning context is unresolved;
- the aggregate consistency boundary is unresolved;
- the rule requires a repository/provider call from Domain;
- a typed ID is being introduced only because every table “should have one”;
- a child needs public mutation that bypasses root invariant;
- a generic `Update(dto)` would hide several business transitions;
- deletion is being modeled as `SoftDeleted` without product semantics;
- restore behavior is being assumed from persistence mechanics;
- event scope is unclear;
- event is shaped around one transport/provider consumer;
- Common/SharedKernel is being used to avoid ownership;
- a rejected mutation already changed state/version/audit/event;
- a no-op still increments version/emits event without explicit contract.

---

# 156. Executable evidence

Current architecture evidence:

```text
backend/src/Notrelix.Domain/Notrelix.Domain.csproj
backend/src/Notrelix.Domain/Common/
backend/src/Notrelix.Domain/SharedKernel/
backend/src/Notrelix.Domain/{context folders}
backend/tests/Notrelix.Domain.Tests
backend/tests/Notrelix.Architecture.Tests
```

Focused command:

```bash
cd backend
dotnet test tests/Notrelix.Domain.Tests/Notrelix.Domain.Tests.csproj
```

A structural Domain change also requires applicable architecture gates.

---

# 157. Related canonical owners

Product semantics:

```text
../../../docs/product/contexts/
```

Backend architecture:

```text
backend-overview.md
application-model.md
infrastructure-and-data.md
platform-and-messaging.md
security-tenancy-authorization.md
testing-and-quality-gates.md
```

Repository architecture:

```text
../../../docs/architecture/bounded-context-map.md
../../../docs/architecture/data-ownership-and-consistency.md
../../../docs/architecture/events-realtime-and-delivery-boundary.md
```

---

# 158. Non-responsibilities

This document does not define:

```text
specific feature product rules
API endpoint shape
Application authorization pipeline implementation
EF mapping/schema
RLS SQL
broker retry policy
frontend behavior
billing price values
SLO/RPO/RTO
```

Use the owning document/context.

---

# 159. Final Domain rule

A healthy Notrelix Domain operation can be explained as:

```text
owned state
+
explicit input
+
explicit external facts
        ↓
validate lifecycle
validate invariant
normalize
detect no-op
prepare prospective change
        ↓
mutate once
audit/version coherently
raise completed scoped fact
```

with:

```text
no hidden database
no hidden provider
no ambient current user/time
no transport concerns
no partial failure mutation
no per-table typed-ID ceremony
no generic shared dumping ground
```

The goal is not to maximize DDD patterns.

The goal is to make **business meaning, consistency, failure, identity, and future extraction boundaries explicit with the least accidental coupling necessary**.
