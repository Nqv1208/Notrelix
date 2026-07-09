# Application Folder Structure and Boundaries

## 1. Canonical Application layout

```txt
Notrelix.Application/
  Common/
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
  Features/
    {BoundedContext}/
      {Module}/
        Commands/{UseCase}/
        Queries/{UseCase}/
        DTOs/
        Services/
        ReadModels/
        Mapping/
        Permissions/
        Cache/
```

## 2. `Common/Requests` là gì?

`Common/Requests` chứa request contracts/markers. Đây là những interface mà command/query implement để pipeline biết request cần gì.

Ví dụ:

```txt
ICommand<TResponse>
IQuery<TResponse>
IWorkspaceRequest
IResourceScopedRequest
IRequirePermission
ITransactionalRequest
IExpectedVersionRequest
IAuthorizedCacheableRequest
IPublicCacheableQuery
IRealtimeRequest
IRequireFeature
IRequireSubscription
IAnonymousRequest
ISystemInternalRequest
```

Không đặt service runtime trong `Common/Requests`.

## 3. `Common/Caching` khác `Common/Requests/Caching` như nào?

`Common/Requests/Caching`:

```txt
Request marker/cache metadata.
```

Ví dụ:

```txt
IAuthorizedCacheableRequest
IPublicCacheableQuery
AuthorizedCacheScope
```

`Common/Caching`:

```txt
Runtime caching abstraction/factory.
```

Ví dụ:

```txt
CacheKeyFactory
CacheKeyOptions
IRedisCacheService
CacheInvalidationKey
```

Rule:

```txt
Query declares cache metadata.
Behavior + CacheKeyFactory build actual cache key.
```

## 4. `Common/Requests/Security` khác `Common/Security` như nào?

`Common/Requests/Security` chứa marker:

```txt
IRequirePermission
IAnonymousRequest
IAuthenticatedRequest
ISystemInternalRequest
IUseCaseSecurityRequirement
UseCaseSecurityKind
```

`Common/Security` chứa service/runtime model:

```txt
IAuthorizationDecisionStore
IPermissionEvaluator
IPermissionService
PermissionContext
PermissionDecision
IPermissionVersionProvider
```

Rule:

```txt
Request declares permission need.
AuthorizationBehavior evaluates permission through Common/Security services.
```

## 5. `Common/Requests/Scoping` khác `Common/Tenancy` và `Common/Context`

`Common/Requests/Scoping`:

```txt
Request says it is global/account/workspace/resource scoped.
```

`Common/Tenancy`:

```txt
Runtime tenant access/resolution abstractions.
```

`Common/Context`:

```txt
Current user/current tenant/current request context abstractions.
```

Rule:

```txt
Handlers use ICurrentRequestContext.
Behaviors/RLS/DbContext use ICurrentTenantContext.
```

## 6. Where should new files go?

### New command/query

```txt
Features/{Context}/{Module}/Commands/{UseCase}/
Features/{Context}/{Module}/Queries/{UseCase}/
```

### New request marker

```txt
Common/Requests/{Concern}/
```

Only create new marker when a pipeline behavior or architecture test consumes it.

### New runtime service abstraction

```txt
Common/{Concern}/
```

Example:

```txt
Common/Security/IPermissionVersionProvider.cs
Common/Caching/CacheKeyFactory.cs
Common/Data/IResourceVersionReader.cs
```

### New behavior

```txt
Common/Behaviors/{BehaviorName}.cs
```

Must be registered in `DependencyInjection.cs` with explicit order comment.

## 7. Forbidden placements

Do not place:

```txt
CacheKeyFactory under Common/Requests/Caching
PermissionService under Common/Requests/Security
Tenant resolver under Common/Requests/Scoping
Use case command/query under Common
Domain entity under Application
Infrastructure implementation under Application
```
