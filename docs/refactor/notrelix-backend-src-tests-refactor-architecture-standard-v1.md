# Notrelix Backend `src/` + `tests/` Refactor Architecture Standard v1

> Target architecture and agent execution rules for refactoring the Notrelix backend into an enterprise-grade `src/` + `tests/` layout.
>
> Scope: backend project layout, test project ownership, dependency boundaries, CI lanes, refactor phases, and agent guardrails.
>
> Goal: move from the current hybrid backend/test layout to a clean enterprise baseline without changing product behavior.

---

## 0. Executive Decision

Notrelix should use a clean backend layout:

```txt
backend/
├── backend.slnx
├── global.json
├── Directory.Build.props
├── Directory.Packages.props
├── Dockerfile
├── src/
│   ├── Notrelix.Domain/
│   ├── Notrelix.Application/
│   ├── Notrelix.Infrastructure/
│   └── Notrelix.API/
└── tests/
    ├── Notrelix.Architecture.Tests/
    ├── Notrelix.Domain.Tests/
    ├── Notrelix.Application.Tests/
    ├── Notrelix.Infrastructure.Tests/
    ├── Notrelix.API.Tests/
    ├── Notrelix.Integration.Tests/
    ├── Notrelix.Contract.Tests/
    ├── Notrelix.Security.Tests/
    ├── Notrelix.Performance.Tests/
    ├── Notrelix.E2E.Tests/
    ├── Notrelix.Testing.Core/
    ├── Notrelix.Testing.Domain/
    ├── Notrelix.Testing.Application/
    └── Notrelix.Testing.Integration/
```

This is the target-state architecture. The refactor must not be constrained by the current hybrid test structure.

The current backend has production projects directly under `backend/` and a hybrid nested test structure under `backend/Notrelix.Tests/`. The target architecture separates production projects from all test projects and removes the parent wrapper test project.

---

## 1. Core Architecture Principle

The test structure must protect the same boundaries as the production architecture.

```txt
Production code is organized by runtime responsibility.
Test code is organized by system guarantee and risk boundary.
```

Therefore:

```txt
src/     = deployable/runtime production projects
tests/   = verification projects, contracts, security, integration, fixtures
```

A test project must not reference more production layers than it needs.

---

## 2. Why `src/` + `tests/` Is Required

The current layout is acceptable for early development, but not ideal for a large enterprise SaaS.

Problems with a hybrid test layout:

```txt
- Production projects and test projects are mixed at the backend root.
- Parent Notrelix.Tests project can become a wrapper/exclusion project.
- Nested test projects under another test project are harder to reason about.
- CI lanes are harder to split cleanly.
- Test support helpers can leak heavy dependencies into pure unit tests.
- The solution structure does not express target architecture clearly.
```

Target advantages:

```txt
- Clear production/test separation.
- Clean dependency direction.
- Faster CI lane design.
- Easier onboarding for future contributors.
- Better long-term growth for integration, contract, security and performance tests.
- Lower future refactor cost.
```

---

## 3. Target Production Projects

### 3.1 `src/Notrelix.Domain`

Purpose:

```txt
- Aggregates
- Entities
- Value objects
- Domain services
- Domain events
- Business invariants
- Shared kernel primitives
```

Must not reference:

```txt
- Application
- Infrastructure
- API
- EF Core
- ASP.NET Core
- MediatR
- HTTP concepts
- Outbox/processed event infrastructure
```

### 3.2 `src/Notrelix.Application`

Purpose:

```txt
- CQRS contracts
- Commands/queries
- Handlers
- Pipeline behaviors
- Application abstractions
- DTOs/results
- Integration event contracts/mappers when part of app boundary
```

May reference:

```txt
- Domain
```

Must not reference:

```txt
- Infrastructure
- API
- ASP.NET Core HTTP types
- DbContext concrete implementations
- Provider concrete classes
```

