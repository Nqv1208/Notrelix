# Notrelix Backend — Enterprise Hardening Roadmap

**Repository:** `Nqv1208/Notrelix`  
**Branch reviewed:** `develop`  
**Scope:** Backend only — `backend/src/Notrelix.API`, `Notrelix.Application`, `Notrelix.Domain`, `Notrelix.Infrastructure`  
**Review posture:** Tech lead / enterprise system design  
**Date:** 2026-06-27

---

## 0. Executive Summary

Notrelix backend is already moving in the correct architectural direction for an enterprise SaaS modular monolith: Clean Architecture, DDD-inspired Domain, CQRS/MediatR Application layer, EF Core persistence, workspace-scoped multi-tenancy, permission system, outbox, caching, realtime, and operational middleware.

However, the system must now shift from **architecture-first** to **architecture-enforced**. The main risk is not that the architecture is wrong; the main risk is that the architecture is currently dependent on developer discipline. Enterprise-grade systems cannot rely on memory, convention, or manual review alone. They need guardrails.

The target of this roadmap is not “make it run.” The target is:

> Make Notrelix backend hard to misuse, hard to bypass, observable, testable, and safe for long-term modular growth.

The highest-priority work is therefore:

1. Enforce Application-layer markers with architecture tests.
2. Prove MediatR pipeline ordering and transactional semantics.
3. Make workspace isolation and permission checks non-bypassable.
4. Formalize Domain event strategy: local domain event, durable domain event, integration event.
5. Reduce accidental coupling around `ApplicationDbContext`.
6. Convert outbox, idempotency, cache invalidation, and realtime into proven, test-backed operational flows.
7. Establish CI gates that prevent future architectural drift.

---

## 1. Current Backend Assessment

### 1.1 What is already good

The backend already has several strong foundations:

- Clear project split: `API`, `Application`, `Domain`, `Infrastructure`.
- Domain is separated into business areas such as Identity, Workspaces, WorkManagement, Documents, Collaboration, Governance, Billing, Integrations, Automation, Analytics.
- Application layer uses CQRS-style request/handler separation and pipeline behaviors.
- Cross-cutting concerns exist as reusable behaviors: validation, workspace context, authorization, idempotency, entitlement, cache, transaction, cache invalidation, realtime, exception mapping.
- `AggregateRoot` includes optimistic concurrency versioning.
- `DomainEvent` has metadata useful for tracing and event-driven flows: `EventId`, `OccurredAt`, `WorkspaceId`, `ActorUserId`, `CorrelationId`, `CausationId`, `EventVersion`.
- Infrastructure contains outbox concepts with message status, retry count, dead-letter state, backoff, and batch defaults.
- API pipeline includes production-aware middleware such as exception handling, forwarded headers, CORS, authentication, workspace resolution, authorization, endpoint mapping, HSTS outside development, and optional HTTPS redirection.

### 1.2 Core risk

The system currently has many enterprise concepts, but the enforcement layer must be strengthened.

The most important risks are:

- A mutating command can forget `ITransactionalRequest`.
- A workspace-scoped request can forget `IWorkspaceRequest`.
- A protected operation can forget `IRequirePermission`.
- A handler can call `SaveChangesAsync` directly and bypass transaction policy.
- Cache invalidation or realtime publishing can happen before durable commit if pipeline order is wrong.
- Idempotency can cache a successful result before transaction commit is guaranteed.
- A large `ApplicationDbContext` can slowly erode bounded-context boundaries.
- Domain events can become inconsistent if every mutation does not increment version or emit the correct event.
- Outbox can exist structurally but still be semantically unsafe without tests for crash, retry, duplicate delivery, and dead-letter flows.

### 1.3 Tech-lead conclusion

Do **not** rewrite the backend.

Do **not** expand more modules before hardening the current architecture.

The right move is to freeze broad feature expansion temporarily and create an **enterprise hardening track** focused on correctness, boundaries, test gates, operational semantics, and security invariants.

---

## 2. Target Architecture Principles

### Principle 1 — Architecture must be enforceable

Every important convention must become a test, analyzer, CI gate, or reusable base abstraction.

Bad:

```text
Developers should remember to add ITransactionalRequest.
```

Good:

```text
CI fails if a mutating command does not implement ITransactionalRequest.
```

### Principle 2 — Application owns use cases, not infrastructure details

Handlers should orchestrate use cases. They should not directly own transaction lifetime, HTTP concerns, cache invalidation, realtime publishing, or outbox dispatch.

### Principle 3 — Domain owns invariants

Business state changes must happen through aggregate methods, not scattered handler assignments.

Bad:

```csharp
board.Name = request.Name;
```

Good:

```csharp
board.Rename(request.Name, actorUserId, now);
```

### Principle 4 — Multi-tenancy must be defensive

Workspace isolation must be enforced at multiple levels:

1. API route/context level.
2. Application marker level.
3. Permission evaluation level.
4. EF query-filter level.
5. Database/RLS level where appropriate.
6. Tests covering cross-workspace access.

### Principle 5 — Side effects must follow commit

External or observable side effects must not run before the database transaction is durable.

Examples:

- Cache invalidation.
- Realtime publish.
- Integration event publish.
- Email sending.
- Webhook dispatch.
- Background job trigger.

### Principle 6 — Modular monolith first, distributed system later

Notrelix should remain a modular monolith until module boundaries are proven. Do not split services early. Instead, create clean internal seams:

- Module-owned Application contracts.
- Module-owned Domain aggregates.
- Module-owned EF configuration.
- Module-owned integration events.
- Module-owned tests.

---

## 3. Roadmap Overview

