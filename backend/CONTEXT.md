---
document_id: BE-CONTEXT
document_type: context-snapshot
status: active
owner: backend-architecture
applies_to:
  - backend
  - backend-coding-agents
  - backend-reviewers
evidence:
  - backend/backend.slnx
  - backend/global.json
  - backend/Directory.Build.props
  - backend/Directory.Packages.props
  - backend/src/
  - backend/tests/
  - backend/docs/architecture/
  - backend/docs/operations/
review_on:
  - backend-project-topology-change
  - bounded-context-placement-change
  - backend-toolchain-change
  - backend-architecture-change
  - approved-exception-change
  - major-source-reorganization
---

# Backend Context

> **This file is a high-signal current-state map for engineers and coding agents. It explains how to interpret the backend that exists today without turning current source accidents into permanent architecture.**
>
> It is intentionally non-normative where it describes source state. Durable architecture rules live under `backend/docs/architecture/`; durable product semantics live under repository `docs/product/`.

Use this file when you need to answer quickly:

```text
What projects exist?
How do they relate?
Where are bounded contexts represented?
What is the current feature placement?
Which current source facts are known exceptions or transition evidence?
Which test project proves which property?
Where should I look next?
```

Do not copy this snapshot into additional `CONTEXT.md` files.

---

# 1. Snapshot role

This document has three kinds of statements.

## Canonical pointer

Example:

```text
Backend layer dependency rules
→ docs/architecture/backend-overview.md
```

This file only routes to the normative owner.

## Current executable evidence

Example:

```text
backend.slnx currently lists five production projects.
```

This is source fact.

## Known current caveat

Example:

```text
Application currently references EF Core under an approved exception.
```

This is neither a new rule nor permission to expand the caveat.

---

# 2. Backend at a glance

Current backend is a .NET 9 modular monolith.

Production project inventory:

```text
Notrelix.Domain
Notrelix.Application
Notrelix.Infrastructure
Notrelix.Platform
Notrelix.API
```

Current architecture intent:

```text
API
  └──► Application
         └──► Domain

Infrastructure
  ├──► Application
  └──► Domain

Platform
  ├──► Application
  └──► Domain

Domain
  └──► no outer production layer
```

The product is decomposed semantically by bounded context **inside** these projects.

Do not infer:

```text
one bounded context
=
one .csproj
```

---

# 3. Current toolchain snapshot

Current backend SDK manifest:

```text
backend/global.json
```

declares:

```text
SDK 9.0.313
rollForward latestPatch
allowPrerelease false
```

Common project target:

```text
net9.0
```

with repository-level build/package configuration under:

```text
Directory.Build.props
Directory.Packages.props
```

Treat those manifests as current version authority.

---

# 4. Solution inventory authority

`backend/backend.slnx` is the authoritative backend project inventory.

Current production:

```text
src/
├── Notrelix.Domain
├── Notrelix.Application
├── Notrelix.Infrastructure
├── Notrelix.API
└── Notrelix.Platform
```

Current tests:

```text
tests/
├── Notrelix.Architecture.Tests
├── Notrelix.Domain.Tests
├── Notrelix.Application.Tests
├── Notrelix.Infrastructure.Tests
├── Notrelix.API.Tests
├── Notrelix.Integration.Tests
└── Notrelix.Platform.Tests
```

Current test-support libraries:

```text
tests/
├── Notrelix.Testing.Core
├── Notrelix.Testing.Domain
├── Notrelix.Testing.Application
└── Notrelix.Testing.Integration
```

A future:

```text
docs/generated/project-map.md
```

may summarize this inventory, but it should be generated from executable project manifests.

---

# 5. Product bounded contexts

Canonical business bounded contexts:

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

Their product meaning belongs to:

```text
../docs/product/contexts/
```

Backend folders implement those semantics but do not redefine them.

---

# 6. Supporting technical capability caveat

