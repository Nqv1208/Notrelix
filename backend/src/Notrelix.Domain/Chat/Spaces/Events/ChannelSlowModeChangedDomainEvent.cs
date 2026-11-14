namespace Notrelix.Domain.Chat.Spaces.Events;

/// <summary>A channel's slow mode was set or turned off (UC-CHT-H08); clients of the channel refresh it.</summary>
[EventName("chat.slow-mode-changed")]
public sealed record ChannelSlowModeChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
