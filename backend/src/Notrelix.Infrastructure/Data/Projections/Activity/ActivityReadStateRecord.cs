namespace Notrelix.Infrastructure.Data.Projections.Activity;

public sealed class ActivityReadStateRecord
{
    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset LastReadAt { get; private set; }

    private ActivityReadStateRecord() { }

    public ActivityReadStateRecord(Guid workspaceId, Guid userId, DateTimeOffset lastReadAt)
    {
        Id = Guid.CreateVersion7();
        WorkspaceId = workspaceId;
        UserId = userId;
        LastReadAt = lastReadAt;
    }
}
