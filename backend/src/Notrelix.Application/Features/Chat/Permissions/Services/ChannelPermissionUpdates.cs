using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Permissions.DTOs;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Permissions;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Application.Features.Chat.Permissions.Services;

/// <summary>
/// Sets or clears a channel's overrides (UC-CHT-H01), the one path the set and the clear share. Needs a channel manager, a Workspace
/// Owner or Admin. An allow may not exceed what its Workspace role holds by default, nor what the caller holds, so a manager cannot lift
/// a role above the role limits (422 permission-exceeds-role); the aggregate keeps the deny-beats-allow and the lockout rules. The audit
/// fact is written once, in the same transaction, and only when something changed.
/// </summary>
public static class ChannelPermissionUpdates
{
    public static async Task<ChannelPermissionsDto> ApplyAsync(
        IChatDbContext db,
        IWorkspaceMemberFacts members,
        ChatAuditFacts audit,
        Guid workspaceId,
        Guid channelId,
        Guid userId,
        IReadOnlyList<ChannelPermissionEntry> requested,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var caller = await ChannelAccess.CallerAsync(members, workspaceId, userId, ct);
        var space = await ChannelAccess.LoadChannelAsync(db, workspaceId, channelId, ct);
        ChannelAccess.RequireVisible(space, caller, userId);
        ChannelAccess.RequireManager(space, caller, userId);

        foreach (var entry in requested.Where(e => e.Effect == ChannelPermissionEffect.Allow))
        {
            if (!ChannelPermissionChecks.Baseline.Holds(ChannelPermissionChecks.Baseline.RoleOf(entry.Subject), entry.Action))
                throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                    ChatRuleCodes.PermissionExceedsRole, $"The {entry.Subject.ToWire()} role cannot be allowed to {entry.Action.ToWire()} here.");
            if (!ChannelPermissionChecks.Baseline.Holds(caller.Role, entry.Action))
                throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                    ChatRuleCodes.PermissionExceedsRole, $"You cannot grant {entry.Action.ToWire()}, which you do not hold yourself.");
        }

        var before = space.PermissionEntries();
        if (space.SetPermissionOverrides(requested, userId, now))
            await audit.PermissionsChangedAsync(space, before, space.PermissionEntries(), ct);

        return ChannelPermissionsDto.From(space);
    }
}
