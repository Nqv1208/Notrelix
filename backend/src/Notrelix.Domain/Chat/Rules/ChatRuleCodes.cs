namespace Notrelix.Domain.Chat.Rules;

/// <summary>
/// Stable error codes for the Chat bounded context. The values are part of the API contract
/// and must not change once published.
/// </summary>
public static class ChatRuleCodes
{
    public const string SpaceNotFound = "space-not-found";
    public const string ChannelArchived = "channel-archived";
    public const string NameConflict = "name-conflict";
    public const string VersionConflict = "version-conflict";
    public const string RateLimited = "rate-limited";
    public const string LimitReached = "limit-reached";

    /// <summary>The last manager of a channel may not leave or be removed while the channel is live.</summary>
    public const string LastManager = "last-manager";

    /// <summary>A space kind whose commands are not part of the current phase was requested.</summary>
    public const string KindNotSupported = "kind-not-supported";

    /// <summary>A sidebar position is not a finite number.</summary>
    public const string InvalidPosition = "invalid-position";

    /// <summary>A message's text is empty or longer than the limit after trim.</summary>
    public const string MessageContentInvalid = "message-content-invalid";

    /// <summary>A message sequence must start at 1; the store assigns it.</summary>
    public const string MessageSequenceInvalid = "message-sequence-invalid";

    /// <summary>A deleted message is a tombstone and cannot be edited.</summary>
    public const string MessageDeleted = "message-deleted";

    /// <summary>Only the author of a message edits or deletes it (UC-CHT-B04, B05).</summary>
    public const string MessageNotAuthor = "message-not-author";

    /// <summary>A system message announces a change and cannot be edited or deleted by a person (CHT-007, UC-CHT-B09).</summary>
    public const string SystemMessageImmutable = "system-message-immutable";

    /// <summary>A system message or a message that is not live cannot be pinned (UC-CHT-F06).</summary>
    public const string MessageNotPinnable = "message-not-pinnable";

    /// <summary>A reply targets a message that is not a root of a thread: a reply itself or a system message (UC-CHT-F01).</summary>
    public const string ThreadRootInvalid = "thread-root-invalid";

    /// <summary>A system message announces a change and carries no reactions (UC-CHT-G06).</summary>
    public const string MessageNotReactable = "message-not-reactable";

    /// <summary>The reaction kind is not on the palette (400, UC-CHT-G06).</summary>
    public const string ReactionKindInvalid = "reaction-kind-invalid";

    /// <summary>A person names themselves as the other side of a direct conversation or a block (400, UC-CHT-E01, E06).</summary>
    public const string SelfTarget = "self-target";

    /// <summary>A group conversation needs at least three participants, the caller included (400, UC-CHT-E02).</summary>
    public const string GroupTooSmall = "group-too-small";
}