### 3.3 `src/Notrelix.Infrastructure`

Purpose:

```txt
- EF Core DbContext
- EF configurations
- Migrations
- Persistence implementations
- Outbox dispatcher/store
- Processed event store
- Idempotency store
- Job locks
- Search projections
- Permission cache stores
- External provider implementations
- Messaging, cache, file storage, email, auth providers
```

May reference:

```txt
- Domain
- Application
```

Must not reference:

```txt
- API
```

### 3.4 `src/Notrelix.API`

Purpose:

```txt
- Minimal API endpoints
- HTTP pipeline
- Authentication/authorization middleware
- ProblemDetails
- OpenAPI
- Composition root
- DI registration
- Health checks
```

May reference:

```txt
- Application
- Infrastructure for composition only
```

Endpoint code must not:

```txt
- Inject DbContext directly
- Return Domain entities
- Return EF entities
- Reference Domain aggregate namespaces directly
- Use Infrastructure concrete services directly outside composition
```

---

## 4. Target Test Projects

### 4.1 `tests/Notrelix.Architecture.Tests`

Purpose:

```txt
Lock architecture boundaries using reflection/source scanning.
```

References:

```txt
- Notrelix.Domain
- Notrelix.Application
- Notrelix.Infrastructure
- Notrelix.API
- Notrelix.Testing.Core
```

Allowed packages:

```txt
- xUnit
- FluentAssertions
- optional: NetArchTest later if adopted by ADR
```

Must test:

```txt
- Domain dependency rules
- Application dependency rules
- Infrastructure dependency rules
- API endpoint rules
- folder/naming rules
- forbidden dependencies
```

### 4.2 `tests/Notrelix.Domain.Tests`

Purpose:

```txt
Pure business behavior tests.
```

References:

```txt
- Notrelix.Domain
- Notrelix.Testing.Core
- Notrelix.Testing.Domain
```

Must not reference:

```txt
- Application
- Infrastructure
- API
- EF Core
- ASP.NET Core
- Testcontainers
- WebApplicationFactory
```

Tests:

```txt
- aggregate creation
- lifecycle/state transitions
- invariants
- value object validation
- domain events
- version increment behavior
- soft delete/restore behavior
```

### 4.3 `tests/Notrelix.Application.Tests`

Purpose:

```txt
Use case orchestration and pipeline behavior tests with fakes/test doubles.
```

References:

```txt
- Notrelix.Application
- Notrelix.Domain
- Notrelix.Testing.Core
- Notrelix.Testing.Domain
- Notrelix.Testing.Application
```

Must not reference:

```txt
- Infrastructure
- API
- Testcontainers
- WebApplicationFactory
- concrete DbContext implementations
```

Tests:

```txt
- TransactionBehavior
- WorkspaceContextBehavior
- AuthorizationBehavior
- IdempotencyBehavior
- EntitlementBehavior
- ConcurrencyBehavior
- CacheBehavior
- CacheInvalidationBehavior
- ExceptionMappingBehavior
- command/query handler behavior
```

### 4.4 `tests/Notrelix.Infrastructure.Tests`

Purpose:

```txt
Verify real technical behavior with real PostgreSQL/Redis where needed.
```

References:

```txt
- Notrelix.Infrastructure
- Notrelix.Application
- Notrelix.Domain
- Notrelix.Testing.Core
- Notrelix.Testing.Domain
- Notrelix.Testing.Application
- Notrelix.Testing.Integration
```

Must not reference:

```txt
- API, unless explicitly required by an approved integration fixture
```

Tests:

```txt
- EF mapping
- migrations
- PostgreSQL schema names
- JSONB mapping
- unique constraints
- concurrency tokens
- workspace filters
- outbox atomicity
- outbox dispatcher claim/retry/dead-letter
- processed event uniqueness
- idempotency store
- job lock manager
- search projection stores
- permission cache store
- token hashing/encryption/signature algorithms
```

