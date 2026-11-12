using Notrelix.Application.Features.Chat.Links.DTOs;
using Notrelix.Application.Features.Chat.Permissions.DTOs;
using Notrelix.Application.Features.Chat.Permissions.Services;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Channels.DTOs;

/// <summary>
/// A channel as one viewer sees it. <see cref="SlowModeSeconds"/> is the least time between two messages of one person (0 means off, UC-CHT-H08). <see cref="Link"/> is the Board, Page or Team the channel is linked to (UC-CHT-H10), with a title only when the viewer can view it. <see cref="IsMember"/>, the membership fields and <see cref="Capabilities"/> are the viewer's own.
/// </summary>
public sealed record ChannelDto(
    Guid Id,
    string Name,
    string? Topic,
    string Kind,
    Guid? GroupId,
    double Position,
    bool IsArchived,
    bool IsMember,
    string? Role,
    string? NotificationLevel,
    long Version,
    DateTimeOffset CreatedAt,
    ChannelCapabilitiesDto Capabilities,
    int SlowModeSeconds = 0,
    ChannelLinkDto? Link = null)
{
    public static ChannelDto From(Space space, Membership? viewerMembership, string workspaceRole) => new(
        space.Id,
        space.Name,
        space.Topic,
        space.Kind.ToWire(),
        space.GroupId,
        space.Position,
        space.IsArchived,
        viewerMembership is { IsActive: true },
        viewerMembership is { IsActive: true } ? viewerMembership.Role.ToWire() : null,
        viewerMembership is { IsActive: true } ? viewerMembership.NotificationLevel.ToWire() : null,
        space.Version,
        space.CreatedAt,
        ChannelPermissionChecks.Capabilities(space, workspaceRole, viewerMembership),
        space.SlowModeSeconds,
        // Only the reference is known here; the handlers that read for a viewer fill in the title (or the generic form) with ChatResourceViews.
        space.HasLink ? new ChannelLinkDto(space.LinkKind!, space.LinkId!.Value, false, null) : null);
}

public sealed record ChannelMemberDto(Guid UserId, string Role, DateTimeOffset JoinedAt);

/// <summary>A channel with its active members. Only a person who can see the channel gets this.</summary>
public sealed record ChannelDetailDto(ChannelDto Channel, IReadOnlyList<ChannelMemberDto> Members);

/// <summary>The people a bulk add reached. Skipped people are not members of the Workspace.</summary>
public sealed record AddChannelMembersResultDto(IReadOnlyList<Guid> Added, IReadOnlyList<Guid> Skipped);

public sealed record ChannelGroupDto(Guid Id, string Name, double Position, long Version)
{
    public static ChannelGroupDto From(ChannelGroup group) => new(group.Id, group.Name, group.Position, group.Version);
}
