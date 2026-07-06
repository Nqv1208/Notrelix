namespace Notrelix.Domain.Common;

/// <summary>
/// Base for events of resources that live within a workspace boundary.
/// Examples: BoardCreated, CommentCreated, SubscriptionStarted.
/// Implements IWorkspaceScoped because the resource is workspace-scoped.
/// </summary>
public abstract record WorkspaceScopedDomainEvent : DomainEvent, IWorkspaceScoped
{
    public Guid AccountId { get; }
    public Guid WorkspaceId { get; }

    protected WorkspaceScopedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        DateTimeOffset occurredAt,
        Guid? actorUserId = null,
        string? correlationId = null,
        string? causationId = null)
        : base(occurredAt, workspaceId, actorUserId)
    {
        AccountId = accountId;
        WorkspaceId = workspaceId;
        CorrelationId = correlationId;
        CausationId = causationId;
    }
}
