namespace Notrelix.Application.Common.Context;

/// <summary>
/// Canonical tenant context for the current request/operation.
/// Replaces loose ICurrentWorkspace + ICurrentAccount usage.
/// Properties are nullable because not every request carries all three.
/// Use Require*() methods in workspace-scoped handlers to get non-null values.
/// </summary>
public interface ICurrentTenantContext
{
    Guid? AccountId { get; }
    Guid? WorkspaceId { get; }
    Guid? UserId { get; }

    bool IsSystemContext { get; }
    bool IsResolved { get; }

    /// <summary>
    /// Returns AccountId or throws if not set.
    /// Use in handlers that require account context.
    /// </summary>
    Guid RequireAccountId();

    /// <summary>
    /// Returns WorkspaceId or throws if not set.
    /// Use in workspace-scoped handlers.
    /// </summary>
    Guid RequireWorkspaceId();

    /// <summary>
    /// Returns UserId or throws if not set.
    /// Use in handlers that require authenticated user.
    /// </summary>
    Guid RequireUserId();

    /// <summary>
    /// Seed user identity from JWT without setting account/workspace.
    /// Called by HttpRequestContextMiddleware before pipeline behaviors run.
    /// TenantBootstrapBehavior will later resolve account/workspace context.
    /// </summary>
    void SetUser(Guid userId);

    /// <summary>
    /// Seed account context from header/route without clearing workspace.
    /// Called by HttpRequestContextMiddleware before pipeline behaviors run.
    /// TenantBootstrapBehavior will verify access and call SetAccount/SetWorkspace.
    /// </summary>
    void SetAccountHint(Guid accountId);

    /// <summary>
    /// Set account-level context (no workspace).
    /// For account commands, login, register, webhooks.
    /// </summary>
    void SetAccount(Guid accountId, Guid? userId);

    /// <summary>
    /// Set workspace-scoped context.
    /// For workspace-scoped commands.
    /// </summary>
    void SetWorkspace(Guid accountId, Guid workspaceId, Guid? userId);

    /// <summary>
    /// Set system/worker context (bypasses tenant filters).
    /// For background jobs, migrations, system operations.
    /// </summary>
    void SetSystem();

    /// <summary>
    /// Clear all context.
    /// </summary>
    void Clear();
}