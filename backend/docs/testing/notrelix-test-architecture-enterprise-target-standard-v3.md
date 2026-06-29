# Notrelix Test Architecture Enterprise Target Standard v3

> Target-state testing architecture for Notrelix as a large enterprise SaaS platform.  
> This document is intentionally not bound to the current test project layout.  
> It defines the architecture Notrelix should refactor toward to avoid repeated restructuring later.

---

## 0. Executive Decision

Notrelix must treat tests as a **quality platform**, not as a folder of unit tests.

The final target model is a **multi-project test architecture** organized by risk boundary, dependency direction, runtime cost and CI lane.

Recommended target:

```txt
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
  Notrelix.E2E.Tests/               # optional until frontend journeys mature

  Notrelix.Testing.Core/
  Notrelix.Testing.Domain/
  Notrelix.Testing.Application/
  Notrelix.Testing.Integration/
```

The key decision is:

```txt
Do not organize tests only by production layer.
Organize tests by the kind of system guarantee they protect.
```

Architecture tests protect boundaries.  
Domain tests protect business invariants.  
Application tests protect use cases and pipeline behavior.  
Infrastructure tests protect PostgreSQL, EF, outbox, stores and real technical guarantees.  
API tests protect HTTP contracts and middleware.  
Integration tests protect cross-layer reliability.  
Contract tests protect external compatibility.  
Security tests protect tenant isolation, authorization and sensitive flows.  
Performance smoke tests protect obvious scaling regressions.  
E2E tests protect only critical product journeys.

---

## 1. Why the Target Model Must Be Multi-Project

A single test project is acceptable for a small application, but Notrelix is not a CRUD app. It has DDD, CQRS, multi-tenancy, PostgreSQL multi-schema, outbox, idempotency, governance permissions, search, billing, automation and realtime collaboration.

A single test project usually references every production project:

```txt
Notrelix.Tests
  -> Domain
  -> Application
  -> Infrastructure
  -> API
```

This is convenient, but it weakens architecture enforcement because any test can accidentally use any layer.

The target model uses compiler boundaries as architecture boundaries:

```txt
Domain.Tests cannot reference Infrastructure.
Application.Tests cannot reference API or Infrastructure.
Infrastructure.Tests can use real dependencies.
API.Tests can boot the HTTP pipeline.
Integration.Tests can compose the runtime.
```

This means wrong tests fail at compile time, not during review.

---

## 2. Core Principle

```txt
A test project is allowed to reference only what that test level is supposed to know.
```

Tests should not become a backdoor that bypasses Clean Architecture.

---

## 3. Target Repository Layout

Use a dedicated `tests/` root to separate production code from test code.

```txt
backend/
├── src/
│   ├── Notrelix.Domain/
│   ├── Notrelix.Application/
│   ├── Notrelix.Infrastructure/
│   └── Notrelix.API/
│
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
    │
    ├── Notrelix.Testing.Core/
    ├── Notrelix.Testing.Domain/
    ├── Notrelix.Testing.Application/
    └── Notrelix.Testing.Integration/
```

If the frontend is not mature, `Notrelix.E2E.Tests` can be documented but not created yet. Do not create empty projects.

---

## 4. Test Support Libraries

A single `Notrelix.Testing` project is convenient, but it can become a hidden dependency leak.

For the final enterprise target, split the test kit by dependency tier.

### 4.1 `Notrelix.Testing.Core`

Purpose: zero-production-dependency shared test utilities.

May contain:

```txt
TestIds
TestClock
SeededRandom
Reflection helpers
Source file scanner
General assertion helpers
Category constants
```

References:

```txt
No production project references.
```

Allowed by:

```txt
All test projects and all other test support projects.
```

### 4.2 `Notrelix.Testing.Domain`

Purpose: domain builders and domain assertions.

References:

```txt
Notrelix.Domain
Notrelix.Testing.Core
```

May contain:

```txt
BoardBuilder
BoardItemBuilder
WorkspaceBuilder
DomainEventAssertions
ValueObjectAssertions
```

Used by:

```txt
Domain.Tests
Application.Tests
Infrastructure.Tests when seeding aggregates is necessary
Integration.Tests
```

### 4.3 `Notrelix.Testing.Application`

Purpose: application fakes and behavior test helpers.

References:

```txt
Notrelix.Application
Notrelix.Domain
Notrelix.Testing.Core
Notrelix.Testing.Domain
```

