namespace Notrelix.Infrastructure.Operations.Cleanup;

/// <summary>
/// Skeleton cleanup job for processed outbox rows (v4 §16). Real implementation
/// runs as a scheduled worker deleting old Processed rows in small safe batches
/// with metrics. Registered as a hosted service only in the behavioral phase to
/// avoid changing runtime behavior now. Not yet wired.
/// </summary>
public sealed class OutboxCleanupJob
{
    // TODO(v4 §16): batch-delete ops.outbox_messages where status = Processed and
    // processed_at older than retention; emit metrics; safe small batches.
}
