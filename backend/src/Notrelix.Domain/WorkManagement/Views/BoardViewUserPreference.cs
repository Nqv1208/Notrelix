using Notrelix.Domain.WorkManagement.Views.Events;
namespace Notrelix.Domain.WorkManagement.Views;

public class BoardViewUserPreference : AggregateRoot, IWorkspaceScoped
{
    private readonly List<FilterRule> _filterRules = new();
    private readonly List<SortRule> _sortRules = new();

    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public Guid ViewId { get; private set; }
    public Guid UserId { get; private set; }

    public IReadOnlyCollection<FilterRule> FilterRules => _filterRules.AsReadOnly();
    public IReadOnlyCollection<SortRule> SortRules => _sortRules.AsReadOnly();
    public GroupRule? GroupRule { get; private set; }

    private BoardViewUserPreference() : base() { }

    public static BoardViewUserPreference Create(
        Guid accountId,
        Guid workspaceId,
        Guid boardId,
        Guid viewId,
        Guid userId,
        DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(viewId);
        Guard.NotEmpty(userId);
        Guard.NotEmpty(accountId);

        var pref = new BoardViewUserPreference
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            BoardId = boardId,
            ViewId = viewId,
            UserId = userId
        };

        pref.SetAuditOnCreate(userId, createdAt);

        pref.RaiseDomainEvent(new BoardViewUserPreferenceCreatedDomainEvent(
            accountId,
            workspaceId,
            boardId,
            viewId,
            userId,
            pref.Id,
            createdAt));

        return pref;
    }

    public void ApplyFilter(
        IEnumerable<FilterRule> rules,
        DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(rules);

        var normalizedRules = rules.ToList();

        BoardViewPreferenceRules.EnsureValidFilterRules(normalizedRules);

        _filterRules.Clear();
        _filterRules.AddRange(normalizedRules);

        SetAuditOnUpdate(UserId, updatedAt);

        RaiseDomainEvent(new BoardViewUserPreferenceFilterChangedDomainEvent(
            AccountId,
            WorkspaceId,
            BoardId,
            ViewId,
            UserId,
            Id,
            updatedAt));
    }

    public void ApplySort(
        IEnumerable<SortRule> rules,
        DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(rules);

        var normalizedRules = rules.ToList();

        BoardViewPreferenceRules.EnsureValidSortRules(normalizedRules);

        _sortRules.Clear();
        _sortRules.AddRange(normalizedRules);

        SetAuditOnUpdate(UserId, updatedAt);

        RaiseDomainEvent(new BoardViewUserPreferenceSortChangedDomainEvent(
            AccountId,
            WorkspaceId,
            BoardId,
            ViewId,
            UserId,
            Id,
            updatedAt));
    }

    public void ApplyGroup(
        GroupRule? groupRule,
        DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();

        if (groupRule is not null)
            BoardViewPreferenceRules.EnsureValidGroupRule(groupRule);

        GroupRule = groupRule;

        SetAuditOnUpdate(UserId, updatedAt);

        RaiseDomainEvent(new BoardViewUserPreferenceGroupChangedDomainEvent(
            AccountId,
            WorkspaceId,
            BoardId,
            ViewId,
            UserId,
            Id,
            updatedAt));
    }
}