### 4.5 `tests/Notrelix.API.Tests`

Purpose:

```txt
Verify HTTP behavior and API contracts through WebApplicationFactory/TestServer.
```

References:

```txt
- Notrelix.API
- Notrelix.Application
- Notrelix.Domain
- Notrelix.Testing.Core
- Notrelix.Testing.Integration
```

May reference:

```txt
- Notrelix.Infrastructure only if required to boot the test host.
```

Tests:

```txt
- routing
- auth middleware
- workspace resolution
- ProblemDetails
- OpenAPI smoke
- endpoint happy paths
- endpoint validation errors
- endpoint authorization failures
- idempotency/concurrency HTTP behavior
```

### 4.6 `tests/Notrelix.Integration.Tests`

Purpose:

```txt
Cross-layer product behavior with real runtime dependencies.
```

Examples:

```txt
- command -> EF -> outbox -> dispatcher -> projection
- workspace isolation across API/Application/DB
- permission evaluator with real stores
- search indexing flow
- billing entitlement enforcement with persistence
```

This project is slower and belongs to the integration CI lane.

### 4.7 `tests/Notrelix.Contract.Tests`

Purpose:

```txt
Protect external contracts.
```

Tests:

```txt
- OpenAPI contract snapshots/smoke
- ProblemDetails contract
- webhook contract
- public API compatibility
```

Do not add noisy snapshots until API shape stabilizes.

### 4.8 `tests/Notrelix.Security.Tests`

Purpose:

```txt
Verify security-sensitive behavior.
```

Tests:

```txt
- tenant isolation
- auth failure behavior
- permission denial
- webhook signature and replay protection
- token hashing
- secret redaction in ProblemDetails/logs
- rate limit behavior
```

### 4.9 `tests/Notrelix.Performance.Tests`

Purpose:

```txt
Performance smoke tests, not full load testing.
```

Tests:

```txt
- board items cursor pagination
- search query smoke
- permission decision smoke
- outbox batch claim smoke
- reporting dashboard query smoke
```

Run only nightly/pre-release.

### 4.10 `tests/Notrelix.E2E.Tests`

Purpose:

```txt
Few critical product journeys only.
```

Examples:

```txt
- register/login -> create workspace -> create board -> create item
- invite member -> accept invitation -> member sees workspace
- permission denied -> private board not accessible
- inbound webhook -> signature verified -> idempotency enforced
```

Do not create E2E tests for every endpoint.

---

## 5. Shared Testing Libraries

### 5.1 `tests/Notrelix.Testing.Core`

References:

```txt
No production project references.
```

Contains:

```txt
- TestIds
- TestClock
- seeded random helper
- assertion helper primitives
- file/path/source scanning utilities
- collection/trait constants
```

Must not contain:

```txt
- Domain builders
- Application fakes
- Infrastructure fixtures
- WebApplicationFactory
- Testcontainers
```

### 5.2 `tests/Notrelix.Testing.Domain`

References:

```txt
- Notrelix.Domain
- Notrelix.Testing.Core
```

Contains:

```txt
- Domain builders
- aggregate factories
- value object factories
- domain event assertions
```

### 5.3 `tests/Notrelix.Testing.Application`

References:

```txt
- Notrelix.Application
- Notrelix.Domain
- Notrelix.Testing.Core
- Notrelix.Testing.Domain
```

Contains:

```txt
- FakeCurrentUser
- FakeCurrentWorkspace
- FakeDateTimeProvider
- FakePermissionService
- FakeEntitlementService
- FakeIdempotencyStore
- fake unit of work/application db abstraction
```

Must not reference Infrastructure/API.

### 5.4 `tests/Notrelix.Testing.Integration`

References:

```txt
- Notrelix.Infrastructure
- Notrelix.API when needed
- Notrelix.Testing.Core
```

Contains:

