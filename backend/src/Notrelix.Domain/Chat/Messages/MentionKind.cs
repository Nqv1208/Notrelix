namespace Notrelix.Domain.Chat.Messages;

/// <summary>
/// What a span of a message's text refers to (CHT-010, UC-CHT-G01, G03, G04): a person, every member (@channel), every member
/// who is present (@here) or another channel.
/// </summary>
public enum MentionKind
{
    User,
    ChannelLink,
    BroadcastChannel,
    BroadcastHere,
}

/// <summary>The persisted and wire spelling of <see cref="MentionKind"/>. The spelling is part of the data contract.</summary>
public static class MentionKindWire
{
    public const string UserKind = "user";
    public const string ChannelLinkKind = "channel-link";
    public const string BroadcastChannelKind = "broadcast-channel";
    public const string BroadcastHereKind = "broadcast-here";

    public static string ToWire(this MentionKind kind) => kind switch
    {
        MentionKind.User => UserKind,
        MentionKind.ChannelLink => ChannelLinkKind,
        MentionKind.BroadcastChannel => BroadcastChannelKind,
        MentionKind.BroadcastHere => BroadcastHereKind,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown mention kind."),
    };

    /// <summary>The kind for a wire value, or null when the value is not a mention kind.</summary>
    public static MentionKind? MentionKindFromWire(string? value) => value switch
    {
        UserKind => MentionKind.User,
        ChannelLinkKind => MentionKind.ChannelLink,
        BroadcastChannelKind => MentionKind.BroadcastChannel,
        BroadcastHereKind => MentionKind.BroadcastHere,
        _ => null,
    };

    public static bool IsBroadcast(this MentionKind kind) => kind is MentionKind.BroadcastChannel or MentionKind.BroadcastHere;
}
