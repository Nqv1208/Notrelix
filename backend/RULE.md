# Notrelix Backend RULE.md — v2 Agent-Safe Edition

> **Scope:** `backend/`  
> **Branch basis:** `develop`  
> **Purpose:** Lock folder structure, naming, layer responsibility, and bounded-context boundaries so human developers and coding agents cannot place files incorrectly.

---

## 0. Correction note from v1

This version fixes the ambiguous/incorrect parts of the earlier rulebook.

The most important correction:

```txt
WRONG / DO NOT USE:
src/Notrelix.Application/{BoundedContext}/{Module}/{UseCase}/...

CORRECT / USE THIS:
src/Notrelix.Application/Features/{BoundedContext}/{Module}/Commands/{UseCase}/...
src/Notrelix.Application/Features/{BoundedContext}/{Module}/Queries/{UseCase}/...
```

The current Application layer is **Feature-based, module-first, CQRS-subfoldered**.

That means:

```txt
Features
 -> BoundedContext
    -> Module
       -> Commands
          -> UseCase
       -> Queries
          -> UseCase
       -> DTOs
       -> Services
       -> ReadModels
       -> Mapping
       -> Permissions
       -> Cache
```

Agents must not invent another structure.

---

## 1. Backend architecture rule

### 1.1 Solution shape

Backend production projects:

```txt
backend/src/Notrelix.Domain
backend/src/Notrelix.Application
backend/src/Notrelix.Infrastructure
backend/src/Notrelix.API
```

Backend test/support projects:

```txt
backend/tests/Notrelix.Architecture.Tests
backend/tests/Notrelix.Domain.Tests
backend/tests/Notrelix.Application.Tests
backend/tests/Notrelix.Infrastructure.Tests
backend/tests/Notrelix.API.Tests
backend/tests/Notrelix.Integration.Tests
backend/tests/Notrelix.Testing.Core
backend/tests/Notrelix.Testing.Domain
backend/tests/Notrelix.Testing.Application
backend/tests/Notrelix.Testing.Integration
```

### 1.2 Dependency direction

```txt
API
 ├── Application
 └── Infrastructure

Infrastructure
 ├── Application
 └── Domain

Application
 └── Domain

Domain
 └── no project reference
```

Rules:

- Domain must not reference Application, Infrastructure, or API.
- Application must not reference Infrastructure or API.
- Infrastructure must not reference API.
- API must not contain business logic.
- Tests may reference the project being tested and test support projects only.
- Architecture tests must enforce these dependencies.

### 1.3 Architecture style

Notrelix backend uses:

```txt
Clean Architecture
+ Modular Monolith
+ Bounded Contexts
+ Pragmatic CQRS
+ MediatR Pipeline Behaviors
+ EF Core infrastructure
+ Outbox for durable side effects
```

The system must remain a modular monolith until there is a real operational need to split services.

---

## 2. Non-negotiable golden rules

1. A bounded context owns its business state.
2. Other bounded contexts must not directly mutate that state.
3. Domain owns invariants.
4. Application owns use cases.
5. Infrastructure owns technical implementation.
6. API owns HTTP only.
7. Commands mutate state.
8. Queries read state.
9. Commands live under `Commands/{UseCase}`.
10. Queries live under `Queries/{UseCase}`.
11. No new Application feature may be placed outside `Features/{BoundedContext}/{Module}`.
12. Cross-context writes must use outbox/integration event/consumer or saga.
13. Handlers must not call `SaveChangesAsync` directly.
14. Workspace-scoped reads must filter by workspace.
15. Permission checks must be Application-level, not only API-level.
16. Durable side effects must be outbox-backed.
17. Integration events are versioned.
18. Consumers are idempotent.
19. Database migrations are production code.
20. Coding agents must follow this file exactly.

---

## 3. Canonical backend folder tree

```txt
backend/
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
    Notrelix.Testing.Core/
    Notrelix.Testing.Domain/
    Notrelix.Testing.Application/
    Notrelix.Testing.Integration/
```

No new backend production code may be created outside `backend/src/*`.

No new backend tests may be created outside `backend/tests/*`.

---

# PART A — DOMAIN LAYER RULES

---

## 4. Domain layer folder structure

Current root structure must be respected:

```txt
src/Notrelix.Domain/
  Analytics/
  Automation/
  Billing/
  Collaboration/
  Common/
  Documents/
  Governance/
  Identity/
  Integrations/
  SharedKernel/
  WorkManagement/
  Workspaces/
  GlobalUsings.cs
  Notrelix.Domain.csproj
```

### 4.1 Domain bounded-context folder rule

A business type must be placed under the bounded context that owns it.

Examples:

```txt
User                         -> Identity
Workspace                    -> Workspaces
Board                        -> WorkManagement
BoardItem                    -> WorkManagement
DocumentPage                 -> Documents
Comment                      -> Collaboration
Role / PermissionPolicy      -> Governance
AutomationRule               -> Automation
IntegrationConnection        -> Integrations
Subscription / Entitlement   -> Billing
Search projection source     -> Search/Application/Infrastructure, not Domain source-of-truth unless promoted
Analytics facts              -> Analytics if source-of-truth, otherwise projection
```

### 4.2 Domain `Common` rule

`Domain/Common` is only for base domain building blocks.

Allowed:

```txt
Entity
AggregateRoot
DomainEvent
IDomainEvent
IWorkspaceScoped
SoftDelete base abstractions
Domain exception base
```

Forbidden:

```txt
BoardHelper
WorkspaceService
UserManager
BillingUtils
PermissionCalculator
```

If the type has business meaning, it belongs to a bounded context.

### 4.3 Domain `SharedKernel` rule

`SharedKernel` must stay tiny.

Allowed only when the same concept has the same meaning across multiple contexts.

Allowed examples:

```txt
Email
Money
Slug
WorkspaceId value wrapper if used globally
```

Forbidden:

```txt
BoardStatus
SubscriptionStatus
DocumentPermission
TaskPriority
```

Those belong to their owning context.

---

## 5. Domain internal structure per bounded context

Use the existing domain folder style, but keep this rule:

```txt
src/Notrelix.Domain/{BoundedContext}/{BusinessArea}/
  {Aggregate}.cs
  {Entity}.cs
  {ValueObject}.cs
  Events/
  Rules/
```

Recommended shape:

```txt
src/Notrelix.Domain/WorkManagement/
  Boards/
    Board.cs
    BoardMember.cs
    BoardVisibility.cs
    Events/
      BoardCreatedDomainEvent.cs
      BoardRenamedDomainEvent.cs
      BoardArchivedDomainEvent.cs
    Rules/
      BoardNameRules.cs

  Items/
    BoardItem.cs
    Events/

  Fields/
    BoardField.cs
    FieldType.cs
    FieldSettings.cs
    Events/
```

Important:

- Do not create `Domain/Features`.
- Do not create `Domain/Commands`.
- Do not create `Domain/Queries`.
- Do not create `Domain/DTOs`.
- Domain does not know Application use cases.

---

## 6. Aggregate rule

An aggregate root must:

- Own its invariant.
- Expose behavior methods.
- Avoid public setters for business state.
- Use factory methods for creation.
- Raise domain events for important business facts.
- Increment version for meaningful mutations.
- Never access infrastructure.
- Never call another bounded context's repository.

Example style:

```csharp
public sealed class Board : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public string Title { get; private set; }

    public static Board Create(
        Guid workspaceId,
        Guid actorUserId,
        string title,
        string? description,
        DateTimeOffset createdAt,
        BoardVisibility visibility)
    {
        // validate and create
    }

    public void Rename(string title, Guid actorUserId, DateTimeOffset occurredAt)
    {
        // validate invariant
        // update state
        // IncrementVersion()
        // AddDomainEvent(...)
    }
}
```

