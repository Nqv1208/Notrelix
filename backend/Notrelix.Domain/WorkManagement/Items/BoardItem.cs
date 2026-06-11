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
        Guid createdBy)
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

        item.SetAuditOnCreate(createdBy);
        item.AddDomainEvent(new BoardItemCreatedEvent(boardId, groupId, item.Id, item.Name, createdBy));
        
        return item;
    }

    public void Rename(string name, Guid updatedBy)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);

        var oldName = Name;
        var normalizedName = name.Trim();
        if (Name == normalizedName) return;

        Name = normalizedName;
        SetAuditOnUpdate(updatedBy);
        AddDomainEvent(new BoardItemRenamedEvent(Id, BoardId, oldName, Name, updatedBy));
    }

    public void MoveToGroup(Guid newGroupId, FractionalIndex newPosition, Guid updatedBy)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(newGroupId);
        Guard.NotNull(newPosition);

        var oldGroupId = GroupId;
        if (GroupId == newGroupId && Position == newPosition) return;

        GroupId = newGroupId;
        Position = newPosition;
        SetAuditOnUpdate(updatedBy);
        
        AddDomainEvent(new BoardItemMovedEvent(Id, BoardId, oldGroupId, newGroupId, newPosition.Value, updatedBy));
    }

    public void UpdateFieldValue(Guid fieldId, FieldValue newValue, Guid updatedBy)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(fieldId);
        Guard.NotNull(newValue);

        var existingValue = _fieldValues.FirstOrDefault(fv => fv.FieldId == fieldId);
        var oldValue = existingValue?.Value ?? FieldValue.Create(JsonValue.EmptyObject());

        if (existingValue == null)
        {
            _fieldValues.Add(BoardItemValue.Create(Id, fieldId, newValue));
        }
        else
        {
            if (existingValue.Value == newValue) return;
            existingValue.UpdateValue(newValue);
        }

        SetAuditOnUpdate(updatedBy);
        AddDomainEvent(new BoardItemFieldValueChangedEvent(Id, BoardId, fieldId, oldValue, newValue, updatedBy));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        AddDomainEvent(new BoardItemSoftDeletedEvent(Id, BoardId, deletedBy));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        AddDomainEvent(new BoardItemRestoredEvent(Id, BoardId, restoredBy));
    }
}
