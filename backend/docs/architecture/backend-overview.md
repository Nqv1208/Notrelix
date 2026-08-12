---
document_id: BE-BACKEND-OVERVIEW
document_type: architecture
status: active
owner: backend-architecture
applies_to:
  - backend
  - backend-production-projects
  - backend-project-boundaries
evidence:
  - backend/backend.slnx
  - backend/src/Notrelix.Domain/Notrelix.Domain.csproj
  - backend/src/Notrelix.Application/Notrelix.Application.csproj
  - backend/src/Notrelix.Infrastructure/Notrelix.Infrastructure.csproj
  - backend/src/Notrelix.Platform/Notrelix.Platform.csproj
  - backend/src/Notrelix.API/Notrelix.API.csproj
  - backend/tests/Notrelix.Architecture.Tests/
review_on:
  - backend-project-topology-change
  - backend-layer-boundary-change
  - bounded-context-extraction
  - application-persistence-boundary-change
  - platform-delivery-boundary-change
  - public-api-boundary-change
---

# Backend Overview

> **The Notrelix backend is a five-project modular monolith with bounded contexts expressed as semantic ownership seams across those projects—not as one project, database, process, or service per context.**
>
> The architecture is designed so teams can build vertical capabilities in parallel now while preserving the ownership and contract seams required for later capability extraction without rewriting the product model.

This document is the canonical backend-level owner for:

- production project topology;
- layer responsibilities;
- compile-time dependency direction;
- bounded-context versus project boundaries;
- vertical feature placement principles;
- cross-context interaction constraints;
- backend extraction posture;
- the relationship between source evidence, architecture rules, tests, and ADRs.

It does **not** own product semantics, detailed Domain/Application/Infrastructure/Platform/API rules, exact endpoint contracts, migration runbooks, or CI job topology.

Those topics have narrower canonical owners.

---

# 1. Architectural objective

The backend must support two goals simultaneously:

```text
today
→ coherent modular monolith
→ low operational overhead
→ strong local transactions
→ fast feature development

future
→ selected capability extraction
→ explicit contracts
→ clear data ownership
→ minimal semantic rewrite
```

The correct design is therefore **not**:

```text
premature microservices
```

and not:

```text
one large undifferentiated application with folders only
```

The target is:

```text
modular monolith
+
bounded-context ownership
+
closed project dependencies
+
explicit contracts
+
executable architecture gates
```

---

# 2. Current production project topology

`backend/backend.slnx` is the production/test project inventory authority.

The five production projects are:

```text
backend/src/
├── Notrelix.Domain
├── Notrelix.Application
├── Notrelix.Infrastructure
├── Notrelix.Platform
└── Notrelix.API
```

This five-project topology is deliberate.

Do not create:

```text
Notrelix.WorkManagement
Notrelix.Documents
Notrelix.Billing
...
```

projects merely because those bounded contexts exist.

A production-project change is a consequential architecture change.

---

# 3. Current compile-time references

Current project manifests establish the following direct references:

```text
Notrelix.Domain
→ no production project reference

Notrelix.Application
→ Notrelix.Domain

Notrelix.Infrastructure
→ Notrelix.Application
→ Notrelix.Domain

Notrelix.Platform
→ Notrelix.Application
→ Notrelix.Domain

Notrelix.API
→ Notrelix.Application
→ Notrelix.Infrastructure
```

Conceptually:

```text
                    ┌────────────────────┐
                    │    Notrelix.API    │
                    └─────────┬──────────┘
                              │
                 ┌────────────┴────────────┐
                 ▼                         ▼
      Notrelix.Application      Notrelix.Infrastructure
                 │                │               │
                 │                └──────┐        │
                 ▼                       ▼        ▼
          Notrelix.Domain        Application   Domain


        Notrelix.Platform
             │        │
             ▼        ▼
        Application  Domain
```

The simplified architectural rule is:

```text
outer mechanics
→ inward contracts
→ Domain meaning
```

---

# 4. Dependency direction

Allowed conceptual direction:

```text
API
→ Application
→ Domain

Infrastructure
→ Application
→ Domain

Platform
→ Application
→ Domain
```

Forbidden conceptual direction:

```text
Domain
→ Application
→ Infrastructure
→ Platform
→ API
```

and:

