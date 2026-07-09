# Current Request Context

## Overview

`ICurrentRequestContext` is a unified facade that combines `ICurrentUser` and `ICurrentTenantContext` into a single interface. Handlers use this instead of injecting both interfaces separately.

## Interface

```csharp
public interface ICurrentRequestContext
{
    Guid UserId { get; }
    string Email { get; }
    string Name { get; }
    bool IsAuthenticated { get; }
    bool IsSystemContext { get; }
    Guid RequireAccountId();
    Guid RequireWorkspaceId();
}
```

## Implementation

`CurrentRequestContext` wraps `ICurrentUser` + `ICurrentTenantContext`:

```csharp
public class CurrentRequestContext : ICurrentRequestContext
{
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenantContext _tenantContext;
    // ...
    public Guid RequireAccountId() => _tenantContext.RequireAccountId();
    public Guid RequireWorkspaceId() => _tenantContext.RequireWorkspaceId();
    public Guid UserId => _currentUser.Id;
    // ...
}
```

Registered in `AuthRegistration.cs` as scoped.

## Migration

All 25+ handlers that previously injected `ICurrentUser` + `ICurrentTenantContext` separately now inject `ICurrentRequestContext` as a single dependency. Field naming convention: `_requestContext`.

## Rules

- Application handlers must not inject `ICurrentTenantContext` directly
- Tenant runtime services, pipeline behaviors, DbContext/RLS services, and infrastructure tenant scopes may use `ICurrentTenantContext`
- System handlers (no user context) may omit `ICurrentRequestContext` entirely