| Phase | Goal | Priority | Outcome |
|---|---|---:|---|
| Phase 0 | Baseline freeze and quality gates | P0 | Stable branch, measurable hardening backlog |
| Phase 1 | Application architecture enforcement | P0 | Marker rules, handler rules, pipeline tests |
| Phase 2 | Transaction, idempotency, side-effect semantics | P0 | Safe commit lifecycle |
| Phase 3 | Workspace security and permission hardening | P0 | Non-bypassable tenant isolation |
| Phase 4 | Domain consistency and event policy | P1 | Consistent aggregate mutation model |
| Phase 5 | Persistence and data boundary hardening | P1 | Controlled DbContext usage, migration discipline |
| Phase 6 | Outbox and messaging reliability | P1 | Retry, idempotent consumers, dead-letter handling |
| Phase 7 | API contract and endpoint governance | P1 | Stable external contracts |
| Phase 8 | Observability and operations | P1 | Production debugging and operational confidence |
| Phase 9 | Performance and scale testing | P2 | Hot-path confidence |
| Phase 10 | Feature development resumes under guardrails | P2 | Sustainable growth |

---

# Phase 0 — Baseline Freeze and Quality Gates

## Objective

Create a stable technical baseline before adding or expanding features.

This phase is not about fixing random build issues. It is about creating a repeatable engineering foundation.

## Current problem

The backend has many enterprise patterns, but without strong quality gates, every future feature can accidentally weaken architecture.

## Target state

Every PR must answer:

- Does it compile?
- Do all tests pass?
- Does it preserve Clean Architecture dependencies?
- Does it preserve Application request marker rules?
- Does it preserve workspace security?
- Does it preserve transaction and outbox semantics?
- Does it introduce migrations intentionally?
- Does it avoid direct infrastructure leakage into Domain/Application?

## Implementation plan

### 0.1 Create backend hardening branch

Recommended branch:

```bash
git checkout develop
git pull
git checkout -b hardening/backend-enterprise-foundation
```

This branch should not include new business features.

### 0.2 Define backend CI stages

CI should run these stages in order:

```text
1. Restore
2. Build
3. Format / analyzer
4. Unit tests
5. Architecture tests
6. Integration tests
7. Migration validation
8. Docker build
```

### 0.3 Add architecture test project if missing

Recommended project:

```text
backend/tests/Notrelix.ArchitectureTests
```

Recommended packages:

```text
NetArchTest.Rules
FluentAssertions
xUnit
```

### 0.4 Add hardening decision records

Create:

```text
docs/architecture/adr/
```

Start with:

```text
ADR-0001-modular-monolith-boundaries.md
ADR-0002-application-pipeline-order.md
ADR-0003-domain-event-classification.md
ADR-0004-transaction-and-side-effect-policy.md
ADR-0005-workspace-security-model.md
ADR-0006-dbcontext-boundary-policy.md
```

Each ADR should contain:

```text
Context
Decision
Consequences
Rejected alternatives
Migration plan
```

## Definition of Done

- CI exists for backend.
- Backend cannot merge if build/test/architecture tests fail.
- Architecture decisions are written before implementation changes.
- New feature work is blocked until P0 hardening has passed.

---

# Phase 1 — Application Architecture Enforcement

## Objective

Make Application-layer rules enforceable. The goal is to prevent use cases from bypassing transaction, workspace, permission, validation, idempotency, and cache policies.

## Current risk

The Application layer already uses marker interfaces such as:

```text
ICommand
IQuery
IWorkspaceRequest
IRequirePermission
ITransactionalRequest
IIdempotentRequest
IRequireEntitlement
ICacheableQuery
IInvalidateCacheRequest
IRealtimeRequest
```

This is good, but marker interfaces are only safe when enforced.

## Target state

Every request type has an explicit policy. No request should be ambiguous.

Each request must be classified as one of:

```text
Command, Query
Workspace-scoped, Global/system-scoped
Public, Authenticated, Permission-protected
Transactional, Non-transactional
Idempotent, Non-idempotent
Cacheable, Non-cacheable
Realtime-producing, Silent
Cache-invalidating, Read-only
```

## Implementation plan

### 1.1 Define Application request taxonomy

Create document:

```text
docs/backend/application-request-taxonomy.md
```

Recommended taxonomy:

| Request type | Required markers |
|---|---|
| Public read query | `IQuery<TResponse>` only, plus explicit public naming or attribute |
| Authenticated global query | `IQuery<TResponse>`, `IRequireAuthenticatedUser` |
| Workspace read query | `IQuery<TResponse>`, `IWorkspaceRequest`, `IRequirePermission` or explicit public workspace marker |
| Workspace mutating command | `ICommand<TResponse>`, `IWorkspaceRequest`, `IRequirePermission`, `ITransactionalRequest` |
| Idempotent external command | Above + `IIdempotentRequest` |
| Entitlement-gated command | Above + `IRequireEntitlement` |
| Cacheable query | Query markers + `ICacheableQuery<TResponse>` |
| Cache-invalidating command | Command markers + `IInvalidateCacheRequest` |
| Realtime command | Command markers + `IRealtimeRequest` |

### 1.2 Add architecture tests for request markers

Examples of required tests:

```text
All Commands must implement ICommand<TResponse>.
All Queries must implement IQuery<TResponse>.
All workspace namespace requests must implement IWorkspaceRequest unless explicitly exempt.
All mutating Commands must implement ITransactionalRequest unless explicitly exempt.
All workspace mutating Commands must implement IRequirePermission unless explicitly exempt.
All IIdempotentRequest types must also be transactional.
All IRealtimeRequest mutating commands must also be transactional.
All IInvalidateCacheRequest commands must also be transactional.
```

Recommended exemption pattern:

```csharp
[ArchitectureExemption(
    Reason = "Global bootstrap command creates first workspace before workspace context exists",
    ApprovedBy = "TechLead",
    ExpiresOn = "2026-09-01")]
```

Do not allow silent exceptions.