```txt
- PostgresFixture
- RedisFixture
- ApiFactory
- AuthenticatedClientFactory
- TestWorkspaceFactory
- database reset strategy
- integration seed helpers
```

This is the only shared testing project allowed to contain Testcontainers/WebApplicationFactory dependencies.

---

## 6. Dependency Matrix

| Project | May Reference | Must Not Reference |
|---|---|---|
| Domain | none | Application, Infrastructure, API, EF Core, ASP.NET Core, MediatR |
| Application | Domain | Infrastructure, API, ASP.NET Core HTTP types |
| Infrastructure | Domain, Application | API |
| API | Application, Infrastructure for composition | Domain aggregates in endpoint code |
| Architecture.Tests | all assemblies for inspection | external runtime dependencies unless needed |
| Domain.Tests | Domain, Testing.Core, Testing.Domain | Application, Infrastructure, API, Docker |
| Application.Tests | Application, Domain, Testing.Core/Domain/Application | Infrastructure, API, Docker |
| Infrastructure.Tests | Infrastructure, Application, Domain, Testing.Integration | API by default |
| API.Tests | API, Application, Domain, Testing.Integration | direct handler testing |
| Integration.Tests | API/Infrastructure/Application/Domain as needed | real external providers |
| Contract.Tests | API/contracts | unstable implementation internals |
| Security.Tests | API/Infrastructure as needed | real external providers |
| Performance.Tests | runtime stack as needed | PR fast lane |
| E2E.Tests | deployed/testhost runtime | broad endpoint coverage |

---

## 7. Project Naming Rules

Use exactly:

```txt
Notrelix.Architecture.Tests
Notrelix.Domain.Tests
Notrelix.Application.Tests
Notrelix.Infrastructure.Tests
Notrelix.API.Tests
Notrelix.Integration.Tests
Notrelix.Contract.Tests
Notrelix.Security.Tests
Notrelix.Performance.Tests
Notrelix.E2E.Tests
Notrelix.Testing.Core
Notrelix.Testing.Domain
Notrelix.Testing.Application
Notrelix.Testing.Integration
```

Do not use:

```txt
Notrelix.Tests as a parent wrapper project
Notrelix.Tests/Domain nested project layout
Generic Tests project that references all layers by default
```

---

## 8. Test Tooling Standard

Use one consistent stack:

```txt
Test framework:        xUnit
Assertions:            FluentAssertions
Mocking:               NSubstitute
API tests:             WebApplicationFactory/TestServer
Database integration:  Testcontainers.PostgreSql
Cache integration:     Testcontainers.Redis only when Redis behavior matters
DB reset:              Respawn-like reset or recreate database/schema per collection
Coverage:              coverlet
Architecture tests:    custom reflection/source scanning first; NetArchTest only by ADR
```

Rules:

```txt
- Do not mix Moq and NSubstitute in new tests.
- Do not add snapshot testing until API contracts stabilize.
- Do not use EF InMemory for PostgreSQL-specific behavior.
- Do not start Docker in Domain/Application tests.
```

---

## 9. Test Categories and CI Traits

Use traits:

```csharp
[Trait("Category", "Architecture")]
[Trait("Category", "Unit")]
[Trait("Category", "Application")]
[Trait("Category", "Integration")]
[Trait("Category", "Api")]
[Trait("Category", "Contract")]
[Trait("Category", "Security")]
[Trait("Category", "Performance")]
[Trait("Category", "E2E")]
[Trait("Category", "Slow")]
```

Rules:

```txt
Architecture, Domain and Application tests must be fast.
Infrastructure/API/Integration tests using Testcontainers must be tagged Integration or Api.
Performance/E2E tests do not run in PR fast lane.
```

---

## 10. Refactor Strategy

### Phase 1 — Structural Layout Only

Goal: move projects, update references, keep behavior unchanged.

Move:

```txt
backend/Notrelix.Domain            -> backend/src/Notrelix.Domain
backend/Notrelix.Application       -> backend/src/Notrelix.Application
backend/Notrelix.Infrastructure    -> backend/src/Notrelix.Infrastructure
backend/Notrelix.API               -> backend/src/Notrelix.API
```

Move existing test projects:

```txt
backend/Notrelix.Tests/Domain         -> backend/tests/Notrelix.Domain.Tests
backend/Notrelix.Tests/Application    -> backend/tests/Notrelix.Application.Tests
backend/Notrelix.Tests/Infrastructure -> backend/tests/Notrelix.Infrastructure.Tests
backend/Notrelix.Tests/API            -> backend/tests/Notrelix.API.Tests
```

Remove:

```txt
backend/Notrelix.Tests/Notrelix.Tests.csproj
```

Only remove the parent project if it is a wrapper/exclusion project and contains no meaningful tests that cannot be moved.

Update:

```txt
- backend.slnx
- ProjectReference paths
- Dockerfile paths
- EF migration commands in docs/scripts
- Makefile/task runner paths if any
- README backend command examples
- CI workflow paths if any
```

Do not:

```txt
- rename namespaces unless required
- refactor business code
- add new test projects unless needed to keep build clean
- create placeholder tests
```

### Phase 2 — Add Target Test Projects

Add only projects that will contain real tests immediately:

```txt
Notrelix.Architecture.Tests
Notrelix.Integration.Tests
Notrelix.Contract.Tests
Notrelix.Security.Tests
Notrelix.Performance.Tests
Notrelix.E2E.Tests
```

Do not create empty enterprise-looking projects.

Minimum recommended immediate additions:

```txt
Notrelix.Architecture.Tests
Notrelix.Testing.Core
Notrelix.Testing.Domain
Notrelix.Testing.Application
Notrelix.Testing.Integration
```

### Phase 3 — Normalize Test Support

Move duplicated builders/fakes/fixtures into the correct `Testing.*` project.

Rules:

```txt
Domain builders -> Testing.Domain
Application fakes -> Testing.Application
Postgres/API fixtures -> Testing.Integration
Pure helpers -> Testing.Core
```

### Phase 4 — P0 Guardrails

Add meaningful P0 tests:

```txt
Architecture.Tests:
- LayerDependencyTests
- DomainArchitectureTests
- ApplicationArchitectureTests
- ApiArchitectureTests

Application.Tests:
- TransactionBehaviorTests
- WorkspaceContextBehaviorTests
- AuthorizationBehaviorTests
- IdempotencyBehaviorTests
- EntitlementBehaviorTests

Infrastructure.Tests:
- PostgresMigrationSmokeTests
- WorkspaceFilterFailClosedTests
- OutboxAtomicityTests
- ProcessedEventStoreTests

API.Tests:
- ProblemDetailsContractTests
- AuthenticationFlowTests
- WorkspaceRoutingTests
- BoardEndpointSmokeTests
- OpenApiContractSmokeTests
```

---

## 11. Architecture Test Rules

### Domain

```txt
Domain must not reference Application.
Domain must not reference Infrastructure.
Domain must not reference API.
Domain must not reference EF Core.
Domain must not reference ASP.NET Core.
Domain must not reference MediatR.
Domain must not contain DbContext, OutboxMessage, ProcessedEvent, ProblemDetails.
Domain must not use DateTime.UtcNow/DateTimeOffset.UtcNow directly if a clock abstraction is required by the architecture.
```

### Application

```txt
Application must not reference Infrastructure.
Application must not reference API.
Application must not use HttpContext, IActionResult, StatusCodes or ASP.NET Core ProblemDetails.
Commands must implement ICommand or ICommand<T>.
Queries must implement IQuery<T>.
Workspace-scoped requests must implement IWorkspaceRequest.
Transactional requests must implement ITransactionalRequest.
Handlers for ITransactionalRequest must not call SaveChangesAsync directly.
Application must depend on abstractions, not Infrastructure concrete providers.
```

