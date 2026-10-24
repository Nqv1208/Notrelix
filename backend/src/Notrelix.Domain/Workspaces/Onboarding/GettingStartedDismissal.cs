namespace Notrelix.Domain.Workspaces.Onboarding;

/// <summary>
/// A member hid the getting-started card of one Workspace (accepted 2026-10-23: dismissal is stored per
/// user and Workspace). Personal state; the card's items themselves are derived, never stored.
/// </summary>
public sealed class GettingStartedDismissal : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset DismissedAt { get; private set; }

    private GettingStartedDismissal() : base() { }

    public static GettingStartedDismissal Create(Guid accountId, Guid workspaceId, Guid userId, DateTimeOffset dismissedAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(userId);

        return new GettingStartedDismissal
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            UserId = userId,
            DismissedAt = dismissedAt,
        };
    }
}
