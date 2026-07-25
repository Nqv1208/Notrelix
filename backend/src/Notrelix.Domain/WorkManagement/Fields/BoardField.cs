using Notrelix.Domain.WorkManagement.Fields.Events;
namespace Notrelix.Domain.WorkManagement.Fields;

public class BoardField : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public string Name { get; private set; } = null!;
    public FieldType Type { get; private set; }
    public FieldSettings Settings { get; private set; } = null!;
    public FractionalIndex Position { get; private set; } = null!;
    public string? DefaultValue { get; private set; }
    public bool IsSystem { get; private set; }
    public DataClassification DataClassification { get; private set; } = DataClassification.Internal;
    public bool IsSensitive { get; private set; }


    private readonly List<FieldOption> _options = new();
    public IReadOnlyCollection<FieldOption> Options => _options.AsReadOnly();

    private BoardField() : base() { }

    public static BoardField Create(
        Guid accountId,
        Guid workspaceId,
        Guid boardId,
        string name,
        FieldType type,
        FieldSettings settings,
        FractionalIndex position,
        Guid createdBy,
        DateTimeOffset createdAt,
        string? defaultValue = null,
        bool isSystem = false,
        DataClassification dataClassification = DataClassification.Internal,
        bool isSensitive = false)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 100);
        Guard.NotNull(settings);
        Guard.NotNull(position);

        FieldSettingsValidator.Validate(settings, type);
        Guard.NotEmpty(accountId);

        var field = new BoardField
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            BoardId = boardId,
            Name = name.Trim(),
            Type = type,
            Settings = settings,
            Position = position,
            DefaultValue = defaultValue,
            IsSystem = isSystem,
            DataClassification = dataClassification,
            IsSensitive = isSensitive
        };

        field.SetAuditOnCreate(createdBy, createdAt);
        field.RaiseDomainEvent(new BoardFieldCreatedDomainEvent(accountId, workspaceId, boardId, field.Id, field.Name, type, createdBy, createdAt));

        return field;
    }

    public void UpdateSettings(FieldSettings settings, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(settings);
        FieldSettingsValidator.Validate(settings, Type);

        if (Settings == settings) return;

        Settings = settings;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new BoardFieldUpdatedDomainEvent(AccountId, WorkspaceId, Id, BoardId, updatedBy, updatedAt));
    }

    public void AddOption(string name, Color color, FractionalIndex position, Guid addedBy, DateTimeOffset addedAt)
    {
        EnsureNotDeleted();
        if (Type != FieldType.Select && Type != FieldType.MultiSelect && Type != FieldType.Status)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_CannotAddOptionsForType, $"Cannot add options to field of type {Type}");

        if (_options.Any(o => string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_DuplicateOptionName, $"Duplicate option name '{name}'.");

        var option = FieldOption.Create(Id, name, color, position);
        _options.Add(option);
        SetAuditOnUpdate(addedBy, addedAt);
        IncrementVersion();
        RaiseDomainEvent(new FieldOptionAddedDomainEvent(AccountId, WorkspaceId, Id, option.Id, option.Name, addedBy, addedAt));
    }

    public void RemoveOption(Guid optionId, Guid removedBy, DateTimeOffset removedAt)
    {
        EnsureNotDeleted();
        var option = _options.FirstOrDefault(o => o.Id == optionId);
        if (option is null)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_OptionNotFound, $"Field option '{optionId}' not found.");

        _options.Remove(option);
        SetAuditOnUpdate(removedBy, removedAt);
        IncrementVersion();
        RaiseDomainEvent(new FieldOptionRemovedDomainEvent(AccountId, WorkspaceId, Id, optionId, removedBy, removedAt));
    }

    public void UpdateOption(Guid optionId, string name, Color color, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);

        var option = _options.FirstOrDefault(o => o.Id == optionId);
        if (option is null)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_OptionNotFound, $"Field option '{optionId}' not found.");

        if (_options.Any(o => o.Id != optionId && string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_DuplicateOptionName, $"Duplicate option name '{name}'.");

        var trimmedName = name.Trim();
        if (option.Name == trimmedName && option.Color == color) return;

        option.Update(name, color);

        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new FieldOptionUpdatedDomainEvent(AccountId, WorkspaceId, Id, optionId, trimmedName, updatedBy, updatedAt));
    }

    public void ReorderOptions(List<Guid> orderedOptionIds, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();

        // ── Validate the complete input set BEFORE any mutation ──────────
        if (orderedOptionIds.Count != _options.Count)
            throw new BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Field_ReorderMustContainAllOptions,
                "Reorder list must contain all options.");

        if (orderedOptionIds.Distinct().Count() != orderedOptionIds.Count)
            throw new BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Field_ReorderMustContainAllOptions,
                "Reorder list must not contain duplicate option IDs.");

        var existingIds = _options.Select(o => o.Id).ToHashSet();
        if (!existingIds.SetEquals(orderedOptionIds))
            throw new BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Field_OptionNotFound,
                "Reorder list must contain every field option exactly once.");

        // ── No-op check ──────────────────────────────────────────────────
        var currentOrder = _options.OrderBy(o => o.Position).Select(o => o.Id).ToList();
        if (currentOrder.SequenceEqual(orderedOptionIds)) return;

        // ── Generate evenly distributed positions ────────────────────────
        var positions = FractionalIndexGenerator.GenerateNKeysBetween(
            lower: null,
            upper: null,
            count: orderedOptionIds.Count);

        var optionsById = _options.ToDictionary(o => o.Id);
        for (var i = 0; i < orderedOptionIds.Count; i++)
            optionsById[orderedOptionIds[i]].UpdatePosition(positions[i]);

        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new FieldOptionsReorderedDomainEvent(AccountId, WorkspaceId, BoardId, Id, orderedOptionIds.AsReadOnly(), updatedBy, updatedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;

        if (IsSystem)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_CannotDeleteSystem, "Cannot delete a system field.");

        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new BoardFieldDeletedDomainEvent(AccountId, WorkspaceId, Id, BoardId, deletedBy, deletedAt));
    }

    public bool CanBeUsedAsKanbanColumn()
    {
        return Type is FieldType.Status
            or FieldType.Select
            or FieldType.Person;
    }

    public void UpdateClassification(DataClassification classification, bool isSensitive, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (DataClassification == classification && IsSensitive == isSensitive) return;
        DataClassification = classification;
        IsSensitive = isSensitive;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new BoardFieldClassificationUpdatedDomainEvent(AccountId, WorkspaceId, BoardId, Id, classification, isSensitive, updatedBy, updatedAt));
    }

    public void UpdatePosition(FractionalIndex position, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(position);

        if (Position.Value == position.Value) return;

        Position = position;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new BoardFieldRestoredDomainEvent(AccountId, WorkspaceId, BoardId, Id, restoredBy, restoredAt));
    }
}
