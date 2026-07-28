using Notrelix.Domain.WorkManagement.Views.Events;
namespace Notrelix.Domain.WorkManagement.Views;

public class SavedFilter : SoftDeletableAggregateRoot, IWorkspaceScoped
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
        Guard.NotEmpty(updatedBy);
        Guard.NotNullOrWhiteSpace(name);

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Name = name.Trim();
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new SavedFilterRenamedDomainEvent(AccountId, WorkspaceId, Id, BoardId, Name, updatedBy, updatedAt));
    }

    public void UpdateVisibility(SavedFilterVisibility visibility, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Visibility = visibility;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new SavedFilterVisibilityUpdatedDomainEvent(AccountId, WorkspaceId, Id, BoardId, visibility, updatedBy, updatedAt));
    }

    public void UpdateFilters(IEnumerable<FilterRule> rules, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(rules);

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        _rules.Clear();
        _rules.AddRange(rules);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new SavedFilterFiltersUpdatedDomainEvent(AccountId, WorkspaceId, Id, BoardId, updatedBy, updatedAt));
    }

    public void UpdateSorts(IEnumerable<SortRule> sortRules, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(sortRules);

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        _sortRules.Clear();
        _sortRules.AddRange(sortRules);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new SavedFilterSortsUpdatedDomainEvent(AccountId, WorkspaceId, Id, BoardId, updatedBy, updatedAt));
    }

    public void UpdateGroup(GroupRule? groupRule, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        GroupRule = groupRule;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new SavedFilterGroupUpdatedDomainEvent(AccountId, WorkspaceId, Id, BoardId, updatedBy, updatedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        var pending = PrepareAuditUpdate(deletedBy, deletedAt);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new SavedFilterSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, BoardId, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        var pending = PrepareAuditUpdate(restoredBy, restoredAt);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new SavedFilterRestoredDomainEvent(AccountId, WorkspaceId, Id, BoardId, restoredBy, restoredAt));
    }
}