### 1.3 Remove direct `SaveChangesAsync` from handlers

Policy:

```text
Handlers do not call SaveChangesAsync directly.
TransactionBehavior owns SaveChangesAsync for transactional commands.
Read-only queries never call SaveChangesAsync.
```

Allowed exceptions:

- Streaming/batch import with controlled unit-of-work boundary.
- Explicit infrastructure maintenance command.
- System bootstrap command, with ADR and architecture exemption.

Architecture test:

```text
No class ending with Handler in Application may call SaveChangesAsync directly.
```

### 1.4 Standardize handler responsibility

Handlers may:

- Load aggregates through Application abstractions.
- Call aggregate methods.
- Compose domain services.
- Return DTO/result.
- Request domain operation through interfaces.

Handlers must not:

- Build SQL manually unless the use case is explicitly a projection/read-model query.
- Publish integration events directly.
- Send email directly.
- Invalidate cache directly.
- Publish realtime directly.
- Start EF transactions directly.
- Access HTTP context directly.

### 1.5 Standardize request foldering

Recommended structure:

```text
Application/
  Features/
    WorkManagement/
      Boards/
        Commands/
          CreateBoard/
            CreateBoardCommand.cs
            CreateBoardCommandHandler.cs
            CreateBoardCommandValidator.cs
            CreateBoardResponse.cs
        Queries/
          GetBoard/
            GetBoardQuery.cs
            GetBoardQueryHandler.cs
            GetBoardResponse.cs
```

Rules:

- One use case per folder.
- Request, handler, validator, response live together.
- Shared DTOs only when they are truly shared.
- Do not create huge module-level `Dtos` folders unless they are stable contracts.

## Definition of Done

- All Application request marker rules are encoded as tests.
- All handler direct `SaveChangesAsync` calls are removed or explicitly exempted.
- Every request has a clear policy classification.
- New request types cannot merge without architecture compliance.

---

# Phase 2 — Transaction, Idempotency, and Side-Effect Semantics

## Objective

Guarantee that state changes, idempotency, cache invalidation, realtime events, and outbox persistence happen in a safe order.

## Current risk

The current `TransactionBehavior` starts a transaction for `ITransactionalRequest`, calls `next()`, calls `SaveChangesAsync`, commits, and returns the response.

This pattern is directionally correct. The enterprise concern is the exact ordering of pipeline behaviors:

```text
Validation
Workspace
Authorization
Cache
Idempotency
Entitlement
CacheInvalidation
Realtime
Transaction
Handler
```

Depending on actual MediatR execution order, a behavior registered before/after transaction may run before commit, after commit, or wrap the wrong side effect.

## Target state

Required ordering:

```text
ExceptionMapping
  Logging
    Validation
      WorkspaceContext
        Authorization
          CacheRead
            IdempotencyLock
              Entitlement
                Transaction
                  Handler
                  DbSaveChanges
                  OutboxPersist
                Commit
              IdempotencyStoreSuccess
              CacheInvalidation
              RealtimePublish
```

The important rule:

> No external or observable side effect should happen before database commit.

## Implementation plan

### 2.1 Write pipeline-order tests

Create tests using fake behaviors that append execution markers.

Expected sequence for a transactional command:

```text
ExceptionMapping.Before
Logging.Before
Validation.Before
Workspace.Before
Authorization.Before
Idempotency.Before
Entitlement.Before
Transaction.Begin
Handler
Transaction.SaveChanges
Transaction.Commit
Entitlement.After
Idempotency.SetResult
CacheInvalidation.AfterCommit
Realtime.AfterCommit
Authorization.After
Workspace.After
Validation.After
Logging.After
ExceptionMapping.After
```

If actual MediatR order does not match, fix registration order or split behaviors.

### 2.2 Introduce explicit after-commit hook model

Recommended abstraction:

```csharp
public interface IAfterCommitAction
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}

public interface IAfterCommitActionQueue
{
    void Enqueue(IAfterCommitAction action);
    Task ExecuteAllAsync(CancellationToken cancellationToken);
}
```

Transaction behavior should:

1. Begin transaction.
2. Execute handler.
3. Save changes.
4. Commit transaction.
5. Execute after-commit actions.

Cache invalidation and realtime should enqueue actions, not execute immediately.

### 2.3 Fix idempotency semantics

Target policy:

```text
Acquire lock before handler.
Do not store success result until transaction commit succeeds.
Release lock on failure.
Store success result after commit.
Return cached result only when status is Completed.
Return conflict only when status is InProgress and not expired.
```

Recommended idempotency states:

```text
InProgress
Completed
Failed
Expired
```

Recommended table/model fields:

```text
key
request_hash
status
response_json
locked_until
created_at
completed_at
failed_at
error
```

Important enterprise rule:

> Same idempotency key with different request hash must be rejected.

### 2.4 Split cache behavior into read and invalidation policies

Recommended:

```text
CacheBehavior<TRequest,TResponse>
- Only for ICacheableQuery
- Executes before handler
- Never wraps commands

CacheInvalidationBehavior<TRequest,TResponse>
- Only for IInvalidateCacheRequest
- Enqueues invalidation after commit
```

### 2.5 Split realtime publish into after-commit action

Realtime events must represent committed state.

Policy:

```text
Realtime publish is never executed inside an open transaction.
Realtime publish failure does not roll back committed business transaction.
Realtime publish failure is logged and optionally retried via outbox if business-critical.
```

## Definition of Done

- Pipeline-order tests exist and pass.
- Idempotency success is stored only after commit.
- Cache invalidation runs only after commit.
- Realtime publish runs only after commit.
- Direct side-effect execution inside handlers is forbidden by architecture tests.

---

# Phase 3 — Workspace Security and Permission Hardening

## Objective

Make multi-tenant isolation non-bypassable.

## Current risk