Forbidden:

```csharp
board.Title = request.Title;
board.UpdatedAt = DateTimeOffset.UtcNow;
```

---

## 7. Domain event rule

Domain events are internal facts raised by aggregates.

Naming:

```txt
{Aggregate}{PastTenseAction}DomainEvent
```

Examples:

```txt
UserRegisteredDomainEvent
WorkspaceCreatedDomainEvent
BoardCreatedDomainEvent
BoardRenamedDomainEvent
BoardArchivedDomainEvent
BoardItemMovedDomainEvent
DocumentPageCreatedDomainEvent
CommentCreatedDomainEvent
SubscriptionActivatedDomainEvent
EntitlementChangedDomainEvent
```

Rules:

- Event name must be past tense.
- Event must not be named like a command.
- Event must include aggregate id.
- Event must include `WorkspaceId` if workspace-scoped.
- Event must include `ActorUserId` if user-triggered.
- Event must include occurrence time.
- Event must not contain full entities.
- Event must not publish itself.
- Event must not call services.

---

## 8. Domain version/concurrency rule

Every aggregate root version is business concurrency metadata.

Rules:

- Creation sets initial version.
- Meaningful mutation increments version.
- No-op does not increment version.
- Commands that update concurrency-sensitive aggregates should carry expected version.
- EF Core must enforce version as concurrency token.
- Concurrency conflict maps to `409 Conflict`.

Concurrency-sensitive examples:

```txt
Workspace
Board
BoardItem
BoardField
BoardView
DocumentPage
DocumentBlock
Role
PermissionPolicy
Subscription
Entitlement
AutomationRule
IntegrationConnection
```

---

# PART B — APPLICATION LAYER RULES

---

## 9. Application root structure — exact

Current root structure:

```txt
src/Notrelix.Application/
  Common/
  EventMappers/
  Events/
  Features/
  DependencyInjection.cs
  GlobalUsings.cs
  Notrelix.Application.csproj
  README.md
```

Do not create bounded contexts directly under `Notrelix.Application`.

Forbidden:

```txt
src/Notrelix.Application/WorkManagement/
src/Notrelix.Application/Boards/
src/Notrelix.Application/Commands/
src/Notrelix.Application/Queries/
src/Notrelix.Application/Services/
```

Correct:

```txt
src/Notrelix.Application/Features/WorkManagement/Boards/Commands/CreateBoardInWorkspace/
src/Notrelix.Application/Features/WorkManagement/Boards/Queries/GetBoard/
```

---

## 10. Application `Common` structure — exact

Current `Common` folders include:

```txt
src/Notrelix.Application/Common/
  Activity/
  Auditing/
  Behaviors/
  Caching/
  Context/
  Data/
  DTOs/
  Email/
  Entitlements/
  Events/
  Exceptions/
  Idempotency/
  Integrations/
  Messaging/
  Models/
  PostCommit/
  RateLimiting/
  Requests/
  Security/
  Storage/
  SystemOperations/
  Tenancy/
  Time/
```

### 10.1 `Common` rule

`Common` is cross-cutting only.

Allowed in `Common`:

```txt
Request markers
Pipeline behaviors
Result model
Paging model
Current request context abstraction (ICurrentRequestContext)
Date/time abstraction
Permission abstractions
Workspace context abstraction
Idempotency abstractions
Entitlement abstractions
Cache abstractions
Email abstraction
Storage abstraction
Messaging abstractions
Post-commit action queue
Integration abstractions
System operations abstractions
Application exceptions
```

Forbidden in `Common`:

```txt
CreateBoardCommand
RegisterUserCommand
BoardDto if only WorkManagement uses it
WorkspaceBusinessService
BillingPlanRules
DocumentBlockMutationService
```

If a type belongs to one feature module, place it under `Features`.

---

## 11. Application Request marker rules

Current marker folder:

```txt
src/Notrelix.Application/Common/Requests/
  ICommand.cs
  IQuery.cs
  Caching/
    AuthorizedCacheScope.cs
    IAuthorizedCacheableRequest.cs
    IPublicCacheableQuery.cs
  Execution/
    RequestExecutionClassifier.cs
    RequestExecutionProfile.cs
  Gates/
    IRequireFeature.cs
    IRequireSubscription.cs
  Realtime/
    IRealtimeRequest.cs
  Scoping/
    IAccountRequest.cs
    IGlobalRequest.cs
    IResourceScopedRequest.cs
    IRlsReadRequest.cs
    IWorkspaceRequest.cs
  Security/
    IAuthenticatedRequest.cs
    IAnonymousRequest.cs
    IRequirePermission.cs
    ISystemInternalRequest.cs
    IUseCaseSecurityRequirement.cs
    UseCaseSecurityKind.cs
  Transactions/
    IExpectedVersionRequest.cs
    IIdempotentRequest.cs
    ITransactionalRequest.cs
```

Use these markers. Do not create duplicate marker interfaces elsewhere.

### 11.1 Concurrency checking rule

Any request implementing `IExpectedVersionRequest` must provide a positive `ExpectedVersion` and a supported `ResourceRef`. The system must fail fast if the resource version cannot be verified. Concurrency checks must never be silently skipped.

The `ConcurrencyBehavior` enforces:
- `ExpectedVersion <= 0` → throws `ValidationException`
- `currentVersion == null` (resource not found) → throws `NotFoundException`
- Unsupported resource type → throws `NotSupportedException` (never caught and skipped)

### 11.2 Permissioned cache version rule

Permissioned cache keys must use `IPermissionVersionProvider`. Permission version must include `accountId`, `workspaceId`, `userId`, and a real permission version stamp. Never use `"default"`, `"unknown"`, or hardcoded permission versions.

The provider queries the following tables for the latest update timestamp:
- `workspace.workspace_members`
- `governance.member_role_assignments`
- `governance.custom_roles`
- `governance.resource_permissions`
- `governance.permission_rules`

Each subquery filters by `account_id`, `workspace_id`, and (for workspace_members) `user_id`.

### 11.3 Request context rule

Application handlers must not inject `ICurrentTenantContext` directly. Use `ICurrentRequestContext` when a handler needs current user/account/workspace data. Tenant runtime services, pipeline behaviors, DbContext/RLS services, and infrastructure tenant scopes may use `ICurrentTenantContext`.

### 11.4 Consumer idempotency rule

Integration consumers must use `DeduplicationConsumeFilter` with claim-before-execute pattern.

- Idempotency key is `event_id + consumer_name`
- Do not implement manual deduplication in consumer handlers
- Do not reintroduce `ConsumerPipelineExecutor`
- Consumers must not execute before claim succeeds
- If consumer fails, transaction rolls back and message can be retried
- Claim record has `Status` field: `Processing`, `Succeeded`, `Failed`

Forbidden:

```txt
Manual deduplication in consumer handlers
ConsumerPipelineExecutor idempotency
Check-then-mark-after pattern (race condition)
```

---

## 12. Application `Features` structure — exact

Root bounded contexts under `Features`:

```txt
src/Notrelix.Application/Features/
  Analytics/
  Automation/
  Billing/
  Collaboration/
  Documents/
  Governance/
  Identity/
  Integrations/
  Operations/
  Search/
  WorkManagement/
  Workspaces/
```

No new bounded context folder may be added without ADR.

### 12.1 Canonical Application layout

