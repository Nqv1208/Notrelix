namespace Notrelix.Application.Features.Chat.ReadState.DTOs;

/// <summary>
/// The unread state of one space for the caller (UC-CHT-D01, D03, D04). UnreadCount counts messages after the boundary
/// that another person wrote and that are not deleted or system messages. MentionCount is 0 until mention facts exist (S2.4).
/// </summary>
public sealed record SpaceUnreadDto(
    Guid SpaceId,
    string Name,
    long ReadSequence,
    long LastSequence,
    int UnreadCount,
    long? FirstUnreadSequence,
    int MentionCount,
    string NotificationLevel,
    bool Muted);

/// <summary>
/// The caller's unread state across spaces. TotalUnread and TotalMentions sum the spaces that are not muted: a muted space
/// does not badge (CHT-019). Spaces are ordered by name.
/// </summary>
public sealed record UnreadSummaryDto(IReadOnlyList<SpaceUnreadDto> Spaces, int TotalUnread, int TotalMentions);

/// <summary>The read boundary of one space after a mark-read request (UC-CHT-D02).</summary>
public sealed record ReadBoundaryDto(Guid SpaceId, long ReadSequence, long LastSequence);

/// <summary>The notification settings of one space for the caller (UC-CHT-D05, D06). MutedUntil is set only for a timed mute.</summary>
public sealed record NotificationSettingsDto(Guid SpaceId, string NotificationLevel, bool Muted, DateTimeOffset? MutedUntil);