```text
Application
→ API
Application
→ Platform implementation
Application
→ Infrastructure implementation
```

unless a specific current exception or accepted architecture decision explicitly permits a narrowly scoped compile-time dependency.

---

# 5. Domain purity is a hard seam

Current `Notrelix.Domain.csproj` intentionally has:

```text
no package references
```

and only:

```text
InternalsVisibleTo → Notrelix.Domain.Tests
```

This is strong executable evidence that Domain is meant to remain provider/runtime independent.

Domain should not need:

```text
EF Core
MediatR
Redis
MassTransit
ASP.NET Core
HTTP client
provider SDK
configuration system
current-user service
```

to enforce owned business rules.

Detailed owner:

```text
domain-modeling.md
```

---

# 6. Application owns the use-case boundary

Application coordinates the complete server-side use case.

It owns architecture concepts such as:

```text
command/query intent
validation
resource/action authorization
tenant resolution
external facts
transaction boundary
expected-version behavior
idempotency orchestration
cross-context coordination
ports
result/error semantics
post-commit enrollment
```

Application may depend on Domain.

Its architecture should not be dictated by a specific provider or database implementation.

Detailed owner:

```text
application-model.md
```

---

# 7. Current Application EF Core exception

Current `Notrelix.Application.csproj` contains:

```text
Microsoft.EntityFrameworkCore
```

This is a current source fact covered by the approved exception:

```text
EX-BE-APP-EF-001
```

The architecture interpretation is:

```text
EF type compatibility currently exists in Application
```

not:

```text
Application owns database persistence
```

New handler-local direct `DbContext` persistence is not authorized merely because the package exists.

If future architecture removes the reason for the exception, remove the dependency instead of institutionalizing it.

---

# 8. Infrastructure owns provider and persistence mechanics

Infrastructure may use outer technologies such as:

```text
Entity Framework Core
Npgsql/PostgreSQL
Redis
ASP.NET Core Identity implementation
JWT implementation
email/provider clients
search/storage adapters
MassTransit provider integration
logging providers
RLS SQL/migrations
```

Infrastructure implements inward contracts.

It must not become the owner of product semantics merely because it contains database/provider code.

Detailed owner:

```text
infrastructure-and-data.md
```

---

# 9. Platform is a mechanism layer

Platform is distinct from Infrastructure because it owns reusable **delivery/runtime behavior**, not provider persistence.

Representative responsibilities:

```text
post-commit delivery
logical message identity
consumer identity
idempotency/dedup
ordering mechanisms
retry/backoff
poison/dead-letter
background execution
consumer hosting
scheduler/claim mechanics
```

Platform does not own:

```text
what Work Management event means
what Billing usage means
what an Automation rule means
which user receives a Notification
```

Those semantics remain in the owning context.

Detailed owner:

```text
platform-and-messaging.md
```

---

# 10. API is transport and composition

API owns:

```text
ASP.NET Core host
routing
binding
authentication integration
versioning
HTTP/OpenAPI
public result translation
dependency composition
host middleware
```

API does not own:

```text
Domain invariants
business permission rules
persistence transactions
provider business semantics
```

The endpoint is an adapter into the Application use case.

Detailed owner:

```text
api-and-contracts.md
```

---

# 11. Bounded contexts are semantic seams

Canonical business contexts are repository-owned and include:

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

The context boundary answers:

```text
Who owns this business fact?
Who owns this lifecycle?
Who is allowed to mutate it?
Which events/contracts expose it?
Which data can be extracted with the capability later?
```

It does **not** answer:

```text
Which .csproj contains the code?
```

by itself.

---

# 12. Context versus layer

A single capability may span layers:

```text
Work Management
├── Domain
├── Application
├── Infrastructure
├── Platform
└── API
```

This is expected.

Each layer owns a different technical responsibility for the **same semantic owner**.

Layer boundaries do not create five different product owners.

---

# 13. Context versus folder

Source folders can help communicate ownership.

They are not sufficient architecture proof.

A folder named:

```text
Search
Operations
Notifications
```

does not automatically create a business bounded context.

A folder named:

```text
WorkManagement
```

does not prove that every type inside obeys Work Management ownership.

Ownership comes from canonical product/system architecture plus source/tests as evidence.

---

# 14. Context versus database

Several bounded contexts may currently share one PostgreSQL database.

