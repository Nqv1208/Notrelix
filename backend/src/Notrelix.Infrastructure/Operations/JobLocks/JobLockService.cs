namespace Notrelix.Infrastructure.Operations.JobLocks;

/// <summary>
/// Skeleton distributed job-lock service (v4 §16). Idempotency and job locks are
/// an Infrastructure/Ops concern (never in Domain). Real implementation acquires
/// an atomic lock (e.g. Redis/Postgres advisory lock) for workers. Not yet wired.
/// </summary>
public sealed class JobLockService
{
    // TODO(v4 §16): TryAcquireAsync(key, ttl) / ReleaseAsync(key, token).
    // Backed by a distributed store; no EF schema added here.
}
