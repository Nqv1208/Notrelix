# Tenant Isolation

## Architecture

Tenant isolation is enforced at multiple layers:

1. **DB-level RLS** — Row-Level Security policies filter all queries by `account_id` and `workspace_id`
2. **Pipeline-level** — `DbRequestScopeBehavior` sets the tenant context before any handler executes
3. **Application-level** — `ICurrentRequestContext` provides typed access to current user, account, and workspace

## RLS (Row-Level Security)

PostgreSQL RLS policies are applied to all tenant-scoped tables. The `DbRequestScopeBehavior` sets session variables (`app.current_account_id`, `app.current_workspace_id`, `app.current_user_id`) before opening a DbContext scope.

RLS policies enforce that:
- Users can only see rows belonging to their account
- Workspace members can only see rows in their workspaces
- System context bypasses RLS for migration/cron operations

## Application Layer

Handlers access tenant context through `ICurrentRequestContext`:

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

A facade wrapping `ICurrentUser` + `ICurrentTenantContext`.

## Rules

- Application handlers must not inject `ICurrentTenantContext` directly — use `ICurrentRequestContext`
- Tenant runtime services, pipeline behaviors, DbContext/RLS services, and infrastructure tenant scopes may use `ICurrentTenantContext`
- Workspace-scoped queries must include workspace filter in the database query (not just rely on RLS)