The backend has `WorkspaceContextBehavior`, `AuthorizationBehavior`, `ICurrentWorkspace`, query filters, and permission evaluation. This is the right direction.

The risk is that security depends on the request implementing the right marker interface.

## Target state

Every workspace-owned resource is protected by layered security:

```text
Route workspace context
Application marker
Permission evaluator
EF query filter
Database constraints / RLS where appropriate
Cross-workspace tests
```

## Implementation plan

### 3.1 Define workspace-scoped resource registry

Create:

```text
Application/Common/Security/WorkspaceResourceRegistry.cs
```

It should map resource types to ownership rules:

```text
Board -> WorkspaceId
BoardItem -> WorkspaceId through Board/Group
Page -> WorkspaceId
Comment -> WorkspaceId through target resource
Attachment -> WorkspaceId through owner resource
Notification -> User-owned, workspace optional
BillingSubscription -> WorkspaceId
```

### 3.2 Make `ResourceRef` mandatory for permission-protected operations

Every `IRequirePermission` should expose:

```csharp
ResourceRef Resource { get; }
PermissionAction Action { get; }
```

Rules:

- `WorkspaceId` must never silently fall back to `Guid.Empty` for workspace operations.
- Global/system operations must have explicit global permission action.
- Resource-not-found and forbidden must be handled carefully to avoid leaking resource existence.

### 3.3 Add architecture tests for workspace namespaces

Rules:

```text
All requests under Features/WorkManagement must be workspace-scoped unless explicitly global.
All requests under Features/Documents must be workspace-scoped unless explicitly global.
All requests under Features/Collaboration must be workspace-scoped or user-scoped.
All requests under Features/Billing for workspace billing must be workspace-scoped.
All workspace commands must require permission.
All workspace queries returning resource data must require permission or be explicitly public/share-link.
```

### 3.4 Add cross-tenant integration tests

Minimum test matrix:

| Flow | Expected result |
|---|---|
| User A from workspace A reads board from workspace B | Forbidden or NotFound |
| User A from workspace A updates board from workspace B | Forbidden or NotFound |
| User A queries document tree of workspace B | Forbidden or NotFound |
| User A comments on item in workspace B | Forbidden or NotFound |
| User with removed membership reuses old workspace header | Forbidden |
| User with workspace member role tries owner-only operation | Forbidden |
| System context background job can process all workspaces safely | Allowed only through explicit system context |

### 3.5 Strengthen EF query filter policy

The current global query filter approach is useful, but dangerous if not tested.

Required tests:

```text
When no workspace is set, workspace-scoped DbSets return no records.
When workspace A is set, workspace B records are filtered.
System context can intentionally bypass workspace filter.
Soft-deleted rows are excluded by default.
IgnoreQueryFilters is not used in Application handlers unless explicitly exempt.
```

### 3.6 Database security policy

For PostgreSQL RLS, choose one of two clear policies:

#### Option A — Application-enforced tenancy first

Use EF filters + Application permission + integration tests. Keep RLS minimal until core stabilizes.

Good for early-stage modular monolith.

#### Option B — Defense-in-depth RLS

Enable RLS only when every tenant table has verified policies.

Do not leave tables with RLS enabled and no policy unless intentionally blocked.

Recommended for later enterprise hardening.

## Recommended decision

Use **Application-enforced tenancy first**, but design schema so RLS can be enabled later cleanly. Avoid partial RLS that creates unpredictable access behavior.

## Definition of Done

- Workspace request rules are architecture-tested.
- Permission-protected operation rules are architecture-tested.
- Cross-tenant integration tests exist for core modules.
- EF query-filter behavior is tested.
- Any `IgnoreQueryFilters` use is reviewed and exempted.

---

# Phase 4 — Domain Consistency and Event Policy

## Objective

Make Domain behavior consistent across aggregates.

## Current risk

The Domain layer is rich and well-structured, but enterprise correctness requires consistency:

- Every mutation should preserve invariants.
- Every mutation should increment aggregate version when state changes.
- Every important state transition should emit a domain event.
- Domain events should be classified by purpose.
- Global events and workspace events should be clearly separated.

## Target state

Every aggregate mutation follows one template:

```text
Validate invariant
Change state
Increment version
Add domain event if meaningful
```

## Implementation plan

### 4.1 Define aggregate mutation policy

Create:

```text
docs/backend/domain-aggregate-mutation-policy.md
```

Policy:

| Mutation type | Increment version | Domain event |
|---|---:|---:|
| Business-visible state change | Yes | Yes |
| Internal recalculation with observable effect | Yes | Usually yes |
| Metadata-only technical update | Maybe | No unless audited |
| Soft delete | Yes | Yes |
| Restore | Yes | Yes |
| Creation | Version starts at 1 | Yes for important aggregate roots |
| Read-only computed property | No | No |

### 4.2 Audit all aggregate methods

Create spreadsheet/checklist:

```text
docs/backend/domain-mutation-audit.md
```

Columns:

```text
Bounded Context
Aggregate
Method
State changed?
Invariant checked?
IncrementVersion?
Domain event?
Event type
WorkspaceId source
ActorUserId source
Test coverage
Decision
```

### 4.3 Standardize domain event classification

Recommended event classes:

```csharp
public abstract record LocalDomainEvent : DomainEvent;
public abstract record DurableDomainEvent : DomainEvent;
public interface IIntegrationEvent;
```

Meaning:

| Type | Purpose | Persistence | Dispatch |
|---|---|---|---|
| LocalDomainEvent | In-process consistency | Not persisted by default | MediatR sync/in-process |
| DurableDomainEvent | Important internal async side effect | Outbox | Internal dispatcher |
| IntegrationEvent | Cross-boundary/external contract | Outbox | Broker/webhook/event bus |

### 4.4 Do not create integration events for every domain event

Only create integration events when:

- Another bounded context needs the event asynchronously.
- External systems/webhooks need it.
- The event is part of audit/compliance workflow.
- The event represents a major lifecycle transition.
- The event is useful for analytics/search/index projection.

Examples:

| Domain event | Integration event? | Reason |
|---|---:|---|
| UserRegistered | Yes | Identity -> workspace provisioning / onboarding |
| WorkspaceCreated | Yes | Analytics, billing, provisioning |
| BoardCreated | Maybe | Search/index/realtime/product analytics |
| BoardRenamed | Maybe | Search/index/realtime if external projection depends on it |
| BoardItemMoved | Usually local/durable | Realtime/search maybe, not necessarily external |
| CommentAdded | Maybe | Notification/realtime/search |
| InvoicePaid | Yes | Billing/integration/compliance |
| PlanLimitChanged | Yes | Entitlement propagation |
| ViewPreferenceChanged | No | Usually local/user preference only |

### 4.5 Add domain tests

Minimum pattern:

```text
Given valid state
When aggregate method is called
Then state changes correctly
And Version increments
And expected DomainEvent is recorded
```

Also test invalid transitions:

```text
Cannot archive already archived board if policy forbids.
Cannot rename board to empty name.
Cannot move item to group from another board.
Cannot restore aggregate if parent workspace is deleted, if such rule exists.
```

## Definition of Done

- Domain mutation policy exists.
- Mutation audit exists.
- Core aggregates are covered by unit tests.
- Event classification is implemented.
- Integration event mapping is explicit and minimal.

---

# Phase 5 — Persistence and Data Boundary Hardening

## Objective

Keep one physical database and one operational EF Core context if necessary, but prevent bounded-context boundary erosion.

## Current risk

`ApplicationDbContext` currently exposes many DbSets from many modules. This is normal in early modular monoliths, but dangerous long-term.

The risk is not the existence of one DbContext. The risk is unrestricted access.

## Target state

A feature handler should only access the data it is allowed to own or read through approved abstractions.

## Implementation plan

### 5.1 Keep one physical DbContext for now

Do not split DbContext prematurely.

Recommended decision:

```text
One physical ApplicationDbContext.
Multiple module-specific Application interfaces.
Module-owned EF configurations.
Strict architecture tests to prevent cross-module leakage.
```

### 5.2 Strengthen module-specific context interfaces

Examples:

```csharp
public interface IWorkManagementDbContext
{
    DbSet<Board> Boards { get; }
    DbSet<BoardItem> BoardItems { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

But do not expose unrelated DbSets.

For cross-module reads, create explicit query services:

```csharp
public interface IWorkspaceMembershipReader
{
    Task<bool> IsMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct);
}
```

### 5.3 Use schema ownership rules

Recommended database schema ownership:

| Schema | Owner |
|---|---|
| identity | Identity module |
| workspace | Workspace module |
| work | WorkManagement module |
| docs | Documents module |
| collab | Collaboration module |
| governance | Governance module |
| billing | Billing module |
| integration | Integrations module |
| automation | Automation module |
| analytics | Analytics module |
| ops | Infrastructure/Operations |
| search | Search projection |

Rule:

```text
Domain-owned business tables belong to module schemas.
Projection tables belong to projection schemas.
Ops tables belong to ops schema.
```

### 5.4 Classify EF-managed vs raw SQL vs external ownership

Recommended classification:

| Table type | Ownership |
|---|---|
| Domain aggregate tables | EF managed |
| Projection tables used by app | EF managed as projection entities |
| Ops tables used by app runtime | EF managed infrastructure entities |
| Pure database optimization structures | Raw SQL migration |
| Future independent service tables | Do not create until service exists |

### 5.5 Migration discipline

Rules:

```text
No manual production DB edits.
No migration that mixes unrelated modules.
No migration without rollback/forward note.
No migration that creates stale duplicate columns.
No migration that enables RLS without matching policy.
No migration that adds polymorphic references without validation strategy.
```

### 5.6 Polymorphic resource reference policy

Polymorphic references like `resource_type + resource_id` are common in enterprise SaaS, but they need validation.

Recommended approach:

```text
Domain/Application validates ResourceRef through ResourceRegistry.
Database stores resource_type/resource_id for flexibility.
Critical references use real FK where lifecycle integrity matters.
Background consistency job detects dangling ResourceRefs.
```

Use real FK for:

- Board -> Workspace.
- BoardItem -> Board.
- Page -> Workspace.
- WorkspaceMember -> Workspace/User.

Use polymorphic ref for:

- Comments on multiple resource types.
- Attachments on multiple resource types.
- Activity logs.
- Audit logs.
- Notifications.

## Definition of Done

- Module-specific context interfaces exist and are used.
- Cross-module direct DbSet access is forbidden or reviewed.
- Migration rules are documented.
- Projection/ops/domain table ownership is clear.
- ResourceRef validation strategy exists.

---

# Phase 6 — Outbox and Messaging Reliability

## Objective

Turn outbox from a structure into a reliable delivery mechanism.

## Current risk

The outbox model has useful fields: event id, source event id, message name, schema version, message type, workspace id, actor user id, payload, status, retry count, next attempt, processing timestamp, processed timestamp, error, dead-letter behavior.

The enterprise risk is semantic:

- Is the outbox written in the same transaction as aggregate changes?
- Are duplicate deliveries safe?
- Does the dispatcher claim messages safely under concurrency?
- Are consumers idempotent?
- Is dead-letter observable?
- Is event versioning stable?

## Target state

Outbox guarantees:

```text
At-least-once delivery.
Idempotent consumers.
Atomic persistence with aggregate transaction.
Safe concurrent dispatch.
Retry with backoff.
Dead-letter after max retries.
Operational visibility.
```

## Implementation plan

### 6.1 Define event persistence policy

Recommended:

```text
Persist IntegrationEvents to outbox.
Persist DurableDomainEvents only when they have async internal consumers.
Do not persist every LocalDomainEvent.
```

### 6.2 Add outbox interceptor or transaction hook

Aggregate domain events should be collected during `SaveChangesAsync` and mapped to outbox messages before commit.

Flow:

```text
Handler changes aggregate
Aggregate records domain event
TransactionBehavior calls SaveChangesAsync
SaveChanges interceptor collects domain events
Mapper creates integration events
Outbox messages inserted
Database commit persists aggregate + outbox atomically
Dispatcher publishes later
```

### 6.3 Add event mapper policy

Recommended:

```csharp
public interface IIntegrationEventMapper
{
    IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent);
}
```

Rules:

- Mapping is explicit.
- One domain event maps to zero or more integration events.
- Mapping does not publish anything.
- Mapping does not call external services.
- Mapping is covered by unit tests.

### 6.4 Implement safe message claiming

Dispatcher claim should be atomic.

PostgreSQL pattern:

```sql
UPDATE ops.outbox_messages
SET status = 'Processing', processing_started_at = now()
WHERE id IN (
    SELECT id
    FROM ops.outbox_messages
    WHERE status IN ('Pending', 'Failed')
      AND next_attempt_at <= now()
    ORDER BY created_at
    FOR UPDATE SKIP LOCKED
    LIMIT @batchSize
)
RETURNING *;
```

Also reclaim stuck processing messages:

```text
Processing where processing_started_at < now - timeout => Failed/Pending retry
```

### 6.5 Consumer idempotency

Each consumer must record processed event:

```text
event_id
consumer_name
processed_at
```

Unique constraint:

```text
UNIQUE(event_id, consumer_name)
```

Consumer flow:

```text
Begin transaction
Try insert processed event key
If duplicate => skip
Handle message
Commit
```

### 6.6 Dead-letter operations

Dead-letter messages need:

- Admin visibility.
- Error reason.
- Retry count.
- Manual retry action.
- Safe replay documentation.

Do not silently ignore dead-letter rows.

### 6.7 Outbox tests

Required tests:

| Test | Expected |
|---|---|
| Aggregate change and outbox insert commit together | Both persisted |
| Handler throws | Neither aggregate change nor outbox persisted |
| Dispatcher publishes success | Message marked processed |
| Dispatcher publish fails | Retry count increments and backoff set |
| Max retries exceeded | Message moved to DeadLetter |
| Two dispatchers claim concurrently | No duplicate claim |
| Consumer receives duplicate event | Processed once |

## Definition of Done

- Outbox persistence is atomic with aggregate commit.
- Dispatcher claim is concurrency-safe.
- Consumers are idempotent.
- Dead-letter is observable.
- Event mapping is explicit and tested.

---

# Phase 7 — API Contract and Endpoint Governance

## Objective

Make external API stable, versioned, and decoupled from Domain internals.

## Current risk

Minimal API can be clean, but without endpoint governance it can become scattered. Enterprise API design needs stable contracts, consistent errors, clear versioning, and module-level ownership.

## Target state

API layer owns HTTP concerns only:

```text
Routing
Request binding
Authentication policy
Response formatting
OpenAPI metadata
Versioning
HTTP status mapping
```

Application owns use case execution.

Domain owns business rules.

Infrastructure owns implementation details.

## Implementation plan

### 7.1 Standard endpoint grouping

Recommended:

```text
API/Endpoints/
  Identity/
  Workspaces/
  WorkManagement/
  Documents/
  Collaboration/
  Billing/
  Governance/
```

Each group should expose:

```csharp
public sealed class BoardsEndpoints : IEndpointGroup
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapPost(...);
        group.MapGet(...);
    }
}
```

### 7.2 Do not expose Domain entities

Rules:

```text
Endpoint request models are API contracts.
Application responses are DTOs/results.
Domain entities never leave API.
EF entities never leave API.
```

### 7.3 Standardize error response

Use one error contract:

```json
{
  "type": "https://notrelix.com/errors/permission-denied",
  "title": "Permission denied",
  "status": 403,
  "detail": "You do not have permission to perform this action.",
  "traceId": "...",
  "correlationId": "...",
  "errors": {}
}
```

Map exceptions consistently:

| Exception | HTTP |
|---|---:|
| ValidationException | 400 |
| UnauthorizedAccessException | 401 |
| ForbiddenException | 403 |
| NotFoundException | 404 |
| ConflictException | 409 |
| ConcurrencyException | 409 |
| RateLimitException | 429 |
| Unhandled exception | 500 |

### 7.4 OpenAPI contract checks

Add CI step:

```text
Generate OpenAPI spec
Compare with previous approved spec
Fail on breaking change unless version is bumped
```

### 7.5 Bootstrap flow contract

For register + workspace provisioning, choose explicit flow.

Recommended enterprise flow:

```text
POST /auth/register
- Creates user
- Emits UserRegistered integration/durable event
- Returns user auth/bootstrap state
- May return provisioningPending = true

