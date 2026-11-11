using Notrelix.Application.Common.Messaging;
using Notrelix.Application.Features.Chat.Realtime;
using Notrelix.Application.Features.Chat.ReadState.DTOs;

namespace Notrelix.Application.Features.Chat.ReadState.Services;

/// <summary>
/// Announces a person's read-state change on their own per-user stream (UC-CHT-C09), so their other devices update
/// their badges. One outbox fact per space, enrolled in the request's transaction and published after commit.
/// </summary>
public static class ReadStateRealtime
{
    public static void Announce(
        IIntegrationEventCollector events,
        Guid? accountId,
        Guid workspaceId,
        Guid userId,
        IEnumerable<SpaceUnreadDto> spaces,
        DateTimeOffset now)
    {
        foreach (var space in spaces)
        {
            events.Add(ChatRealtime.Unread(
                accountId, workspaceId, userId, space.SpaceId, space.UnreadCount, space.MentionCount,
                correlationId: null, causationId: null, now));
        }
    }
}