Current Application `Features/` contains folders including:

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
WorkManagement
Workspaces
Notifications
Operations
Search
```

The first group aligns with product contexts/capabilities.

The existence of:

```text
Notifications
Operations
Search
```

as source folders does **not** automatically promote each one to a business bounded context.

Supporting technical/product capabilities can have code placement without receiving independent semantic/data ownership equal to the canonical context map.

Before introducing a new context, use the repository bounded-context criteria.

---

# 7. Current Application placement direction

Current intended Application placement is module-first inside context:

```text
Features/{BoundedContext}/{Module}/Commands/{UseCase}
Features/{BoundedContext}/{Module}/Queries/{UseCase}
```

Interpretation:

```text
Context
→ semantic ownership

Module
→ cohesive capability/resource grouping

Command / Query
→ use-case intent
```

Do not add new code to a legacy/alternate structure solely because a nearby feature still uses one.

Source debt is not placement precedent.

---

# 8. Domain snapshot

Current Domain project:

```text
src/Notrelix.Domain
```

is intended to remain pure.

Current project-level evidence has no external package references.

The Domain owns:

```text
aggregate behavior
entities/value objects
state transitions
business invariants
Domain events
```

The Domain does not own:

```text
EF
PostgreSQL
Redis
HTTP
provider SDKs
current-user service
environment config
messaging broker
```

Read canonical:

```text
docs/architecture/domain-modeling.md
```

---

# 9. Domain visibility snapshot

Current Domain exposes internals only where deliberately needed for Domain tests.

Do not broaden `InternalsVisibleTo` to outer production layers as a convenience dependency.

If production code needs a Domain capability, prefer an intentional public/internal contract consistent with architecture rather than test-style visibility.

---

# 10. Application snapshot

Current Application source:

```text
src/Notrelix.Application
```

owns use-case orchestration.

The canonical model expects Application to coordinate:

```text
commands/queries
validation
authorization
tenant/resource resolution
transactions
external facts
expected-version behavior
idempotency
cache interaction
post-commit enrollment
result semantics
```

Read:

```text
docs/architecture/application-model.md
```

---

# 11. Application package snapshot

Current Application project references packages including:

```text
MediatR
FluentValidation
AutoMapper
Microsoft.EntityFrameworkCore
Microsoft.Extensions.Hosting.Abstractions
```

The EF Core package is a **current exception/transition fact**.

It is not a declaration that Application owns persistence.

Known governance:

```text
EX-BE-APP-EF-001
```

means new persistence usage is **not** authorized merely because EF types already appear in Application.

---

# 12. Application EF exception interpretation

Correct:

```text
Some approved/current Application compatibility requires EF package presence.
New code still follows canonical Application-port / Infrastructure-implementation boundary.
```

Incorrect:

```text
Application already references EF,
therefore handlers may use DbContext directly.
```

When feasible, remove the dependency that requires the exception rather than expanding the exception.

---

# 13. Pipeline mental model

Application cross-cutting behavior is intentionally pipeline-oriented.

A protected state-changing use case conceptually flows through concerns such as:

```text
request
→ validation
→ tenant/resource resolution
→ authorization
→ expected version / idempotency as applicable
→ transaction
→ handler/Domain behavior
→ outbox/post-commit enrollment
→ commit
→ post-commit delivery
→ result
```

Exact order/markers belong to Application architecture/source/tests.

Do not duplicate these concerns handler-by-handler without an architecture reason.

---

# 14. Authorization mental model

Backend security is layered:

```text
API authentication
        ↓
Application resource/action authorization
        ↓
Domain invariant
        ↓
Infrastructure persistence scoping / RLS
        ↓
Platform tenant scope for async work
```

No single layer alone is enough.

Read:

```text
docs/architecture/security-tenancy-authorization.md
```

---

# 15. Tenant scope mental model

Important scope dimensions include:

```text
Account
Workspace
resource
user/principal
```

depending on the capability.

Scope remains explicit across:

```text
HTTP
Application
persistence/RLS
cache
messages
background consumers
realtime
```

Do not assume a globally unique resource ID makes tenant scoping unnecessary.

---

# 16. Infrastructure snapshot

Current Infrastructure project:

```text
src/Notrelix.Infrastructure
```

contains implementation mechanics such as:

```text
EF Core
Npgsql/PostgreSQL
Redis/cache
authentication/security providers
provider adapters
MassTransit/provider integration
logging
migrations
RLS mechanisms
```

Read:

```text
docs/architecture/infrastructure-and-data.md
```

---

# 17. Persistence authority mental model

Backend persistence should be read as:

```text
Product/Domain meaning
        ↓
