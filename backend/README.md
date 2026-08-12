---
document_id: BE-README
document_type: repository-entry
status: active
owner: backend-architecture
applies_to:
  - backend
evidence:
  - backend/backend.slnx
  - backend/global.json
  - backend/Directory.Build.props
  - backend/Directory.Packages.props
  - backend/src/
  - backend/tests/
  - backend/docs/
review_on:
  - backend-project-topology-change
  - backend-toolchain-change
  - backend-architecture-change
  - backend-runtime-change
  - backend-command-change
---

# Notrelix Backend

> **Notrelix Backend is a .NET modular monolith that owns server-authoritative product behavior, application orchestration, durable persistence, tenant/security enforcement, public API contracts, and reliable background delivery.**
>
> Bounded contexts remain semantic ownership boundaries inside the modular monolith. They are **not** automatically separate `.csproj` projects or deployable services.

---

# Tech stack

Current executable manifests define the exact package versions. The backend currently uses:

| Area | Technology |
|---|---|
| Runtime / language | .NET 9 / C# / `net9.0` |
| SDK | .NET SDK `9.0.313` (`latestPatch`, no prerelease) |
| Web host | ASP.NET Core |
| Application messaging | MediatR |
| Validation | FluentValidation |
| Object mapping | AutoMapper |
| Persistence | Entity Framework Core 9 |
| Relational database | PostgreSQL through Npgsql |
| Database tenancy defense | PostgreSQL Row-Level Security where required |
| Cache | Redis / StackExchange.Redis |
| Async messaging | MassTransit with RabbitMQ adapter |
| Authentication | ASP.NET Core Identity / JWT Bearer |
| Password hashing | BCrypt |
| Logging | Serilog |
| API description | ASP.NET Core OpenAPI / Swashbuckle |
| API versioning | Asp.Versioning |
| Unit/integration test framework | xUnit |
| Assertions / mocks | FluentAssertions, Moq; NSubstitute is available |
| Infrastructure integration tests | Testcontainers / PostgreSQL |
| Static/source-analysis gates | Roslyn APIs where required by architecture/contract gates |

Exact dependency versions are centrally managed in:

```text
backend/Directory.Packages.props
```

The target framework and common compiler settings are defined in:

```text
backend/Directory.Build.props
```

Do not copy package versions into feature documentation as a second version authority.

---

# 1. Backend purpose

The backend provides the server-authoritative half of the Notrelix workspace platform.

It is responsible for:

```text
business invariants and lifecycle
use-case orchestration
authorization and tenant scope
transactional persistence
public HTTP/OpenAPI contracts
reliable asynchronous delivery
integration/provider adapters
runtime composition
database migration and RLS mechanisms
```

The backend does **not** independently own:

```text
product semantics outside the owning Product context
frontend interaction architecture
organization staffing
production SLO/RPO/RTO values
deployment-provider-specific infrastructure choices
```

Those are routed to their canonical repository owners.

---

# 2. Start here

For ordinary backend work, read in this order:

```text
1. ../PRODUCT.md
2. ../RULE.md
3. ../AGENTS.md
4. ./AGENTS.md
5. owning product-context document under ../docs/product/contexts/
6. relevant backend architecture document under ./docs/architecture/
7. relevant ADR if the architecture choice matters
8. current source/tests/manifests for executable evidence
```

For tests, also read:

```text
backend/tests/AGENTS.md
```

Do **not** assume there is a per-production-project `AGENTS.md`.

The target backend documentation model deliberately avoids one local agent file per layer unless local execution semantics genuinely require one.

---

# 3. Solution inventory

`backend/backend.slnx` is the authoritative inventory for backend production/test projects.

Current production projects:

```text
src/
├── Notrelix.Domain
├── Notrelix.Application
├── Notrelix.Infrastructure
├── Notrelix.Platform
└── Notrelix.API
```

Current test projects:

```text
tests/
├── Notrelix.Architecture.Tests
├── Notrelix.Domain.Tests
├── Notrelix.Application.Tests
├── Notrelix.Infrastructure.Tests
├── Notrelix.Platform.Tests
├── Notrelix.API.Tests
└── Notrelix.Integration.Tests
```

Current testing support projects:

```text
tests/
├── Notrelix.Testing.Core
├── Notrelix.Testing.Domain
├── Notrelix.Testing.Application
└── Notrelix.Testing.Integration
```

Do not maintain a manually authored project inventory elsewhere as an equal authority.

A generated project map may summarize the solution later:

```text
backend/docs/generated/project-map.md
```

but it must be generated from executable project manifests.

---

# 4. Architectural shape

The backend remains a five-project modular monolith.

Conceptually:

```text
                       ┌──────────────────────┐
                       │    Notrelix.API      │
                       │ HTTP / composition   │
                       └──────────┬───────────┘
                                  │
                                  ▼
                       ┌──────────────────────┐
                       │ Notrelix.Application │
                       │ use cases / policy   │
                       └──────────┬───────────┘
                                  │
                                  ▼
                       ┌──────────────────────┐
                       │   Notrelix.Domain    │
                       │ business semantics   │
                       └──────────────────────┘

        ┌──────────────────────────┐   ┌──────────────────────────┐
        │ Notrelix.Infrastructure  │   │   Notrelix.Platform      │
        │ persistence / providers  │   │ delivery/runtime mech.   │
        └────────────┬─────────────┘   └────────────┬─────────────┘
                     │                              │
                     └──────► Application / Domain ◄┘
```

The simplified intended dependency rule is:

```text
API             → Application → Domain
Infrastructure  → Application → Domain
Platform        → Application → Domain
Domain          → no outer production project
```

Actual project references and architecture tests remain executable evidence.

---

# 5. Bounded contexts

The backend implements the product contexts defined at repository level.

Current canonical business contexts include:

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

A context is a **semantic/data ownership seam**.

It is not automatically:

```text
one project
one database
one service
one team
one API controller folder
```

The current architecture intentionally keeps the five backend projects rather than creating one `.csproj` per bounded context.

---

# 6. Layer responsibilities

## Domain

`Notrelix.Domain` owns:

```text
aggregate/entity/value-object behavior
business invariants
state transitions
domain events
domain-specific validation of owned state
```

Domain receives external facts from the caller when a rule depends on information it does not own.

Domain must remain free of provider/runtime/persistence dependencies.

Current project evidence intentionally contains no package references.

Read:

```text
docs/architecture/domain-modeling.md
```

---

## Application

`Notrelix.Application` owns:

```text
commands and queries
use-case orchestration
validation pipeline
authorization and resource scope
transaction boundaries
idempotency/concurrency orchestration
ports
cross-context coordination contracts
post-commit enrollment
public/application result mapping
```

Read:

```text
docs/architecture/application-model.md
```

### Current EF Core exception

The Application project currently references `Microsoft.EntityFrameworkCore`.

That package presence is an existing approved exception/transition:

```text
EX-BE-APP-EF-001
```

It does **not** authorize new handler-local `DbContext` or direct persistence implementation.

New code follows the canonical Application-port / Infrastructure-implementation boundary unless a new governed decision explicitly changes it.

---

## Infrastructure

`Notrelix.Infrastructure` owns implementation mechanics for:

```text
EF Core mappings/context
PostgreSQL
migrations
RLS mechanisms
Redis/cache adapters
authentication/provider implementation
email/provider adapters
search/storage adapters
persistence repositories/queries
```

It may depend inward on Application and Domain.

It must not move provider mechanics into Domain or make persistence shape the business model automatically.

Read:

```text
docs/architecture/infrastructure-and-data.md
```

---

## Platform

`Notrelix.Platform` owns reusable runtime/delivery mechanisms such as:

```text
post-commit delivery
messaging
consumer hosting
logical message/consumer identity
idempotency
retry/backoff
poison/dead-letter behavior
ordering machinery
background execution mechanics
```

Platform supplies mechanisms.

The owning product context still owns event meaning and business effects.

Read:

```text
docs/architecture/platform-and-messaging.md
```

---

## API

`Notrelix.API` is the transport and composition boundary.

It owns:

```text
ASP.NET Core host
routing/binding
authentication integration
HTTP/OpenAPI surface
versioning
transport-level request/response mapping
composition root
```

It does not own Domain invariants or authorize business behavior through ad-hoc endpoint logic.

Read:

```text
docs/architecture/api-and-contracts.md
```