```txt
src/Notrelix.Application/Features/{BoundedContext}/
  {Module}/
    Commands/
      {UseCase}/
        {UseCase}.cs
        {UseCase}CommandValidator.cs

    Queries/
      {UseCase}/
        {UseCase}.cs
        {UseCase}QueryValidator.cs

    DTOs/
      {ModuleDto}.cs

    Services/
      {NarrowPurposeService}.cs

    ReadModels/
      {ModuleReadModel}.cs

    Mapping/
      {ModuleMappingProfile}.cs

    Permissions/
      {ModulePermissionConstants}.cs

    Cache/
      {ModuleCacheKeys}.cs

  Common/
    DTOs/
    Services/
    Mapping/
    Permissions/
    Cache/

  Abstractions/
    I{Context}DbContext.cs
    I{Context}ReadService.cs
```

### 12.2 Current WorkManagement example

Correct module-first shape:

```txt
src/Notrelix.Application/Features/WorkManagement/
  Abstractions/
  Approvals/
  BoardFields/
  BoardGroups/
  BoardItems/
  BoardSchema/
  BoardSearch/
  BoardViews/
  Boards/
  Checklists/
  Common/
  FieldOptions/
  Forms/
  Formulas/
  ItemLinks/
  Labels/
  MyWork/
  Relations/
  Rollups/
  Templates/
  Workload/
```

Correct Boards shape:

```txt
src/Notrelix.Application/Features/WorkManagement/Boards/
  Commands/
    AddBoardMember/
    ArchiveBoard/
    CreateBoardBySlug/
    CreateBoardInWorkspace/
    RemoveBoardMember/
    UnarchiveBoard/
    UpdateBoard/

  Queries/
    GetBoard/
    GetBoardMembers/
    GetBoards/
    GetBoardsBySlug/
    GetFullBoard/

  DTOs/
```

### 12.3 Use-case folder rule

A use case folder must be inside `Commands` or `Queries`.

Correct:

```txt
Features/WorkManagement/Boards/Commands/CreateBoardInWorkspace/
Features/WorkManagement/Boards/Queries/GetBoard/
```

Forbidden:

```txt
Features/WorkManagement/Boards/CreateBoardInWorkspace/
Features/WorkManagement/CreateBoardInWorkspace/
Features/WorkManagement/Commands/Boards/CreateBoardInWorkspace/
Features/WorkManagement/Queries/Boards/GetBoard/
Application/WorkManagement/Boards/CreateBoardInWorkspace/
```

The old legacy style is forbidden for new code:

```txt
Features/{Context}/Commands/{Module}/{UseCase}
Features/{Context}/Queries/{Module}/{UseCase}
```

Use module-first only:

```txt
Features/{Context}/{Module}/Commands/{UseCase}
Features/{Context}/{Module}/Queries/{UseCase}
```

---

## 13. Application module list — exact target

Use only these modules unless ADR expands the context.

### 13.1 Identity

```txt
Features/Identity/
  Auth/
  Users/
  Profiles/
  Sessions/
  Credentials/
  OAuth/
  Security/
  SSO/
  ApiTokens/
```

Examples:

```txt
Features/Identity/Auth/Commands/RegisterUser/
Features/Identity/Auth/Commands/Login/
Features/Identity/Users/Queries/GetCurrentUser/
Features/Identity/Security/Commands/ChangePassword/
```

### 13.2 Workspaces

```txt
Features/Workspaces/
  Workspaces/
  Members/
  Invitations/
  Spaces/
  Teams/
  Settings/
  WorkspaceHome/
```

Examples:

```txt
Features/Workspaces/Workspaces/Commands/CreateWorkspace/
Features/Workspaces/Members/Commands/InviteMember/
Features/Workspaces/Settings/Commands/UpdateWorkspaceSettings/
```

### 13.3 Governance

```txt
Features/Governance/
  Permissions/
  PermissionRules/
  ResourcePermissions/
  ShareLinks/
  Roles/
  Policies/
  AuditLogs/
  SecurityEvents/
```

Examples:

```txt
Features/Governance/Roles/Commands/CreateRole/
Features/Governance/Permissions/Queries/GetEffectivePermissions/
```

### 13.4 WorkManagement

```txt
Features/WorkManagement/
  Abstractions/
  Approvals/
  BoardFields/
  BoardGroups/
  BoardItems/
  BoardSchema/
  BoardSearch/
  BoardViews/
  Boards/
  Checklists/
  Common/
  FieldOptions/
  Forms/
  Formulas/
  ItemLinks/
  Labels/
  MyWork/
  Relations/
  Rollups/
  Templates/
  Workload/
```

Examples:

```txt
Features/WorkManagement/Boards/Commands/CreateBoardInWorkspace/
Features/WorkManagement/Boards/Queries/GetBoard/
Features/WorkManagement/BoardItems/Commands/CreateBoardItem/
Features/WorkManagement/BoardFields/Commands/CreateBoardField/
```

### 13.5 Documents

```txt
Features/Documents/
  Pages/
  Blocks/
  Versions/
  ResourceLinks/
  Templates/
  Export/
```

Examples:

```txt
Features/Documents/Pages/Commands/CreatePage/
Features/Documents/Blocks/Commands/UpdateBlock/
Features/Documents/Pages/Queries/GetPage/
```

### 13.6 Collaboration

```txt
Features/Collaboration/
  Comments/
  Reactions/
  Mentions/
  Notifications/
  Activity/
  Attachments/
  Watchers/
  Presence/
```

Examples:

```txt
Features/Collaboration/Comments/Commands/CreateComment/
Features/Collaboration/Notifications/Queries/GetNotifications/
```

### 13.7 Automation

```txt
Features/Automation/
  Rules/
  Engine/
  Executions/
  Scheduled/
  Templates/
```

Examples:

```txt
Features/Automation/Rules/Commands/CreateAutomationRule/
Features/Automation/Executions/Queries/GetAutomationRuns/
```

### 13.8 Integrations

```txt
Features/Integrations/
  Connections/
  Webhooks/
  Providers/
  Sync/
  Inbound/
```

Examples:

```txt
Features/Integrations/Connections/Commands/CreateConnection/
Features/Integrations/Webhooks/Commands/RegisterWebhook/
```

### 13.9 Billing

```txt
Features/Billing/
  Plans/
  Subscriptions/
  Entitlements/
  Usage/
  Invoices/
  Payments/
  Webhooks/
```

Examples:

```txt
Features/Billing/Subscriptions/Commands/StartSubscription/
Features/Billing/Entitlements/Queries/GetWorkspaceEntitlements/
```

### 13.10 Analytics

```txt
Features/Analytics/
  Dashboards/
  Widgets/
  Snapshots/
  Metrics/
```

### 13.11 Search

```txt
Features/Search/
  GlobalSearch/
  BoardSearch/
  Indexing/
  Permissions/
```

### 13.12 Operations

```txt
Features/Operations/
  ImportExport/
  Jobs/
  Idempotency/
  Admin/
```

---

## 14. Application file naming — strict

### 14.1 Command use case folder

Folder:

```txt
Features/{Context}/{Module}/Commands/{UseCase}/
```

Files:

```txt
{UseCase}.cs
{UseCase}CommandValidator.cs
```

Current repo style combines command record and handler in `{UseCase}.cs`.

Example:

```txt
Features/WorkManagement/Boards/Commands/CreateBoardInWorkspace/
  CreateBoardInWorkspace.cs
  CreateBoardInWorkspaceCommandValidator.cs
```

Inside `{UseCase}.cs`:

```csharp
namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace;

public record CreateBoardInWorkspaceCommand(...) 
    : ICommand<Result<Guid>>,
      ITransactionalRequest,
      IRequirePermission,
      IWorkspaceRequest;

public sealed class CreateBoardInWorkspaceCommandHandler
    : IRequestHandler<CreateBoardInWorkspaceCommand, Result<Guid>>
{
}
```

### 14.2 Query use case folder

Folder:

```txt
Features/{Context}/{Module}/Queries/{UseCase}/
```

Files:

```txt
{UseCase}.cs
{UseCase}QueryValidator.cs // only if query has meaningful input validation
```

Example:

```txt
Features/WorkManagement/Boards/Queries/GetBoard/
  GetBoard.cs
```

Inside `{UseCase}.cs`:

```csharp
namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoard;

public record GetBoardQuery(...) 
    : IQuery<Result<BoardDto>>,
      IRequirePermission,
      IWorkspaceRequest;

public sealed class GetBoardQueryHandler
    : IRequestHandler<GetBoardQuery, Result<BoardDto>>
{
}
```

### 14.3 When to split command and handler into separate files

Default current repo convention:

```txt
{UseCase}.cs contains request + handler.
{UseCase}CommandValidator.cs contains validator.
```

Allowed split only when the file becomes too large:

```txt
{UseCase}Command.cs
{UseCase}CommandHandler.cs
{UseCase}CommandValidator.cs
```

But do not mix styles in the same module without reason.

If a module already uses the combined style, continue combined style.

### 14.4 DTO placement

Preferred order:

1. If DTO is shared by several modules in the same bounded context:

```txt
Features/{Context}/Common/DTOs/{DtoName}.cs
```

Example:

```txt
Features/WorkManagement/Common/DTOs/BoardDto.cs
```

2. If DTO is only used by one module:

```txt
Features/{Context}/{Module}/DTOs/{DtoName}.cs
```

Example:

```txt
Features/WorkManagement/Boards/DTOs/BoardSummaryDto.cs
```

3. If DTO is truly cross-application generic:

```txt
Common/DTOs/{DtoName}.cs
```

Example:

```txt
Common/DTOs/PagedResultDto.cs
```

Forbidden:

```txt
Features/WorkManagement/DTOs/BoardDto.cs
Application/DTOs/BoardDto.cs
API/Contracts reused as Application DTO
Domain/DTOs/BoardDto.cs
```

### 14.5 Application namespace rule

Namespace must mirror folder path.

Correct:

```csharp
namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace;
namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoard;
namespace Notrelix.Application.Features.WorkManagement.Common.DTOs;
```

Forbidden:

```csharp
namespace Notrelix.Application.WorkManagement.Boards;
namespace Notrelix.Application.Boards.Commands;
namespace Notrelix.Application.Features.WorkManagement.Commands.Boards;
```

---

## 15. Application command rules

Every command must:

- Live under `Features/{Context}/{Module}/Commands/{UseCase}/`.
- Be named `{UseCase}Command`.
- Have a handler named `{UseCase}CommandHandler`.
- Have a validator named `{UseCase}CommandValidator`.
- Implement `ICommand<TResponse>`.
- Implement `ITransactionalRequest` if it writes state.
- Implement `IWorkspaceRequest` if workspace-scoped.
- Implement `IRequirePermission` if permission-protected.
- Implement `IIdempotentRequest` if retryable or externally triggered.
- Implement `IRequireSubscription` if feature/quota-protected.
- Implement `IExpectedVersionRequest` if optimistic concurrency is required.
- Implement `IAuthorizedCacheableRequest` if it invalidates authorized cache.
- Implement `IRealtimeRequest` if it should notify clients after commit.
- Return `Result<T>` or approved Application result model.

Forbidden:

```csharp
public record CreateBoardRequest(...)
public class CreateBoardService
public class BoardCommandHandler
```

---

## 16. Application query rules

Every query must:

- Live under `Features/{Context}/{Module}/Queries/{UseCase}/`.
- Be named `{UseCase}Query`.
- Have handler named `{UseCase}QueryHandler`.
- Implement `IQuery<TResponse>`.
- Implement `IWorkspaceRequest` if workspace-scoped.
- Implement `IRequirePermission` if permission-protected.
- Implement `IPublicCacheableQuery<T>` only if safe.
- Return DTOs, not Domain entities.
- Use `AsNoTracking()` for EF read queries unless tracking is required.
- Filter workspace-scoped data by `WorkspaceId`.
- Apply permission/visibility rules.

Critical rule:

```txt
A workspace-scoped query must include workspace filter in the database query.
```

Wrong:

```csharp
.FirstOrDefaultAsync(b => b.Id == request.BoardId, ct)
```

Correct:

```csharp
.FirstOrDefaultAsync(
    b => b.Id == request.BoardId
      && b.WorkspaceId == request.WorkspaceId,
    ct)
```

Cache key must also be workspace-safe:

Wrong:

```csharp
public string CacheKey => $"board:{BoardId}";
```

Correct:

```csharp
public string CacheKey => $"workspace:{WorkspaceId}:board:{BoardId}";
```

---

## 17. Application handler rules

Handlers orchestrate one use case.

Allowed:

- Load aggregate from owning context abstraction.
- Call Domain behavior methods.
- Add aggregate/entity to the owning DbContext abstraction.
- Use `ICurrentUser`.
- Use `IDateTimeProvider`.
- Use `IWorkspaceAccessChecker`.
- Use permission/entitlement abstractions through pipeline or focused ports.
- Return `Result<T>`.

Forbidden:

- Calling `SaveChangesAsync`.
- Starting transaction manually.
- Sending email directly.
- Publishing integration event directly if it must be durable.
- Calling another bounded context's DbContext for write.
- Putting business invariant in handler instead of Domain.
- Returning Domain entities.
- Using `DateTimeOffset.UtcNow` directly.
- Using `IHttpContextAccessor`.
- Querying without workspace filter.

### 17.1 DbContext abstraction rule in handlers

Use context-specific Application abstraction.

Example:

```csharp
private readonly IWorkManagementDbContext _context;
```

Do not inject Infrastructure `ApplicationDbContext` into Application handlers.

Forbidden:

```csharp
private readonly ApplicationDbContext _context;
```

### 17.2 Same bounded context write rule

A WorkManagement handler may write WorkManagement-owned aggregates.

Allowed:

```txt
WorkManagement/Boards/CreateBoardInWorkspace
 -> writes Board
 -> writes BoardField default fields
```

because Board and BoardField are WorkManagement-owned.

### 17.3 Cross bounded context write rule

A WorkManagement handler must not write Billing, Identity, Workspaces, Documents, or Governance aggregates directly.

Forbidden:

```txt
Features/WorkManagement/Boards/Commands/CreateBoardInWorkspace
 -> _workspaceContext.Workspaces.Update(...)
 -> _billingContext.Entitlements.Add(...)
 -> _governanceContext.Roles.Add(...)
```

Correct:

```txt
CreateBoardInWorkspace
 -> writes WorkManagement state
 -> emits outbox/integration event if another context must react
```

---

## 18. Application validator rules

Validator placement:

```txt
Features/{Context}/{Module}/Commands/{UseCase}/{UseCase}CommandValidator.cs
Features/{Context}/{Module}/Queries/{UseCase}/{UseCase}QueryValidator.cs
```

Validators can check:

- Required fields.
- Length.
- Format.
- Range.
- Enum validity.
- Request-level cross-field consistency.

Validators must not:

- Call external APIs.
- Mutate request.
- Perform permission checks.
- Enforce aggregate state transitions.
- Execute business workflows.
- Query another bounded context for write decisions.

---

## 19. Application services rule

`Services/` is optional and dangerous.

Allowed only for narrow, focused, reusable orchestration inside the same module/context.

Allowed examples:

```txt
Features/WorkManagement/Boards/Services/BoardSlugGenerator.cs
Features/WorkManagement/Boards/Cache/BoardCacheKeys.cs
Features/WorkManagement/Common/Services/FractionalIndexAllocator.cs
```

Forbidden:

```txt
Features/WorkManagement/Services/BoardService.cs
Features/Identity/Services/UserService.cs
Common/Services/AppService.cs
Common/Helpers/Helper.cs
```

If an agent wants to create a service, it must answer:

```txt
Why is this not a use case handler?
Why is this not Domain logic?
Why is this not Infrastructure implementation?
Which module owns it?
Which use cases call it?
```

---

## 20. Application cross-context rules

### 20.1 Read from another context

Allowed patterns:

1. Application port/read service.
2. Projection/read model.
3. Snapshot copied by integration event.
4. Governance permission evaluator.
5. Billing entitlement checker.

Forbidden:

```txt
Use another context's DbSet directly.
Include another context aggregate.
Mutate after reading.
```

### 20.2 Write to another context

Default: forbidden.

Use:

```txt
Outbox + integration event + consumer
```

or:

```txt
Saga/process manager
```

### 20.3 Example: register user and create personal workspace

Wrong:

```txt
Identity/Auth/Commands/RegisterUser
 -> creates User
 -> directly creates Workspace
 -> directly creates Governance role
 -> one handler mutates 3 bounded contexts
```

Correct:

```txt
Identity/Auth/Commands/RegisterUser
 -> creates User
 -> records identity.user_registered.v1 integration event in outbox
 -> commit

Workspaces consumer/process manager
 -> consumes identity.user_registered.v1
 -> creates personal workspace
 -> emits workspace.created.v1 if needed

Governance consumer/process manager
 -> consumes workspace.created.v1
 -> creates owner permissions/policies if Governance owns that state
```

If product requires immediate workspace result, create an explicit process manager/ADR. Do not silently cross-write.

---

# PART C — INFRASTRUCTURE LAYER RULES

---

## 21. Infrastructure root structure — exact

Current root structure includes:

```txt
src/Notrelix.Infrastructure/
  Auditing/
  Auth/
  BackgroundJobs/
  Billing/
  Caching/
  Data/
  DependencyInjection/
  Email/
  Events/
  Identity/
  Integrations/
  Messaging/
  Middleware/
  Observability/
  Operations/
  Ops/
  Options/
  RateLimiting/
  ReadModels/
  Realtime/
  Reporting/
  Search/
  Security/
  Services/
  Storage/
  DependencyInjection.cs
  GlobalUsings.cs
  Notrelix.Infrastructure.csproj
```

### 21.1 Infrastructure placement rule

Use the most specific existing folder.

Examples:

```txt
EF Core mapping                 -> Data/Configurations/{Context}/
DbContext                       -> Data/
Migrations                      -> Data/Migrations/
EF interceptors                 -> Data/Interceptors/
Outbox storage/dispatcher       -> Data/Outbox/ or Events/Outbox if already established
Redis cache implementation      -> Caching/
SignalR implementation          -> Realtime/
Email provider                  -> Email/
File storage provider           -> Storage/
Auth/token implementation       -> Auth/ or Security/
External integration provider   -> Integrations/{Provider}/
Billing provider implementation -> Billing/
Search provider implementation  -> Search/Providers/
Background workers              -> BackgroundJobs/ or Operations/Jobs/
Options binding                 -> Options/
```

Forbidden:

```txt
Infrastructure/Helpers/
Infrastructure/Managers/
Infrastructure/CommonServices/
Infrastructure/Features/
```

---

## 22. Infrastructure Data structure — exact

Current Data structure:

```txt
src/Notrelix.Infrastructure/Data/
  Configurations/
  Converters/
  Governance/
  Interceptors/
  Migrations/
  Ops/
  Outbox/
  Projections/
  Seed/
  ApplicationDbContext.cs
  ApplicationDbContextFactory.cs
  ApplicationDbContextInitialiser.cs
  DateTimeProvider.cs
  DbSchemas.cs
  SeedDataOptions.cs
```

### 22.1 EF configuration folder rule

Current EF configurations are grouped by bounded context:

```txt
src/Notrelix.Infrastructure/Data/Configurations/
  Analytics/
  Automation/
  Billing/
  Collaboration/
  Documents/
  Governance/
  Identity/
  Integrations/
  Ops/
  Search/
  WorkManagement/
  Workspaces/
```

New EF configuration must go here:

```txt
Data/Configurations/{BoundedContext}/{EntityName}Configuration.cs
```

Examples:

```txt
Data/Configurations/WorkManagement/BoardConfiguration.cs
Data/Configurations/WorkManagement/BoardItemConfiguration.cs
Data/Configurations/Documents/DocumentPageConfiguration.cs
Data/Configurations/Billing/SubscriptionConfiguration.cs
```

Forbidden:

```txt
Data/BoardConfiguration.cs
Data/Configurations/BoardConfiguration.cs
Infrastructure/WorkManagement/BoardConfiguration.cs
Application/Features/.../BoardConfiguration.cs
Domain/.../BoardConfiguration.cs
```

### 22.2 DbContext rule

Application handlers must use Application abstractions such as:

```txt
IWorkManagementDbContext
IIdentityDbContext
IWorkspaceDbContext
IBillingDbContext
```

Infrastructure implements these using `ApplicationDbContext`.

Rules:

- `ApplicationDbContext` is Infrastructure detail.
- Do not inject it into Application.
- Do not let one context abstraction expose every table.
- Context abstractions should expose only tables owned by that bounded context or approved read models.

### 22.3 Migration rule

Migrations live only in:

```txt
Data/Migrations/
```

Rules:

- Migration must match one feature/change set.
- No unrelated schema changes.
- New tables use correct schema.
- No production table in `public`.
- Use snake_case.
- Use `workspace_id` for workspace-scoped data.
- Use `deleted_at` for soft delete.
- Use indexes with `workspace_id` for workspace-scoped tables.
- Use concurrency column where aggregate requires it.

---

## 23. Infrastructure implementation rule

Infrastructure implements Application ports.

Examples:

```txt
Application Common Abstraction        Infrastructure implementation

IDateTimeProvider                  -> Data/DateTimeProvider.cs
ICacheService                      -> Caching/*
IEmailSender                       -> Email/*
IRealtimePublisher                 -> Realtime/*
IFileStorage                       -> Storage/*
IIntegrationEventBus               -> Messaging/*
IPermissionEvaluator implementation-> Security/Governance/* if technical
IBillingGateway                    -> Billing/*
ISearchProvider                    -> Search/Providers/*
```

Forbidden:

- Provider-specific SDK types leaking into Application.
- Infrastructure making business decisions.
- Infrastructure mutating another bounded context outside Application use case.
- Infrastructure sending business events not requested by Application/Outbox.

---

# PART D — API LAYER RULES

---

## 24. API root structure — exact

Current root structure:

```txt
src/Notrelix.API/
  Contracts/
  Endpoints/
  ErrorHandling/
  Extensions/
  Middleware/
  Options/
  Properties/
  RateLimiting/
  Versioning/
  DependencyInjection.cs
  GlobalUsings.cs
  Notrelix.API.csproj
  Notrelix.API.http
  Program.Visible.cs
  Program.cs
  appsettings*.json
```

