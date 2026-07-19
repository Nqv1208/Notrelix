namespace Notrelix.Domain.WorkManagement.Fields;

public class BoardField : AggregateRoot, IWorkspaceScoped
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
    public bool IsFormula { get; private set; }
    public string? FormulaExpression { get; private set; }
    public string MirrorSourceJson { get; private set; } = "{}";

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
        bool isSensitive = false,
        bool isFormula = false,
        string? formulaExpression = null,
        string? mirrorSourceJson = null)
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
            IsSensitive = isSensitive,
            IsFormula = isFormula,
            FormulaExpression = formulaExpression,
            MirrorSourceJson = mirrorSourceJson ?? "{}"
        };

        field.SetAuditOnCreate(createdBy, createdAt);
        field.AddDomainEvent(new BoardFieldCreatedDomainEvent(accountId, workspaceId, boardId, field.Id, field.Name, type, createdBy, createdAt));

        return field;
    }

    public void UpdateSettings(FieldSettings settings, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(settings);
        FieldSettingsValidator.Validate(settings, Type);

        Settings = settings;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new BoardFieldUpdatedDomainEvent(AccountId, WorkspaceId, Id, BoardId, updatedBy, updatedAt));
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
        IncrementVersion();
        AddDomainEvent(new FieldOptionAddedDomainEvent(AccountId, WorkspaceId, Id, option.Id, option.Name, addedBy, addedAt));
    }

    public void RemoveOption(Guid optionId, Guid removedBy, DateTimeOffset removedAt)
    {
        EnsureNotDeleted();
        var option = _options.FirstOrDefault(o => o.Id == optionId);
        if (option is null)
            throw new NotFoundException(nameof(FieldOption), optionId);

        _options.Remove(option);
        SetAuditOnUpdate(removedBy, removedAt);
        IncrementVersion();
        AddDomainEvent(new FieldOptionRemovedDomainEvent(AccountId, WorkspaceId, Id, optionId, removedBy, removedAt));
    }

    public void UpdateOption(Guid optionId, string name, Color color, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);

        var option = _options.FirstOrDefault(o => o.Id == optionId);
        if (option is null)
            throw new NotFoundException(nameof(FieldOption), optionId);

        if (_options.Any(o => o.Id != optionId && string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleException($"Duplicate option name '{name}'.");

        option.Update(name, color);

        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new FieldOptionUpdatedDomainEvent(AccountId, WorkspaceId, Id, optionId, name.Trim(), updatedBy, updatedAt));
    }

    public void ReorderOptions(List<Guid> orderedOptionIds, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (orderedOptionIds.Count != _options.Count)
            throw new BusinessRuleException("Reorder list must contain all options.");

        var positions = new[] { "a0", "a1", "a2", "a3", "a4", "a5", "a6", "a7", "a8", "a9",
            "aA", "aB", "aC", "aD", "aE", "aF", "aG", "aH", "aI", "aJ" };

        for (var i = 0; i < orderedOptionIds.Count; i++)
        {
            var option = _options.FirstOrDefault(o => o.Id == orderedOptionIds[i]);
            if (option is null)
                throw new NotFoundException(nameof(FieldOption), orderedOptionIds[i]);

            option.UpdatePosition(FractionalIndex.Create(positions[i]));
        }

        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new BoardFieldReorderedDomainEvent(AccountId, WorkspaceId, Id, BoardId, 0, updatedBy, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;

        if (IsSystem)
            throw new BusinessRuleException("Cannot delete a system field.");

        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new BoardFieldDeletedDomainEvent(AccountId, WorkspaceId, Id, BoardId, deletedBy, deletedAt));
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
        AddDomainEvent(new BoardFieldClassificationUpdatedDomainEvent(AccountId, WorkspaceId, BoardId, Id, classification, isSensitive, updatedBy, updatedAt));
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

    public void UpdateFormula(bool isFormula, string? expression, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (IsFormula == isFormula && FormulaExpression == expression) return;
        IsFormula = isFormula;
        FormulaExpression = expression;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new BoardFieldFormulaUpdatedDomainEvent(AccountId, WorkspaceId, BoardId, Id, isFormula, expression, updatedBy, updatedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new BoardFieldRestoredDomainEvent(AccountId, WorkspaceId, BoardId, Id, restoredBy, restoredAt));
    }
}
