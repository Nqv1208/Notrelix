namespace Notrelix.Application.Features.Chat.Reactions.DTOs;

/// <summary>
/// One kind of reaction on a message as readers see it: the palette emoji, how many people reacted with it, and whether the
/// caller is one of them (UC-CHT-G06). The count is the membership set's size, never a stored counter.
/// </summary>
public sealed record ReactionSummaryDto(string Emoji, int Count, bool ReactedByMe);

/// <summary>The reaction summary of one message after a change (UC-CHT-G06, G07).</summary>
public sealed record MessageReactionsDto(Guid MessageId, IReadOnlyList<ReactionSummaryDto> Reactions);

/// <summary>The reaction and whether this request made it (false when the person already had it).</summary>
public sealed record ReactionResultDto(MessageReactionsDto Message, bool Created);

/// <summary>One person's reaction, with when they reacted (UC-CHT-G07: who reacted).</summary>
public sealed record ReactorDto(string Emoji, Guid UserId, DateTimeOffset ReactedAt);

/// <summary>A page of reactors, earliest reaction first. NextCursor is null on the last page.</summary>
public sealed record ReactorPageDto(IReadOnlyList<ReactorDto> Items, string? NextCursor);
