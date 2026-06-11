using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.WorkManagement.Fields;

namespace Notrelix.Domain.WorkManagement.Items;

public class BoardItem : SoftDeletableEntity
{
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public Guid GroupId { get; private set; }
    public string Name { get; private set; } = null!;
    public FractionalIndex Position { get; private set; } = null!;
    
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
        DateTimeOffset createdAt)
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
            Position = position
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
        AddDomainEvent(new BoardItemRenamedEvent(WorkspaceId, Id, BoardId, oldName, Name, updatedBy, updatedAt));
    }

    public void MoveToGroup(Guid newGroupId, FractionalIndex newPosition, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(newGroupId);
        Guard.NotNull(newPosition);

        var oldGroupId = GroupId;
        if (GroupId == newGroupId && Position == newPosition) return;

        GroupId = newGroupId;
        Position = newPosition;
        SetAuditOnUpdate(updatedBy, updatedAt);
        
        AddDomainEvent(new BoardItemMovedEvent(WorkspaceId, Id, BoardId, oldGroupId, newGroupId, newPosition.Value, updatedBy, updatedAt));
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
        AddDomainEvent(new BoardItemFieldValueChangedEvent(WorkspaceId, Id, BoardId, field.Id, oldValue, newValue, updatedBy, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        AddDomainEvent(new BoardItemSoftDeletedEvent(WorkspaceId, Id, BoardId, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        AddDomainEvent(new BoardItemRestoredEvent(WorkspaceId, Id, BoardId, restoredBy, restoredAt));
    }
}