Application use-case boundary
        ↓
Infrastructure mapping/query
        ↓
PostgreSQL schema/index/RLS
```

Do not reverse this into:

```text
table exists
→ therefore Domain model should mirror it
```

The database strengthens/persists semantics; it does not automatically define them.

---

# 18. PostgreSQL snapshot

PostgreSQL is the authoritative relational persistence class for production semantics.

Properties that can depend on PostgreSQL include:

```text
RLS
Npgsql mappings/conversions
locking/concurrency behavior
migration DDL
index behavior
relational constraints
```

Tests claiming these properties must use PostgreSQL-realistic evidence.

---

# 19. RLS mental model

RLS is defense-in-depth.

Canonical security path:

```text
Application authorizes
+
Infrastructure/RLS constrains tenant DB access
```

Not:

```text
RLS exists
therefore Application can skip authorization
```

and not:

```text
Application authorizes
therefore RLS does not matter
```

Both protect different failure modes.

---

# 20. Platform snapshot

Current Platform project:

```text
src/Notrelix.Platform
```

owns reusable delivery/runtime mechanics.

Canonical topics:

```text
outbox
post-commit
message envelope
consumer identity
idempotency
ordering
retry
poison/dead-letter
background execution
```

Read:

```text
docs/architecture/platform-and-messaging.md
```

---

# 21. Platform business-boundary mental model

Correct:

```text
Work Management owns "Item changed".
Platform transports/delivers it reliably.
```

Incorrect:

```text
Platform owns Item change semantics because it serializes the message.
```

The same applies to Automation, Billing, Documents, Integrations, and other contexts.

Mechanism ownership never absorbs business meaning.

---

# 22. Outbox/post-commit mental model

For durable side effects that must follow a state change:

```text
authoritative state change
+
outbox/post-commit enrollment
        ↓ same local commit
commit succeeds
        ↓
delivery mechanism
```

Avoid required best-effort after-commit work with no durable enrollment.

Do not run irreversible provider side effects inside the local DB transaction merely to simulate a distributed transaction.

---

# 23. Messaging identity mental model

Reliable processing reasons about stable logical identities:

```text
message/event
producer
consumer
tenant/resource where applicable
idempotency/dedup
ordering scope
```

A poison/retry/order state should be diagnosable by logical identity.

Do not use global queue state where the invariant is consumer/resource-scoped.

---

# 24. API snapshot

Current API project:

```text
src/Notrelix.API
```

owns:

```text
ASP.NET Core host
routing
binding
authentication host integration
HTTP/OpenAPI
versioning
composition root
public result translation
```

Read:

```text
docs/architecture/api-and-contracts.md
```

API does not become the business-rule layer.

---

# 25. Contract mental model

Public boundaries may include:

```text
REST/OpenAPI
integration events
realtime payloads
webhooks/provider contracts
generated consumers
```

The backend producer owns its semantic contract.

Do not expose accidental:

```text
EF entities
provider DTOs
CLR type names
internal exceptions
```

as public identity.

---

# 26. Cross-context mental model

For every cross-context interaction, write:

```text
Owner A owns fact/state X.
Consumer B needs Y.
B obtains it through contract/event/query Z.
```

If instead the implementation requires:

```text
B directly edits A's table
```

the boundary is probably wrong or unresolved.

---

# 27. Current testing snapshot

Current test topology matches the five production architecture seams plus integration/architecture proof:

```text
Domain.Tests
Application.Tests
Infrastructure.Tests
Platform.Tests
API.Tests
Integration.Tests
Architecture.Tests
```

Testing support:

```text
Testing.Core
Testing.Domain
Testing.Application
Testing.Integration
```

Read:

```text
tests/AGENTS.md
docs/architecture/testing-and-quality-gates.md
```

---

# 28. Current unit-test package evidence

Current Domain/Application/API/Platform test projects use combinations of:

```text
xUnit
FluentAssertions
Moq
```

with shared test support where appropriate.

This does not mandate mock-heavy test style.

The preferred proof remains contract/scenario behavior at the cheapest reliable seam.

---

# 29. Current EF InMemory caveat in tests

Current:

```text
Application.Tests
API.Tests
Infrastructure.Tests
Integration test support
```

include EF Core/InMemory packages in places.

Interpretation:

```text
InMemory is available for tests whose protected property does not depend on real PostgreSQL semantics.
```

It does **not** prove:

```text
RLS
PostgreSQL locking
Npgsql-specific mapping
real migration DDL
production query/index behavior
```

Use PostgreSQL/Testcontainers for those properties.

---

# 30. Current PostgreSQL test evidence

Current:

```text
Infrastructure.Tests
Integration.Tests
```

reference Testcontainers PostgreSQL.

Integration tests also reference the production Application/Infrastructure/API graph and shared integration support.

Use those seams for production-realistic persistence/tenant behavior.

---

# 31. Architecture-test snapshot

Current Architecture test project references production assemblies and Roslyn APIs.

Architecture tests are intended to enforce structural properties such as:

```text
dependency direction
placement
forbidden references
pipeline/boundary rules
```

Do not weaken an architecture test solely because source violates a still-canonical rule.

Classify the source or govern an exception/change instead.

---

# 32. Testing-support mental model

Testing support libraries exist to reduce setup duplication.

They can own reusable:

```text
builders
fixtures
factories
test clocks/IDs
host/database harnesses
common assertions where truly generic
```

They must not own hidden product truth.

Bad:

```text
generic fixture silently grants every permission
```

because it can mask authorization failures.

---

# 33. Local runtime snapshot

Repository local development currently uses Docker/Compose for dependencies/full-stack execution.

Useful root flows include:

```text
make dev-up
make db-up
make db-migrate
make db-seed
make db-rls
make be-build
make be-test
```

Exact commands are executable Makefile evidence.

Read:

```text
../docs/delivery/local-development.md
docs/operations/configuration-and-runtime.md
```

---

# 34. Environment snapshot

Current safe local template path:

```text
../.env.example
→ copy to ../.env.dev
```

Do not commit `.env.dev`.

Backend source does not own environment semantics simply because a config value is consumed there.

Repository Infrastructure/Backend runtime docs define that boundary.

---

# 35. Current runtime dependency classes

Current backend ecosystem includes:

```text
PostgreSQL
→ authoritative relational persistence

