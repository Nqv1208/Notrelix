
namespace Notrelix.Infrastructure.Ops;

public sealed class DevNullIdempotencyStore : IIdempotencyStore
{
    public Task<bool> TryAcquireLockAsync(string key, TimeSpan ttl)
        => Task.FromResult(true);

    public Task ReleaseLockAsync(string key)
        => Task.CompletedTask;

    public Task SetResultAsync<T>(string key, T result)
        => Task.CompletedTask;

    public Task<object?> GetResultAsync(string key)
        => Task.FromResult<object?>(null);
}
