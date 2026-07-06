namespace Notrelix.Application.Common.Context;

public sealed class ExecutionContext : IExecutionContextAccessor
{
    // User
    public Guid? UserId { get; private set; }
    public string? Email { get; private set; }
    public string? Name { get; private set; }
    public bool IsAuthenticated => UserId.HasValue && UserId.Value != Guid.Empty;

    // Tenant
    public Guid? AccountId { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public bool IsSystemContext { get; private set; }

    // Correlation
    public Guid CorrelationId { get; private set; } = Guid.NewGuid();
    public Guid? CausationId { get; private set; }

    // State
    public bool IsResolved => UserId.HasValue || IsSystemContext;

    public void SetUser(Guid userId, string email, string name)
    {
        UserId = userId;
        Email = email;
        Name = name;
    }

    public void SetTenant(Guid accountId, Guid workspaceId)
    {
        AccountId = accountId;
        WorkspaceId = workspaceId;
    }

    public void SetAccount(Guid accountId)
    {
        AccountId = accountId;
        WorkspaceId = null;
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
        UserId = null;
        Email = null;
        Name = null;
        AccountId = null;
        WorkspaceId = null;
        IsSystemContext = false;
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
