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
}