POST /me/bootstrap or GET /me/bootstrap
- Returns current user, workspaces, active workspace, permissions, feature flags, onboarding state
```

Do not overload register with all bootstrap responsibilities if provisioning is asynchronous.

## Definition of Done

- API contracts are DTO-based.
- Error response is standardized.
- Endpoint groups are module-owned.
- OpenAPI is generated in CI.
- Breaking API changes are intentional.

---

# Phase 8 — Observability and Operations

## Objective

Make production behavior debuggable.

## Current risk

Architecture can be correct but still hard to operate if failures are invisible.

## Target state

Every important request and background process can be traced by:

```text
correlation_id
causation_id
workspace_id
actor_user_id
request_name
event_id
outbox_message_id
job_id
```

## Implementation plan

### 8.1 Logging policy

Structured logs must include:

```text
CorrelationId
CausationId
WorkspaceId
ActorUserId
RequestName
RequestId
TraceId
```

Do not log:

```text
Password
Access token
Refresh token
API key
Raw secret
Payment card data
Sensitive integration payload
```

### 8.2 Metrics

Minimum metrics:

| Metric | Why |
|---|---|
| HTTP request duration | API latency |
| HTTP error count by status | Reliability |
| DB query duration | Persistence bottleneck |
| Transaction duration | Lock/latency issues |
| Outbox pending count | Delivery lag |
| Outbox dead-letter count | Operational incident |
| Idempotency conflict count | Client retry behavior |
| Permission denied count | Security/product insight |
| Cache hit/miss ratio | Cache effectiveness |
| Realtime publish failure count | Collaboration reliability |
| Background job failure count | Ops visibility |

### 8.3 Health checks

Health checks should distinguish:

```text
liveness: process is alive
readiness: dependencies are ready
startup: app initialized successfully
```

Readiness should include:

- PostgreSQL.
- Redis.
- Outbox dispatcher health.
- Migration state.
- Required configuration validity.

### 8.4 Operational dashboards

Minimum dashboards:

```text
API latency/errors
Database latency/errors
Outbox lag/dead letters
Background job failures
Auth failures
Rate limit hits
Workspace access denials
```

### 8.5 Incident runbooks

Create:

```text
docs/operations/runbooks/
  outbox-dead-letter.md
  redis-down.md
  database-migration-failure.md
  high-api-latency.md
  permission-regression.md
  tenant-isolation-incident.md
