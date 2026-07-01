namespace Notrelix.Domain.WorkManagement.Relations;

public class MirrorValueSnapshot : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid RelationId { get; private set; }
    public Guid ConnectionId { get; private set; }
    public Guid SourceFieldId { get; private set; }
    public Guid? MirroredFieldId { get; private set; }
    public string? ValueJson { get; private set; }
    public string? ValueHash { get; private set; }
    public bool IsStale { get; private set; }
    public DateTimeOffset ComputedAt { get; private set; }

    private MirrorValueSnapshot() : base() { }

    public static MirrorValueSnapshot Create(
        Guid accountId,
        Guid workspaceId,
        Guid relationId,
        Guid connectionId,
        Guid sourceFieldId,
        Guid? mirroredFieldId,
        string? valueJson,
        string? valueHash,
        DateTimeOffset computedAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(relationId);
        Guard.NotEmpty(connectionId);
        Guard.NotEmpty(sourceFieldId);
        Guard.NotEmpty(accountId);

        return new MirrorValueSnapshot
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            RelationId = relationId,
            ConnectionId = connectionId,
            SourceFieldId = sourceFieldId,
            MirroredFieldId = mirroredFieldId,
            ValueJson = valueJson,
            ValueHash = valueHash,
            IsStale = false,
            ComputedAt = computedAt
        };
    }

    public void MarkStale()
    {
        IsStale = true;
    }

    public void UpdateValue(string? valueJson, string? valueHash, DateTimeOffset computedAt)
    {
        ValueJson = valueJson;
        ValueHash = valueHash;
        IsStale = false;
        ComputedAt = computedAt;
    }
}
