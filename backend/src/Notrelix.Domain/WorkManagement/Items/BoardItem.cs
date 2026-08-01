using Notrelix.Domain.WorkManagement.Items.Events;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Fields;
using System.Text.Json;

namespace Notrelix.Domain.WorkManagement.Items;

public class BoardItem : SoftDeletableAggregateRoot, IWorkspaceScoped
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

    public static BoardItem CreateRoot(
        Guid accountId,
        Guid workspaceId,
        Guid boardId,
        Guid groupId,
        string name,
        FractionalIndex position,
        Guid createdBy,
        DateTimeOffset createdAt,
        string? itemKey = null,
        long? itemSequence = null,
        DateTimeOffset? startedAt = null,
        DateTimeOffset? dueAt = null)
    {
        return CreateCore(
            accountId, workspaceId, boardId, groupId, name, position, createdBy, createdAt,
            parentItemId: null, itemLevel: 0,
            itemKey, itemSequence, startedAt, dueAt);
    }

    public static BoardItem CreateChild(
        ItemParentPath parentPath,
        Guid groupId,
        string name,
        FractionalIndex position,
        Guid createdBy,
        DateTimeOffset createdAt,
        string? itemKey = null,
        long? itemSequence = null,
        DateTimeOffset? startedAt = null,
        DateTimeOffset? dueAt = null)
    {
        Guard.NotNull(parentPath);
        return CreateCore(
            parentPath.AccountId, parentPath.WorkspaceId, parentPath.BoardId, groupId, name, position, createdBy, createdAt,
            parentPath.ParentItemId, parentPath.ChildLevel,
            itemKey, itemSequence, startedAt, dueAt);
    }

    private static BoardItem CreateCore(
        Guid accountId,
        Guid workspaceId,
        Guid boardId,
        Guid groupId,
        string name,
        FractionalIndex position,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? parentItemId,
        int itemLevel,
        string? itemKey,
        long? itemSequence,
        DateTimeOffset? startedAt,
        DateTimeOffset? dueAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(groupId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 500);
        Guard.NotNull(position);
        Guard.NotEmpty(accountId);
        Guard.NotNegative(itemLevel);

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
        item.RaiseDomainEvent(new BoardItemCreatedDomainEvent(accountId, workspaceId, boardId, groupId, item.Id, item.Name, createdBy, createdAt, parentItemId, itemLevel));

        return item;
    }

    public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 500);

        var normalizedName = name.Trim();
        if (Name == normalizedName) return;

        var oldName = Name;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Name = normalizedName;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemRenamedDomainEvent(AccountId, WorkspaceId, Id, BoardId, oldName, Name, updatedBy, updatedAt));
    }

    public void MoveToGroup(BoardGroupRef group, FractionalIndex newPosition, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(group);
        Guard.NotNull(newPosition);

        if (group.WorkspaceId != WorkspaceId)
            throw new BusinessRuleException(CommonRuleCodes.Common_WorkspaceScopeMismatch, $"Workspace scope mismatch. Expected '{WorkspaceId}', got '{group.WorkspaceId}'.");

        if (group.BoardId != BoardId)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_BoardScopeMismatch, $"Board scope mismatch. Expected '{BoardId}', got '{group.BoardId}'.");

        if (GroupId == group.GroupId && Position == newPosition) return;

        var oldGroupId = GroupId;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        GroupId = group.GroupId;
        Position = newPosition;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemMovedDomainEvent(AccountId, WorkspaceId, Id, BoardId, oldGroupId, group.GroupId, newPosition.Value, updatedBy, updatedAt));
    }

    public void UpdateFieldValue(BoardField field, FieldValue newValue, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(field);
        Guard.NotNull(newValue);

        if (field.WorkspaceId != WorkspaceId)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_BelongsToDifferentWorkspace, "Field belongs to a different workspace.");

        if (field.BoardId != BoardId)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_DoesNotBelongToBoard, "Field does not belong to this board.");

        if (field.IsDeleted)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_CannotUpdateDeleted, "Cannot update value for a deleted field.");

        if (field.IsSystem)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_CannotUpdateSystem, "Cannot update a system field.");

        if (field.Type is FieldType.Formula or FieldType.Rollup)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_CannotWriteComputed, "Cannot manually write to a computed field (Formula or Rollup).");

        FieldValueValidator.Validate(newValue, field.Type, field.Settings);

        if (field.Type is FieldType.Select or FieldType.Status)
        {
            var optionId = ParseOptionId(newValue, WorkManagementRuleCodes.WorkManagement_Field_InvalidOptionValue);
            if (!field.Options.Any(option => option.Id == optionId))
                throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_InvalidOptionValue, $"Value '{optionId}' is not a valid option for field '{field.Name}'.");
        }
        else if (field.Type == FieldType.MultiSelect)
        {
            var selectedIds = ParseDistinctOptionIds(newValue);
            var allowedIds = field.Options.Select(option => option.Id).ToHashSet();
            if (!selectedIds.All(allowedIds.Contains))
                throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidMultiSelectValue, $"Value contains an option that is not valid for field '{field.Name}'.");
        }

        var existingValue = _fieldValues.FirstOrDefault(fv => fv.FieldId == field.Id);
        var oldValue = existingValue?.Value ?? FieldValue.Empty();

        if (existingValue != null && existingValue.Value == newValue)
            return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);

        if (existingValue == null)
        {
            _fieldValues.Add(BoardItemValue.Create(Id, field.Id, newValue));
        }
        else
        {
            existingValue.UpdateValue(newValue);
        }

        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemFieldValueChangedDomainEvent(AccountId, WorkspaceId, Id, BoardId, field.Id, oldValue, newValue, updatedBy, updatedAt));
    }

    private static Guid ParseOptionId(FieldValue value, string ruleCode)
    {
        var raw = value.Data.Value.Trim('"');
        if (!Guid.TryParse(raw, out var optionId))
            throw new BusinessRuleException(ruleCode, $"Value '{raw}' is not a valid option id.");
        return optionId;
    }

    private static IReadOnlyList<Guid> ParseDistinctOptionIds(FieldValue value)
    {
        using var msDoc = JsonDocument.Parse(value.Data.Value);
        if (msDoc.RootElement.ValueKind != JsonValueKind.Array)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidMultiSelectValue, "Value for field type MultiSelect must be an array of option IDs.");

        var ids = new List<Guid>();
        var seen = new HashSet<Guid>();
        foreach (var element in msDoc.RootElement.EnumerateArray())
        {
            var raw = element.GetString();
            if (!Guid.TryParse(raw, out var optionId))
                throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidMultiSelectValue, $"Value '{raw}' is not a valid option id.");

            if (!seen.Add(optionId))
                throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_DuplicateOptionId, $"Option id '{optionId}' appears more than once.");

            ids.Add(optionId);
        }

        return ids;
    }

    public void MoveToRoot(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);

        if (ParentItemId is null && ItemLevel == 0) return;

        var previousParentItemId = ParentItemId;
        var previousLevel = ItemLevel;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        ParentItemId = null;
        ItemLevel = 0;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemParentChangedDomainEvent(AccountId, WorkspaceId, BoardId, Id, previousParentItemId, null, previousLevel, 0, updatedBy, updatedAt));
    }

    public void MoveUnder(ItemParentPath parentPath, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(parentPath);

        if (parentPath.AccountId != AccountId)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_ParentMustBelongToSameAccount, $"Parent item must belong to the same account. Expected '{AccountId}', got '{parentPath.AccountId}'.");

        if (parentPath.WorkspaceId != WorkspaceId)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_ParentMustBelongToSameWorkspace, $"Parent item must belong to the same workspace. Expected '{WorkspaceId}', got '{parentPath.WorkspaceId}'.");

        if (parentPath.BoardId != BoardId)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_ParentMustBelongToSameBoard, $"Parent item must belong to the same board. Expected '{BoardId}', got '{parentPath.BoardId}'.");

        if (parentPath.ParentItemId == Id)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_CannotBeOwnParent, "An item cannot be its own parent.");

        if (parentPath.AncestorIds.Contains(Id))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_ParentAssignmentWouldCreateCycle, "Item parent assignment would create a cycle.");

        var newLevel = parentPath.ChildLevel;
        if (ParentItemId == parentPath.ParentItemId && ItemLevel == newLevel) return;

        var previousParentItemId = ParentItemId;
        var previousLevel = ItemLevel;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        ParentItemId = parentPath.ParentItemId;
        ItemLevel = newLevel;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemParentChangedDomainEvent(AccountId, WorkspaceId, BoardId, Id, previousParentItemId, ParentItemId, previousLevel, ItemLevel, updatedBy, updatedAt));
    }

    public void SetTimeline(DateTimeOffset? startedAt, DateTimeOffset? dueAt, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);
        if (startedAt != null && dueAt != null && dueAt < startedAt)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_DueDateMustBeAfterStartDate, "Due date must be after start date.");

        if (StartedAt == startedAt && DueAt == dueAt) return;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        StartedAt = startedAt;
        DueAt = dueAt;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemTimelineSetDomainEvent(AccountId, WorkspaceId, BoardId, Id, startedAt, dueAt, updatedBy, updatedAt));
    }

    public void Complete(DateTimeOffset completedAt, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);

        if (completedAt == default)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_CompletedAtRequired, "Completion timestamp must be provided.");

        if (CompletedAt.HasValue) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        CompletedAt = completedAt;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemCompletedDomainEvent(AccountId, WorkspaceId, BoardId, Id, completedAt, updatedBy, updatedAt));
    }

    public void Reopen(Guid reopenedBy, DateTimeOffset reopenedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(reopenedBy);

        if (!CompletedAt.HasValue) return;

        var pending = PrepareAuditUpdate(reopenedBy, reopenedAt);
        CompletedAt = null;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemReopenedDomainEvent(AccountId, WorkspaceId, BoardId, Id, reopenedBy, reopenedAt));
    }

    public void Delete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemDeletedDomainEvent(AccountId, WorkspaceId, Id, BoardId, deletedBy, deletedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(archivedBy);
        if (IsArchived) return;
        var pending = PrepareAuditUpdate(archivedBy, archivedAt);
        IsArchived = true;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemArchivedDomainEvent(AccountId, WorkspaceId, BoardId, Id, archivedBy, archivedAt));
    }

    public void Unarchive(Guid unarchivedBy, DateTimeOffset unarchivedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(unarchivedBy);
        if (!IsArchived) return;
        var pending = PrepareAuditUpdate(unarchivedBy, unarchivedAt);
        IsArchived = false;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemUnarchivedDomainEvent(AccountId, WorkspaceId, BoardId, Id, unarchivedBy, unarchivedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemRestoredDomainEvent(AccountId, WorkspaceId, Id, BoardId, restoredBy, restoredAt));
    }

    private void EnsureNotArchived()
    {
        if (IsArchived)
            throw new BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Item_CannotModifyArchived,
                "Cannot modify an archived board item.");
    }
}