```

## Definition of Done

- Structured logging is consistent.
- Core metrics exist.
- Health checks distinguish liveness/readiness.
- Outbox dead-letter has runbook.
- Correlation IDs flow through API, domain events, outbox, and logs.

---

# Phase 9 — Performance and Scale Testing

## Objective

Prove hot paths before feature growth.

## Target hot paths

Priority flows:

```text
Register + login
Workspace bootstrap
Board list
Board detail
Board item list
Board item update
Document tree load
Page/block load
Comment creation
Notification list
Search indexing/query
Permission evaluation
Realtime update
```

## Implementation plan

### 9.1 Add realistic seed profiles

Seed profiles:

| Profile | Purpose |
|---|---|
| Small | Local dev |
| Medium | Integration testing |
| Large | Performance smoke |

Large profile example:

```text
10 workspaces
100 users
50 boards/workspace
1,000 items/workspace
500 pages/workspace
10,000 comments total
100,000 activity/audit rows
```

### 9.2 Add query performance tests

Use integration tests to assert query count or latency budget for hot reads.

Example budgets:

```text
GET /workspaces/{id}/boards <= 200ms p95 on medium profile
GET /boards/{id}/items <= 300ms p95 on medium profile
GET /pages/tree <= 250ms p95 on medium profile
Permission evaluation <= 50ms p95 with cache
```

### 9.3 Add index review checklist

For every new query:

```text
Is there a matching index?
Does query filter by workspace_id?
Does query filter out deleted_at?
Does query order by indexed column?
Does pagination use cursor for large data?
Is Include causing cartesian explosion?
```

### 9.4 Introduce query policy

Rules:

```text
Use AsNoTracking for read-only queries.
Use projection DTOs for list endpoints.
Avoid loading aggregate graphs for list screens.
Use cursor pagination for large collections.
Avoid unbounded queries.
```

## Definition of Done

- Hot path list exists.
- Performance smoke tests exist.
- Large seed profile exists.
- Index checklist is part of PR review.
- Slow query logging is enabled in development/staging.

---

# Phase 10 — Resume Feature Development Under Guardrails

## Objective

Continue building Notrelix features without returning to uncontrolled refactor.

## Target policy

No new feature should bypass the hardened architecture.

Feature PR checklist:

```text
Does the use case belong to the correct bounded context?
Does the request implement required markers?
Does the handler avoid SaveChangesAsync?
Does the aggregate own state changes?
Are permissions explicit?
Are workspace boundaries tested?
Are cache/realtime/outbox side effects after commit?
Are API contracts DTO-based?
Are migrations scoped and reviewed?
Are tests included?
```

## Recommended feature sequencing after hardening

### First core vertical slice

```text
Register
Login
Workspace provisioning
/me/bootstrap
Workspace switch
Board create/list/detail
Board item create/update/move
Realtime board update
```

### Second vertical slice

```text
Documents page/tree/block editing
Comments
Mentions
Notifications
Search indexing
```

### Third vertical slice

```text
Roles/permissions UI support
Share links
Audit/activity logs
Plan/entitlement enforcement
```

### Later

```text
Automation
Integrations
Advanced billing
Analytics dashboards
Reporting
External webhooks
```

---

# Enterprise Definition of Done

A backend change is enterprise-ready only when all of the following are true:

## Architecture

- Correct layer ownership.
- Correct bounded context ownership.
- No Domain dependency on Application/Infrastructure/API.
- No Application dependency on Infrastructure implementation.
- No API leaking Domain entities.

## Application

- Request has correct markers.
- Handler does not call `SaveChangesAsync` directly.
- Handler does not publish external side effects directly.
- Validation exists for external input.
- Authorization policy is explicit.

## Domain

- Aggregate method enforces invariants.
- Version increments for state-changing mutation.
- Domain event emitted where meaningful.
- Invalid transition is tested.

## Persistence

- Query is workspace-filtered where required.
- Query is soft-delete aware.
- Migration is scoped.
- Index exists for expected hot query.
- No accidental cross-module DbSet access.

## Security

- Cross-tenant access is tested.
- Permission denial path is tested.
- Sensitive data is not logged.
- Secrets are not stored in plaintext.

## Reliability

- Transaction behavior is tested.
- Idempotency behavior is tested if applicable.
- Outbox behavior is tested if applicable.
- Side effects happen after commit.
- Retry/dead-letter policy exists for async work.

## Observability

- Logs include correlation/workspace/user context.
- Important failures have metrics.
- Operational failure has runbook when relevant.

---

# Recommended Sprint Plan

## Sprint H0 — Hardening Foundation

Goal:

```text
Create guardrails before modifying business features.
```

Tasks:

- Add `Notrelix.ArchitectureTests`.
- Add dependency direction tests.
- Add Application request marker tests.
- Add handler `SaveChangesAsync` forbidden test.
- Add CI backend pipeline.
- Add ADR folder and first six ADRs.

Exit criteria:

```text
CI fails when architecture rules are broken.
```

## Sprint H1 — Transaction and Pipeline Semantics

Tasks:

- Add MediatR pipeline-order tests.
- Verify/fix behavior registration order.
- Introduce after-commit action queue.
- Move cache invalidation to after-commit.
- Move realtime publish to after-commit.
- Adjust idempotency success storage after commit.

Exit criteria:

```text
Side effects cannot run before durable commit.
```

## Sprint H2 — Workspace Security

Tasks:

- Create workspace resource registry.
- Strengthen `ResourceRef` validation.
- Add cross-tenant integration tests.
- Add EF query filter tests.
- Add architecture tests for workspace feature requests.
- Review `IgnoreQueryFilters` usage.

Exit criteria:

```text
Core cross-workspace access attempts are blocked and tested.
```

## Sprint H3 — Domain Consistency

Tasks:

- Create mutation audit checklist.
- Audit core aggregates: Workspace, Board, BoardItem, Page, Block, Comment, Subscription, Entitlement.
- Ensure state-changing methods increment version.
- Ensure meaningful domain events exist.
- Add domain unit tests for core aggregate transitions.

Exit criteria:

```text
Core aggregate behavior is consistent and test-backed.
```

## Sprint H4 — Outbox Reliability

Tasks:

- Finalize event classification.
- Implement explicit integration event mapper.
- Ensure outbox insert is atomic with aggregate commit.
- Implement safe claim with `FOR UPDATE SKIP LOCKED`.
- Add processed-event idempotency.
- Add dead-letter visibility and runbook.
- Add dispatcher/consumer tests.

Exit criteria:

```text
Outbox provides at-least-once delivery with idempotent consumers.
```

## Sprint H5 — Persistence Boundary

Tasks:

- Review `ApplicationDbContext` exposure.
- Strengthen module-specific DbContext interfaces.
- Add tests preventing cross-module direct DbSet access where inappropriate.
- Finalize projection/ops/domain table ownership policy.
- Add migration validation CI step.

Exit criteria:

```text
One physical DbContext remains, but module access is controlled.
```

## Sprint H6 — API Contract Governance

Tasks:

- Standardize endpoint groups.
- Ensure DTO-only API responses.
- Standardize problem details response.
- Generate OpenAPI in CI.
- Add breaking-change review policy.
- Finalize `/me/bootstrap` contract.

Exit criteria:

```text
API contracts are stable, documented, and version-aware.
```

## Sprint H7 — Observability and Operations

Tasks:

- Standardize structured log fields.
- Add metrics for API, DB, outbox, idempotency, auth, cache, realtime.
- Split health checks into liveness/readiness.
- Add operational dashboards.
- Add runbooks.

Exit criteria:

```text
Production failures are diagnosable without reading source code first.
```

---

# Anti-Patterns to Block Going Forward

## Application anti-patterns

```text
Handler calls SaveChangesAsync directly.
Handler publishes realtime directly.
Handler sends email directly.
Handler writes outbox directly without event mapper policy.
Handler checks permission manually instead of marker/pipeline.
Handler uses HTTP context.
Handler updates aggregate properties directly.
```

## Domain anti-patterns

```text
Public setters for business state.
State changes without invariant checks.
State changes without version increment.
Business decisions in EF configuration.
Domain depends on Infrastructure/Application.
Domain event without workspace/actor clarity.
```

## Infrastructure anti-patterns

```text
Application imports Infrastructure implementation.
One feature directly queries another module's DbSet without approved reader.
Outbox publishes before commit.
Consumer is not idempotent.
Dead-letter rows are ignored.
Secrets stored in plaintext.
```

## API anti-patterns

```text
Endpoint returns EF/Domain entity.
Endpoint contains business rule.
Endpoint builds SQL query.
Endpoint performs permission logic manually.
Endpoint has inconsistent error response.
Endpoint adds breaking contract without version decision.
```

---

# Final Recommendation

The backend direction is good. The next step should not be another broad refactor and should not be random bug fixing.

The correct tech-lead move is:

```text
Freeze feature expansion.
Add architecture enforcement.
Prove transaction/security/outbox semantics.
Harden core vertical slice.
Then resume feature development under CI guardrails.
```

The first target should be a fully hardened vertical slice:

```text
Register -> Workspace provisioning -> /me/bootstrap -> Board create/list/detail -> Board item mutation -> Realtime update
```

Once this slice is secure, transactional, observable, tested, and contract-stable, the rest of Notrelix can scale without repeated architectural rewrites.

---

# Source Paths Reviewed

The plan was based on the public `develop` branch structure and these key backend areas:

```text
backend/src/Notrelix.API/Program.cs
backend/src/Notrelix.Application/DependencyInjection.cs
backend/src/Notrelix.Application/Common/Behaviors/
backend/src/Notrelix.Domain/Common/AggregateRoot.cs
backend/src/Notrelix.Domain/Common/DomainEvent.cs
backend/src/Notrelix.Infrastructure/Data/ApplicationDbContext.cs
backend/src/Notrelix.Infrastructure/Data/Outbox/OutboxMessage.cs
```