May contain:

```txt
FakeCurrentUser
FakeCurrentWorkspace
FakePermissionEvaluator
FakeEntitlementService
FakeIdempotencyStore
FakeUnitOfWork
PipelineBehaviorHarness
```

Used by:

```txt
Application.Tests
Integration.Tests when needed
```

### 4.4 `Notrelix.Testing.Integration`

Purpose: real runtime fixtures.

References:

```txt
Notrelix.Infrastructure
Notrelix.API when API factory is needed
Notrelix.Testing.Core
Notrelix.Testing.Domain
Notrelix.Testing.Application
```

May contain:

```txt
PostgresFixture
RedisFixture
ApiFactory
AuthenticatedClientFactory
DatabaseResetter
TestWorkspaceFactory
OutboxTestHarness
```

Used by:

```txt
Infrastructure.Tests
API.Tests
Integration.Tests
Contract.Tests when contract generation needs runtime host
Security.Tests when HTTP/runtime is needed
Performance.Tests
```

---

## 5. Project Responsibility Matrix

| Project | Purpose | Real DB? | Docker? | CI lane | Main protection |
|---|---|---:|---:|---|---|
| `Architecture.Tests` | Dependency, naming, folder and forbidden-reference rules | No | No | Fast PR | Architecture boundaries |
| `Domain.Tests` | Pure domain rules | No | No | Fast PR | Invariants and domain events |
| `Application.Tests` | CQRS handlers and pipeline behaviors | No | No | Fast PR | Use case orchestration |
| `Infrastructure.Tests` | EF, PostgreSQL, outbox, stores, providers | Yes | Yes | Integration | Technical correctness |
| `API.Tests` | HTTP pipeline, middleware, ProblemDetails, endpoint smoke | Optional | Optional | Fast/Integration | API contract behavior |
| `Integration.Tests` | Cross-layer runtime scenarios | Yes | Yes | Integration/Nightly | System reliability |
| `Contract.Tests` | OpenAPI, ProblemDetails, webhooks, public API contracts | Optional | Optional | Nightly/Pre-release | External compatibility |
| `Security.Tests` | Tenant isolation, authz, token/security-sensitive behavior | Mixed | Mixed | Fast/Integration/Nightly | Security guarantees |
| `Performance.Tests` | Performance smoke only | Yes | Yes | Nightly/Pre-release | Scaling regressions |
| `E2E.Tests` | Few critical product journeys | Yes | Yes | Nightly/Pre-release | Product confidence |

---

## 6. Dependency Rules

### 6.1 Production dependency direction

```txt
Domain -> no project dependency
Application -> Domain
Infrastructure -> Application + Domain
API -> Application + Infrastructure composition
```

### 6.2 Test dependency direction

```txt
Architecture.Tests -> production assemblies or source scanner only
Domain.Tests -> Domain + Testing.Core + Testing.Domain
Application.Tests -> Application + Domain + Testing.Core + Testing.Domain + Testing.Application
Infrastructure.Tests -> Infrastructure + Application + Domain + Testing.* as needed
API.Tests -> API + Application + Domain + Testing.Integration, Infrastructure only for host composition
Integration.Tests -> API + Infrastructure + Application + Domain + Testing.Integration
Contract.Tests -> API or generated artifacts + Testing.Integration if runtime generation is needed
Security.Tests -> target layer depending on test type
Performance.Tests -> API/Infrastructure runtime + Testing.Integration
E2E.Tests -> external browser/API client fixtures only
```

### 6.3 Forbidden shortcuts

```txt
Domain.Tests must not reference Infrastructure or API.
Application.Tests must not reference Infrastructure or API.
Application.Tests must not use concrete DbContext.
Domain/Application tests must not start Docker.
API endpoint tests must not call handlers directly.
Infrastructure tests must not use EF InMemory for PostgreSQL-specific behavior.
```

---

## 7. Tooling Standard

Use one clear stack:

```txt
Test framework:        xUnit
Assertions:            FluentAssertions
Mocking:               NSubstitute
Architecture tests:    Custom reflection/source scanning first; NetArchTest optional later
API tests:             WebApplicationFactory / TestServer
Database integration:  Testcontainers PostgreSQL
Cache integration:     Testcontainers Redis only when Redis behavior matters
DB reset:              Respawn-like reset or recreate schema/database per collection
Coverage:              coverlet
Snapshot testing:      Optional, only for stable OpenAPI/ProblemDetails/webhook contracts
E2E browser:           Playwright only when frontend flows mature
```

