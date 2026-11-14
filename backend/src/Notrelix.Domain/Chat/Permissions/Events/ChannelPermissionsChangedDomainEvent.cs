namespace Notrelix.Domain.Chat.Permissions.Events;

/// <summary>A channel's permission overrides were set or cleared (UC-CHT-H01); clients refresh what they may do.</summary>
[EventName("chat.permissions-changed")]
public sealed record ChannelPermissionsChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
