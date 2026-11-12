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

    /// <summary>The shortest retention window a Workspace can set, in days (UC-CHT-H05): a one-day window would remove a conversation before anyone read it.</summary>
    public const int RetentionMinDays = 7;

    /// <summary>The longest retention window a Workspace can set, in days (ten years, UC-CHT-H05).</summary>
    public const int RetentionMaxDays = 3650;

    /// <summary>The most messages one retention batch removes from one space (UC-CHT-H05). The job repeats until nothing expired is left.</summary>
    public const int RetentionBatchSize = 200;

    /// <summary>The most messages one chat export holds (UC-CHT-H06). A request that finds more fails with export-too-large and the Admin narrows the range.</summary>
    public const int ExportMaxMessages = 50_000;

    /// <summary>The most bytes one chat export file holds (UC-CHT-H06): 20 MiB.</summary>
    public const long ExportMaxBytes = 20L * 1024 * 1024;

    /// <summary>How long a ready export can be downloaded, in hours (UC-CHT-H06). After that the file is deleted.</summary>
    public const int ExportLifetimeHours = 24;
}