Do not mix Moq and NSubstitute in new tests. Pick NSubstitute for Notrelix unless an existing area already uses another library and migration is out of scope.

---

## 8. Test Pyramid

Recommended enterprise ratio:

```txt
Architecture tests:        10–15%
Domain unit tests:         25–35%
Application tests:         25–30%
Infrastructure/API tests:  15–25%
Integration/Contract/Sec:  10–15%
E2E smoke tests:            3–7%
Performance smoke:          small, nightly only
```

Do not invert the pyramid by relying mostly on E2E tests.

---

## 9. `Notrelix.Architecture.Tests`

### Purpose

Lock boundaries before runtime.

### References

May reference production assemblies for reflection-based checks, or scan source files.

### Required tests

```txt
Domain_ShouldNotReference_Application_Infrastructure_API
Domain_ShouldNotReference_EFCore_AspNetCore_MediatR
Application_ShouldNotReference_Infrastructure_API
Application_ShouldNotUse_AspNetCoreHttpTypes
Infrastructure_ShouldNotReference_API
API_Endpoints_ShouldNotInject_DbContext
API_Endpoints_ShouldNotReturn_DomainOrEfEntities
HandlersForTransactionalRequests_ShouldNotCall_SaveChangesAsync
IntegrationEvents_ShouldHave_StableMessageNameAndSchemaVersion
ProcessedEvents_ShouldUse_EventIdAndConsumerName
```

### Rule

Architecture tests must run in every PR fast lane.

---

## 10. `Notrelix.Domain.Tests`

### Purpose

Protect business truth.

### Allowed

```txt
Pure domain objects
Value objects
Aggregates
Domain events
TestClock/TestIds
Domain builders
```

### Forbidden

```txt
Database
EF Core
MediatR
ASP.NET Core
Infrastructure mocks
Testcontainers
HTTP concepts
```

### Folder pattern

```txt
Notrelix.Domain.Tests/
  WorkManagement/
    Boards/
      BoardCreationTests.cs
      BoardLifecycleTests.cs
      BoardInvariantTests.cs
      BoardDomainEventTests.cs
      BoardSoftDeleteTests.cs
  Governance/
  Billing/
  Collaboration/
  Automation/
  SharedKernel/
```

### Required assertion rule

Every important domain mutation should test:

```txt
state change
version increment when state changes
domain event emitted when required
invalid state rejected
no-op behavior explicit
```

---

## 11. `Notrelix.Application.Tests`

### Purpose

Protect use cases and pipeline behavior without real infrastructure.

### Allowed

```txt
Command/query handlers
Pipeline behaviors
Application abstractions
Fakes/test doubles
Domain aggregates where needed
```

### Forbidden

```txt
Concrete Infrastructure implementations
Real DbContext
WebApplicationFactory
Testcontainers
HTTP endpoint calls
External providers
```

### Required P0 behavior tests

```txt
ValidationBehaviorTests
WorkspaceContextBehaviorTests
AuthorizationBehaviorTests
TransactionBehaviorTests
IdempotencyBehaviorTests
EntitlementBehaviorTests
ConcurrencyBehaviorTests
CacheInvalidationBehaviorTests
ExceptionMappingBehaviorTests
RealtimeBehaviorTests when realtime pipeline exists
```

### Transaction rule

```txt
If request implements ITransactionalRequest, handler must not call SaveChangesAsync.
TransactionBehavior owns commit.
Never allow both handler SaveChanges and TransactionBehavior SaveChanges for the same request.
```

---

## 12. `Notrelix.Infrastructure.Tests`

### Purpose

Verify real technical behavior.

### Must use real PostgreSQL for

```txt
Migrations
Schema names
Foreign keys and unique constraints
JSONB mapping
Concurrency tokens
Raw SQL stores
Outbox claim queries
FOR UPDATE SKIP LOCKED
Processed event uniqueness
Workspace filters
Soft-delete filters
RLS/session-variable behavior if used
```

### Required P0 tests

```txt
PostgresMigrationSmokeTests
EfMappingSmokeTests
WorkspaceFilterFailClosedTests
OutboxAtomicityTests
OutboxDispatcherClaimTests
OutboxRetryTests
OutboxDeadLetterTests
ProcessedEventStoreTests
IdempotencyStoreTests
JobLockManagerTests
SearchProjectionStoreTests when implemented
PermissionCacheStoreTests when implemented
```

