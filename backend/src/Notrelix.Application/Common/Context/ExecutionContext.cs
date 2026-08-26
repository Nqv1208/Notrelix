namespace Notrelix.Application.Common.Context;

public sealed class ExecutionContext : IExecutionContextAccessor
{
    private Guid? _seedUserId;
    private Guid? _seedAccountId;
    private Guid? _seedWorkspaceId;

    public ExecutionContextSnapshot? Snapshot { get; private set; }

    // User
    public Guid? UserId => Snapshot?.UserId ?? _seedUserId;
    public string? Email { get; private set; }
    public string? Name { get; private set; }
    public bool IsAuthenticated => UserId.HasValue && UserId.Value != Guid.Empty;

    // Tenant
    public Guid? AccountId => Snapshot?.AccountId ?? _seedAccountId;
    public Guid? WorkspaceId => Snapshot?.WorkspaceId ?? _seedWorkspaceId;
    public bool IsSystemContext { get; private set; }

    // Correlation
    public Guid CorrelationId { get; private set; } = Guid.NewGuid();
    public Guid? CausationId { get; private set; }

    // State
    public bool IsResolved => Snapshot is not null || UserId.HasValue || IsSystemContext;

    public void SetSnapshot(ExecutionContextSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (Snapshot is not null)
        {
            throw new InvalidOperationException("Execution context snapshot has already been resolved.");
        }

        Snapshot = snapshot;
    }

    public void SetUser(Guid userId, string email, string name)
    {
        _seedUserId = userId;
        Email = email;
        Name = name;
    }

    public void SetTenant(Guid accountId, Guid workspaceId)
    {
        _seedAccountId = accountId;
        _seedWorkspaceId = workspaceId;
    }

    public void SetAccount(Guid accountId)
    {
        _seedAccountId = accountId;
        _seedWorkspaceId = null;
    }

    public void SetCorrelation(Guid correlationId, Guid? causationId = null)
    {
        CorrelationId = correlationId;
        CausationId = causationId;
    }

    public void SetSystem()
    {
        IsSystemContext = true;
    }

    public void Clear()
    {
        _seedUserId = null;
        Email = null;
        Name = null;
        _seedAccountId = null;
        _seedWorkspaceId = null;
        IsSystemContext = false;
        Snapshot = null;
    }

    public Guid RequireUserId()
    {
        if (!IsAuthenticated)
            throw new UnauthorizedAccessException("User context is required but not set.");
        return UserId!.Value;
    }

    public Guid RequireAccountId()
    {
        if (!AccountId.HasValue || AccountId.Value == Guid.Empty)
            throw new InvalidOperationException("Account context is required but not set.");
        return AccountId.Value;
    }

    public Guid RequireWorkspaceId()
    {
        if (!WorkspaceId.HasValue || WorkspaceId.Value == Guid.Empty)
            throw new InvalidOperationException("Workspace context is required but not set.");
        return WorkspaceId.Value;
    }
}