That does not mean they share table ownership.

Rule:

```text
shared database
≠ shared semantic ownership
```

Each table/aggregate/schema relationship should have one logical context owner.

Foreign contexts must not mutate the owner's tables directly as their ordinary integration contract.

---

# 15. Context versus process

All contexts can run in one API/process today.

Rule:

```text
shared process
≠ internal access permission
```

A future service split should expose boundaries already present conceptually.

Do not exploit in-process access in ways that would force a semantic rewrite later.

---

# 16. Context versus team

Team assignment can change.

Architecture ownership should remain stable.

Rule:

```text
team topology
≠ bounded-context topology
```

Do not split or merge contexts solely because staffing changes.

Repository owner:

```text
../../docs/delivery/team-ownership.md
```

---

# 17. Vertical-slice delivery

Backend architecture is layered, but feature delivery is vertical.

A complete change can legitimately touch:

```text
Domain model
Application request/handler
Infrastructure mapping/query
Platform event delivery
API endpoint/OpenAPI
tests
migration
generated consumer
```

This is not a layer violation.

The violation would be putting the **wrong responsibility** in the wrong layer.

---

# 18. Correct vertical slice

Example:

```text
Change Board Item status
        │
        ▼
Domain:
validate owned transition
        │
        ▼
Application:
authorize item update
load facts
invoke Domain
commit
        │
        ▼
Infrastructure:
persist mapped state
        │
        ▼
Platform:
deliver committed event
        │
        ▼
API:
HTTP contract/result
```

Each layer has one reason to exist.

---

# 19. Incorrect vertical slice

Example:

```text
API endpoint
→ checks role string
→ uses DbContext
→ mutates entity property
→ calls provider
→ publishes broker message
```

This collapses:

```text
transport
authorization
business invariant
persistence
external side effect
delivery
```

into one outer layer.

The code may compile, but the architecture is not valid.

---

# 20. Application feature placement

Current placement direction is module-first within context:

```text
Features/{BoundedContext}/{Module}/Commands/{UseCase}
Features/{BoundedContext}/{Module}/Queries/{UseCase}
```

The exact folder convention is a placement mechanism, not semantic authority.

Use it to communicate:

```text
context
→ module/capability
→ use-case intent
```

Do not create a new folder family if the current architecture already has an appropriate owner.

---

# 21. Cross-context interaction rule

A cross-context interaction should be expressible as:

```text
Context A owns fact/state X.
Context B needs fact/action Y.
B obtains Y through explicit contract Z.
```

Typical Z:

```text
Application service/port
read/query contract
committed integration event
explicit orchestration
```

Do not use:

```text
foreign DbSet
foreign repository internals
shared mutable Domain object
provider table
```

as the contract.

---

# 22. Synchronous cross-context interaction

Use synchronous coordination when:

```text
the current use case needs an answer now
```

Examples:

```text
Can this resource be shared?
Does this Account have an entitlement?
What authoritative parent/scope does this resource belong to?
```

The source owner still owns the fact.

The consumer receives a stable contract/fact.

---

# 23. Asynchronous cross-context interaction

Use committed asynchronous facts when:

```text
the source can commit independently
and
the consumer can react eventually
```

Examples:

```text
resource changed
membership changed
subscription changed
page changed
```

The event is a cross-boundary contract.

Do not expose an internal Domain CLR record automatically.

---

# 24. No distributed transaction illusion

When two contexts have separate semantic ownership, do not pretend a local EF transaction is the long-term contract simply because both tables currently share one DB.

Choose intentionally:

```text
same-context/local atomicity
or
cross-context orchestration/eventual consistency
```

based on the product invariant.

Future extraction must preserve the invariant, not the current technical shortcut.

---

# 25. Transaction ownership

Application decides the local use-case transaction.

Infrastructure implements the persistence transaction.

Platform participates in durable post-commit enrollment/delivery as architecture requires.

Rule:

```text
transaction semantics
→ Application architecture

transaction technology
→ Infrastructure
```

---

# 26. Post-commit boundary

Required side effects that occur after source state commit need a durable/explicit bridge where reliability matters.

Conceptual flow:

```text
Domain/Application state change
+
outbox/post-commit enrollment
        │
        ▼
local commit
        │
        ▼
Platform delivery
        │
        ▼
consumer/provider/realtime
```