---

# 7. Security and tenancy

Security is not one outer-layer feature.

Protected work must preserve:

```text
authentication at host boundary
Application authorization
explicit Account/Workspace/resource scope
RLS defense-in-depth where applicable
tenant-safe cache keys
tenant-safe background execution
tenant-safe realtime/event delivery
secret-safe provider/runtime configuration
```

Read:

```text
docs/architecture/security-tenancy-authorization.md
```

Repository-wide quality owner:

```text
../docs/quality/security-quality-standard.md
```

---

# 8. Cross-context behavior

A bounded context may consume facts or request work from another context through explicit contracts.

Avoid:

```text
direct foreign aggregate mutation
foreign DbSet mutation
shared mutable "Common" model
provider table used as cross-context API
internal Domain event type used as public contract by accident
```

Prefer:

```text
Application contract
query/read contract
committed integration event
explicit orchestration
```

according to consistency requirements.

The consumer does not acquire ownership of the source fact.

---

# 9. Vertical feature principle

A complete backend feature may legitimately cross several projects.

Example:

```text
Work Management behavior
        ↓
Domain invariant
        ↓
Application command
        ↓
authorization + transaction
        ↓
Infrastructure persistence
        ↓
outbox / Platform delivery
        ↓
API/OpenAPI
        ↓
tests / migration / generated consumer
```

Do not split work by project merely because the repository has layers.

Prefer the smallest **complete, compatible business transaction**.

When compatibility requires staged rollout, use explicit stages rather than one invalid intermediate merge.

---

# 10. Public contracts

Contract changes are designed before independent producer/consumer implementation.

Relevant surfaces include:

```text
REST/OpenAPI
integration/public events
realtime payloads
generated clients
provider/webhook contracts
persisted compatibility identities
```

Read repository policy:

```text
../docs/delivery/contract-first-delivery.md
```

Backend-specific contract owner:

```text
docs/architecture/api-and-contracts.md
```

Do not expose:

```text
EF entities
provider DTOs
Domain CLR names as public event identity
internal package/private implementation detail
```

as accidental public contracts.

---

# 11. Persistence and data change

Persistence evolution follows:

```text
business meaning
→ Application/Domain contract
→ mapping/schema
→ migration
→ existing-data proof
→ staged rollout where required
```

The EF model and migration chain/current generated schema are persistence evidence.

Pending model changes are resolved intentionally.

Do not suppress model drift to make startup green.

Read:

```text
docs/operations/migrations-and-data-change.md
../docs/delivery/migration-policy.md
```

For material migrations instantiate:

```text
../docs/templates/migration-plan-template.md
```

---

# 12. Runtime dependencies

Current implementation includes dependency roles such as:

```text
PostgreSQL
→ authoritative relational persistence

Redis
→ scoped cache/acceleration

RabbitMQ through MassTransit
→ async delivery transport where configured

provider adapters
→ email/auth/integration/external mechanics
```

The dependency's runtime technology does not become product authority.

Runtime/degradation/recovery semantics are documented at repository and backend level.

---

# 13. Local prerequisites

Required baseline:

```text
.NET SDK 9.0.313-compatible installation
Docker / Docker Compose for local dependencies/full stack
repository environment configuration
```

From repository root, current safe environment bootstrap is:

```bash
cp .env.example .env.dev
```

Populate only local values.

Never commit `.env.dev`.

Read:

```text
../docs/delivery/local-development.md
docs/operations/configuration-and-runtime.md
```

---

# 14. Build and test

From `backend/`:

```bash
dotnet restore backend.slnx
dotnet build backend.slnx
dotnet test backend.slnx
```

These are the broad local solution commands.

During implementation, run the narrowest meaningful test first, then the broader proof required by the changed contract.

Examples:

```bash
dotnet test tests/Notrelix.Domain.Tests/Notrelix.Domain.Tests.csproj
dotnet test tests/Notrelix.Application.Tests/Notrelix.Application.Tests.csproj
dotnet test tests/Notrelix.Platform.Tests/Notrelix.Platform.Tests.csproj
dotnet test tests/Notrelix.Integration.Tests/Notrelix.Integration.Tests.csproj
```

Do not claim full backend validation after running one focused project.

---

