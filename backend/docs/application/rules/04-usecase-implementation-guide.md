# Use Case Implementation Guide

## 1. Command folder template

```txt
Features/{BoundedContext}/{Module}/Commands/{UseCase}/
  {UseCase}Command.cs
  {UseCase}CommandHandler.cs        # optional if not same file
  {UseCase}CommandValidator.cs      # if validation is non-trivial
  {UseCase}Result.cs                # optional
```

Small use cases may keep command + handler + response in one file if readability is still good.

## 2. Query folder template

```txt
Features/{BoundedContext}/{Module}/Queries/{UseCase}/
  {UseCase}Query.cs
  {UseCase}QueryHandler.cs          # optional if not same file
  {UseCase}QueryValidator.cs        # if needed
  {UseCase}Result.cs                # optional
  {UseCase}CacheIdentity.cs         # if cache identity is not trivial
```

## 3. Command implementation rules

Command handler should:

1. Load required aggregate/entity through bounded-context DbContext abstraction.
2. Validate existence and state.
3. Call domain method/factory.
4. Add/update entity in context.
5. Return `Result<T>` or `Result`.

Command handler must not:

```txt
Call SaveChangesAsync directly.
Publish message bus event directly.
Send email/webhook directly.
Build cache key.
Set tenant context.
Bypass permission check.
Mutate entity properties directly when domain method exists.
```

Example:

```csharp
public sealed record CreateShareLinkCommand(
    ResourceType ResourceType,
    Guid ResourceId,
    string Level,
    DateTimeOffset? ExpiresAt)
    : ICommand<Result<CreateShareLinkResponse>>,
      IResourceScopedRequest,
      IRequirePermission,
      ITransactionalRequest
{
    public ResourceRef Resource => ResourceRef.Create(ResourceType, ResourceId);
    public PermissionAction Action => ResourceType switch
    {
        ResourceType.Board => PermissionAction.ShareBoardView,
        ResourceType.Page => PermissionAction.SharePage,
        _ => PermissionAction.ManageWorkspace
    };
}
```

## 4. Query implementation rules

Query handler should:

1. Use read-only query pattern.
2. Use `AsNoTracking()` unless update is intended.
3. Project to DTO/result directly where possible.
4. Respect tenant/RLS and resource scope.
5. Avoid N+1 query patterns.

Query handler must not:

```txt
Mutate state.
Call SaveChangesAsync.
Publish events.
Perform external side effects.
Use authorized cache before permission.
Return data that can leak tenant/resource existence.
```

## 5. Result pattern

Use:

```csharp
Result.Success()
Result.Failure("error")
Result<T>.Success(data)
Result<T>.Failure("error")
```

Use `Result.Failure` for expected business failures if the system still uses string errors.
Use exceptions for system/configuration/security failures that should be mapped centrally.

Expected future improvement: typed `Error` with code/message/type.

## 6. Validator rules

Validator handles input shape, not authorization.

Good validator rules:

```txt
Title not empty
Page size range
Date range valid
Required Guid not empty
Level enum valid
```

Bad validator rules:

```txt
User has permission
Workspace is active
Board exists
Plan allows feature
```

Those belong in pipeline/domain/application service.

## 7. Handler dependency rules

Allowed common dependencies:

```txt
Bounded context DbContext abstraction
ICurrentRequestContext
IDateTimeProvider
Application service abstraction
Post-commit/event collector abstraction
Mapper if projection is not simple
ILogger when useful
```

Forbidden dependencies in handler:

```txt
ApplicationDbContext concrete
ICurrentTenantContext
HttpContext
MassTransit IPublishEndpoint/IBus
RabbitMQ client
Redis concrete client
External API concrete client
Email sender for immediate send
Webhook dispatcher for immediate send
```

## 8. DTO rules

DTOs are application/API contracts, not domain objects.

Do not expose:

```txt
Domain entity directly
Sensitive token hash unless intentionally required
Internal permission structures
Provider access token
Raw audit/internal payload
```

## 9. Use case DoD

A use case is complete when:

```txt
Folder path is module-first.
Request has correct markers.
Validator exists if needed.
Handler uses bounded context abstraction.
Handler does not SaveChanges.
Permission marker/action/resource is correct.
Transaction marker is present for mutation.
Cache metadata is safe if cacheable.
Expected version is enforced if mutation is concurrency-sensitive.
Tests cover success, failure, permission/tenant edge where relevant.
```

## 10. Handler architecture rules

| Rule | Enforcement |
|------|-------------|
| No `IApplicationDbContext` in handlers | `DbContextBoundaryTests` |
| No cross-bounded-context `DbContext` injection | `DbContextBoundaryTests` |
| No `IgnoreQueryFilters()` in handler code | `SystemContextUsageTests` |
| Every request must have security classification | `UseCaseSecurityClassificationTests` |
| No `DateTimeOffset.UtcNow` in Domain | Code review + domain tests |

## 11. Use case PR checklist

Every use case PR must answer all questions below.

### Identity

- [ ] 1. What bounded context owns this use case?
- [ ] 2. Is it a command or query?

### Security classification

- [ ] 3. Is it Anonymous, Authenticated, AccountScoped, WorkspaceScoped, or SystemInternal?

  | Classification | Marker |
  |----------------|--------|
  | Anonymous | `IAnonymousRequest` |
  | Authenticated | `IAuthenticatedRequest` |
  | Account-scoped | `IAccountRequest` (extends `IUseCaseSecurityRequirement`) |
  | Workspace-scoped | `IWorkspaceRequest` (extends `IUseCaseSecurityRequirement`) |
  | System internal | `ISystemInternalRequest` + `ISystemOperation` |

- [ ] 4. What AccountId/WorkspaceId source is used? (route param, header, or handler)

### Permission

- [ ] 5. What permission action is required? (e.g. `PermissionAction.ManageThing`)
- [ ] 6. What `ResourceRef` is checked? (workspace-level, board-level, item-level)

### Transactional guarantees

- [ ] 7. Is it transactional? Add `ITransactionalRequest` if yes.
- [ ] 8. Is it idempotent? Add `IIdempotentRequest` if yes.

### Caching

- [ ] 9. Is it cacheable?
  - Public data → `IPublicCacheableQuery`
  - User/workspace-scoped → `IAuthorizedCacheableRequest` with appropriate `CacheScope`

### Feature gating

- [ ] 10. Is it subscription-gated? Add `IRequireSubscription`.
  - Feature-gated? Add `IRequireFeature`.

### Side effects

- [ ] 11. Does it create audit/activity log? Add `IAuditableRequest` or `IActivityRequest`.
- [ ] 12. Does it publish domain/integration event? Ensure handler calls aggregate method that raises the event.
- [ ] 13. Does it enqueue post-commit action? (cache invalidation, realtime dispatch)

### Real-time

- [ ] 14. Does it need real-time push? Add `IRealtimeRequest`.
