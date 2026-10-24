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

    /// <summary>
    /// Whether this host sets database session context at all. Bounded scopes are entered only when it is
    /// true; when it is false (an unconstrained connection, in-memory hosts) callers keep their unscoped
    /// lookups.
    /// </summary>
    bool IsSessionContextEnabled { get; }

    /// <summary>The bounded scope this request last entered through this context, or <see cref="RlsBoundedScope.None"/>.</summary>
    RlsBoundedScope ActiveBoundedScope { get; }

    /// <summary>
    /// Binds the person an invitation is accepted by, inside the token scope already entered for that
    /// invitation. Returns false (nothing is bound) when the invitation is not pending and unexpired or its
    /// address is not the person's. Requires an open transaction.
    /// </summary>
    Task<bool> EnterInviteeScopeAsync(Guid userId, SystemOperationReason reason, CancellationToken cancellationToken);

    /// <summary>
    /// Enters the registration scope for one brand-new user (no user with this id exists). Inside a token
    /// scope for a pending invitation the registration also carries the invitation binding. Returns false
    /// when the scope cannot be entered. Requires an open transaction.
    /// </summary>
    Task<bool> EnterRegistrationScopeAsync(Guid userId, SystemOperationReason reason, CancellationToken cancellationToken);
}

/// <summary>The bounded database scopes a request can enter; each ends with its transaction.</summary>
public enum RlsBoundedScope
{
    None,
    User,
    Token,
    Invitee,
    Registration,
    Credential,
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
