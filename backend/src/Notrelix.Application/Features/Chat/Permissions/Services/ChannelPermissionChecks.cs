using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Permissions.DTOs;
using Notrelix.Application.Features.Governance.Authorization;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Permissions;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Permissions.Services;

/// <summary>
/// The one place Chat asks whether a person may post, react, attach, pin, broadcast or invite in a channel (UC-CHT-H01). Governance
/// decides the Workspace role's default before the handler runs; this adds the channel's own overrides on top, so a deny of the channel
/// refuses an action the role would allow and an allow never exceeds the role. A space with no overrides, a conversation and a
/// Workspace Owner or Admin behave exactly as before. The check reads the standing and the overrides from the database, so a handler
/// that calls it after taking the space lock sees a change that committed while it waited (the standing re-read rule).
/// </summary>
public static class ChannelPermissionChecks
{
    public static bool IsWorkspaceAdmin(string role) => role is "Owner" or "Admin";

    /// <summary>Whether the channel's overrides leave the action to the caller. A conversation has no overrides.</summary>
    public static async Task<bool> AllowsAsync(
        IChatDbContext db, Space space, WorkspaceMemberFact caller, Guid userId, ChannelAction action, CancellationToken ct)
    {
        if (space.IsConversation || IsWorkspaceAdmin(caller.Role)) return true;

        var isManager = await db.ChatMemberships.AsNoTracking().AnyAsync(
            m => m.SpaceId == space.Id && m.UserId == userId && m.LeftAt == null && m.Role == MembershipRole.Manager, ct);
        var rows = await db.ChatPermissionOverrides.AsNoTracking()
            .Where(o => o.SpaceId == space.Id && o.Action == action)
            .ToListAsync(ct);

        return ChannelPermissionResolver.IsAllowed(
            rows.Select(r => r.ToEntry()),
            action,
            isWorkspaceAdmin: false,
            isManager,
            ChannelAccess.IsGuest(caller));
    }

    /// <summary>Refuses with 403 when the channel's overrides turn the action off for the caller.</summary>
    public static async Task RequireAsync(
        IChatDbContext db, Space space, WorkspaceMemberFact caller, Guid userId, ChannelAction action, CancellationToken ct)
    {
        if (!await AllowsAsync(db, space, caller, userId, action, ct))
            throw new ForbiddenException($"Your role cannot {Verb(action)} in this channel.");
    }

    /// <summary>The viewer's capability flags from a space whose overrides are loaded (see <see cref="ChannelAccess.LoadLiveAsync"/>).</summary>
    public static ChannelCapabilitiesDto Capabilities(Space space, string workspaceRole, Membership? viewer)
    {
        var admin = IsWorkspaceAdmin(workspaceRole);
        var member = viewer is { IsActive: true };
        var manager = viewer is { IsManager: true };
        var guest = string.Equals(workspaceRole, ChannelAccess.GuestRole, StringComparison.Ordinal);
        var live = member && !space.IsArchived;
        var entries = space.PermissionEntries();

        bool Can(ChannelAction action) =>
            live
            && Baseline.Holds(workspaceRole, action)
            && (space.IsConversation || ChannelPermissionResolver.IsAllowed(entries, action, admin, manager, guest));

        return new ChannelCapabilitiesDto(
            CanPost: Can(ChannelAction.Post),
            CanReact: Can(ChannelAction.React),
            CanAttach: Can(ChannelAction.Attach),
            CanPin: Can(ChannelAction.Pin),
            CanBroadcast: Can(ChannelAction.Broadcast),
            CanInvite: Can(ChannelAction.Invite),
            CanModerate: !space.IsArchived && (member || space.IsPublic) && WorkspaceRoleBaseline.Decide(workspaceRole, PermissionAction.ChatMessageModerate) == BaselineDecision.Allow,
            CanManagePermissions: !space.IsConversation && !space.IsArchived && (manager || admin));
    }

    private static string Verb(ChannelAction action) => action switch
    {
        ChannelAction.Post => "post",
        ChannelAction.React => "react",
        ChannelAction.Attach => "attach files",
        ChannelAction.Pin => "pin messages",
        ChannelAction.Broadcast => "mention everyone",
        ChannelAction.Invite => "invite people",
        _ => "do this",
    };

    /// <summary>The Workspace role's default for each channel action, read from the Governance baseline table (no second table).</summary>
    public static class Baseline
    {
        /// <summary>Whether the Workspace role holds the action by default.</summary>
        public static bool Holds(string workspaceRole, ChannelAction action) =>
            WorkspaceRoleBaseline.Decide(workspaceRole, action switch
            {
                ChannelAction.Post or ChannelAction.Attach => PermissionAction.ChatMessagePost,
                ChannelAction.React => PermissionAction.ChatMessageReact,
                ChannelAction.Pin => PermissionAction.ChatMessagePin,
                ChannelAction.Broadcast => PermissionAction.ChatMentionBroadcast,
                ChannelAction.Invite => PermissionAction.ChatMemberInvite,
                _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown channel action."),
            }) != BaselineDecision.Deny;

        /// <summary>The Workspace role a subject stands for, to check that an allow does not exceed it.</summary>
        public static string RoleOf(ChannelPermissionSubject subject) =>
            subject == ChannelPermissionSubject.Guest ? ChannelAccess.GuestRole : "Member";
    }
}
