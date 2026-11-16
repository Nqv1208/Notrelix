namespace Notrelix.Application.Features.Chat.Calls;

/// <summary>A media room the gateway created.</summary>
public sealed record CallRoom(string RoomKey, string Region);

/// <summary>
/// A room join ticket for one person (SPEC-CHAT-MEDIA-CALLS section 11): scoped to one room and one person, short-lived, and never
/// written to a log.
/// </summary>
public sealed record CallJoinToken(string Url, string Token, DateTimeOffset ExpiresAt);

/// <summary>A signed media server event after verification (a participant joined or left, the room finished).</summary>
public sealed record CallMediaWebhookEvent(string EventId, string Kind, string RoomKey, Guid? PersonId);

/// <summary>
/// The only way Chat talks to the media plane (SYS-ADR-002 decision 7). Self-hosted LiveKit and LiveKit Cloud are two adapters
/// behind it; only a fake exists in Phase 5a. A failure throws <see cref="CallMediaUnavailableException"/>, which the handlers turn
/// into the stable code <c>chat.calls.unavailable</c> (nothing stays Ringing).
/// </summary>
public interface IChatCallMediaGateway
{
    Task<CallRoom> CreateRoomAsync(string roomKey, string region, int maxParticipants, CancellationToken cancellationToken);

    Task<CallJoinToken> IssueTokenAsync(
        string roomKey, Guid personId, bool canPublishVideo, TimeSpan ttl, CancellationToken cancellationToken);

    Task EndRoomAsync(string roomKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListRoomsAsync(CancellationToken cancellationToken);

    /// <summary>Verifies the signature over the raw body before anything is parsed; null when the signature is wrong.</summary>
    CallMediaWebhookEvent? VerifyWebhook(string rawBody, string? authorization);
}

/// <summary>The media plane cannot be reached.</summary>
public sealed class CallMediaUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
