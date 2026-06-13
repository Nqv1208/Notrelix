using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Fields;

namespace Notrelix.Domain.WorkManagement.Items;

public class BoardItem : AggregateRoot, IWorkspaceScoped
{
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
    
    private readonly List<BoardItemValue> _fieldValues = new();
    public IReadOnlyCollection<BoardItemValue> FieldValues => _fieldValues.AsReadOnly();

    private BoardItem() : base() { }

    public static BoardItem Create(
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
        Guard.NotNull(position);

        var item = new BoardItem
        {
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
        item.AddDomainEvent(new BoardItemCreatedEvent(workspaceId, boardId, groupId, item.Id, item.Name, createdBy, createdAt));
        
        return item;
    }

    public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);

        var oldName = Name;
        var normalizedName = name.Trim();
        if (Name == normalizedName) return;

        Name = normalizedName;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new BoardItemRenamedEvent(WorkspaceId, Id, BoardId, oldName, Name, updatedBy, updatedAt));
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
        AddDomainEvent(new BoardItemMovedEvent(WorkspaceId, Id, BoardId, oldGroupId, group.GroupId, newPosition.Value, updatedBy, updatedAt));
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
        AddDomainEvent(new BoardItemFieldValueChangedEvent(WorkspaceId, Id, BoardId, field.Id, oldValue, newValue, updatedBy, updatedAt));
    }

    public void AssignParentItem(Guid? parentItemId, int itemLevel)
    {
        EnsureNotDeleted();
        if (parentItemId == Id)
            throw new BusinessRuleException("An item cannot be its own parent.");

        ParentItemId = parentItemId;
        ItemLevel = itemLevel;
        IncrementVersion();
    }

    public void SetTimeline(DateTimeOffset? startedAt, DateTimeOffset? dueAt, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (startedAt != null && dueAt != null && dueAt < startedAt)
            throw new BusinessRuleException("Due date must be after start date.");

        StartedAt = startedAt;
        DueAt = dueAt;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void Complete(DateTimeOffset? completedAt, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        CompletedAt = completedAt;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new BoardItemSoftDeletedEvent(WorkspaceId, Id, BoardId, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new BoardItemRestoredEvent(WorkspaceId, Id, BoardId, restoredBy, restoredAt));
    }
}
