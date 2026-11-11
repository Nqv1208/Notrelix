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

    /// <summary>The longest client message id, in characters. The client picks it; the server only keeps it.</summary>
    public const int ClientMessageIdMaxLength = 128;

    /// <summary>The longest channel name, in characters (UC-CHT-A01).</summary>
    public const int ChannelNameMaxLength = 80;

    /// <summary>The longest channel topic, in characters. Not fixed by the product docs yet; a Phase 1 default.</summary>
    public const int ChannelTopicMaxLength = 250;

    /// <summary>The longest channel group name, in characters (UC-CHT-A02).</summary>
    public const int ChannelGroupNameMaxLength = 60;
}
