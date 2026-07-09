# Request Contracts and Pipeline Rules

## 1. Request contract model

Each command/query must declare its execution needs through marker interfaces.

Example mutation command:

```csharp
public sealed record UpdateBoardCommand(
    Guid BoardId,
    long ExpectedVersion,
    string Title)
    : ICommand<Result<BoardDto>>,
      IResourceScopedRequest,
      IRequirePermission,
      ITransactionalRequest,
      IExpectedVersionRequest
{
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId);
    public PermissionAction Action => PermissionAction.UpdateBoard;
}
```

Example query:

```csharp
public sealed record GetBoardQuery(Guid BoardId)
    : IQuery<Result<BoardDto>>,
      IResourceScopedRequest,
      IRequirePermission,
      IRlsReadRequest,
      IAuthorizedCacheableRequest
{
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId);
    public PermissionAction Action => PermissionAction.ReadBoard;
    public AuthorizedCacheScope CacheScope => AuthorizedCacheScope.Workspace;
    public object CacheIdentity => new GetBoardCacheIdentity(BoardId);
}

public sealed record GetBoardCacheIdentity(Guid BoardId);
```

## 2. Pipeline order contract

Canonical runtime intent:

```txt
ExceptionMapping
Tracing
Validation
RequestContractGuard
TenantBootstrap
ResourceScope
PostCommitScope
PublicCache
DbRequestScope
  Authorization
  Concurrency
  SubscriptionGate
  FeatureGate
  Idempotency
  Handler
  PostCommitEnqueue
  AuthorizedCache
  SaveChanges
  Commit
PostCommitScope.Flush
```

Exact registration may vary, but these constraints must hold:

```txt
Authorization must run before authorized cache hit can return.
Concurrency check must run after tenant/resource scope and inside DB/RLS scope.
Handler must run inside DB/RLS scope when request needs DB.
SaveChanges and Commit must happen before post-commit flush.
Public cache must never apply to tenant-scoped request.
Authorized cache must never run before permission check.
```

## 3. RequestContractGuard rules

Guard must reject invalid combinations:

```txt
Anonymous + SystemInternal
Anonymous + TenantScoped
Global + WorkspaceScoped
Global + ResourceScoped
Global + IRequirePermission
PublicCache + AuthorizedCache
PublicCache + TenantScoped
```

## 4. Marker selection guide

| Scenario | Required markers |
|---|---|
| Public login/register/start OAuth | `IAnonymousRequest` |
| Authenticated global user query | `IAuthenticatedRequest` or no anonymous marker, depending rule |
| Workspace command | `IWorkspaceRequest`, `IRequirePermission`, `ITransactionalRequest` |
| Resource-only route like `/boards/{id}` | `IResourceScopedRequest`, `IRequirePermission` |
| Account-level settings | `IAccountRequest`, `IRequirePermission` |
| Mutation with expected version | `IExpectedVersionRequest`, `ITransactionalRequest` |
| Read with RLS | `IRlsReadRequest` |
| Public cache | `IPublicCacheableQuery` only, no tenant/security marker |
| Authorized cache | `IAuthorizedCacheableRequest` + security marker |
| Realtime after mutation | `IRealtimeRequest` |
| Feature gate | `IRequireFeature` |
| Subscription/plan gate | `IRequireSubscription` |

## 5. Request classifier rule

The classifier is the central source for request execution profile.

Do not duplicate request classification logic inside behaviors. If a behavior needs to know whether request is tenant-scoped, cacheable, system, transactional, etc., use the classifier/profile instead of ad-hoc checks unless the behavior specifically owns that marker.

## 6. Adding a new marker

Only add a marker if all are true:

1. It changes pipeline behavior or architecture rule.
2. It has clear naming and one responsibility.
3. It has tests.
4. It is documented in `RULE.md` and this docs pack.
5. It does not duplicate an existing marker.

Bad marker examples:

```txt
INeedsSomething
IUseAdvancedMode
IHandledBySystem
```

Good marker examples:

```txt
ITransactionalRequest
IExpectedVersionRequest
IAuthorizedCacheableRequest
IRequirePermission
```
