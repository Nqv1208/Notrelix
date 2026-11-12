using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Mentions.DTOs;
using Notrelix.Application.Features.Chat.Permissions.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Permissions;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Mentions.Services;

/// <summary>The mentions that are stored for a message, and what the send did not do for the rest (shown to the sender only).</summary>
public sealed record MentionResolution(IReadOnlyList<MentionSpan> Spans, IReadOnlyList<MentionNoticeDto> Notices);

/// <summary>
/// Decides which of the mentions a client sent are stored (CHT-010, COL-030, UC-CHT-G01 to G04). The shape of the spans is the
/// domain rule (<see cref="MentionSpans"/>); who may be mentioned is decided here, against the space and the Workspace, on the
/// server, whatever the client believed:
/// a person in the space is stored; a person of the Workspace who is not in the space is stored too and the sender is warned
/// (cannot-view: nobody is notified, nothing is granted, and the person is never counted); anyone else, and every person to a
/// Guest sender, is plain text (unknown) so no one learns who the Workspace holds; a channel link is stored when the sender can
/// see the channel and is plain text otherwise, without telling a missing channel from a private one; a broadcast is stored when
/// the sender holds the broadcast action, is not a Guest and the channel has someone else to reach, and above the member
/// threshold it needs the confirmation.
/// Positions in the notices are those of the stored (trimmed) content.
/// </summary>
public static class MentionResolver
{
    public const string CannotView = "cannot-view";
    public const string Unknown = "unknown";
    public const string LinkNotVisible = "link-not-visible";
    public const string BroadcastNotAllowed = "broadcast-not-allowed";
    public const string NoAudience = "no-audience";

    public static async Task<MentionResolution> ResolveAsync(
        IChatDbContext db,
        IWorkspaceMemberFacts members,
        IBroadcastAuthorizer broadcast,
        Space space,
        WorkspaceMemberFact caller,
        string sentContent,
        IReadOnlyList<MentionInput>? inputs,
        bool confirmBroadcast,
        CancellationToken cancellationToken)
    {
        if (inputs is null or { Count: 0 })
            return new MentionResolution([], []);

        var spans = MentionSpans.Normalize(sentContent, inputs.Select(ToSpan).ToList());
        var isGuest = string.Equals(caller.Role, ChannelAccess.GuestRole, StringComparison.Ordinal);

        var visibleLinks = await VisibleChannelsAsync(
            db, space.WorkspaceId, caller.UserId, isGuest,
            spans.Where(s => s.Kind == MentionKind.ChannelLink).Select(s => s.TargetId!.Value).Distinct().ToList(),
            cancellationToken);

        var activeMembers = space.Memberships.Count(m => m.IsActive);
        bool? canBroadcast = null;
        var workspaceMemberCache = new Dictionary<Guid, bool>();
        var stored = new List<MentionSpan>(spans.Count);
        var notices = new List<MentionNoticeDto>();
        var sawBroadcast = false;

        foreach (var span in spans)
        {
            switch (span.Kind)
            {
                case MentionKind.User:
                    {
                        var target = span.TargetId!.Value;
                        if (space.IsActiveMember(target))
                        {
                            stored.Add(span);
                            break;
                        }

                        if (!isGuest && await IsWorkspaceMemberAsync(members, space.WorkspaceId, target, workspaceMemberCache, cancellationToken))
                        {
                            stored.Add(span);
                            notices.Add(new MentionNoticeDto(CannotView, span.Start, span.Length, target));
                        }
                        else
                        {
                            notices.Add(new MentionNoticeDto(Unknown, span.Start, span.Length, null));
                        }
                        break;
                    }
                case MentionKind.ChannelLink:
                    if (visibleLinks.Contains(span.TargetId!.Value))
                        stored.Add(span);
                    else
                        notices.Add(new MentionNoticeDto(LinkNotVisible, span.Start, span.Length, null));
                    break;
                default:
                    {
                        // @channel and @here.
                        canBroadcast ??= !isGuest
                            && await ChannelPermissionChecks.AllowsAsync(db, space, caller, caller.UserId, ChannelAction.Broadcast, cancellationToken)
                            && await broadcast.CanBroadcastAsync(space.WorkspaceId, cancellationToken);
                        if (canBroadcast != true)
                        {
                            notices.Add(new MentionNoticeDto(BroadcastNotAllowed, span.Start, span.Length, null));
                            break;
                        }

                        // A direct conversation has one other person: there is no audience to broadcast to (UC-CHT-G03).
                        if (activeMembers < 2 || space.Kind == SpaceKind.Direct)
                        {
                            notices.Add(new MentionNoticeDto(NoAudience, span.Start, span.Length, null));
                            break;
                        }

                        sawBroadcast = true;
                        stored.Add(span);
                        break;
                    }
            }
        }

        if (sawBroadcast && activeMembers > ChatLimits.BroadcastConfirmThreshold && !confirmBroadcast)
            throw new ConflictException(
                $"@channel and @here in a channel of more than {ChatLimits.BroadcastConfirmThreshold} members need confirmation.",
                ChatRuleCodes.BroadcastConfirmationRequired);

        return new MentionResolution(stored, notices);
    }

    /// <summary>
    /// The warnings for the mentions a message already holds, for a retry of a send (UC-CHT-G02): each stored person who is not an
    /// active member of the space now. The sender sees the same warning the first send gave.
    /// </summary>
    public static IReadOnlyList<MentionNoticeDto> StoredWarnings(Space space, IEnumerable<MentionSpan> stored) =>
        stored
            .Where(s => s.Kind == MentionKind.User && !space.IsActiveMember(s.TargetId!.Value))
            .Select(s => new MentionNoticeDto(CannotView, s.Start, s.Length, s.TargetId))
            .ToList();

    /// <summary>
    /// The channels among <paramref name="ids"/> a person may see: a live channel of the Workspace that is public (not for a
    /// Guest) or that the person is an active member of. A missing channel and a private one the person is not in are the same.
    /// </summary>
    public static async Task<HashSet<Guid>> VisibleChannelsAsync(
        IChatDbContext db, Guid workspaceId, Guid userId, bool isGuest, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return [];

        var visible = await db.ChatSpaces.AsNoTracking()
            .Where(s => ids.Contains(s.Id) && s.WorkspaceId == workspaceId && s.DeletedAt == null
                        && (s.Kind == SpaceKind.ChannelPublic || s.Kind == SpaceKind.ChannelPrivate)
                        && ((!isGuest && s.Kind == SpaceKind.ChannelPublic)
                            || db.ChatMemberships.Any(m => m.SpaceId == s.Id && m.UserId == userId && m.LeftAt == null)))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
        return [.. visible];
    }

    private static async Task<bool> IsWorkspaceMemberAsync(
        IWorkspaceMemberFacts members, Guid workspaceId, Guid userId, Dictionary<Guid, bool> cache, CancellationToken ct)
    {
        if (cache.TryGetValue(userId, out var known))
            return known;

        var fact = await members.FindAsync(workspaceId, userId, ct);
        var isMember = fact is { IsActive: true };
        cache[userId] = isMember;
        return isMember;
    }

    private static MentionSpan ToSpan(MentionInput input)
    {
        var kind = MentionKindWire.MentionKindFromWire(input.Kind)
            ?? throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(ChatRuleCodes.MentionInvalid, "The mention kind is not known.");
        var target = kind switch
        {
            MentionKind.User => input.UserId,
            MentionKind.ChannelLink => input.SpaceId,
            _ => null,
        };
        return new MentionSpan(kind, input.Start, input.Length, target);
    }
}
