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

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);

        FieldSettingsValidator.Validate(settings, Type);

        if (Settings == settings) return;

        Settings = settings;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new BoardFieldUpdatedDomainEvent(AccountId, WorkspaceId, Id, BoardId, updatedBy, updatedAt));
    }

    public void AddOption(string name, Color color, FractionalIndex position, Guid addedBy, DateTimeOffset addedAt)
    {
        EnsureNotDeleted();
        if (Type != FieldType.Select && Type != FieldType.MultiSelect && Type != FieldType.Status)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_CannotAddOptionsForType, $"Cannot add options to field of type {Type}");

        var normalizedName = NormalizeOptionName(name);

        if (_options.Any(o => string.Equals(o.Name, normalizedName, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_DuplicateOptionName, $"Duplicate option name '{normalizedName}'.");

        var audit = PrepareAuditUpdate(addedBy, addedAt);

        var option = FieldOption.Create(Id, normalizedName, color, position);
        _options.Add(option);
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new FieldOptionAddedDomainEvent(AccountId, WorkspaceId, Id, option.Id, option.Name, addedBy, addedAt));
    }

    public void RemoveOption(Guid optionId, Guid removedBy, DateTimeOffset removedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(removedBy);

        var audit = PrepareAuditUpdate(removedBy, removedAt);

        var option = _options.FirstOrDefault(o => o.Id == optionId);
        if (option is null)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_OptionNotFound, $"Field option '{optionId}' not found.");

        _options.Remove(option);
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new FieldOptionRemovedDomainEvent(AccountId, WorkspaceId, Id, optionId, removedBy, removedAt));
    }

    public void UpdateOption(Guid optionId, string name, Color color, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);

        var normalizedName = NormalizeOptionName(name);

        var option = _options.FirstOrDefault(o => o.Id == optionId);
        if (option is null)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_OptionNotFound, $"Field option '{optionId}' not found.");

        if (_options.Any(o => o.Id != optionId && string.Equals(o.Name, normalizedName, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_DuplicateOptionName, $"Duplicate option name '{normalizedName}'.");

        if (option.Name == normalizedName && option.Color == color) return;

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);

        option.Update(normalizedName, color);

        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new FieldOptionUpdatedDomainEvent(AccountId, WorkspaceId, Id, optionId, normalizedName, updatedBy, updatedAt));
    }

    public void ReorderOptions(IReadOnlyList<Guid> orderedOptionIds, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

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

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);

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

        ApplyAuditUpdate(audit);
        IncrementVersion();

        // Defensive copy: event payload must not reference caller's mutable list
        var orderedCopy = orderedOptionIds.ToArray();
        RaiseDomainEvent(new FieldOptionsReorderedDomainEvent(AccountId, WorkspaceId, BoardId, Id, orderedCopy, updatedBy, updatedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;

        if (IsSystem)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_CannotDeleteSystem, "Cannot delete a system field.");

        var audit = PrepareAuditUpdate(deletedBy, deletedAt);

        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new BoardFieldDeletedDomainEvent(AccountId, WorkspaceId, Id, BoardId, deletedBy, deletedAt));
    }

    public bool CanBeUsedAsKanbanColumn()
    {
        return Type is FieldType.Status
            or FieldType.Select
            or FieldType.Person;
    }

    internal static string NormalizeOptionName(string name)
    {
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 100);
        return name.Trim();
    }

    public void UpdateClassification(DataClassification classification, bool isSensitive, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);

        if (DataClassification == classification && IsSensitive == isSensitive) return;
        DataClassification = classification;
        IsSensitive = isSensitive;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new BoardFieldClassificationUpdatedDomainEvent(AccountId, WorkspaceId, BoardId, Id, classification, isSensitive, updatedBy, updatedAt));
    }

    public void UpdatePosition(FractionalIndex position, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(position);
        Guard.NotEmpty(updatedBy);

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);

        if (Position.Value == position.Value) return;

        Position = position;
        ApplyAuditUpdate(audit);
        IncrementVersion();
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;

        var audit = PrepareAuditUpdate(restoredBy, restoredAt);

        if (!MarkRestored(restoredBy, restoredAt)) return;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new BoardFieldRestoredDomainEvent(AccountId, WorkspaceId, BoardId, Id, restoredBy, restoredAt));
    }
}
