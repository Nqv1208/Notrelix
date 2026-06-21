namespace Notrelix.Infrastructure.Data.Ops.Entities;

public sealed class JobLockRecord
{
    public Guid Id { get; private set; }
    public string LockKey { get; private set; } = null!;
    public string LockedBy { get; private set; } = null!;
    public long FencingToken { get; private set; }
    public DateTimeOffset LockedUntil { get; private set; }
    public string MetadataJson { get; private set; } = null!;
    public DateTimeOffset AcquiredAt { get; private set; }
    public DateTimeOffset? RenewedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private JobLockRecord() { }

    public static JobLockRecord Create(
        Guid id,
        string lockKey,
        string lockedBy,
        long fencingToken,
        DateTimeOffset lockedUntil,
        string metadataJson,
        DateTimeOffset acquiredAt,
        DateTimeOffset createdAt)
    {
        return new JobLockRecord
        {
            Id = id,
            LockKey = lockKey,
            LockedBy = lockedBy,
            FencingToken = fencingToken,
            LockedUntil = lockedUntil,
            MetadataJson = metadataJson,
            AcquiredAt = acquiredAt,
            CreatedAt = createdAt,
        };
    }
}