Redis
→ cache/acceleration

RabbitMQ / MassTransit
→ messaging transport/mechanism where enabled

external providers
→ adapter-owned remote side effects
```

Treat each by authority class.

Do not let cache/broker/provider become business source-of-truth accidentally.

---

# 36. Current CI mental model

Backend CI currently separates protected properties into multiple jobs/gates rather than one undifferentiated `dotnet test`.

Typical protected areas include:

```text
quality/security
architecture
core layer behavior
Platform
API/OpenAPI
integration / PostgreSQL / RLS / production graph
container packaging
final completion gate
```

Exact workflow topology is current evidence and may evolve.

The protected properties should remain.

---

# 37. Current architecture decision history

Backend ADRs currently registered:

```text
ADR-001 Pipeline boundary
ADR-002 RLS bootstrap connection lifecycle
ADR-003 CSRF protection
ADR-004 Rate limiting architecture
```

They preserve rationale.

They are not a replacement for current architecture docs.

---

# 38. Known important exception snapshot

Current known architecture exception:

```text
EX-BE-APP-EF-001
```

Meaning:

```text
Application currently requires EF package/reference compatibility.
New persistence usage is not authorized.
Removal should occur when the approved abstraction/transition no longer needs EF.
```

This exception should be visible enough that agents neither:

- accidentally remove a still-required dependency;
- normalize it into a permanent Application persistence rule.

---

# 39. Source-debt categories

When reading legacy or transitional source, use:

```text
DOC_STALE
→ docs lag current intended/source truth

SOURCE_DEBT
→ source violates intended canonical rule

TRANSITION
→ both old/new intentionally coexist during migration

CONTRACT_CHANGE
→ current contract itself is changing

UNRESOLVED
→ no approved authority yet
```

Do not collapse all disagreement into “docs wrong”.

---

# 40. Common wrong inferences

Do not infer:

```text
folder exists
→ bounded context exists

project contains package
→ layer owns that technology

test package exists
→ it proves every behavior of that technology

shared DB
→ contexts share data ownership

