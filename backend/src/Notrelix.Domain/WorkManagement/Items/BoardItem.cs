namespace Notrelix.Domain.WorkManagement.Items;

public class BoardItem : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public Guid GroupId { get; private set; }
    public string Name { get; private set; } = null!;
    public FractionalIndex Position { get; private set; } = null!;
    public Guid? ParentItemId { get; private set; }
    public string? ItemKey { get; private set; }
    public long? ItemSequence { get; private set; }
    public int ItemLevel { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? DueAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public bool IsArchived { get; private set; }

    private readonly List<BoardItemValue> _fieldValues = new();
    public IReadOnlyCollection<BoardItemValue> FieldValues => _fieldValues.AsReadOnly();

    private BoardItem() : base() { }

    public static BoardItem Create(
        Guid accountId,
        Guid workspaceId,
        Guid boardId,
        Guid groupId,
        string name,
        FractionalIndex position,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? parentItemId = null,
        string? itemKey = null,
        long? itemSequence = null,
        int itemLevel = 0,
        DateTimeOffset? startedAt = null,
        DateTimeOffset? dueAt = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(groupId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 500);
        Guard.NotNull(position);
        Guard.NotEmpty(accountId);

        var item = new BoardItem
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            BoardId = boardId,
            GroupId = groupId,
            Name = name.Trim(),
            Position = position,
            ParentItemId = parentItemId,
            ItemKey = itemKey,
            ItemSequence = itemSequence,
            ItemLevel = itemLevel,
            StartedAt = startedAt,
            DueAt = dueAt
        };

        item.SetAuditOnCreate(createdBy, createdAt);
        item.AddDomainEvent(new BoardItemCreatedDomainEvent(accountId, workspaceId, boardId, groupId, item.Id, item.Name, createdBy, createdAt));

        return item;
    }

    public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 500);

        var oldName = Name;
        var normalizedName = name.Trim();
        if (Name == normalizedName) return;

        Name = normalizedName;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new BoardItemRenamedDomainEvent(AccountId, WorkspaceId, Id, BoardId, oldName, Name, updatedBy, updatedAt));
    }

    public void MoveToGroup(BoardGroupRef group, FractionalIndex newPosition, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(group);
        Guard.NotNull(newPosition);

        if (group.WorkspaceId != WorkspaceId)
            throw new WorkspaceMismatchException(WorkspaceId, group.WorkspaceId);

        if (group.BoardId != BoardId)
            throw new BoardMismatchException(BoardId, group.BoardId);

        var oldGroupId = GroupId;
        if (GroupId == group.GroupId && Position == newPosition) return;

        GroupId = group.GroupId;
        Position = newPosition;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new BoardItemMovedDomainEvent(AccountId, WorkspaceId, Id, BoardId, oldGroupId, group.GroupId, newPosition.Value, updatedBy, updatedAt));
    }

    public void UpdateFieldValue(BoardField field, FieldValue newValue, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(field);
        Guard.NotNull(newValue);

        if (field.WorkspaceId != WorkspaceId)
            throw new BusinessRuleException("Field belongs to a different workspace.");

        if (field.BoardId != BoardId)
            throw new BusinessRuleException("Field does not belong to this board.");

        if (field.IsDeleted)
            throw new BusinessRuleException("Cannot update value for a deleted field.");

        if (field.IsSystem)
            throw new BusinessRuleException("Cannot update a system field.");

        FieldValueValidator.Validate(newValue, field.Type, field.Settings);

        if (field.Type is FieldType.Select or FieldType.Status)
        {
            var optionId = newValue.Data.Value.Trim('"');
            if (!field.Options.Any(o => o.Id.ToString() == optionId))
                throw new BusinessRuleException($"Value '{optionId}' is not a valid option for field '{field.Name}'.");
        }
        else if (field.Type == FieldType.MultiSelect)
        {
            // MultiSelect: validate each value is a valid option
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(newValue.Data.Value);
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    var optionId = element.GetString();
                    if (!field.Options.Any(o => o.Id.ToString() == optionId))
                        throw new BusinessRuleException($"Value '{optionId}' is not a valid option for field '{field.Name}'.");
                }
            }
            catch
            {
                throw new BusinessRuleException($"Value for field type {field.Type} must be an array of option IDs.");
            }
        }

        var existingValue = _fieldValues.FirstOrDefault(fv => fv.FieldId == field.Id);
        var oldValue = existingValue?.Value ?? FieldValue.Empty();

        if (existingValue == null)
        {
            _fieldValues.Add(BoardItemValue.Create(Id, field.Id, newValue));
        }
        else
        {
            if (existingValue.Value == newValue) return;
            existingValue.UpdateValue(newValue);
        }

        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new BoardItemFieldValueChangedDomainEvent(AccountId, WorkspaceId, Id, BoardId, field.Id, oldValue, newValue, updatedBy, updatedAt));
    }

    public void AssignParentItem(Guid? parentItemId, int itemLevel, Func<Guid, (Guid BoardId, Guid?)> getParentInfo, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();

        if (parentItemId.HasValue)
        {
            var parentInfo = getParentInfo(parentItemId.Value);
            if (parentInfo.BoardId != BoardId)
                throw new BusinessRuleException("Parent item must belong to the same board.");
        }

        BoardItemRules.EnsureNoCycle(Id, parentItemId, id => getParentInfo(id).Item2);

        ParentItemId = parentItemId;
        ItemLevel = itemLevel;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new BoardItemParentAssignedDomainEvent(AccountId, WorkspaceId, BoardId, Id, parentItemId, itemLevel, updatedBy, updatedAt));
    }

    public void SetTimeline(DateTimeOffset? startedAt, DateTimeOffset? dueAt, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (startedAt != null && dueAt != null && dueAt < startedAt)
            throw new BusinessRuleException("Due date must be after start date.");

        if (StartedAt == startedAt && DueAt == dueAt) return;
        StartedAt = startedAt;
        DueAt = dueAt;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new BoardItemTimelineSetDomainEvent(AccountId, WorkspaceId, BoardId, Id, startedAt, dueAt, updatedBy, updatedAt));
    }

    public void Complete(DateTimeOffset? completedAt, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (CompletedAt == completedAt) return;
        CompletedAt = completedAt;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new BoardItemCompletedDomainEvent(AccountId, WorkspaceId, BoardId, Id, completedAt, updatedBy, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new BoardItemSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, BoardId, deletedBy, deletedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        if (IsArchived) return;
        IsArchived = true;
        SetAuditOnUpdate(archivedBy, archivedAt);
        IncrementVersion();
        AddDomainEvent(new BoardItemArchivedDomainEvent(AccountId, WorkspaceId, BoardId, Id, archivedBy, archivedAt));
    }

    public void Unarchive(Guid unarchivedBy, DateTimeOffset unarchivedAt)
    {
        EnsureNotDeleted();
        if (!IsArchived) return;
        IsArchived = false;
        SetAuditOnUpdate(unarchivedBy, unarchivedAt);
        IncrementVersion();
        AddDomainEvent(new BoardItemUnarchivedDomainEvent(AccountId, WorkspaceId, BoardId, Id, unarchivedBy, unarchivedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new BoardItemRestoredDomainEvent(AccountId, WorkspaceId, Id, BoardId, restoredBy, restoredAt));
    }
}
