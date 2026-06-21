namespace Notrelix.Infrastructure.Data.Ops.Stores;

public interface IJobLockManager
{
    Task<long?> AcquireAsync(string lockKey, string lockedBy, TimeSpan duration, CancellationToken ct = default);
    Task<bool> RenewAsync(string lockKey, string lockedBy, TimeSpan duration, CancellationToken ct = default);
    Task<bool> ReleaseAsync(string lockKey, string lockedBy, CancellationToken ct = default);
}
