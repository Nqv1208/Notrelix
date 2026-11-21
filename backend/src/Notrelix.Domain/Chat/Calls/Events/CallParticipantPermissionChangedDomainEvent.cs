namespace Notrelix.Domain.Chat.Calls.Events;

/// <summary>
/// A person's right to publish media changed while they are in a call (their channel turned read-only, or writable again). The media
/// room must apply it; that is the work of the event's consumer, which retries until it succeeds.
/// </summary>
[EventName("chat.call-participant-permission-changed")]
public sealed record CallParticipantPermissionChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    Guid CallId,
    Guid PersonId,
    string RoomKey,
    bool CanPublish,
    bool CanPublishVideo,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
