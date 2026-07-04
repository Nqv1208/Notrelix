namespace Notrelix.Application.Common.Context;

/// <summary>
/// Unified execution context for the current request/operation.
/// Replaces loose ICurrentUser + ICurrentTenantContext + ICorrelationContext usage.
/// All properties are nullable because not every request carries all values.
/// </summary>
public interface IExecutionContext
{
    // User
    Guid? UserId { get; }
    string? Email { get; }
    string? Name { get; }
    bool IsAuthenticated { get; }

    // Tenant
    Guid? AccountId { get; }
    Guid? WorkspaceId { get; }
    bool IsSystemContext { get; }

    // Correlation
    Guid CorrelationId { get; }
    Guid? CausationId { get; }

    // State queries
    bool IsResolved { get; }

    // Setters
    void SetUser(Guid userId, string email, string name);
    void SetTenant(Guid accountId, Guid workspaceId);
    void SetAccount(Guid accountId);
    void SetCorrelation(Guid correlationId, Guid? causationId = null);
    void SetSystem();
    void Clear();

    // Require helpers (throw if not set)
    Guid RequireUserId();
    Guid RequireAccountId();
    Guid RequireWorkspaceId();
}
