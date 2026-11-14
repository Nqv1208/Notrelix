namespace Notrelix.Domain.Chat.Spaces.Events;

/// <summary>
/// The retention job removed messages from a space (UC-CHT-H05): they became tombstones or were purged. One fact per space and batch, with
/// no message ids and no content; clients of the channel refetch the history they hold.
/// </summary>
[EventName("chat.retention-applied")]
public sealed record ChannelRetentionAppliedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
