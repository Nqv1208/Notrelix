using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Fields;

public class BoardField : SoftDeletableEntity
{
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public string Name { get; private set; } = null!;
    public FieldType Type { get; private set; }
    public FieldSettings Settings { get; private set; } = null!;
    public FractionalIndex Position { get; private set; } = null!;
    public string? DefaultValue { get; private set; }
    public bool IsSystem { get; private set; }

    private readonly List<FieldOption> _options = new();
    public IReadOnlyCollection<FieldOption> Options => _options.AsReadOnly();

    private BoardField() : base() { }

    public static BoardField Create(
        Guid workspaceId, 
        Guid boardId, 
        string name, 
        FieldType type, 
        FieldSettings settings, 
        FractionalIndex position, 
        Guid createdBy,
        string? defaultValue = null, 
        bool isSystem = false)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(settings);
        Guard.NotNull(position);

        var field = new BoardField
        {
            WorkspaceId = workspaceId,
            BoardId = boardId,
            Name = name.Trim(),
            Type = type,
            Settings = settings,
            Position = position,
            DefaultValue = defaultValue,
            IsSystem = isSystem
        };

        field.SetAuditOnCreate(createdBy);
        field.AddDomainEvent(new BoardFieldCreatedEvent(boardId, field.Id, field.Name, type, createdBy));
        
        return field;
    }

    public void UpdateSettings(FieldSettings settings, Guid updatedBy)
    {
        EnsureNotDeleted();
        Guard.NotNull(settings);
        
        Settings = settings;
        SetAuditOnUpdate(updatedBy);
        AddDomainEvent(new BoardFieldUpdatedEvent(Id, BoardId, updatedBy));
    }

    public void AddOption(string name, Color color, FractionalIndex position, Guid addedBy)
    {
        EnsureNotDeleted();
        if (Type != FieldType.Select && Type != FieldType.MultiSelect && Type != FieldType.Status)
            throw new BusinessRuleException($"Cannot add options to field of type {Type}");

        var option = FieldOption.Create(Id, name, color, position);
        _options.Add(option);
        SetAuditOnUpdate(addedBy);
        AddDomainEvent(new FieldOptionAddedEvent(Id, option.Id, option.Name, addedBy));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        
        if (IsSystem)
            throw new BusinessRuleException("Cannot delete a system field.");

        base.SoftDelete(deletedBy, deletedAt, reason);
        AddDomainEvent(new BoardFieldDeletedEvent(Id, BoardId, deletedBy));
    }
}
