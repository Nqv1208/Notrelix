namespace Notrelix.Domain.WorkManagement.Relations;

public class BoardItemConnection : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid RelationId { get; private set; }
    public Guid SourceBoardId { get; private set; }
    public Guid SourceItemId { get; private set; }
    public Guid TargetBoardId { get; private set; }
    public Guid TargetItemId { get; private set; }
    public BoardItemSyncStatus SyncStatus { get; private set; } = BoardItemSyncStatus.InSync;
    public string MetadataJson { get; private set; } = "{}";
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private BoardItemConnection() : base() { }

    public static BoardItemConnection Create(
        Guid accountId,
        Guid workspaceId,
        Guid relationId,
        Guid sourceBoardId,
        Guid sourceItemId,
        Guid targetBoardId,
        Guid targetItemId,
        Guid? createdBy,
        DateTimeOffset createdAt,
        BoardItemSyncStatus syncStatus = BoardItemSyncStatus.InSync,
        string? metadataJson = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(relationId);
        Guard.NotEmpty(sourceBoardId);
        Guard.NotEmpty(sourceItemId);
        Guard.NotEmpty(targetBoardId);
        Guard.NotEmpty(targetItemId);

        if (sourceItemId == targetItemId)
            throw new BusinessRuleException("Cannot connect an item to itself.");

        Guard.NotEmpty(accountId);

        return new BoardItemConnection
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            RelationId = relationId,
            SourceBoardId = sourceBoardId,
            SourceItemId = sourceItemId,
            TargetBoardId = targetBoardId,
            TargetItemId = targetItemId,
            SyncStatus = syncStatus,
            MetadataJson = metadataJson ?? "{}",
            CreatedBy = createdBy,
            CreatedAt = createdAt
        };
    }

    public void UpdateSyncStatus(BoardItemSyncStatus status, DateTimeOffset updatedAt)
    {
        SyncStatus = status;
        UpdatedAt = updatedAt;
    }
}
