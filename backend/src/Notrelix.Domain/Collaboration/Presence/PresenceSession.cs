namespace Notrelix.Domain.Collaboration.Presence;

public class PresenceSession : Entity, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public string? ConnectionId { get; private set; }
    public PresenceStatus Status { get; private set; }
    public DateTimeOffset LastSeenAt { get; private set; }

    private PresenceSession() : base() { }

    public static PresenceSession Create(Guid workspaceId, Guid userId, DateTimeOffset lastSeenAt, string? connectionId = null)
    {
        return new PresenceSession
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            ConnectionId = connectionId,
            Status = PresenceStatus.Online,
            LastSeenAt = lastSeenAt
        };
    }
}
