using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Activity;

public class ActivityLog : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public Guid ActorId { get; private set; }
    public ActivityType Type { get; private set; }
    public ResourceRef Target { get; private set; } = null!;
    public ActivityMetadata Metadata { get; private set; } = null!;
    public DateTimeOffset Timestamp { get; private set; }

    private ActivityLog() : base() { }

    public static ActivityLog Record(
        Guid workspaceId, 
        Guid actorId, 
        ActivityType type, 
        ResourceRef target, 
        ActivityMetadata? metadata = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(actorId);
        Guard.NotNull(target);

        return new ActivityLog
        {
            WorkspaceId = workspaceId,
            ActorId = actorId,
            Type = type,
            Target = target,
            Metadata = metadata ?? ActivityMetadata.Empty(),
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