### 24.1 API Endpoints structure

Current endpoints:

```txt
src/Notrelix.API/Endpoints/
  Admin/
  Automation/
  Collaboration/
  Documents/
  Governance/
  Health/
  Identity/
  WorkManagement/
  Workspaces/
  EndpointRouteBuilderExtensions.cs
```

WorkManagement example:

```txt
src/Notrelix.API/Endpoints/WorkManagement/
  BoardFields/
  BoardGroups/
  BoardItems/
  BoardViews/
  Boards/
  Checklists/
  Labels/
```

### 24.2 API Contracts structure

Current contracts:

```txt
src/Notrelix.API/Contracts/
  Admin/
  Automation/
  Collaboration/
  Documents/
  Governance/
  Identity/
  WorkManagement/
  Workspaces/
```

Rules:

- API request/response contracts live in `API/Contracts/{Context}/{Module}`.
- API endpoints live in `API/Endpoints/{Context}/{Module}`.
- API contracts are not Application DTOs.
- Application DTOs are not Domain entities.
- Do not place HTTP contracts in Application unless intentionally shared as API-independent DTOs.

---

## 25. Endpoint placement rule

For every Application command/query, endpoint goes to matching API context/module.

Example:

```txt
Application:
Features/WorkManagement/Boards/Commands/CreateBoardInWorkspace/CreateBoardInWorkspace.cs

API endpoint:
Endpoints/WorkManagement/Boards/BoardsEndpoints.cs
or
Endpoints/WorkManagement/Boards/CreateBoardEndpoint.cs
depending existing module style

API contract:
Contracts/WorkManagement/Boards/CreateBoardRequest.cs
```

Rules:

- Endpoint must only map HTTP to command/query.
- Endpoint must send command/query through MediatR.
- Endpoint must not inject DbContext.
- Endpoint must not call Infrastructure provider.
- Endpoint must not contain business rules.
- Endpoint must not decide permissions beyond auth route metadata.

Allowed endpoint flow:

```txt
HTTP request
 -> API request contract
 -> Application command/query
 -> MediatR
 -> Result mapping
 -> HTTP response
```

Forbidden:

```txt
HTTP request
 -> DbContext
 -> entity mutation
 -> SaveChangesAsync
```

---

# PART E — FEATURE IMPLEMENTATION FLOW

---

## 26. Exact decision flow before adding a file

Before creating any file, answer in this order:

```txt
1. Is this Domain, Application, Infrastructure, API, or Test?
2. Which bounded context owns the state?
3. Which module inside that context owns the use case?
4. Is it a Command or Query?
5. Does it need a DTO?
6. Does it need API contract?
7. Does it need EF configuration/migration?
8. Does it write another bounded context?
9. Does it need outbox/saga?
10. Which test project proves it?
```

If any answer is unclear, do not create a random `Services` or `Common` file.

---

## 27. Command implementation file map

Example: create board in workspace.

```txt
Domain:
src/Notrelix.Domain/WorkManagement/Boards/Board.cs
src/Notrelix.Domain/WorkManagement/Boards/Events/BoardCreatedDomainEvent.cs

Application:
src/Notrelix.Application/Features/WorkManagement/Boards/Commands/CreateBoardInWorkspace/CreateBoardInWorkspace.cs
src/Notrelix.Application/Features/WorkManagement/Boards/Commands/CreateBoardInWorkspace/CreateBoardInWorkspaceCommandValidator.cs
src/Notrelix.Application/Features/WorkManagement/Common/DTOs/BoardDto.cs  // only if shared
or
src/Notrelix.Application/Features/WorkManagement/Boards/DTOs/BoardDto.cs   // only if module-local

Infrastructure:
src/Notrelix.Infrastructure/Data/Configurations/WorkManagement/BoardConfiguration.cs
src/Notrelix.Infrastructure/Data/Migrations/{timestamp}_AddBoardSomething.cs

API:
src/Notrelix.API/Contracts/WorkManagement/Boards/CreateBoardRequest.cs
src/Notrelix.API/Endpoints/WorkManagement/Boards/BoardsEndpoints.cs

Tests:
backend/tests/Notrelix.Domain.Tests/WorkManagement/Boards/BoardTests.cs
backend/tests/Notrelix.Application.Tests/Features/WorkManagement/Boards/CreateBoardInWorkspaceTests.cs
backend/tests/Notrelix.API.Tests/WorkManagement/Boards/BoardsEndpointsTests.cs
backend/tests/Notrelix.Integration.Tests/WorkManagement/Boards/CreateBoardFlowTests.cs
```

---

## 28. Query implementation file map

Example: get board.

```txt
Application:
src/Notrelix.Application/Features/WorkManagement/Boards/Queries/GetBoard/GetBoard.cs
src/Notrelix.Application/Features/WorkManagement/Boards/Queries/GetBoard/GetBoardQueryValidator.cs // optional
src/Notrelix.Application/Features/WorkManagement/Common/DTOs/BoardDto.cs

API:
src/Notrelix.API/Contracts/WorkManagement/Boards/GetBoardResponse.cs // if HTTP response differs
src/Notrelix.API/Endpoints/WorkManagement/Boards/BoardsEndpoints.cs

Tests:
backend/tests/Notrelix.Application.Tests/Features/WorkManagement/Boards/GetBoardTests.cs
backend/tests/Notrelix.API.Tests/WorkManagement/Boards/GetBoardEndpointTests.cs
```

Query must include workspace filter.

---

## 29. Cross-bounded-context implementation map

Example: user registration creates personal workspace.

### 29.1 Wrong structure

Forbidden:

```txt
Features/Identity/Auth/Commands/RegisterUser/RegisterUser.cs
 -> directly writes User
 -> directly writes Workspace
 -> directly writes Governance policies
```

### 29.2 Correct structure

```txt
Identity owns registration:
src/Notrelix.Application/Features/Identity/Auth/Commands/RegisterUser/RegisterUser.cs

Domain event:
src/Notrelix.Domain/Identity/Users/Events/UserRegisteredDomainEvent.cs

Integration event contract:
src/Notrelix.Application/Common/Events/Identity/UserRegisteredIntegrationEventV1.cs
or
src/Notrelix.Application/Events/Identity/UserRegisteredIntegrationEventV1.cs
depending current established event folder

Event mapper:
src/Notrelix.Application/EventMappers/Identity/UserRegisteredEventMapper.cs

Outbox persistence:
src/Notrelix.Infrastructure/Data/Outbox/*

Workspaces consumer/process manager:
src/Notrelix.Infrastructure/Messaging/Consumers/Workspaces/CreatePersonalWorkspaceOnUserRegisteredConsumer.cs
or if process state is needed:
src/Notrelix.Infrastructure/BackgroundJobs/Workspaces/WorkspaceProvisioningProcessManager.cs

Workspaces Application command:
src/Notrelix.Application/Features/Workspaces/Workspaces/Commands/CreatePersonalWorkspace/CreatePersonalWorkspace.cs
```

Rules:

- Identity emits fact: user registered.
- Workspaces reacts and creates workspace.
- Governance reacts if it owns permission setup.
- Each context writes only its own state.
- Consumer is idempotent.
- Outbox message is durable.
- Saga/process manager is used if more than one step must be tracked.

---

# PART F — BOUNDED CONTEXT RULES

---

## 30. Context ownership matrix

