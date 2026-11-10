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
}
