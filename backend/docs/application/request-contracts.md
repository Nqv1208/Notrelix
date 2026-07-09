# Request Contracts

## Overview

Request contracts are organized under `Common/Requests/` in subdirectories by concern:

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

## Marker Categories

### Caching

| Marker | Purpose |
|---|---|
| `IPublicCacheableQuery` | Query response can be cached publicly (no permission check on cache hit) |
| `IAuthorizedCacheableRequest` | Request response can be cached but re-authorized on cache hit |
| `AuthorizedCacheScope` | Enum: Workspace, Account, User, Permissioned |

### Gates

| Marker | Purpose |
|---|---|
| `IRequireFeature` | Request requires a specific feature flag to be enabled |
| `IRequireSubscription` | Request requires a specific subscription plan/entitlement |

### Scoping

| Marker | Purpose |
|---|---|
| `IWorkspaceRequest` | Request is scoped to a workspace (provides `WorkspaceId`) |
| `IAccountRequest` | Request is scoped to an account |
| `IGlobalRequest` | Request is global (no tenant scope) |
| `IResourceScopedRequest` | Request targets a specific resource |
| `IRlsReadRequest` | Request should use RLS read-only scope |

### Security

| Marker | Purpose |
|---|---|
| `IAuthenticatedRequest` | Request requires an authenticated user |
| `IAnonymousRequest` | Request is accessible without authentication |
| `ISystemInternalRequest` | Request is for system-internal use only |
| `IRequirePermission` | Request requires specific resource permission |

### Transactions

| Marker | Purpose |
|---|---|
| `ITransactionalRequest` | Request writes state and should participate in a transaction |
| `IExpectedVersionRequest` | Request requires optimistic concurrency checking |
| `IIdempotentRequest` | Request is idempotent (safe to retry) |
