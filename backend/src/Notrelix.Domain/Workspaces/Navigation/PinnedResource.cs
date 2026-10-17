using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Domain.Workspaces.Navigation;

/// <summary>
/// A personal pin of a Board or Doc by a Workspace Member (WX-DEC-011). Pins are
/// personal, reference-only and ordered by pin time; reordering is deferred.
/// The per-member cap is enforced by <see cref="WorkspaceNavigationRules.EnsureCanPin"/>
/// with the current pin count supplied by the caller.
/// </summary>
public sealed class PinnedResource : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public string ResourceKind { get; private set; } = null!;
    public Guid ResourceId { get; private set; }
    public DateTimeOffset PinnedAt { get; private set; }

    private PinnedResource() : base() { }

    public static PinnedResource Create(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        string resourceKind,
        Guid resourceId,
        int currentPinCount,
        DateTimeOffset pinnedAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(userId);
        Guard.NotEmpty(resourceId);
        WorkspaceNavigationRules.EnsureSupportedKind(resourceKind);
        WorkspaceNavigationRules.EnsureValidTimestamp(pinnedAt);
        WorkspaceNavigationRules.EnsureCanPin(currentPinCount);

        return new PinnedResource
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            UserId = userId,
            ResourceKind = resourceKind,
            ResourceId = resourceId,
            PinnedAt = pinnedAt,
        };
    }
}
