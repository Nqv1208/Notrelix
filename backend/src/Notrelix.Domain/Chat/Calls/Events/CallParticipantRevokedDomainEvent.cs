namespace Notrelix.Domain.Chat.Calls.Events;

/// <summary>
/// A person's access to a call's space ended while they were in the call, and the call released them. The media room must drop them too;
/// that is the work of the event's consumer, which retries until it succeeds, while the database side (the participation closed) has
/// already committed with this fact.
/// </summary>
[EventName("chat.call-participant-revoked")]
public sealed record CallParticipantRevokedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    Guid CallId,
    Guid PersonId,
    string RoomKey,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
