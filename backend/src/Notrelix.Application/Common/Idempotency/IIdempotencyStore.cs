namespace Notrelix.Application.Common.Idempotency;

public interface IIdempotencyStore
{
    Task<bool> TryAcquireLockAsync(string key, TimeSpan ttl);
    Task ReleaseLockAsync(string key);
    Task SetResultAsync<T>(string key, T result);
    Task<object?> GetResultAsync(string key);
}
