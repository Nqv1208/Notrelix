namespace Notrelix.Domain.Chat.Permissions;

/// <summary>One requested override: a subject, an action and the effect. The input of <see cref="Spaces.Space.SetPermissionOverrides"/>.</summary>
public readonly record struct ChannelPermissionEntry(
    ChannelPermissionSubject Subject,
    ChannelAction Action,
    ChannelPermissionEffect Effect);

/// <summary>
/// One override of a channel's default permission set (UC-CHT-H01): for one role and one action, allow or deny. Owned by the Space
/// aggregate, which keeps at most one row per (subject, action) and decides the invariants; the Workspace-role baseline that an
/// allow may not exceed is checked by the handler, which can see Governance. Keyed by (space, subject, action).
/// </summary>
public sealed class ChannelPermissionOverride : IWorkspaceScoped
{
    public Guid SpaceId { get; private set; }
    public ChannelPermissionSubject Subject { get; private set; }
    public ChannelAction Action { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public ChannelPermissionEffect Effect { get; private set; }

    private ChannelPermissionOverride() { }

    internal static ChannelPermissionOverride Create(Guid spaceId, Guid accountId, Guid workspaceId, ChannelPermissionEntry entry)
    {
        Guard.NotEmpty(spaceId);
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);

        return new ChannelPermissionOverride
        {
            SpaceId = spaceId,
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Subject = entry.Subject,
            Action = entry.Action,
            Effect = entry.Effect,
        };
    }

    internal bool SetEffect(ChannelPermissionEffect effect)
    {
        if (Effect == effect) return false;

        Effect = effect;
        return true;
    }

    public ChannelPermissionEntry ToEntry() => new(Subject, Action, Effect);
}
