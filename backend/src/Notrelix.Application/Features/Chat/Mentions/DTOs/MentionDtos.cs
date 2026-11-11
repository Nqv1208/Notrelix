namespace Notrelix.Application.Features.Chat.Mentions.DTOs;

/// <summary>
/// One mention a client sends with a message (UC-CHT-G01, G03, G04). Kind is user, channel-link, broadcast-channel or
/// broadcast-here. Start and Length count UTF-16 characters of the content as sent. A person is named by UserId, a channel by
/// SpaceId; a broadcast names neither.
/// </summary>
public sealed record MentionInput(string? Kind, int Start, int Length, Guid? UserId, Guid? SpaceId);

/// <summary>
/// One stored mention as a reader sees it. Start and Length count UTF-16 characters of the stored content. A channel link the
/// reader cannot see is left out of the list, so the reader sees plain text only (UC-CHT-G04).
/// </summary>
public sealed record MentionDto(string Kind, int Start, int Length, Guid? UserId, Guid? SpaceId);

/// <summary>
/// What the send did not do for one mention, shown to the sender only (UC-CHT-G02, G03, G04). Code is cannot-view (the person is
/// not in the channel: the mention is stored, nobody is notified and nothing is granted), unknown (not a person the sender can
/// mention: shown as text), link-not-visible (not a channel the sender can see: shown as text), broadcast-not-allowed (the
/// sender has no broadcast action: shown as text) or no-audience (the channel has no one else to reach: shown as text). UserId
/// is set for cannot-view only.
/// </summary>
public sealed record MentionNoticeDto(string Code, int Start, int Length, Guid? UserId);

/// <summary>
/// A person the composer can mention in a space (UC-CHT-G01, G02). InSpace is false for a person of the Workspace who is not in
/// the channel: the picker marks them and the send warns (COL-030).
/// </summary>
public sealed record MentionableDto(Guid UserId, bool InSpace);

/// <summary>
/// The mention picker of a space: the people, whether the caller may broadcast, and the member count above which a broadcast
/// needs the confirmation (UC-CHT-G03). MemberCount is the number of active members.
/// </summary>
public sealed record MentionablesDto(IReadOnlyList<MentionableDto> People, bool CanBroadcast, int BroadcastConfirmAbove, int MemberCount);