# 15. Root Docker/Make helpers

From repository root, current helpers include:

```bash
make dev-up
make dev-down
make dev-logs

make db-up
make db-migrate
make db-seed
make db-init
make db-rls
make db-psql

make be-build
make be-test
make be-shell
```

Use the repository commands rather than inventing private bootstrap workflows.

Exact current helper definitions remain in:

```text
../Makefile
```

---

# 16. Database workflow

For local database work:

```text
model/config change
→ reviewed EF migration
→ local apply
→ RLS/index/constraint verification
→ Infrastructure/Integration proof
```

Do not:

```text
manually modify local DB
→ forget migration
```

and do not use empty-database success as the only migration proof for a change that touches existing production data.

---

# 17. Test topology

Tests prove different contracts.

| Test project | Primary responsibility |
|---|---|
| `Notrelix.Domain.Tests` | pure behavior, invariants, no-op, event/version semantics |
| `Notrelix.Application.Tests` | handlers, validators, pipeline, authorization, result semantics |
| `Notrelix.Infrastructure.Tests` | mappings, adapters, persistence/provider mechanics, RLS/migrations |
| `Notrelix.Platform.Tests` | outbox/post-commit, messaging, idempotency, ordering, retry/poison |
| `Notrelix.API.Tests` | binding, auth integration, OpenAPI/error/host contracts |
| `Notrelix.Integration.Tests` | production graph, PostgreSQL/RLS, cross-layer reliability |
| `Notrelix.Architecture.Tests` | dependency/placement/forbidden reference rules |

Testing support libraries under `Notrelix.Testing.*` provide reusable setup.

They must not hide product assertions in generic helpers.

Read:

```text
tests/AGENTS.md
docs/architecture/testing-and-quality-gates.md
../docs/quality/testing-strategy.md
```

---

# 18. Required evidence

A backend change is not complete because it compiles.

Evidence follows the property.

Examples:

```text
Domain invariant
→ Domain test

Application authorization
→ Application test + integration negative case where needed

RLS
→ PostgreSQL-realistic Infrastructure/Integration test

messaging ordering/idempotency
→ Platform test + integration where source transaction matters

OpenAPI
→ API contract/OpenAPI drift evidence

project dependency
→ Architecture test

migration
→ clean + existing-state upgrade proof
```

A required suite selecting zero relevant tests is a failure.

---

# 19. CI

Backend CI is repository evidence, not architecture.

Current CI separates concerns such as:

```text
quality/security
architecture
core layer tests
Platform tests
API/OpenAPI
integration
Docker packaging
final gate
```

Exact job names may evolve.

The protected properties must not disappear merely because workflow topology changes.

Never disable a required gate to land an architectural shortcut.

---

# 20. Documentation map

Backend documentation:

```text
docs/
├── README.md
├── architecture/
│   ├── backend-overview.md
│   ├── domain-modeling.md
│   ├── application-model.md
│   ├── infrastructure-and-data.md
│   ├── platform-and-messaging.md
│   ├── api-and-contracts.md
│   ├── security-tenancy-authorization.md
│   └── testing-and-quality-gates.md
├── operations/
│   ├── configuration-and-runtime.md
│   └── migrations-and-data-change.md
├── decisions/
│   └── README.md
└── generated/
    └── project-map.md
```

The generated project map is not manually authored authority.

---

# 21. Documentation authority

For backend work:

```text
product meaning
→ ../docs/product/contexts/**

system boundary
→ ../docs/architecture/**

backend architecture
→ ./docs/architecture/**

backend runtime/data procedure
→ ./docs/operations/**

historical backend architecture rationale
→ ./docs/decisions/**

current project/package/source fact
→ solution/project/source/test manifests
```

No roadmap, audit, freeze checklist, migration tracker, or old engineering tree becomes current architecture merely because it contains useful historical knowledge.

---

# 22. Source/doc disagreement

When source and canonical documentation disagree, classify the discrepancy.

Possible classes:

```text
DOC_STALE
SOURCE_DEBT
TRANSITION
CONTRACT_CHANGE
UNRESOLVED
```

Do not blindly:

```text
change docs to match source
```

or:

```text
change source to match docs
```

before identifying which owner is authoritative.

---

# 23. Architecture decisions