same process
→ contexts may access each other's internals

MassTransit is used
→ all events are integration events

Redis is used
→ cache is authority

RLS exists
→ Application auth is optional

one GitHub workflow is green
→ all intended tests ran

code compiles
→ architecture is valid
```

---

# 41. Fast routing by problem

## Business rule bug

Read:

```text
product context
→ domain-modeling
→ Domain source/tests
```

## Authorization bug

Read:

```text
product context
→ security-tenancy-authorization
→ application-model
→ Application/API/Integration tests
```

## Query/persistence/RLS bug

Read:

```text
infrastructure-and-data
→ migrations-and-data-change
→ Infrastructure/Integration tests
```

## Retry/order/duplicate delivery bug

Read:

```text
platform-and-messaging
→ Platform/Integration tests
```

## Public API drift

Read:

```text
api-and-contracts
→ contract-first-delivery
→ API/OpenAPI tests
```

## Cross-context coupling

Read:

```text
repository bounded-context-map
→ backend-overview/application-model
→ architecture tests
```

## Migration incident

Read:

```text
migration-policy
→ migrations-and-data-change
→ recovery-and-data-safety
```

---

# 42. Fast routing by source project

```text
Notrelix.Domain
→ docs/architecture/domain-modeling.md

Notrelix.Application
→ docs/architecture/application-model.md

Notrelix.Infrastructure
→ docs/architecture/infrastructure-and-data.md

Notrelix.Platform
→ docs/architecture/platform-and-messaging.md

Notrelix.API
→ docs/architecture/api-and-contracts.md

cross-cutting auth/tenant
→ docs/architecture/security-tenancy-authorization.md

tests/gates
→ docs/architecture/testing-and-quality-gates.md
→ tests/AGENTS.md
```

---

# 43. Fast routing by delivery risk

```text
schema/data
→ ../docs/delivery/migration-policy.md

public contract
→ ../docs/delivery/contract-first-delivery.md

architecture
→ ../docs/governance/decision-and-exception-policy.md
→ ../docs/templates/architecture-change-template.md

security/tenant
→ ../docs/quality/security-quality-standard.md

runtime/dependency
→ ../docs/infrastructure/**
→ ../docs/operations/**

release
→ ../docs/delivery/release-and-rollout.md
```

---

# 44. Context update rule

Update this snapshot when a **current-state fact** materially changes, such as:

```text
project inventory
SDK/toolchain
feature placement convention
major test topology
approved exception state
major runtime dependency class
```

Do not update this file merely to repeat every new feature.

Feature semantics belong to product/context docs.

---

# 45. Context staleness rule

If this file says:

```text
current source has X
```

but manifests/source no longer have X, this file is stale.

Fix the snapshot.

If this file says:

```text
canonical owner says Y
```

and the canonical owner has changed, update the pointer.

Do not keep historical snapshots inside the active context file.

Git history is sufficient for ordinary historical versions.

---

# 46. Context non-responsibilities

This file does not own:

```text
full product semantics
full backend architecture
exact CI job topology
exact package versions beyond current orientation
migration execution steps
incident runbooks
team staffing
future roadmap
freeze status
```

Those are routed elsewhere.

---

# 47. New-agent orientation checklist

Before implementing backend work, a new agent should be able to identify:

```text
[ ] bounded context
[ ] source project(s)
[ ] use-case/module
[ ] aggregate/resource owner
[ ] authorization owner
[ ] tenant scope
[ ] persistence/RLS seam
[ ] async/event seam
[ ] contract consumer(s)
[ ] test project(s)
[ ] current caveat/exception if any
[ ] canonical doc to read next
```

---

# 48. Final backend context model

The backend should be mentally modeled as:

```text
Product contexts
        │
        ▼
server-authoritative use cases
        │
        ├── Domain invariants
        ├── Application orchestration/auth/transaction
        ├── Infrastructure persistence/RLS/providers
        ├── Platform reliable delivery
        └── API public transport
        │
        ▼
tests/gates prove each protected property
```

with:

```text
current source
= executable evidence

canonical docs
= intended authority

known exception
= bounded temporary deviation

migration/transition
= explicit coexistence

coding agent
= implementer, not architecture inventor
```