Do not rely on:

```text
commit
→ fire-and-forget task
```

for a required effect.

---

# 27. Public-contract boundary

The backend has several public/semi-public boundaries:

```text
HTTP/OpenAPI
integration events
realtime payloads
webhooks
generated frontend contracts
provider protocol mappings
```

Each contract needs a stable semantic owner.

Do not use:

```text
EF entity
database row
provider DTO
internal Domain exception
CLR type full name
```

as accidental public contract.

---

# 28. Generated consumer rule

If an API contract generates frontend types:

```text
backend producer
→ OpenAPI/contract artifact
→ generator
→ frontend consumer
```

Do not hand-edit the generated consumer when the backend producer is wrong.

Repository contract policy:

```text
../../docs/delivery/contract-first-delivery.md
```

---

# 29. Security boundary is cross-layer

Backend security is intentionally defense-in-depth:

```text
API authentication
        ↓
Application authorization
        ↓
Domain invariant
        ↓
Infrastructure tenant persistence/RLS
        ↓
Platform background tenant scope
```

A change touching one layer must not accidentally weaken another.

Detailed owner:

```text
security-tenancy-authorization.md
```

---

# 30. Tenant scope is not a transport detail

Account/Workspace/resource scope can affect:

```text
authorization
database access
cache key
message
background execution
realtime subscription
provider connection
analytics/search projection
```

Do not treat tenant scope as “just a route parameter”.

---

# 31. SharedKernel versus Common

Current Domain has:

```text
Common/
SharedKernel/
```

These directories are not generic dumping grounds.

A type belongs in a cross-context shared area only when:

```text
its semantics are stable across contexts
its invariants are genuinely shared
its ownership is not secretly one context
sharing reduces semantic duplication rather than hides ownership
```

Examples of plausible shared concepts include:

```text
Email
Money
DateRange
ResourceKind/ResourceRef
stable ordering primitive
```

subject to actual source contracts.

---

# 32. Shared admission test

Before moving a type into Common/SharedKernel, answer:

```text
Do at least two contexts use the same meaning?
Would either context need to change it independently?
Does the type carry lifecycle/business policy from one context?
Can it remain provider/persistence independent?
Does sharing make future extraction harder?
```

If meaning is context-specific, keep it context-owned.

---

# 33. Do not create broad service abstractions

Avoid generic interfaces such as:

```text
IEntityService
IRepository<T> for every model
IManager
ICommonService
ISharedProvider
```

without stable shared semantics.

Abstraction is valuable when it protects a boundary, not when it hides concrete ownership.

---

# 34. Dependency inversion

Use inward contracts where an inner layer needs an outer mechanism.

Conceptually:

```text
Application
defines port
        ▲
        │ implements
Infrastructure
```

not:

```text
Application
references provider implementation
```

A port should describe what the use case needs, not mirror an SDK.

---

# 35. Provider isolation

Provider-specific concepts remain at the edge.

Example:

```text
Application:
SendNotificationPort

Infrastructure:
ResendEmailSender
```

not:

```text
Application:
ResendClient request DTO
```

The same applies to OAuth, payment, calendar, storage, search, and messaging providers.

---

# 36. Persistence isolation

Persistence mappings may need information about Domain types.

Domain should not need information about mappings.

Rule:

```text
Domain shape
→ mapping adapts
```

not:

```text
ORM limitation
→ Domain semantics distorted by default
```

When persistence constraints matter to product correctness, handle them deliberately and document the trade-off.

---

# 37. Platform isolation

Platform may reference Domain/Application contracts because it executes delivery/runtime mechanisms.

Domain/Application should not rely on a specific Platform concrete implementation to express business meaning.

A product event must make sense even if the underlying broker/consumer host later changes.

---

# 38. API isolation

API request/response types are transport contracts.

Do not pass endpoint DTOs deep into Domain as business models.

Map:

```text
HTTP request
→ Application request/contract
→ Domain intent/value
```

and:

```text
Application result
→ HTTP response
```

as appropriate.

---

# 39. Error ownership

Each layer owns a different part of failure semantics.

```text
Domain
→ business invariant violation / invalid transition

Application
→ authorization, not found, conflict, use-case result

Infrastructure
→ provider/persistence failure classification

Platform
→ delivery/retry/poison outcome

API
→ public HTTP translation
```

