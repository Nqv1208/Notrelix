using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.WorkManagement.Relations;

public class BoardRelation : SoftDeletableEntity, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid SourceBoardId { get; private set; }
    public Guid TargetBoardId { get; private set; }
    public Guid? SourceFieldId { get; private set; }
    public Guid? TargetFieldId { get; private set; }
    public BoardRelationType RelationType { get; private set; } = BoardRelationType.ConnectBoards;
    public BoardRelationDirection Direction { get; private set; } = BoardRelationDirection.TwoWay;
    public BoardRelationSyncMode SyncMode { get; private set; } = BoardRelationSyncMode.Manual;
    public BoardRelationStatus Status { get; private set; } = BoardRelationStatus.Active;
    public string ConfigJson { get; private set; } = "{}";
    public long Version { get; private set; } = 1;

    private BoardRelation() : base() { }

    public static BoardRelation Create(
        Guid workspaceId,
        Guid sourceBoardId,
        Guid targetBoardId,
        Guid? sourceFieldId,
        Guid? targetFieldId,
        Guid createdBy,
        DateTimeOffset createdAt,
        BoardRelationType relationType = BoardRelationType.ConnectBoards,
        BoardRelationDirection direction = BoardRelationDirection.TwoWay,
        BoardRelationSyncMode syncMode = BoardRelationSyncMode.Manual,
        string? configJson = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(sourceBoardId);
        Guard.NotEmpty(targetBoardId);

        if (sourceBoardId == targetBoardId)
            throw new BusinessRuleException("Cannot create a relation from a board to itself.");

        var relation = new BoardRelation
        {
            WorkspaceId = workspaceId,
            SourceBoardId = sourceBoardId,
            TargetBoardId = targetBoardId,
            SourceFieldId = sourceFieldId,
            TargetFieldId = targetFieldId,
            RelationType = relationType,
            Direction = direction,
            SyncMode = syncMode,
            ConfigJson = configJson ?? "{}",
            Status = BoardRelationStatus.Active
        };

        relation.SetAuditOnCreate(createdBy, createdAt);
        return relation;
    }

    public void Pause()
    {
        EnsureNotDeleted();
        Status = BoardRelationStatus.Paused;
        Version++;
    }

    public void Resume()
    {
        EnsureNotDeleted();
        Status = BoardRelationStatus.Active;
        Version++;
    }

    public void MarkBroken()
    {
        EnsureNotDeleted();
        Status = BoardRelationStatus.Broken;
        Version++;
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        Status = BoardRelationStatus.Deleted;
    }
}
