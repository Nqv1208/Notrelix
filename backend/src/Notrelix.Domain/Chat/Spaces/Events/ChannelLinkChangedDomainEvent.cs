namespace Notrelix.Domain.Chat.Spaces.Events;

/// <summary>A channel's link to a Board, Page or Team was set, replaced or removed (UC-CHT-H10); clients of the channel refresh it.</summary>
[EventName("chat.link-changed")]
public sealed record ChannelLinkChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