| Bounded context | Owns | Does not own |
|---|---|---|
| Identity | user identity, credentials, login, sessions, security settings | workspace lifecycle, billing plan, board state |
| Workspaces | workspace lifecycle, members, invitations, teams, settings | user credentials, billing subscription rules |
| Governance | roles, permissions, policies, resource permission rules, security events | board content, document content, payment provider |
| WorkManagement | boards, items, fields, views, groups, labels, checklists, forms, workload | comments, notifications, user identity, subscription |
| Documents | pages, blocks, versions, resource links, templates, export | board item workflow, billing |
| Collaboration | comments, reactions, mentions, notifications, attachments, watchers, presence | source board/document state |
| Automation | rules, triggers, executions, scheduled jobs | board/document aggregate ownership |
| Integrations | connections, provider config, webhooks, sync, inbound events | provider-independent business aggregate rules |
| Billing | plans, subscriptions, entitlements, usage, invoices, payments | workspace creation, board creation |
| Analytics | dashboards, widgets, snapshots, metrics | source-of-truth business aggregates |
| Search | global search, board search, indexing, permission-aware search projection | source-of-truth business aggregates |
| Operations | import/export, jobs, idempotency, admin ops | product domain ownership |

---

## 31. Cross-context communication rules

### 31.1 Same-context operations

Allowed direct write:

```txt
WorkManagement Boards command creates Board and BoardField
Documents Page command creates Page and Blocks
Collaboration Comment command creates Comment and Mention
Billing Subscription command creates Subscription and Entitlement
```

Only if all aggregates/entities are owned by the same bounded context.

### 31.2 Cross-context read

Allowed:

```txt
WorkManagement asks Governance IPermissionEvaluator.
Automation reads WorkManagement resource snapshot through read port.
Billing exposes entitlement checker to other contexts.
Search consumes projections.
```

Forbidden:

```txt
WorkManagement handler queries Billing table directly.
Documents handler loads Board aggregate and changes it.
Identity handler creates Workspace directly.
```

### 31.3 Cross-context write

Use:

```txt
DomainEvent -> IntegrationEventMapper -> Outbox -> Dispatcher -> Consumer -> Target Context Command
```

or:

```txt
ProcessManager/Saga
```

---

# PART G — TRANSACTION, OUTBOX, CACHE, REALTIME

---

## 32. Transaction rule

Current code uses `ITransactionalRequest`.

Therefore:

- Every mutating command must implement `ITransactionalRequest`.
- Query must not implement `ITransactionalRequest`.
- Handler must not call `SaveChangesAsync`.
- Transaction behavior owns commit.
- Domain event/outbox persistence must happen inside the same transaction.
- Post-commit effects must happen after commit or through durable outbox.

Preferred future improvement:

```txt
ICommand<T> should become transactional by default.
INonTransactionalCommand<T> should be the exception.
```

Until that refactor is done, agents must explicitly add `ITransactionalRequest` to every mutating command.

---

## 33. Outbox rule

Use outbox for durable side effects:

```txt
cross-bounded-context event
billing/payment event
workspace provisioning
email that must not be lost
webhook
search indexing if consistency matters
external integration sync
long-running workflow
```

Do not use direct publish for important effects.

Required flow:

```txt
Aggregate mutation
 -> domain event
 -> integration event mapper
 -> outbox message saved in same transaction
 -> commit
 -> dispatcher publishes
 -> consumer processes idempotently
```

---

## 34. Cache rule

Cache key must include scope.

Wrong:

```txt
board:{BoardId}
boards
current-user
```

Correct:

```txt
workspace:{WorkspaceId}:board:{BoardId}
workspace:{WorkspaceId}:boards:list:{hash}
user:{UserId}:current
workspace:{WorkspaceId}:user:{UserId}:permissions:{hash}
```

If result depends on permission, user, role, workspace, or feature flag, include that scope in key.

---

## 35. Realtime rule

Realtime is UI notification, not source of truth.

Rules:

- Use `IRealtimeRequest` for use cases that notify clients.
- Realtime happens after commit.
- Payload should be small.
- Client must refetch authoritative data.
- Missing realtime event must not corrupt data.

---

# PART H — TEST AND ARCHITECTURE ENFORCEMENT

---

## 36. Test placement

Use current test projects.

```txt
Domain tests:
backend/tests/Notrelix.Domain.Tests/{Context}/{Module}/

Application tests:
backend/tests/Notrelix.Application.Tests/Features/{Context}/{Module}/{UseCase}Tests.cs

Architecture tests:
backend/tests/Notrelix.Architecture.Tests/

Infrastructure tests:
backend/tests/Notrelix.Infrastructure.Tests/{Area}/

API tests:
backend/tests/Notrelix.API.Tests/{Context}/{Module}/

Integration tests:
backend/tests/Notrelix.Integration.Tests/{Context}/{Module}/
```

If existing test folders do not yet mirror feature structure, new tests should still use the future-safe mirror unless a project convention already exists.

---

## 37. Mandatory architecture tests

Add or maintain tests for:

```txt
Domain must not reference Application/Infrastructure/API
Application must not reference Infrastructure/API
Infrastructure must not reference API
API endpoints must not inject DbContext
Application handlers must not call SaveChangesAsync
Command folders must be under Features/{Context}/{Module}/Commands/{UseCase}
Query folders must be under Features/{Context}/{Module}/Queries/{UseCase}
No use case directly under Features/{Context}/{Module}
No new Features/{Context}/Commands/{Module} legacy path
Command records must end with Command
Query records must end with Query
Command handlers must end with CommandHandler
Query handlers must end with QueryHandler
Mutating commands must implement ITransactionalRequest
Workspace-scoped requests must implement IWorkspaceRequest
Permission-protected requests must implement IRequirePermission
Integration events must be versioned
Workspace-scoped query cache keys must include workspace id
```

---

## 38. PR review checklist

Reject PR if:

- Application file is not under `Features/{Context}/{Module}`.
- Command is not under `Commands/{UseCase}`.
- Query is not under `Queries/{UseCase}`.
- Namespace does not match folder.
- Handler calls `SaveChangesAsync`.
- Endpoint accesses DbContext.
- Domain has public setter for business state.
- Business rule is in API.
- Business rule is in Infrastructure.
- Cross-context write is direct.
- Workspace query lacks workspace filter.
- Cache key ignores workspace/user.
- Durable effect has no outbox.
- Integration event is not versioned.
- New service/helper is vague.
- Migration touches unrelated schema.
- File is placed in `Common` because agent was unsure.

---

# PART I — CODING AGENT RULES

---

## 39. Agent must follow exact path template

For command:

```txt
src/Notrelix.Application/Features/{Context}/{Module}/Commands/{UseCase}/{UseCase}.cs
src/Notrelix.Application/Features/{Context}/{Module}/Commands/{UseCase}/{UseCase}CommandValidator.cs
```

For query:

```txt
src/Notrelix.Application/Features/{Context}/{Module}/Queries/{UseCase}/{UseCase}.cs
src/Notrelix.Application/Features/{Context}/{Module}/Queries/{UseCase}/{UseCase}QueryValidator.cs
```

For API contract:

```txt
src/Notrelix.API/Contracts/{Context}/{Module}/{RequestName}.cs
```

For API endpoint:

```txt
src/Notrelix.API/Endpoints/{Context}/{Module}/{Module}Endpoints.cs
```

For EF config:

```txt
src/Notrelix.Infrastructure/Data/Configurations/{Context}/{EntityName}Configuration.cs
```

For Domain aggregate/event:

```txt
src/Notrelix.Domain/{Context}/{BusinessArea}/{Aggregate}.cs
src/Notrelix.Domain/{Context}/{BusinessArea}/Events/{Aggregate}{Action}DomainEvent.cs
```

