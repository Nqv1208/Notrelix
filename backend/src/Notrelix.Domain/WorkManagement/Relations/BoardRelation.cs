namespace Notrelix.Domain.WorkManagement.Relations;

public class BoardRelation : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid SourceBoardId { get; private set; }
    public Guid TargetBoardId { get; private set; }
    public Guid? SourceFieldId { get; private set; }
    public Guid? TargetFieldId { get; private set; }
    public BoardRelationType RelationType { get; private set; } = BoardRelationType.ConnectBoards;
    public RelationDirection Direction { get; private set; } = RelationDirection.TwoWay;
    public BoardRelationSyncMode SyncMode { get; private set; } = BoardRelationSyncMode.Manual;
    public BoardRelationStatus Status { get; private set; } = BoardRelationStatus.Active;
    public string ConfigJson { get; private set; } = "{}";

    private BoardRelation() : base() { }

    private static string ValidateJson(string? value)
    {
        var json = value ?? "{}";
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
        }
        catch (System.Text.Json.JsonException)
        {
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_FieldSettings_InvalidJsonFormat, "ConfigJson must be valid JSON.");
        }
        return json;
    }

    public static BoardRelation Create(
        Guid accountId,
        Guid workspaceId,
        Guid sourceBoardId,
        Guid targetBoardId,
        Guid? sourceFieldId,
        Guid? targetFieldId,
        Guid createdBy,
        DateTimeOffset createdAt,
        BoardRelationType relationType = BoardRelationType.ConnectBoards,
        RelationDirection direction = RelationDirection.TwoWay,
        BoardRelationSyncMode syncMode = BoardRelationSyncMode.Manual,
        string? configJson = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(sourceBoardId);
        Guard.NotEmpty(targetBoardId);

        if (sourceBoardId == targetBoardId)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Relation_CannotCreateSelfReferencing, "Cannot create a relation from a board to itself.");

        Guard.NotEmpty(accountId);

        var relation = new BoardRelation
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            SourceBoardId = sourceBoardId,
            TargetBoardId = targetBoardId,
            SourceFieldId = sourceFieldId,
            TargetFieldId = targetFieldId,
            RelationType = relationType,
            Direction = direction,
            SyncMode = syncMode,
            ConfigJson = ValidateJson(configJson),
            Status = BoardRelationStatus.Active
        };

        relation.SetAuditOnCreate(createdBy, createdAt);
        relation.RaiseDomainEvent(new BoardRelationCreatedDomainEvent(accountId, workspaceId, relation.Id, sourceBoardId, targetBoardId, createdBy, createdAt));
        return relation;
    }

    public void Pause(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == BoardRelationStatus.Paused) return;
        Status = BoardRelationStatus.Paused;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new BoardRelationPausedDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void Resume(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == BoardRelationStatus.Active) return;
        Status = BoardRelationStatus.Active;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new BoardRelationResumedDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void MarkBroken(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == BoardRelationStatus.Broken) return;
        Status = BoardRelationStatus.Broken;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new BoardRelationMarkedBrokenDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        Status = BoardRelationStatus.Deleted;
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new BoardRelationDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        Status = BoardRelationStatus.Active;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new BoardRelationRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
