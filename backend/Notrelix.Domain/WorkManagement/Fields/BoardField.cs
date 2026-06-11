using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Fields;

public class BoardField : AggregateRoot
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
        DateTimeOffset createdAt,
        string? defaultValue = null, 
        bool isSystem = false)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(settings);
        Guard.NotNull(position);

        FieldSettingsValidator.Validate(settings, type);

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

        field.SetAuditOnCreate(createdBy, createdAt);
        field.AddDomainEvent(new BoardFieldCreatedEvent(workspaceId, boardId, field.Id, field.Name, type, createdBy, createdAt));
        
        return field;
    }

    public void UpdateSettings(FieldSettings settings, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(settings);
        
        Settings = settings;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new BoardFieldUpdatedEvent(WorkspaceId, Id, BoardId, updatedBy, updatedAt));
    }

    public void AddOption(string name, Color color, FractionalIndex position, Guid addedBy, DateTimeOffset addedAt)
    {
        EnsureNotDeleted();
        if (Type != FieldType.Select && Type != FieldType.MultiSelect && Type != FieldType.Status)
            throw new BusinessRuleException($"Cannot add options to field of type {Type}");

        if (_options.Any(o => string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleException($"Duplicate option name '{name}'.");

        var option = FieldOption.Create(Id, name, color, position);
        _options.Add(option);
        SetAuditOnUpdate(addedBy, addedAt);
        AddDomainEvent(new FieldOptionAddedEvent(WorkspaceId, Id, option.Id, option.Name, addedBy, addedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        
        if (IsSystem)
            throw new BusinessRuleException("Cannot delete a system field.");

        base.SoftDelete(deletedBy, deletedAt, reason);
        AddDomainEvent(new BoardFieldDeletedEvent(WorkspaceId, Id, BoardId, deletedBy, deletedAt));
    }

    public bool CanBeUsedAsKanbanColumn()
    {
        return Type is FieldType.Status
            or FieldType.Select
            or FieldType.Person;
    }
}
