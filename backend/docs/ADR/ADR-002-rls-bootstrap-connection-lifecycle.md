# ADR-002: RLS Bootstrap Connection Lifecycle

## Status

Accepted

## Context

Row-Level Security (RLS) in PostgreSQL requires session-level context variables (`app.current_user_id`, `app.current_account_id`, etc.) to be set via `set_config()` before queries execute. These variables are read by RLS policy functions to determine row visibility.

The challenge: `TenantBootstrapBehavior` runs early in the pipeline (zone: Outer) to resolve workspace access. At this point, the full RLS context cannot be set because AccountId and WorkspaceId are not yet known — they're being resolved by the bootstrap itself.

Previously, the bootstrap query ran without any RLS session context. While `IgnoreQueryFilters()` bypasses EF Core's global query filters, the PostgreSQL RLS policies still evaluate. Without `app.current_user_id` set, RLS policies that check user membership would see an empty string and deny access.

## Decision

`TenantBootstrapStore` owns the physical connection lifecycle for bootstrap queries:

1. **Get the physical NpgsqlConnection** via `ApplicationDbContext.Database.GetDbConnection()` — same scoped DbContext instance used by the rest of the request.

2. **Open the connection** if not already open. EF Core detects `State == Open` and reuses it.

3. **Set minimal RLS session context** before the bootstrap query:
   - `app.current_user_id` → the authenticated user's ID
   - `app.request_scope` → `"app"` (not a worker/system request)
   - `app.correlation_id` → from `Activity.Current` for distributed tracing

   Note: `app.current_account_id` and `app.current_workspace_id` are NOT set here because they're unknown at bootstrap time. The bootstrap query uses `IgnoreQueryFilters()` to bypass EF Core filters, and the RLS policies check `current_user_id` for user-level access.

4. **The bootstrap query runs** with minimal RLS context. Permission evaluation via `IPermissionEvaluator` runs on the same connection with the same session context.

5. **Later in the pipeline**, `DbRequestScopeBehavior` calls `RlsSessionContext.ApplyAsync()` which overwrites ALL session variables with the full context (including AccountId and WorkspaceId) using `is_local=true` (transaction-local). This is safe because:
   - Same physical connection (scoped DbContext)
   - Transaction-local settings override session settings
   - Npgsql pool reset (`DISCARD ALL`) cleans up on connection return

## Consequences

- Bootstrap queries now have `app.current_user_id` set, enabling user-level RLS policy evaluation
- The connection is guaranteed open before any bootstrap query, preventing `NpgsqlOperationCanceledException` on lazy-open
- No additional abstraction needed (`IRlsContextInitializer` rejected) — `TenantBootstrapStore` directly owns the connection
- The connection lifecycle is: open → set_config → query → (later) full RLS context → transaction → commit → pool return

## Related

- `TenantBootstrapStore.cs` — owns bootstrap connection lifecycle
- `RlsSessionContext.cs` — sets full RLS context later in the pipeline
- `DbRequestScopeBehavior.cs` — calls `RlsSessionContext.ApplyAsync()`
- `RlsPolicyApplier.cs` — existing reference for `GetDbConnection()` pattern
- ADR-001 Pipeline Boundary — documents zone model
