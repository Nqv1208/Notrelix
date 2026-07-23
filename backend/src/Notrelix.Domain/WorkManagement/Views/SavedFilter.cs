namespace Notrelix.Domain.WorkManagement.Views;

public class SavedFilter : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public Guid? ViewId { get; private set; }
    public string Name { get; private set; } = null!;
    public SavedFilterVisibility Visibility { get; private set; }

    private readonly List<FilterRule> _rules = new();
    private readonly List<SortRule> _sortRules = new();

    public IReadOnlyCollection<FilterRule> Rules => _rules.AsReadOnly();
    public IReadOnlyCollection<SortRule> SortRules => _sortRules.AsReadOnly();
    public GroupRule? GroupRule { get; private set; }

    private SavedFilter() : base() { }

    public static SavedFilter Create(
        Guid accountId,
        Guid workspaceId,
        Guid boardId,
        string name,
        IEnumerable<FilterRule> rules,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? viewId = null,
        SavedFilterVisibility visibility = SavedFilterVisibility.Private,
        IEnumerable<SortRule>? sortRules = null,
        GroupRule? groupRule = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(rules);
        Guard.NotEmpty(accountId);

        var filter = new SavedFilter
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            BoardId = boardId,
            ViewId = viewId,
            Name = name.Trim(),
            Visibility = visibility,
            GroupRule = groupRule
        };

        filter._rules.AddRange(rules);

        if (sortRules is not null)
            filter._sortRules.AddRange(sortRules);

        filter.SetAuditOnCreate(createdBy, createdAt);
        filter.RaiseDomainEvent(new SavedFilterCreatedDomainEvent(filter.Id, accountId, workspaceId, boardId, filter.Name, createdBy, createdAt, viewId));

        return filter;
    }

    public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);

        Name = name.Trim();
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new SavedFilterRenamedDomainEvent(AccountId, WorkspaceId, Id, BoardId, Name, updatedBy, updatedAt));
    }

    public void UpdateVisibility(SavedFilterVisibility visibility, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();

        Visibility = visibility;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new SavedFilterVisibilityUpdatedDomainEvent(AccountId, WorkspaceId, Id, BoardId, visibility, updatedBy, updatedAt));
    }

    public void UpdateFilters(IEnumerable<FilterRule> rules, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(rules);

        _rules.Clear();
        _rules.AddRange(rules);
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new SavedFilterFiltersUpdatedDomainEvent(AccountId, WorkspaceId, Id, BoardId, updatedBy, updatedAt));
    }

    public void UpdateSorts(IEnumerable<SortRule> sortRules, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(sortRules);

        _sortRules.Clear();
        _sortRules.AddRange(sortRules);
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new SavedFilterSortsUpdatedDomainEvent(AccountId, WorkspaceId, Id, BoardId, updatedBy, updatedAt));
    }

    public void UpdateGroup(GroupRule? groupRule, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();

        GroupRule = groupRule;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new SavedFilterGroupUpdatedDomainEvent(AccountId, WorkspaceId, Id, BoardId, updatedBy, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new SavedFilterSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, BoardId, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new SavedFilterRestoredDomainEvent(AccountId, WorkspaceId, Id, BoardId, restoredBy, restoredAt));
    }
}