### Infrastructure

```txt
Infrastructure may reference Domain and Application.
Infrastructure must not reference API.
EF configurations belong in Infrastructure.
OutboxMessage and ProcessedEvent records belong in Infrastructure.
Infrastructure services must implement Application abstractions.
Background workers must be idempotent.
ProcessedEvent uniqueness must be EventId + ConsumerName.
```

### API

```txt
API endpoint files must not inject DbContext directly.
API endpoint files must not return Domain aggregates/entities.
API endpoint files must not return EF entities.
API endpoint files must not use Infrastructure concrete services directly outside composition.
API must return HTTP contracts/DTOs/ProblemDetails.
Minimal API is the default unless an ADR approves controllers.
```

---

## 12. Test Ownership Rules

```txt
Domain behavior change -> Domain.Tests
Application use case or pipeline change -> Application.Tests
EF/persistence/outbox/cache/provider change -> Infrastructure.Tests
Endpoint/HTTP/middleware/ProblemDetails change -> API.Tests
Cross-layer behavior -> Integration.Tests
Public contract change -> Contract.Tests
Tenant/security behavior -> Security.Tests
Performance regression risk -> Performance.Tests
Critical product journey -> E2E.Tests
```

---

## 13. Non-Negotiable Guardrails

Forbidden:

```txt
- Placeholder tests
- Assert.True(true) tests
- Empty enterprise-looking projects/folders
- EF InMemory for PostgreSQL-specific behavior
- Docker/Testcontainers in Domain/Application tests
- Real external provider calls in normal tests
- Test order dependency
- Random data without seed when asserting exact values
- Mixing Moq and NSubstitute in new tests
- Big-bang business refactor during layout refactor
- Changing namespaces unnecessarily
- Returning Domain/EF entities from API tests/contracts
```

Required:

```txt
- Every test protects a rule, behavior, contract, reliability guarantee or product journey.
- Every new test project has at least one real test or real shared helper immediately used.
- Every integration test that requires PostgreSQL uses real PostgreSQL.
- Every workspace isolation test uses at least two workspaces.
- Every outbox/consumer idempotency test verifies EventId + ConsumerName.
```

---

## 14. CI Lanes

### Fast PR Lane

Run:

```txt
dotnet restore
dotnet build
Notrelix.Architecture.Tests
Notrelix.Domain.Tests
Notrelix.Application.Tests
API architecture tests that do not need runtime dependencies
```

Target:

```txt
3–5 minutes
```

### Integration PR Lane

Run when backend infrastructure/API paths change or label is applied:

```txt
Notrelix.Infrastructure.Tests with Category=Integration
Notrelix.API.Tests with Category=Api/Integration
Notrelix.Integration.Tests
```

Target:

```txt
10–15 minutes
```

### Nightly Full Lane

Run:

```txt
all tests
migration smoke
outbox retry/dead-letter
webhook tests
OpenAPI contract tests
security smoke
performance smoke
large seed profile
```

### Pre-Release Lane

Run:

```txt
all tests
database migration apply/rollback smoke
production-like seed
critical E2E smoke journeys
billing/webhook/search/reporting checks
```

---

## 15. Agent Refactor Prompt

Use this prompt when assigning the refactor to a coding agent.

