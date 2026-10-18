using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Domain.Workspaces.Navigation;

/// <summary>
/// One "recently opened" reference in a Workspace Member's personal navigation
/// state (WX-DEC-011). It stores the resource reference only, never its title.
/// Retention (the newest <see cref="WorkspaceNavigationRules.RecentRetentionLimit"/>
/// entries per member per Workspace) is applied by
/// <see cref="WorkspaceNavigationRules.SelectRecentsToEvict"/>.
/// </summary>
public sealed class RecentResource : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public string ResourceKind { get; private set; } = null!;
    public Guid ResourceId { get; private set; }
    public DateTimeOffset LastOpenedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private RecentResource() : base() { }

    public static RecentResource Create(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        string resourceKind,
        Guid resourceId,
        DateTimeOffset openedAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(userId);
        Guard.NotEmpty(resourceId);
        WorkspaceNavigationRules.EnsureSupportedKind(resourceKind);
        WorkspaceNavigationRules.EnsureValidTimestamp(openedAt);

        return new RecentResource
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            UserId = userId,
            ResourceKind = resourceKind,
            ResourceId = resourceId,
            LastOpenedAt = openedAt,
            CreatedAt = openedAt,
        };
    }

    /// <summary>
    /// Records another open. An open that is not newer than the stored one is a
    /// semantic no-op (a retried or out-of-order request never moves the entry back).
    /// Returns whether the entry changed.
    /// </summary>
    public bool RecordOpen(DateTimeOffset openedAt)
    {
        WorkspaceNavigationRules.EnsureValidTimestamp(openedAt);

        if (openedAt <= LastOpenedAt)
            return false;

        LastOpenedAt = openedAt;
        return true;
    }
}
