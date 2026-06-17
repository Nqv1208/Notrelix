using Notrelix.Domain.WorkManagement.Relations.Events;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.WorkManagement.Relations.Events;

namespace Notrelix.Domain.WorkManagement.Relations;

public class BoardRelation : AggregateRoot, IWorkspaceScoped
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
        relation.AddDomainEvent(new BoardRelationCreatedDomainEvent(workspaceId, relation.Id, sourceBoardId, targetBoardId, createdBy, createdAt));
        return relation;
    }

    public void Pause(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == BoardRelationStatus.Paused) return;
        Status = BoardRelationStatus.Paused;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new BoardRelationPausedDomainEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void Resume(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == BoardRelationStatus.Active) return;
        Status = BoardRelationStatus.Active;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new BoardRelationResumedDomainEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void MarkBroken(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == BoardRelationStatus.Broken) return;
        Status = BoardRelationStatus.Broken;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new BoardRelationMarkedBrokenDomainEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        Status = BoardRelationStatus.Deleted;
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new BoardRelationDeletedDomainEvent(WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        Status = BoardRelationStatus.Active;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new BoardRelationRestoredDomainEvent(WorkspaceId, Id, restoredBy, restoredAt));
    }
}
