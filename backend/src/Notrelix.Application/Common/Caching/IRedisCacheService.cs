namespace Notrelix.Application.Common.Caching;

public interface IRedisCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<long> IncrementAsync(string key, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically reads and removes a value in a single Redis operation (GET + DEL).
    /// Exactly one concurrent caller receives the value; all others receive default.
    /// </summary>
    Task<T?> GetDeleteAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically increments a counter and deletes another key when the
    /// counter exceeds <paramref name="max"/>. Used for cumulative attempt
    /// budgets that must not reset on wall-clock windows.
    /// Returns the new counter value and whether the delete was performed.
    /// </summary>
    Task<(long Attempts, bool Exceeded)> IncrementWithConditionalDeleteAsync(
        string incrementKey, string deleteKey, long max, TimeSpan? ttl = null,
        CancellationToken cancellationToken = default);
}
