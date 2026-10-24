namespace Notrelix.Application.Common.Data.Rls;

/// <summary>
/// Applies Row-Level Security session context for tenant isolation.
/// Infrastructure owns the database-specific implementation.
/// </summary>
public interface IRlsSessionContext
{
    /// <summary>Applies the full tenant context (user, account, workspace) of the current request.</summary>
    Task ApplyAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Applies the <c>user</c> scope: the authenticated user and nothing else. Used by Global requests, the
    /// resource locator and bootstrap lookups, which run before (or without) an account or workspace.
    /// Requires an open transaction; the scope ends with it. A no-op when session context is disabled.
    /// </summary>
    Task ApplyUserScopeAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Enters a <c>token</c> scope bound to the account and workspace the database derives from the
    /// presented token digest. Returns the derived binding, or <c>null</c> when no token matches (no scope is
    /// entered then). Requires an open transaction. The entry is logged and counted with its reason; the
    /// token value is never logged.
    /// </summary>
    Task<TokenScopeBinding?> EnterTokenScopeAsync(TokenScopeRequest request, CancellationToken cancellationToken);
}

/// <summary>What a presented one-time token is for; selects the table the database looks the digest up in.</summary>
public enum TokenScopePurpose
{
    WorkspaceInvitation,
}

/// <summary>A token digest presented for a bounded scope. <paramref name="TokenHash"/> is the digest, never the raw token.</summary>
public sealed record TokenScopeRequest(
    TokenScopePurpose Purpose,
    string TokenHash,
    int HashVersion,
    SystemOperationReason Reason);

/// <summary>The account and workspace a token scope was bound to by the database.</summary>
public sealed record TokenScopeBinding(Guid AccountId, Guid WorkspaceId);
