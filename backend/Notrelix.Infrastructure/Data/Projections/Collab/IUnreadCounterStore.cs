namespace Notrelix.Infrastructure.Data.Projections.Collab;

public interface IUnreadCounterStore
{
    Task<int> IncrementAsync(Guid workspaceId, Guid userId, string counterType, CancellationToken ct = default);
    Task<int> DecrementAsync(Guid workspaceId, Guid userId, string counterType, int delta = 1, CancellationToken ct = default);
    Task<int> GetAsync(Guid workspaceId, Guid userId, string counterType, CancellationToken ct = default);
    Task RebuildFromNotificationsAsync(Guid workspaceId, Guid userId, string counterType, CancellationToken ct = default);
}
