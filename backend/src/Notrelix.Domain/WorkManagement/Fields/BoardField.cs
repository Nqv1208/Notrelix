using System.Text.Json;
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
    public FieldValue? DefaultValue { get; private set; }
    public bool IsSystem { get; private set; }
    public DataClassification DataClassification { get; private set; } = DataClassification.Internal;
    public bool IsSensitive { get; private set; }


    private readonly List<FieldOption> _options = new();
    public IReadOnlyCollection<FieldOption> Options => _options.AsReadOnly();

    private bool IsOptionBacked => Type is FieldType.Select or FieldType.MultiSelect or FieldType.Status;

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
        FieldValue? defaultValue = null,
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

        if (defaultValue is not null)
        {
            if (type is FieldType.Select or FieldType.MultiSelect or FieldType.Status)
            {
                throw new BusinessRuleException(
                    WorkManagementRuleCodes.WorkManagement_Field_DefaultRequiresConfiguredOptions,
                    "An option-backed field cannot carry a default value at creation because options are not configured yet. Use SetDefaultValue after adding options.");
            }

            FieldValueValidator.Validate(defaultValue, type, settings);
        }

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

    /// <summary>
    /// Renames the field (WMREQ026/030). The name is presentation metadata; field
    /// identity is unchanged. Renaming to the current (trimmed) name is a no-op.
    /// </summary>
    public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNullOrWhiteSpace(name);
        var normalized = name.Trim();
        Guard.MaxLength(normalized, 100);

        if (Name == normalized) return;

        var oldName = Name;
        var audit = PrepareAuditUpdate(updatedBy, updatedAt);
        Name = normalized;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new BoardFieldRenamedDomainEvent(AccountId, WorkspaceId, Id, BoardId, oldName, normalized, updatedBy, updatedAt));
    }

    public void UpdateSettings(FieldSettings settings, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(settings);

        FieldSettingsValidator.Validate(settings, Type);

        if (Settings == settings) return;

        if (DefaultValue is not null)
            ValidateDefaultValue(DefaultValue, settings);

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);
        Settings = settings;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new BoardFieldUpdatedDomainEvent(AccountId, WorkspaceId, Id, BoardId, updatedBy, updatedAt));
    }

    /// <summary>
    /// Adds an option. An option of a Status field requires a work-status
    /// <paramref name="category"/>; options of other field types must not carry one.
    /// </summary>
    public FieldOption AddOption(string name, Color color, FractionalIndex position, Guid addedBy, DateTimeOffset addedAt, WorkStatusCategory? category = null)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(addedBy);
        if (Type != FieldType.Select && Type != FieldType.MultiSelect && Type != FieldType.Status)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_CannotAddOptionsForType, $"Cannot add options to field of type {Type}");

        if (Type == FieldType.Status && category is null)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_StatusOptionCategoryRequired, "An option of a Status field requires a work-status category.");
        EnsureCategoryAllowed(category);

        var normalizedName = NormalizeOptionName(name);

        if (_options.Any(o => string.Equals(o.Name, normalizedName, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_DuplicateOptionName, $"Duplicate option name '{normalizedName}'.");

        var option = FieldOption.Create(Id, normalizedName, color, position, category);
        var audit = PrepareAuditUpdate(addedBy, addedAt);
        _options.Add(option);
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new FieldOptionAddedDomainEvent(AccountId, WorkspaceId, Id, option.Id, option.Name, addedBy, addedAt));
        return option;
    }

    public void RemoveOption(Guid optionId, Guid removedBy, DateTimeOffset removedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(removedBy);

        var option = _options.FirstOrDefault(o => o.Id == optionId);
        if (option is null)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_OptionNotFound, $"Field option '{optionId}' not found.");

        if (DefaultValueReferencesOption(optionId))
            throw new BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Field_OptionUsedByDefault,
                $"Option '{optionId}' is referenced by the field default value and cannot be removed.");

        EnsureKeepsTodoAndDone(option, newCategory: null);

        var audit = PrepareAuditUpdate(removedBy, removedAt);
        _options.Remove(option);
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new FieldOptionRemovedDomainEvent(AccountId, WorkspaceId, Id, optionId, removedBy, removedAt));
    }

    /// <summary>
    /// Renames/recolours an option and, for a Status field, optionally changes its
    /// work-status category (null keeps the current one). Changing the category is
    /// a field change: it bumps the field version.
    /// </summary>
    public void UpdateOption(Guid optionId, string name, Color color, Guid updatedBy, DateTimeOffset updatedAt, WorkStatusCategory? category = null)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNullOrWhiteSpace(name);
        EnsureCategoryAllowed(category);

        var normalizedName = NormalizeOptionName(name);

        var option = _options.FirstOrDefault(o => o.Id == optionId);
        if (option is null)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_OptionNotFound, $"Field option '{optionId}' not found.");

        if (_options.Any(o => o.Id != optionId && string.Equals(o.Name, normalizedName, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_DuplicateOptionName, $"Duplicate option name '{normalizedName}'.");

        var newCategory = category ?? option.Category;
        if (option.Name == normalizedName && option.Color == color && option.Category == newCategory) return;

        if (newCategory != option.Category)
            EnsureKeepsTodoAndDone(option, newCategory);

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);

        option.Update(normalizedName, color);
        option.UpdateCategory(newCategory);

        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new FieldOptionUpdatedDomainEvent(AccountId, WorkspaceId, Id, optionId, normalizedName, updatedBy, updatedAt));
    }

    /// <summary>
    /// Normal option drag (WM-V3-APP-OPTION-001): moves one option to a position the
    /// caller resolved under the FieldOptions ordering lock, rewriting only its key.
    /// Moving an option to the key it already has is a semantic no-op.
    /// </summary>
    public void MoveOption(Guid optionId, FractionalIndex position, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(position);
        Guard.NotEmpty(updatedBy);

        var option = _options.FirstOrDefault(o => o.Id == optionId)
            ?? throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_OptionNotFound, $"Field option '{optionId}' not found.");

        if (option.Position.Value == position.Value) return;

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);
        option.UpdatePosition(position);
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new FieldOptionMovedDomainEvent(AccountId, WorkspaceId, BoardId, Id, optionId, position, updatedBy, updatedAt));
    }

    /// <summary>
    /// Maintenance rebalance only (WM-V3-APP-OPTION-001): regenerates evenly spaced
    /// keys for the given complete order. No user-facing request calls it; normal
    /// drag uses <see cref="MoveOption"/>.
    /// </summary>
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

        // ── No-op check before audit preparation ─────────────────────────
        var currentOrder = _options.OrderBy(o => o.Position).Select(o => o.Id).ToList();
        if (currentOrder.SequenceEqual(orderedOptionIds)) return;

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);

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

    public void SetDefaultValue(FieldValue? defaultValue, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        ValidateDefaultValue(defaultValue, Settings);

        if (Equals(DefaultValue, defaultValue)) return;

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);
        DefaultValue = defaultValue;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new BoardFieldDefaultValueUpdatedDomainEvent(AccountId, WorkspaceId, Id, BoardId, defaultValue, updatedBy, updatedAt));
    }

    public void Delete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;

        if (IsSystem)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_CannotDeleteSystem, "Cannot delete a system field.");

        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new BoardFieldDeletedDomainEvent(AccountId, WorkspaceId, Id, BoardId, deletedBy, deletedAt));
    }

    /// <summary>
    /// Seeds a new, empty Status field with <see cref="DefaultWorkStatuses"/> in
    /// order (one category each, so the field starts with a Todo and a Done option).
    /// <paramref name="creatorLocale"/> is the creator's profile language fact supplied by Application.
    /// </summary>
    public void AddDefaultStatusOptions(Guid addedBy, DateTimeOffset addedAt, string? creatorLocale = null)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(addedBy);
        if (Type != FieldType.Status || _options.Count > 0)
            throw new BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Field_DefaultStatusesRequireEmptyStatusField,
                "Default statuses can only seed a Status field that has no options.");

        var defaults = DefaultWorkStatuses.For(creatorLocale);
        var positions = FractionalIndexGenerator.GenerateNKeysBetween(lower: null, upper: null, count: defaults.Count);
        for (var i = 0; i < defaults.Count; i++)
            AddOption(defaults[i].Name, Color.Create(defaults[i].Color), positions[i], addedBy, addedAt, defaults[i].Category);
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

        if (DataClassification == classification && IsSensitive == isSensitive) return;

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);
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

        if (Position.Value == position.Value) return;

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);
        Position = position;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new BoardFieldReorderedDomainEvent(AccountId, WorkspaceId, BoardId, Id, position, updatedBy, updatedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;

        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new BoardFieldRestoredDomainEvent(AccountId, WorkspaceId, BoardId, Id, restoredBy, restoredAt));
    }

    private void EnsureCategoryAllowed(WorkStatusCategory? category)
    {
        if (category is not null && Type != FieldType.Status)
            throw new BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Field_OptionCategoryNotAllowed,
                $"Only options of a Status field carry a work-status category; this field is {Type}.");
    }

    /// <summary>
    /// A Status field keeps at least one Todo and one Done option: removing an
    /// option (<paramref name="newCategory"/> null) or moving it to another category
    /// must not take away the last option of either.
    /// </summary>
    private void EnsureKeepsTodoAndDone(FieldOption option, WorkStatusCategory? newCategory)
    {
        if (Type != FieldType.Status) return;

        foreach (var required in new[] { WorkStatusCategory.Todo, WorkStatusCategory.Done })
        {
            if (option.Category != required || newCategory == required) continue;
            if (_options.Any(o => o.Id != option.Id && o.Category == required)) continue;

            throw new BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Field_StatusRequiresTodoAndDone,
                $"A Status field must keep at least one {required} option.");
        }
    }

    private void ValidateDefaultValue(FieldValue? defaultValue, FieldSettings settings)
    {
        if (defaultValue is null) return;

        // Always run canonical shape validation first
        FieldValueValidator.Validate(defaultValue, Type, settings);

        // For option-backed fields, additionally verify option membership
        if (!IsOptionBacked) return;

        var optionIds = ExtractValidatedOptionIds(defaultValue);
        var configuredOptionIds = _options.Select(o => o.Id).ToHashSet();
        var unknownOptionIds = optionIds.Where(id => !configuredOptionIds.Contains(id)).ToArray();

        if (unknownOptionIds.Length > 0)
        {
            throw new BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Field_InvalidOptionValue,
                $"Default value references an option that does not belong to the field: {string.Join(", ", unknownOptionIds)}");
        }
    }

    /// <summary>
    /// Extracts option IDs from a default value that has already passed FieldValueValidator.
    /// Assumes the JSON shape is valid.
    /// </summary>
    private IReadOnlyCollection<Guid> ExtractValidatedOptionIds(FieldValue value)
    {
        using var doc = JsonDocument.Parse(value.Data.Value);
        var root = doc.RootElement;

        return Type switch
        {
            FieldType.Select or FieldType.Status =>
                [Guid.Parse(root.GetString()!)],

            FieldType.MultiSelect =>
                root.EnumerateArray()
                    .Select(item => Guid.Parse(item.GetString()!))
                    .ToArray(),

            _ => Array.Empty<Guid>()
        };
    }

    private bool DefaultValueReferencesOption(Guid optionId)
    {
        if (DefaultValue is null) return false;

        var data = DefaultValue.Data.Value;
        if (data is null or "null") return false;

        try
        {
            using var doc = JsonDocument.Parse(data);
            return ExtractOptionIds(doc.RootElement).Contains(optionId);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static IEnumerable<Guid> ExtractOptionIds(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            var ids = new List<Guid>();
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String && Guid.TryParse(item.GetString(), out var id))
                    ids.Add(id);
            }
            return ids;
        }

        if (element.ValueKind == JsonValueKind.String && Guid.TryParse(element.GetString(), out var single))
            return new[] { single };

        return Array.Empty<Guid>();
    }
}
