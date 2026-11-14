namespace Notrelix.Application.Features.Collaboration.Public.Notifications;

/// <summary>
/// Why Chat hands a person a notification intent (UC-CHT-G09, CHT-019). The codes are persisted in the notification's deduplication
/// key and are part of the contract: never rename or reuse one.
/// </summary>
public static class ChatNotificationReasons
{
    /// <summary>The person was mentioned by id in the message.</summary>
    public const string Mention = "mention";

    /// <summary>A direct or group conversation received a message.</summary>
    public const string DirectMessage = "direct-message";

    /// <summary>A reply arrived in a thread the person follows.</summary>
    public const string ThreadReply = "thread-reply";

    /// <summary>The message carried an @channel broadcast.</summary>
    public const string Broadcast = "broadcast";

    public static readonly IReadOnlyList<string> All = [Mention, DirectMessage, ThreadReply, Broadcast];

    public static bool IsKnown(string? reason) => reason is not null && All.Contains(reason, StringComparer.Ordinal);
}

/// <summary>
/// The Chat fact an intent is about: where the message was sent and by whom. <see cref="Preview"/> is a short plain excerpt Chat decided
/// the recipients may see; Collaboration stores it as given and never reads Chat's tables. <see cref="SpaceName"/> is empty for a direct
/// conversation.
/// </summary>
public sealed record ChatNotificationSource(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    Guid MessageId,
    Guid? ThreadRootId,
    Guid ActorUserId,
    Guid? SourceEventId,
    string? SpaceName,
    string? Preview,
    DateTimeOffset At);

/// <summary>One person to notify and the reason, decided by Chat from its own facts.</summary>
public sealed record ChatNotificationIntent(Guid RecipientUserId, string Reason);

/// <summary>What one hand-over did: how many notifications were written, were already there, or were left out by the recipient's own choice.</summary>
public sealed record ChatNotificationDelivery(int Delivered, int Duplicates, int Suppressed)
{
    public static readonly ChatNotificationDelivery None = new(0, 0, 0);
}

/// <summary>
/// Collaboration-owned boundary for Chat notification intents (UC-CHT-G09, CHT-019). Chat decides who is a recipient and why (mention,
/// message in a conversation, thread reply, broadcast) from its own facts, and checks at delivery that the person can still see the
/// space and has not muted it. Collaboration decides delivery: it applies the recipient's preferences and thread mute, writes one Inbox
/// notification per message, recipient and reason (a repeat of the same fact adds nothing) and never notifies the actor. Provider
/// delivery is not notification truth. Chat never reads or writes Collaboration persistence.
/// </summary>
public interface IChatNotificationIntents
{
    /// <summary>Hands the intents of one message over. Idempotent by (message, recipient, reason). The notifications are saved before it returns.</summary>
    Task<ChatNotificationDelivery> DeliverAsync(
        ChatNotificationSource source,
        IReadOnlyCollection<ChatNotificationIntent> intents,
        CancellationToken cancellationToken);
}