### Rule

EF InMemory is not acceptable for PostgreSQL-specific behavior.

---

## 13. `Notrelix.API.Tests`

### Purpose

Verify HTTP pipeline and endpoint contracts.

### Must use

```txt
WebApplicationFactory / TestServer
HTTP requests through the real pipeline
```

### Required P0 tests

```txt
ProblemDetailsContractTests
AuthenticationFlowTests
WorkspaceRoutingTests
BoardEndpointSmokeTests
OpenApiContractSmokeTests
IdempotencyHeaderTests when API supports idempotency
ConcurrencyHeaderTests when API supports expected version / If-Match
```

### API tests must not

```txt
Call handlers directly
Assert handler internals
Return/compare Domain entities
Return/compare EF entities
Use real external providers
```

---

## 14. `Notrelix.Integration.Tests`

### Purpose

Verify cross-layer behavior that no single layer can prove.

### Examples

```txt
Command -> Domain mutation -> EF SaveChanges -> Outbox message persisted
Outbox dispatcher -> Integration event bus fake -> ProcessedEvent recorded
Create board via API -> persisted in DB -> workspace isolation holds
Permission change -> cache invalidation -> permission evaluator result changes
Notification created -> unread counter updated -> API badge result changes
Search indexing job -> search document updated
```

### Rule

Integration tests must be few, high-value and focused on reliability boundaries.

---

## 15. `Notrelix.Contract.Tests`

### Purpose

Protect compatibility with frontend, external integrations and public API consumers.

### Test areas

```txt
OpenAPI generation
ProblemDetails contract
Webhook request/response contracts
Public API DTO contract
Event contract schema if exposed
```

### Snapshot rule

Only snapshot stable contracts. Normalize ordering. Review diffs intentionally.

---

## 16. `Notrelix.Security.Tests`

### Purpose

Security-sensitive tests deserve explicit visibility.

### Required areas

```txt
Tenant isolation
Authorization fail-closed behavior
Webhook signature validation
Webhook replay protection
Token hashing / refresh token storage
API token never stored raw
ProblemDetails does not leak secrets, SQL or stack traces
Rate limiting behavior when implemented
```

### Placement rule

If the test is pure algorithmic, keep it fast. If it verifies HTTP/runtime/DB isolation, use integration fixtures.

---

## 17. `Notrelix.Performance.Tests`

### Purpose

Performance smoke, not full load testing.

### Run only in

```txt
Nightly
Pre-release
Manual performance gate
```

### Required smoke areas

```txt
Board item listing with cursor pagination
Board schema read
Search query
Permission decision/effective permission
Outbox dispatcher batch claim
Reporting dashboard query
```

### Assertion style

Use broad thresholds and seeded datasets. Do not make performance tests flaky.

---

## 18. `Notrelix.E2E.Tests`

### Purpose

Protect only critical journeys.

### Create only when frontend/runtime is mature

Do not create E2E project early if it will contain placeholder tests.

### Critical journeys

```txt
Register/Login -> Create workspace -> Create board -> Create item -> Update field
Invite member -> Accept invitation -> Member sees workspace
Permission denied -> User cannot access private board
Create item -> Outbox event persisted -> projection simulated
Billing entitlement enforced -> quota exceeded blocked
Automation trigger -> execution recorded
Inbound webhook -> signature verified -> idempotency enforced
```

---

## 19. CI/CD Lanes

### 19.1 PR Fast Lane

Runs on every PR:

```txt
dotnet restore
dotnet build
Architecture.Tests
Domain.Tests
Application.Tests
API architecture/contract-light tests that do not require Docker
```

Target: 3–5 minutes.

### 19.2 PR Integration Lane

Runs when Infrastructure/API paths change, or when label is applied:

```txt
Infrastructure.Tests Category=Integration
API.Tests Category=Api or Integration
Integration.Tests P0 scenarios
PostgreSQL Testcontainers
Redis Testcontainers only when needed
```

Target: 10–15 minutes.

### 19.3 Nightly Full Lane

```txt
All tests
Migration smoke
Outbox retry/dead-letter
Webhook tests
Security smoke
OpenAPI contract snapshots
Performance smoke
Large seed profile
```