### 39.1 Agent must not create these paths

Forbidden:

```txt
src/Notrelix.Application/{Context}/
src/Notrelix.Application/Commands/
src/Notrelix.Application/Queries/
src/Notrelix.Application/Services/
src/Notrelix.Application/Features/{Context}/Commands/{Module}/
src/Notrelix.Application/Features/{Context}/Queries/{Module}/
src/Notrelix.Application/Features/{Context}/{Module}/{UseCase}/
src/Notrelix.Domain/Features/
src/Notrelix.Domain/DTOs/
src/Notrelix.Infrastructure/Features/
src/Notrelix.API/Services/
```

### 39.2 Agent must inspect before coding

Before editing, agent must inspect:

```txt
RULE.md
Application README
Existing similar use case in same module
Domain aggregate
Context DbContext abstraction
Existing endpoint module
Existing API contracts
Existing tests
```

### 39.3 Agent must report after coding

Agent output must include:

```txt
Files created
Files modified
Bounded context touched
Module touched
Use case path
Markers used
Cross-context dependency, if any
Migration, if any
Tests added
Architecture rules preserved
```

---

# PART J — CONCRETE TEMPLATES

---

## 40. Command template

```csharp
using MediatR;
using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.{Context}.{Module}.Commands.{UseCase};

public sealed record {UseCase}Command(
    Guid WorkspaceId
) : ICommand<Result<Guid>>,
    ITransactionalRequest,
    IWorkspaceRequest,
    IRequirePermission
{
    public PermissionAction Action => PermissionAction.{Action};
    public ResourceRef Resource => ResourceRef.Create(ResourceType.{Resource}, {ResourceId}, WorkspaceId);
}

public sealed class {UseCase}CommandHandler
    : IRequestHandler<{UseCase}Command, Result<Guid>>
{
    private readonly I{Context}DbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public {UseCase}CommandHandler(
        I{Context}DbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle({UseCase}Command request, CancellationToken ct)
    {
        var now = _dateTimeProvider.UtcNow;

        // Load aggregate with workspace filter.
        // Call domain behavior.
        // Add/update owning context state only.
        // Do not SaveChangesAsync.

        return Result.Success(Guid.Empty);
    }
}
```

Validator:

```csharp
using FluentValidation;

namespace Notrelix.Application.Features.{Context}.{Module}.Commands.{UseCase};

public sealed class {UseCase}CommandValidator : AbstractValidator<{UseCase}Command>
{
    public {UseCase}CommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
    }
}
```

---

## 41. Query template

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.{Context}.{Module}.Queries.{UseCase};

public sealed record {UseCase}Query(
    Guid WorkspaceId,
    Guid ResourceId
) : IQuery<Result<{DtoName}>>,
    IWorkspaceRequest,
    IRequirePermission,
    IPublicCacheableQuery<Result<{DtoName}>>
{
    public PermissionAction Action => PermissionAction.{Action};
    public ResourceRef Resource => ResourceRef.Create(ResourceType.{Resource}, ResourceId, WorkspaceId);

    public string CacheKey => $"workspace:{WorkspaceId}:{ResourceId}";
    public TimeSpan? Ttl => TimeSpan.FromMinutes(5);
}

public sealed class {UseCase}QueryHandler
    : IRequestHandler<{UseCase}Query, Result<{DtoName}>>
{
    private readonly I{Context}DbContext _context;

    public {UseCase}QueryHandler(I{Context}DbContext context)
    {
        _context = context;
    }

    public async Task<Result<{DtoName}>> Handle({UseCase}Query request, CancellationToken ct)
    {
        var dto = await _context.{DbSet}
            .AsNoTracking()
            .Where(x => x.WorkspaceId == request.WorkspaceId)
            .Where(x => x.Id == request.ResourceId)
            .Select(x => new {DtoName}(...))
            .FirstOrDefaultAsync(ct);

        if (dto is null)
        {
            throw new NotFoundException(nameof({DtoName}), request.ResourceId);
        }

        return Result.Success(dto);
    }
}
```

---

## 42. API endpoint template

```csharp
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Notrelix.API.Contracts.{Context}.{Module};
using Notrelix.Application.Features.{Context}.{Module}.Commands.{UseCase};

namespace Notrelix.API.Endpoints.{Context}.{Module};

public static class {Module}Endpoints
{
    public static RouteGroupBuilder Map{Module}Endpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", CreateAsync)
            .RequireAuthorization();

        return group;
    }

    private static async Task<Results<Created<{ResponseName}>, ProblemHttpResult>> CreateAsync(
        Guid workspaceId,
        {RequestName} request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new {UseCase}Command(
            workspaceId,
            request.SomeValue);

        var result = await sender.Send(command, ct);

        // map Result to HTTP response through existing API conventions
    }
}
```

---

## 43. EF configuration template

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.{Context}.{BusinessArea};

namespace Notrelix.Infrastructure.Data.Configurations.{Context};

public sealed class {EntityName}Configuration : IEntityTypeConfiguration<{EntityName}>
{
    public void Configure(EntityTypeBuilder<{EntityName}> builder)
    {
        builder.ToTable("{table_name}", DbSchemas.{Schema});

        builder.HasKey(x => x.Id);

        builder.Property(x => x.WorkspaceId)
            .IsRequired();

        builder.Property(x => x.Version)
            .IsConcurrencyToken();

        builder.HasIndex(x => new { x.WorkspaceId, x.Id });

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
```

---

# PART K — FINAL AGENT-SAFE SUMMARY

When adding backend code, the default location is never guessed.

Use this matrix:

| Need | Path |
|---|---|
| Domain aggregate | `Domain/{Context}/{BusinessArea}/{Aggregate}.cs` |
| Domain event | `Domain/{Context}/{BusinessArea}/Events/{Event}.cs` |
| Command | `Application/Features/{Context}/{Module}/Commands/{UseCase}/{UseCase}.cs` |
| Command validator | `Application/Features/{Context}/{Module}/Commands/{UseCase}/{UseCase}CommandValidator.cs` |
| Query | `Application/Features/{Context}/{Module}/Queries/{UseCase}/{UseCase}.cs` |
| Query validator | `Application/Features/{Context}/{Module}/Queries/{UseCase}/{UseCase}QueryValidator.cs` |
| Context DTO | `Application/Features/{Context}/Common/DTOs/{Dto}.cs` |
| Module DTO | `Application/Features/{Context}/{Module}/DTOs/{Dto}.cs` |
| Application abstraction | `Application/Features/{Context}/Abstractions/I{Context}DbContext.cs` or `Application/Common/Security/` or `Application/Common/Messaging/` if truly cross-cutting |
| EF config | `Infrastructure/Data/Configurations/{Context}/{Entity}Configuration.cs` |
| Migration | `Infrastructure/Data/Migrations/` |
| Outbox | `Infrastructure/Data/Outbox/` |
| API request | `API/Contracts/{Context}/{Module}/{Request}.cs` |
| API endpoint | `API/Endpoints/{Context}/{Module}/{Module}Endpoints.cs` |
| Domain test | `tests/Notrelix.Domain.Tests/{Context}/{Module}/` |
| Application test | `tests/Notrelix.Application.Tests/Features/{Context}/{Module}/` |
| API test | `tests/Notrelix.API.Tests/{Context}/{Module}/` |
| Integration test | `tests/Notrelix.Integration.Tests/{Context}/{Module}/` |

If the correct path is not in this table, do not create the file until the rulebook is updated.