```txt
Refactor the Notrelix backend to the enterprise `src/` + `tests/` target layout.

Current state:
- Production projects are directly under backend/.
- Test projects are nested under backend/Notrelix.Tests/.
- The parent Notrelix.Tests project must not remain as a wrapper/exclusion project.

Target layout:
backend/
  backend.slnx
  global.json
  Directory.Build.props
  Directory.Packages.props
  Dockerfile
  src/
    Notrelix.Domain/
    Notrelix.Application/
    Notrelix.Infrastructure/
    Notrelix.API/
  tests/
    Notrelix.Architecture.Tests/
    Notrelix.Domain.Tests/
    Notrelix.Application.Tests/
    Notrelix.Infrastructure.Tests/
    Notrelix.API.Tests/
    Notrelix.Integration.Tests/
    Notrelix.Contract.Tests/
    Notrelix.Security.Tests/
    Notrelix.Performance.Tests/
    Notrelix.E2E.Tests/
    Notrelix.Testing.Core/
    Notrelix.Testing.Domain/
    Notrelix.Testing.Application/
    Notrelix.Testing.Integration/

Execute in phases.

Phase 1 only:
1. Move production projects into backend/src/.
2. Move existing nested test projects into backend/tests/.
3. Remove backend/Notrelix.Tests/Notrelix.Tests.csproj if it only acts as parent/wrapper/exclusion project.
4. Update backend.slnx paths.
5. Update all ProjectReference paths.
6. Update Dockerfile and any script/documentation paths that reference old project locations.
7. Do not change namespaces unless compilation requires it.
8. Do not modify business logic.
9. Do not add placeholder tests.
10. Delete or replace PlaceholderTests.cs with meaningful architecture/API tests.
11. Run dotnet restore, dotnet build, dotnet test.

Dependency rules after Phase 1:
- Domain.Tests references only Domain + Testing.Core/Testing.Domain if available.
- Application.Tests references Application + Domain + Testing.Core/Testing.Domain/Testing.Application if available.
- Infrastructure.Tests references Infrastructure + Application + Domain + Testing.Integration if available.
- API.Tests references API and test host dependencies; Infrastructure only if required to boot WebApplicationFactory.
- No test project references more layers than necessary.

Phase 2:
- Add Notrelix.Architecture.Tests and Testing.* projects.
- Add real architecture tests; do not create empty projects/folders.

Phase 3:
- Add Integration/Contract/Security/Performance/E2E projects only if each contains at least one real test immediately.

Acceptance criteria:
- Solution builds.
- dotnet test passes.
- backend.slnx points to src/ and tests/ paths only.
- No backend/Notrelix.Tests wrapper project remains.
- No placeholder tests remain.
- No empty enterprise-looking test projects/folders are created.
- No production behavior changes.
- Dockerfile/EF/script paths are updated.
- Test dependency boundaries follow the architecture standard.
```

---

## 16. Acceptance Criteria for Final Target

```txt
[ ] backend/src contains all production projects.
[ ] backend/tests contains all test and testing-support projects.
[ ] backend/Notrelix.Tests parent wrapper project is removed.
[ ] backend.slnx has no old project paths.
[ ] ProjectReference paths are correct.
[ ] Dockerfile paths are correct.
[ ] EF migration commands/docs are updated.
[ ] dotnet restore passes.
[ ] dotnet build passes.
[ ] dotnet test passes.
[ ] No placeholder tests exist.
[ ] No empty enterprise projects/folders exist.
[ ] Domain.Tests has no Infrastructure/API dependency.
[ ] Application.Tests has no Infrastructure/API dependency.
[ ] Infrastructure.Tests does not reference API by default.
[ ] API.Tests uses WebApplicationFactory for HTTP behavior.
[ ] PostgreSQL behavior tests use Testcontainers.PostgreSQL.
[ ] CI lanes can run fast tests separately from integration/API tests.
```

---

## 17. Final Standard

The final backend architecture is:

```txt
src/ protects deployable production boundaries.
tests/ protects system guarantees and risk boundaries.
Architecture tests lock structure.
Domain tests lock business model.
Application tests lock use cases and pipelines.
Infrastructure tests lock technical reliability.
API tests lock HTTP behavior.
Integration tests lock cross-layer behavior.
Contract tests lock public contracts.
Security tests lock tenant/security guarantees.
Performance tests catch regressions.
E2E tests cover only critical journeys.
```

A test architecture is successful when it makes the system safer to change without making everyday development slow.
