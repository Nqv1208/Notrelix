namespace Notrelix.Domain.Collaboration.Watchers;

public enum WatchLevel
{
    All,
    MentionsOnly,
    None
}

public class ResourceWatcher : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public ResourceRef Target { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public WatchLevel Level { get; private set; }

    private ResourceWatcher() : base() { }

    public static ResourceWatcher Create(Guid accountId, Guid workspaceId, ResourceRef target, Guid userId, Guid createdBy, DateTimeOffset createdAt, WatchLevel level = WatchLevel.All)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(target);
        Guard.NotEmpty(userId);
        Guard.NotEmpty(createdBy);

        if (target.WorkspaceId.HasValue && target.WorkspaceId.Value != workspaceId)
            throw new WorkspaceMismatchException(workspaceId, target.WorkspaceId.Value);

        var watcher = new ResourceWatcher
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Target = target,
            UserId = userId,
            Level = level
        };

        watcher.SetAuditOnCreate(createdBy, createdAt);
        watcher.RaiseDomainEvent(new ResourceWatchedDomainEvent(accountId, workspaceId, watcher.Id, target, userId, createdAt));
        return watcher;
    }

    public void Unwatch(Guid unwatchedBy, DateTimeOffset removedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(unwatchedBy);

        base.SoftDelete(unwatchedBy, removedAt);
        SetAuditOnUpdate(unwatchedBy, removedAt);
        IncrementVersion();
        RaiseDomainEvent(new ResourceUnwatchedDomainEvent(AccountId, WorkspaceId, Id, removedAt));
    }
}
