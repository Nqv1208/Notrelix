namespace Notrelix.Application.Features.Chat.Abstractions;

/// <summary>
/// Serializes the starts of one direct conversation (UC-CHT-E01). <see cref="LockPairAsync"/> holds a lock for the pair until the
/// request's transaction ends, so two starts of the same pair run one after the other: the second one reads the conversation the
/// first one stored. The lock is per pair, so starts of other pairs never wait for it.
/// </summary>
public interface IDirectConversationLock
{
    Task LockPairAsync(Guid workspaceId, Guid userLow, Guid userHigh, CancellationToken cancellationToken);
}
