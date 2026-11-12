namespace Notrelix.Application.Features.Chat.Search.DTOs;

/// <summary>One matched stretch of a snippet, in UTF-16 code units of the snippet text. Never markup: the client draws the highlight.</summary>
public sealed record ChatSearchRangeDto(int Start, int Length);

/// <summary>
/// A short plain-text excerpt of a message and where the typed text matched inside it. <see cref="CutBefore"/> and <see cref="CutAfter"/>
/// say the excerpt starts or ends inside the message, so the client can show an ellipsis. The text is the author's own text, never HTML.
/// </summary>
public sealed record ChatSearchSnippetDto(string Text, IReadOnlyList<ChatSearchRangeDto> Ranges, bool CutBefore, bool CutAfter);

/// <summary>
/// One message hit (UC-CHT-H07). The jump target is the space, the message and, for a thread reply, the root of its thread. For a direct or
/// group conversation <see cref="SpaceName"/> is empty or the group's name and the client names the conversation by its people.
/// </summary>
public sealed record ChatSearchHitDto(
    Guid MessageId,
    Guid SpaceId,
    string SpaceName,
    string SpaceKind,
    bool SpaceArchived,
    Guid AuthorId,
    DateTimeOffset CreatedAt,
    long Sequence,
    Guid? ThreadRootId,
    bool AlsoSentToChannel,
    ChatSearchSnippetDto Snippet);

/// <summary>A page of hits, newest first. <see cref="NextCursor"/> is null on the last page. There is no total: a total would count hidden spaces' messages' absence.</summary>
public sealed record ChatSearchPageDto(IReadOnlyList<ChatSearchHitDto> Items, string? NextCursor);
