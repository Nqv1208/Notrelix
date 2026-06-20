namespace Notrelix.Infrastructure.Data.Ops.Stores;

public interface IIdempotencyStore
{
    Task<Guid?> TryAcquireAsync(
        string idempotencyKey,
        string scope,
        string requestMethod,
        string requestPath,
        string requestHash,
        Guid? workspaceId,
        Guid? userId,
        DateTimeOffset expiresAt,
        DateTimeOffset now,
        CancellationToken ct = default);

    Task<bool> CompleteAsync(
        Guid id,
        int responseStatusCode,
        string? responseBodyJson,
        DateTimeOffset now,
        CancellationToken ct = default);

    Task<bool> FailAsync(
        Guid id,
        string errorMessage,
        DateTimeOffset now,
        CancellationToken ct = default);
}
