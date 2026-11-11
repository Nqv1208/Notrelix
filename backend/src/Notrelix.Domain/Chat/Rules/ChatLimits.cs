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
}