### 19.4 Pre-Release Lane

```txt
All tests
DB migration apply/rollback smoke
Production-like seed
Critical E2E smoke journeys
Billing/webhook simulation
Search/reporting performance smoke
```

---

## 20. Test Category Standard

Use traits consistently:

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

Fast PR excludes:

```txt
Integration
Performance
E2E
Slow
```

---

## 21. Testcontainers Strategy

Do not start one PostgreSQL container per test.

Recommended:

```txt
One PostgreSQL container per test assembly or collection.
Apply migrations once per fixture startup.
Reset database between tests.
Use collection fixtures for shared DB tests.
Disable parallelization only for shared DB collections.
```

Redis container is used only when Redis behavior matters.

---

## 22. Database Reset Strategy

Preferred:

```txt
Respawn-like reset between tests.
Keep migration metadata if needed.
Reset tables in dependency-safe order.
```

Alternative:

```txt
Fresh schema/database per test class when isolation matters more than speed.
```

Do not rely on mutable global seed data.

---

## 23. Tenant Isolation Standard

Every tenant isolation test must create at least:

```txt
Workspace A
Workspace B
User A member of Workspace A
User B member of Workspace B
Resource A in Workspace A
Resource B in Workspace B
```

Single-workspace tests cannot prove tenant isolation.

Required tests:

```txt
WorkspaceScopedQuery_ShouldReturnOnlyCurrentWorkspaceData
WorkspaceScopedCommand_ShouldRejectOtherWorkspaceResource
WorkspaceContextBehavior_WhenWorkspaceMissing_ShouldFailClosed
DbContext_WhenWorkspaceContextMissing_ShouldNotReturnAllTenants
PrivateResourceWithoutPermission_ShouldReturn403OrPolicyDefined404
```

---

## 24. Outbox and Event Reliability Standard

Outbox is P0.

Required tests:

```txt
SaveAggregate_ShouldPersistIntegrationEvent_InSameTransaction
SaveAggregate_WhenSaveFails_ShouldNotPersistOutboxMessage
DomainEventInterceptor_ShouldNotPersistAllDomainEventsByDefault
DomainEventInterceptor_ShouldMapDomainEventToIntegrationEventBeforeCommit
OutboxDispatcher_ShouldClaimPendingMessages
OutboxDispatcher_ShouldClaimFailedMessagesWhenNextAttemptDue
OutboxDispatcher_ShouldRecoverStuckProcessingMessages
OutboxDispatcher_WhenPublishFails_ShouldScheduleRetry
OutboxDispatcher_WhenMaxRetriesExceeded_ShouldDeadLetter
OutboxDispatcher_WhenMessageNameUnknown_ShouldDeadLetterOrFail_NotMarkProcessed
ProcessedEventStore_ShouldAllowSameEventForDifferentConsumers
ProcessedEventStore_ShouldRejectSameEventForSameConsumer
```

Consumer idempotency key:

```txt
(event_id, consumer_name)
```

Never only:

```txt
event_id
```

---

## 25. API ProblemDetails Contract

Every error response should include:

```txt
type
title
status
detail or safe empty detail
instance
errorCode
traceId
correlationId when available
workspaceId when available
errors for validation failures
```

Never leak:

```txt
stack traces
SQL
connection strings
secrets
tokens
provider raw errors that expose sensitive data
```

---

## 26. Naming Convention

Use behavior-oriented names:

```txt
MethodName_StateUnderTest_ExpectedBehavior
```

Examples:

```txt
Rename_WhenNameChanges_UpdatesNameIncrementsVersionAndRaisesEvent
Handle_WhenRequestIsTransactional_CallsSaveChangesOnce
CreateBoard_WhenUserHasPermission_Returns201AndLocationHeader
OutboxDispatcher_WhenPublishFails_SchedulesRetry
WorkspaceFilter_WhenContextMissing_FailsClosed
```

Forbidden:

```txt
Test1
ShouldWork
CreateBoardTest
PlaceholderTests
```

---

## 27. Folder Rules

Do not create empty folders or empty projects.

A folder may exist only when it contains at least one meaningful test or support file.

A project may exist only when it has a clear CI lane and at least one meaningful test, except documented future projects not yet created.

---

## 28. Package Rules

Do not add overlapping libraries without ADR.

Forbidden without explicit decision:

