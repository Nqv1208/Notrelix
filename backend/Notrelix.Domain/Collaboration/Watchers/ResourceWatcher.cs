using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Watchers;

public enum WatchLevel
{
    All,
    MentionsOnly,
    None
}

public class ResourceWatcher : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public ResourceRef Target { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public WatchLevel Level { get; private set; }

    private ResourceWatcher() : base() { }

    public static ResourceWatcher Create(Guid workspaceId, ResourceRef target, Guid userId, DateTimeOffset createdAt, WatchLevel level = WatchLevel.All)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(target);
        Guard.NotEmpty(userId);

        var watcher = new ResourceWatcher
        {
            WorkspaceId = workspaceId,
            Target = target,
            UserId = userId,
            Level = level
        };

        watcher.AddDomainEvent(new ResourceWatchedEvent(workspaceId, watcher.Id, target, userId, createdAt));
        return watcher;
    }
}
