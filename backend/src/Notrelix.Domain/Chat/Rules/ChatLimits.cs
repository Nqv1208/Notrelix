namespace Notrelix.Domain.Chat.Rules;

/// <summary>
/// Fixed limits for the Chat bounded context.
/// </summary>
public static class ChatLimits
{
    public const int MessageMaxLength = 4000;
    public const int PageSizeDefault = 50;
    public const int PageSizeMax = 100;
    public const int GroupCap = 8;
    public const int PinLimit = 50;

    /// <summary>
    /// The most distinct reaction kinds one message holds. The palette has six kinds (CHT-011, COL-009), so this bound is reached only
    /// if the palette grows; the handler checks it under the space lock.
    /// </summary>
    public const int ReactionKindsPerMessageMax = 6;

    /// <summary>The most reactions one person holds on one message (one per palette kind).</summary>
    public const int ReactionsPerUserPerMessageMax = 6;

    /// <summary>Days without a reply after which a thread is archived (UC-CHT-F05; foundation spec section 34).</summary>
    public const int ThreadInactivityDays = 7;

    /// <summary>The longest client message id, in characters. The client picks it; the server only keeps it.</summary>
    public const int ClientMessageIdMaxLength = 128;

    /// <summary>The longest reason a moderator gives for removing a message, in characters after trim (UC-CHT-H03).</summary>
    public const int ModerationReasonMaxLength = 500;

    /// <summary>The most overrides one channel holds: every role for every action.</summary>
    public const int PermissionOverridesMax = 18;

    /// <summary>The longest channel name, in characters (UC-CHT-A01).</summary>
    public const int ChannelNameMaxLength = 80;

    /// <summary>The longest channel topic, in characters. Not fixed by the product docs yet; a Phase 1 default.</summary>
    public const int ChannelTopicMaxLength = 250;

    /// <summary>The longest channel group name, in characters (UC-CHT-A02).</summary>
    public const int ChannelGroupNameMaxLength = 60;

    /// <summary>The most mentions (people, broadcasts and channel links together) one message holds (UC-CHT-G01 to G04).</summary>
    public const int MentionsPerMessageMax = 50;

    /// <summary>
    /// A channel with more active members than this asks for a confirmation before an @channel or @here is sent (CHT-010,
    /// UC-CHT-G03). The Workspace limit is not configurable yet, so this is the default.
    /// </summary>
    public const int BroadcastConfirmThreshold = 50;

    /// <summary>
    /// The longest slow mode a space can set, in seconds (UC-CHT-H08): the minimum time between two messages of one person.
    /// Zero turns it off.
    /// </summary>
    public const int SlowModeMaxSeconds = 3600;
}
