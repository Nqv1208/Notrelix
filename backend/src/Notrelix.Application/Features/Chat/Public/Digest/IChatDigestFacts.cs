namespace Notrelix.Application.Features.Chat.Public.Digest;

/// <summary>One chat notification a person has not read, as Collaboration holds it: the space and message it is about and why it exists.</summary>
public sealed record ChatDigestCandidate(Guid NotificationId, Guid SpaceId, Guid MessageId, string Reason, DateTimeOffset At);

/// <summary>
/// What Chat lets a digest say about one candidate, decided now from Chat's own rows (UC-CHT-H09). <see cref="Preview"/> is the current
/// message text, at most <see cref="IChatDigestFacts.PreviewMaxLength"/> characters, which the recipient may see; Collaboration shortens it.
/// <see cref="SpaceName"/> is null for a direct or group conversation. <see cref="Path"/> is the product path that opens the space.
/// </summary>
public sealed record ChatDigestFact(
    Guid NotificationId,
    Guid SpaceId,
    string? SpaceName,
    Guid MessageId,
    string Reason,
    DateTimeOffset At,
    string? Preview,
    string Path);

/// <summary>
/// Chat-published fact for the missed-mention digest (UC-CHT-H09, CHT-019): which of a person's unread chat notifications still deserve a
/// line in an email, and what the line may say. The decision is made at send time, never from the age of the notification: the person
/// must still be an active member of a live space (a person who left, was removed or lost access gets nothing), the space must not be
/// muted for them, the message must still exist and not be read in Chat, a thread reply needs a followed, unmuted thread, and the author
/// is never a recipient. Collaboration decides whether and how to send; it never reads Chat persistence.
/// </summary>
public interface IChatDigestFacts
{
    public const int PreviewMaxLength = 400;

    Task<IReadOnlyList<ChatDigestFact>> DeliverableAsync(
        Guid workspaceId,
        Guid userId,
        IReadOnlyCollection<ChatDigestCandidate> candidates,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
