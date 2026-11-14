using Notrelix.Domain.Chat.Permissions;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Permissions.DTOs;

/// <summary>One override of a channel: a role (manager, member, guest), an action and allow or deny (UC-CHT-H01).</summary>
public sealed record ChannelPermissionEntryDto(string Subject, string Action, string Effect)
{
    public static ChannelPermissionEntryDto From(ChannelPermissionEntry entry) =>
        new(entry.Subject.ToWire(), entry.Action.ToWire(), entry.Effect.ToWire());
}

/// <summary>The overrides of a channel with the channel's version, which the next set or clear must send as expectedVersion.</summary>
public sealed record ChannelPermissionsDto(Guid ChannelId, IReadOnlyList<ChannelPermissionEntryDto> Entries, long Version)
{
    public static ChannelPermissionsDto From(Space space) => new(
        space.Id,
        space.PermissionEntries().Select(ChannelPermissionEntryDto.From).ToList(),
        space.Version);
}

/// <summary>
/// What the viewer may do in a channel now, so a client can disable its controls (UC-CHT-H01). The flags combine the viewer's Workspace
/// role default with the channel's overrides; the server still checks every action, so a flag is a hint and never the authority. A
/// custom role grant is not reflected: it can only make a control that shows disabled work after a refresh.
/// </summary>
public sealed record ChannelCapabilitiesDto(
    bool CanPost,
    bool CanReact,
    bool CanAttach,
    bool CanPin,
    bool CanBroadcast,
    bool CanInvite,
    bool CanModerate,
    bool CanManagePermissions);