Do not leak outer exception details inward or to clients.

---

# 40. Versioning and compatibility

Versioning can apply to:

```text
aggregate concurrency
public API
integration event
persisted schema
generated consumer
provider contract
```

These are distinct.

Do not conflate:

```text
Aggregate.Version
```

with:

```text
API v1/v2
```

or:

```text
event contract Version
```

Detailed owners handle each one.

---

# 41. Current package-role evidence

Current project manifests support the intended boundaries:

```text
Domain
→ no packages

Application
→ MediatR, FluentValidation, AutoMapper, EF exception

Infrastructure
→ EF/Npgsql, Redis, Identity/JWT, Resend, MassTransit/RabbitMQ, Serilog

Platform
→ DI/logging abstractions only

API
→ ASP.NET Core host, versioning, OpenAPI, JWT host integration
```

Package presence is current evidence.

It is not automatically permission to expand the package's semantic role.

---

# 42. Internals visibility

Current production-boundary intent is narrow.

Examples:

```text
Domain internals
→ Domain.Tests

Platform internals
→ Platform.Tests for selected test seams
```

Do not use `InternalsVisibleTo` to let outer production layers bypass public architecture contracts.

Test seam is not a production dependency seam.

---

# 43. Architecture tests

Structural rules that can be automated should be executable.

Examples:

```text
Domain has no forbidden outer dependencies
project references remain allowed
bounded-context isolation rules
pipeline-owned authorization rules
public contract boundaries
```

The architecture test suite is evidence.

Do not weaken the gate to match a convenience implementation while the canonical rule remains valid.

---

# 44. Architecture evidence hierarchy

For a project-boundary question inspect:

```text
1. canonical architecture docs
2. accepted ADR/exception where applicable
3. backend.slnx
4. project files
5. architecture tests
6. source namespaces/usage
```

A stale comment or legacy folder is weaker evidence.

---

# 45. Current source can contain debt

Source may temporarily contain:

```text
approved exception
migration transition
legacy placement
historical compatibility
```

Correct response:

```text
classify it
```

not:

```text
copy it into new architecture
```

Repository drift classes:

```text
DOC_STALE
SOURCE_DEBT
TRANSITION
CONTRACT_CHANGE
UNRESOLVED
```

---

# 46. New dependency review

Before adding a package/project dependency ask:

```text
Which layer responsibility requires it?
Can an existing outer adapter own it?
Does it reverse dependency direction?
Does it introduce provider concepts inward?
Does it create a new runtime failure mode?
Does it affect future extraction?
Does architecture testing need a new/updated rule?
```

A NuGet package is an architecture input, not just a coding convenience.

---

# 47. New production project admission

A new project is justified only by a durable architectural boundary requiring independent:

```text
dependency closure
build/test ownership
runtime/extraction boundary
security boundary
reuse boundary
```

and not merely:

```text
folder size
team size
number of classes
bounded-context count
```

A new project normally requires architecture decision review.

---

# 48. Service extraction admission

Do not extract a service because a context “should be microservice”.

Evaluate:

```text
semantic cohesion
data ownership
transaction independence
independent scaling
operational ownership
failure isolation
deployment cadence
contract maturity
migration cost
```

Repository owner:

```text
../../docs/architecture/capability-extraction-strategy.md
```

---

# 49. Extraction readiness

A capability is easier to extract when today it already has:

```text
one semantic owner
explicit data ownership
limited cross-context writes
stable application contracts
stable events
tenant identity/scope
idempotent async behavior
independent tests
operational visibility
```

Do this now because it improves the monolith too.

Do not add network boundaries purely for future-proofing.

---

# 50. Shared database extraction posture

A shared database can remain during the modular-monolith stage.

Extraction preparation means reducing semantic table coupling, not necessarily moving schemas immediately.

Forbidden long-term coupling:

```text
Context B writes Context A's tables as normal behavior
```

Preferred:

```text
B calls A's use-case contract
or
B consumes A's committed fact
```

---

# 51. Cross-context query posture

Read needs can be satisfied through:

```text
owner query contract
read projection
replicated fact
analytics/search projection
```

depending on freshness/performance.

Do not expose a foreign repository simply to avoid designing read ownership.

---