Backend ADRs live under:

```text
docs/decisions/
```

Current backend history includes:

```text
ADR-001 Pipeline boundary
ADR-002 RLS bootstrap connection lifecycle
ADR-003 CSRF protection
ADR-004 Rate limiting architecture
```

ADRs preserve historical rationale.

Current architecture is defined in architecture docs.

When an accepted decision changes, supersede rather than silently rewrite the old ADR.

---

# 24. Change classification

Before a material backend change, classify it using:

```text
../docs/delivery/change-classification.md
```

Common backend examples:

```text
new private helper
→ often C0

additive API field
→ C1

behavioral semantic change
→ C2

breaking API/event change
→ C3

schema/backfill/RLS
→ C4 (+ C6 where tenant/security)

project/layer/context dependency
→ C5

authorization/tenant/security
→ C6

runtime/config/dependency
→ C7

destructive/financial/retention
→ C8
```

Obligations are cumulative.

---

# 25. Definition of Done

Backend completion follows repository:

```text
../docs/delivery/definition-of-done.md
```

At minimum, the applicable change must have:

```text
correct semantic owner
correct architecture owner
security/tenant preservation
contract compatibility
migration/data proof if applicable
tests at the right seam
required broader gates
documentation/ADR updates
rollout/recovery plan if required
exact revision evidence
```

---

# 26. Common anti-patterns

Do not introduce:

```text
Domain → Infrastructure dependency
provider SDK in Domain
new handler-local DbContext persistence
role-string authorization scattered in handlers
foreign context table mutation
broad Common/Shared dumping ground
public contracts tied to EF/provider DTOs
cache as product/permission truth
event retry without idempotency
cursor/order advancement before successful processing
migration that assumes empty DB
RLS inferred only from column-name convention
feature-specific policy buried in API endpoint
```

If current source contains one of these as known debt, classify it rather than using it as precedent.

---

# 27. New backend capability workflow

For a new capability:

```text
1. identify Product context
2. identify aggregate/resource owner
3. define operation semantics
4. define authorization + tenant scope
5. identify external/cross-context facts
6. determine Domain behavior
7. determine Application orchestration
8. determine persistence/RLS/migration
9. determine event/async consequences
10. determine HTTP/OpenAPI contract
11. determine frontend/generated consumer impact
12. write proof matrix
13. classify rollout/migration
14. implement smallest complete compatible change
```

Do not start from:

```text
controller
table
service class
repository
```

and invent product semantics afterward.

---

# 28. Backend review questions

Before review, answer:

```text
Which bounded context owns the behavior?
Which aggregate/resource owns consistency?
What is the command/query semantic?
Who is authorized?
Which tenant/resource scope is authoritative?
What external facts does Domain need supplied?
Which transaction commits the state?
What event/outbox/realtime work follows commit?
Does existing data migrate?
Which old consumers coexist?
Which tests prove each protected property?
Which architecture/ADR/docs changed?
```

If these answers are unresolved, the change is not ready for normal implementation/review.

---

# 29. Do not create docs for symmetry

The backend target deliberately does **not** require:

```text
one README per source project
one AGENTS.md per source project
one docs folder per bounded context
one ADR per feature
one project per bounded context
```

Create scoped documentation only when behavior/instructions genuinely differ and a local owner is necessary.

---

# 30. Current-source caveats

Current source evidence can contain temporary/approved exceptions.

A notable example is:

```text
Notrelix.Application
→ currently references Microsoft.EntityFrameworkCore
```

under existing exception `EX-BE-APP-EF-001`.

The correct inference is:

```text
current exception exists
```

not:

```text
Application now owns EF persistence
```

Preserve this distinction during documentation migration.

---

# 31. Final backend orientation

Before editing backend code, be able to answer:

```text
What product/context rule am I implementing?
Which backend layer owns each responsibility?
Which project references are allowed?
What is the server-authoritative permission/tenant path?
What is the transaction boundary?
What persistence/RLS/migration changes occur?
What public/event/realtime contract changes?
What async idempotency/ordering/retry behavior changes?
Which current source facts are evidence versus debt/exception?
Which tests/gates prove the result?
```

If you cannot answer these from canonical docs and source evidence, read the relevant owner before coding.
