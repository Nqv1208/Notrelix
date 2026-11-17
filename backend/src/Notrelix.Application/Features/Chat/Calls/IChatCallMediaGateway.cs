namespace Notrelix.Application.Features.Chat.Calls;

/// <summary>A media room the gateway created.</summary>
public sealed record CallRoom(string RoomKey, string Region);

/// <summary>
/// A room join ticket for one person (SPEC-CHAT-MEDIA-CALLS section 11): scoped to one room and one person, short-lived, and never
/// written to a log.
/// </summary>
public sealed record CallJoinToken(string Url, string Token, DateTimeOffset ExpiresAt);

/// <summary>
/// A signed media server event after verification (a participant joined or left, the room finished). <paramref name="ParticipantJoinedAt"/> is
/// when the media server seated that participant (the session the event belongs to); null when the server did not say.
/// </summary>
public sealed record CallMediaWebhookEvent(string EventId, string Kind, string RoomKey, Guid? PersonId, DateTimeOffset? ParticipantJoinedAt = null);

/// <summary>
/// The only way Chat talks to the media plane (SYS-ADR-002 decision 7). Self-hosted LiveKit and LiveKit Cloud are two adapters
/// behind it; an in-memory stand-in and the LiveKit adapter implement it, chosen by <c>Chat:Calls:Media:Provider</c>. A failure throws <see cref="CallMediaUnavailableException"/>, which the handlers turn
/// into the stable code <c>chat.calls.unavailable</c> (nothing stays Ringing).
/// </summary>
public interface IChatCallMediaGateway
{
    Task<CallRoom> CreateRoomAsync(string roomKey, string region, int maxParticipants, CancellationToken cancellationToken);

    Task<CallJoinToken> IssueTokenAsync(
        string roomKey, Guid personId, bool canPublishVideo, TimeSpan ttl, CancellationToken cancellationToken);

    Task EndRoomAsync(string roomKey, CancellationToken cancellationToken);

    /// <summary>Removes one person from a live room at once: their access ended, so their media connection must not outlive it.</summary>
    Task RemoveParticipantAsync(string roomKey, Guid personId, CancellationToken cancellationToken);

    /// <summary>
    /// Applies a changed right to publish to a person who is connected now (a channel turned read-only): the media server updates the live
    /// connection, so it does not wait for the token to expire. A person who is not in the room is a no-op.
    /// </summary>
    Task UpdateParticipantPermissionAsync(string roomKey, Guid personId, bool canPublish, bool canPublishVideo, CancellationToken cancellationToken);

    /// <summary>The people currently connected to a room, for the reconciliation that drops anyone without an open participation.</summary>
    Task<IReadOnlyList<Guid>> ListParticipantsAsync(string roomKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListRoomsAsync(CancellationToken cancellationToken);

    /// <summary>Verifies the signature over the raw body before anything is parsed; null when the signature is wrong.</summary>
    CallMediaWebhookEvent? VerifyWebhook(string rawBody, string? authorization);
}

/// <summary>The media plane cannot be reached.</summary>
public sealed class CallMediaUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