# 52. Query optimization does not transfer ownership

A projection may combine facts from several contexts for fast reads.

The projection owns:

```text
derived representation
```

not:

```text
source business truth
```

Writes still go to semantic owners.

---

# 53. Deletion/lifecycle boundary

Lifecycle terminology is product-owned.

Do not impose one universal:

```text
SoftDelete()
```

meaning across every aggregate.

Contexts may require:

```text
archive
remove
revoke
cancel
tombstone
resolve
restore
```

with different semantics.

A reusable soft-delete mechanism, where present, does not mean every context should adopt generic soft-delete product language.

---

# 54. Auditing boundary

Audit fields/mechanisms can be shared.

Business history, security Audit, user Activity, and version history are not automatically the same concept.

Do not merge them into one generic audit table/event because all are “history”.

---

# 55. Time and randomness boundary

Domain should be deterministic with respect to supplied facts.

Application/outer layer supplies:

```text
current time
generated tokens/random values
provider facts
actor
authorization result
```

when the use case needs them.

Do not read ambient time/random/provider state deep inside Domain.

---

# 56. Configuration boundary

Runtime configuration belongs to outer composition.

Domain rules should not vary because:

```text
ASPNETCORE_ENVIRONMENT
```

changed.

If product behavior is configurable, model the product/config fact explicitly through the owning layer/context.

---

# 57. Logging boundary

Logging is an operational mechanism.

Do not put logging calls inside Domain solely to observe business rules.

Expose meaningful result/events and log at orchestration/runtime boundaries.

Do not leak secrets/private payloads.

---

# 58. Realtime boundary

Realtime delivers freshness.

It does not own source state.

Backend mutation:

```text
authoritative commit
→ event/realtime consequence
```

not:

```text
websocket message
→ authoritative business truth without normal use-case enforcement
```

Repository owner:

```text
../../docs/architecture/events-realtime-and-delivery-boundary.md
```

---

# 59. Automation boundary

Automation can trigger or consume product facts.

Automation does not gain ownership of the underlying Work Management/Documents/Integrations state.

An Automation action requests the owning context to perform the target behavior under the appropriate authority.

---

# 60. Billing boundary

Billing owns commercial facts such as entitlement/usage/subscription semantics.

A feature context does not invent its own paid-plan rule.

Likewise, Billing does not become authorization owner for resource-level access.

Entitlement and authorization remain distinct.

---

# 61. Governance boundary

Governance owns policies/roles/resource permission facts.

A context may enforce its local invariant while Application asks Governance/authorization contracts for external access facts.

Do not import Governance persistence internals into every context.

---

# 62. Identity versus Account/Workspace

Authentication identity and product Account/Workspace membership are related but distinct ownership concerns.

Do not infer Workspace authorization solely from authenticated user existence.

Tenant membership/role/resource policy must be resolved through its owner.

---

# 63. Search and Analytics

Search/Analytics are supporting/derived capabilities.

Their projections may span source contexts.

They do not become source mutation paths.

Correct:

```text
source context commit
→ projection/index update
```

Incorrect:

```text
search index state
→ write source aggregate
```

unless a specific accepted architecture explicitly defines a command based on a user action, which still routes through the owning use case.

---

# 64. Notification capability

Notifications can consume product facts and route user-visible messages.

Notification delivery does not own the source fact.

Do not turn a notification record into the canonical representation of the business event that caused it.

---

# 65. Performance does not justify ownership bypass

Before introducing:

```text
direct foreign table read
shared cache
denormalized cross-context column
batch mutation
```

to optimize performance, preserve semantic owner and define projection/contract semantics.

A fast invalid architecture creates expensive future coupling.

---

# 66. Reliability does not justify Domain pollution

Do not move:

```text
retry
broker envelope
provider idempotency
transaction retry
logging
```

into Domain to make behavior “reliable”.

Reliability is layered around deterministic business behavior.

---

# 67. Security does not justify transport-owned business policy

API middleware can enforce host-level security.

Resource-level product authorization belongs to Application/Governance contracts.

Do not encode:

```text
if route starts with /admin
```

as the primary business permission model.

---

# 68. Migration does not create permanent dual truth

During data/ownership migration:

```text
old + new
```

may coexist.

The migration must still identify one semantic authority by phase.

