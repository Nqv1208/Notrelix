namespace Notrelix.Domain.Common;

/// <summary>
/// Base for lifecycle events of the Workspace aggregate itself.
/// Examples: WorkspaceCreated, WorkspaceRenamed, WorkspaceArchived.
/// Carries WorkspaceId for correlation/audit but does NOT implement IWorkspaceScoped
/// because the Workspace is the tenant root, not a resource within a workspace.
/// </summary>
public abstract record WorkspaceRootDomainEvent : DomainEvent
{
    public Guid WorkspaceId { get; }

    protected WorkspaceRootDomainEvent(
        Guid workspaceId,
        DateTimeOffset occurredAt,
        Guid? actorUserId = null,
        string? correlationId = null,
        string? causationId = null)
        : base(occurredAt, workspaceId, actorUserId)
    {
        WorkspaceId = workspaceId;
        CorrelationId = correlationId;
        CausationId = causationId;
    }
}