```txt
Mixing Moq and NSubstitute in new code
Adding snapshot library for unstable contracts
Adding full load-test framework to unit/integration projects
Adding Playwright before frontend E2E journeys are ready
```

---

## 29. P0 Refactor Roadmap

### Phase 0 — Establish solution structure

```txt
Create tests/ root.
Create target test projects that will contain real tests now.
Create Testing.Core/Domain/Application/Integration only as needed by actual tests.
Wire project references according to dependency rules.
Remove placeholder tests.
Run dotnet test.
```

### Phase 1 — Architecture guardrails

```txt
Architecture.Tests with layer dependency rules.
Domain architecture rules.
Application architecture rules.
API endpoint architecture rules.
Transactional handler SaveChanges rule.
```

### Phase 2 — Domain and Application P0

```txt
Domain tests for WorkManagement core aggregates.
Application behavior tests for Transaction, Workspace, Authorization, Idempotency, Entitlement.
```

### Phase 3 — Infrastructure P0

```txt
Postgres Testcontainers fixture.
Migration smoke.
Workspace filter fail-closed.
Outbox atomicity.
Processed event idempotency.
Idempotency store.
Job lock manager.
```

### Phase 4 — API P0

```txt
ProblemDetails contract.
Auth flow.
Workspace routing.
Board endpoint smoke.
OpenAPI smoke.
```

### Phase 5 — Enterprise maturity

```txt
Integration reliability tests.
Contract snapshots.
Security smoke.
Performance smoke.
Critical E2E journeys.
Nightly/pre-release CI lanes.
```

---

## 30. Definition of Done by Change Type

### Domain change

```txt
Domain invariant tests added.
Domain event tests added if event emitted.
Version/no-op behavior tested.
No Infrastructure/API dependency introduced.
```

### Application change

```txt
Handler/use case tests added.
Required markers verified.
Pipeline behavior impact tested.
No SaveChanges in ITransactionalRequest handlers.
Permission/workspace/entitlement failure path tested.
```

### Infrastructure change

```txt
Real dependency test added if behavior depends on PostgreSQL/Redis/provider semantics.
Retry/idempotency tested when relevant.
Failure path tested.
No secret leak.
```

### API change

```txt
HTTP happy path tested.
ProblemDetails tested for relevant error.
Auth/workspace/permission tested.
Response contract tested.
OpenAPI updated when public/frontend-visible.
No Domain/EF entity returned.
```

### Outbox/consumer change

```txt
Atomic persistence tested.
Dispatcher claim tested.
Retry/dead-letter tested.
Consumer idempotency tested with EventId + ConsumerName.
Unknown message behavior tested.
```

---

## 31. Agent Guardrails

When an agent modifies tests:

```txt
Do not add placeholder tests.
Do not assert true only.
Do not disable tests without explicit reason.
Do not create empty projects or folders.
Do not use EF InMemory for PostgreSQL-specific behavior.
Do not start Docker in Domain/Application tests.
Do not call real external providers.
Do not depend on test order.
Do not introduce random packages without ADR.
Do not mix mocking libraries in new areas.
Do not test private implementation details unless it is an architecture guardrail.
```

When adding a feature:

```txt
Domain behavior change -> Domain.Tests
Application use case/pipeline change -> Application.Tests
Infrastructure persistence/outbox/provider change -> Infrastructure.Tests
API route/contract/error change -> API.Tests and possibly Contract.Tests
Cross-layer behavior -> Integration.Tests
Security-sensitive behavior -> Security.Tests
Performance-sensitive hot path -> Performance.Tests smoke when mature
Critical product journey -> E2E.Tests only if truly critical
```

---

## 32. Final Standard

The final Notrelix test architecture is:

```txt
Architecture.Tests lock the boundaries.
Domain.Tests lock the business model.
Application.Tests lock use cases and pipelines.
Infrastructure.Tests lock PostgreSQL, EF, outbox and technical stores.
API.Tests lock HTTP behavior and contracts.
Integration.Tests lock cross-layer reliability.
Contract.Tests lock external compatibility.
Security.Tests lock tenant isolation and sensitive guarantees.
Performance.Tests catch obvious scale regressions.
E2E.Tests protect only critical product journeys.
Testing.* libraries provide shared support without leaking dependencies.
CI lanes decide when each confidence level runs.
```

A test belongs in the suite only if it protects a real rule, behavior, contract, reliability guarantee, security invariant or critical product journey.