After transition, remove compatibility paths according to migration proof.

Repository owner:

```text
../../docs/delivery/migration-policy.md
```

---

# 69. Backward compatibility is architecture work

Independent deployment units can include:

```text
old mobile client
old browser bundle
background worker
queued message
replay archive
provider webhook
```

Do not assume backend and frontend merge simultaneously means all consumers update atomically.

---

# 70. Architecture change process

For a material topology/dependency/ownership change:

```text
classify change
→ inspect canonical owner
→ ADR if consequential
→ architecture-change artifact if needed
→ compatibility/migration plan
→ implementation
→ architecture/integration proof
→ update canonical docs
```

Template:

```text
../../docs/templates/architecture-change-template.md
```

---

# 71. Architecture exceptions

An exception means the current rule remains correct but is temporarily violated.

It must not become silent precedent.

Existing example:

```text
EX-BE-APP-EF-001
```

Do not expand an exception because adjacent code already uses it.

Canonical policy:

```text
../../docs/governance/decision-and-exception-policy.md
```

---

# 72. Required architecture review questions

Before approving a structural change, answer:

```text
Which product context owns the behavior?
Which project/layer owns each mechanism?
What compile-time dependency changes?
Does Domain remain pure?
Does Application remain use-case/policy oriented?
Does Infrastructure remain adapter/persistence oriented?
Does Platform remain generic delivery mechanism?
Does API remain transport/composition?
What cross-context contract changes?
What data ownership changes?
What future extraction seam improves or degrades?
Which tests/gates prove the new boundary?
```

---

# 73. Architecture failure modes

Common failure modes:

```text
folder structure is mistaken for context ownership
one context directly mutates another's tables
Application grows provider-specific code
Domain gains framework/provider packages
Platform accumulates business policy
API becomes transaction/business layer
Common becomes a dumping ground
shared process is treated as internal trust
test-only InternalsVisibleTo becomes production coupling
legacy exception becomes precedent
microservice extraction is attempted before contract/data ownership exists
```

---

# 74. Architecture stop conditions

Stop and require an explicit architecture decision if:

- a sixth production project appears necessary;
- a new direct project reference violates current closure;
- Domain needs an outer package/provider;
- Application needs new direct persistence implementation;
- Platform needs context-specific business policy;
- API must bypass Application to implement a use case;
- two contexts both appear to own the same writable fact;
- a context can only integrate by direct foreign table mutation;
- service extraction requires redesigning product semantics;
- a current exception must be broadened rather than removed;
- architecture tests must be weakened to accept the proposal.

---

# 75. Executable evidence

Primary evidence:

```text
backend/backend.slnx
backend/src/**/*.csproj
backend/tests/Notrelix.Architecture.Tests
```

Broad commands:

```bash
cd backend
dotnet build backend.slnx
dotnet test backend.slnx
```

Exact test/gate requirements depend on change classification.

---

# 76. Related canonical backend docs

```text
domain-modeling.md
application-model.md
infrastructure-and-data.md
platform-and-messaging.md
api-and-contracts.md
security-tenancy-authorization.md
testing-and-quality-gates.md
```

Repository-level owners:

```text
../../docs/architecture/bounded-context-map.md
../../docs/architecture/contract-boundaries.md
../../docs/architecture/data-ownership-and-consistency.md
../../docs/architecture/events-realtime-and-delivery-boundary.md
../../docs/architecture/capability-extraction-strategy.md
```

---

# 77. Non-responsibilities

This file does not define:

```text
exact aggregate invariants
handler implementation recipe
database schema
RLS SQL
message broker config
endpoint JSON shape
frontend package topology
CI YAML
release command
migration batch size
SLO/RPO/RTO numbers
```

Use the narrower owner.

---

# 78. Final backend architecture rule

A backend design is aligned when it can be explained as:

```text
one product semantic owner
        ↓
one Application use-case boundary
        ↓
pure Domain behavior where applicable
        ↓
outer persistence/provider implementation
        ↓
reliable Platform delivery where needed
        ↓
thin API transport/composition
```

while:

```text
bounded contexts remain semantically independent
project dependencies remain closed inward
cross-context interaction is explicit
shared abstractions remain truly shared
current exceptions remain bounded
tests/gates prove the structural rules
future extraction changes topology rather than product meaning
```
