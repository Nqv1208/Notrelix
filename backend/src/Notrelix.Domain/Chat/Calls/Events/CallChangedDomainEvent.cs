namespace Notrelix.Domain.Chat.Calls.Events;

/// <summary>
/// A call changed state or its roster changed (SYS-ADR-002 decision 8): started, answered, joined, left, media state, ended. The
/// roster is the people currently in the call. Clients of the space and, while it rings, the invited people refresh from it.
/// </summary>
[EventName("chat.call-changed")]
public sealed record CallChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    Guid CallId,
    Guid InitiatorId,
    Guid ActorId,
    CallKind Kind,
    CallState State,
    CallEndReason? EndReason,
    bool RingsMembers,
    long Version,
    IReadOnlyList<Guid> ParticipantIds,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
