using static Notrelix.Domain.Common.Exceptions.CommonRuleCodes;

namespace Notrelix.Domain.Common;

/// <summary>
/// Aggregate root with soft-delete lifecycle (Version + deletion).
/// Use this for aggregates that have public SoftDelete/Restore with business logic.
/// </summary>
public abstract class SoftDeletableAggregateRoot : AggregateRoot
{
    internal readonly record struct PendingDeletion(
        PendingAuditUpdate Audit,
        Guid? ActorId,
        DateTimeOffset OccurredAt,
        string? Reason);

    internal readonly record struct PendingRestore(
        PendingAuditUpdate Audit,
        Guid? ActorId,
        DateTimeOffset OccurredAt);

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
    public string? DeleteReason { get; private set; }
    public DateTimeOffset? RestoredAt { get; private set; }
    public Guid? RestoredBy { get; private set; }

    protected SoftDeletableAggregateRoot() : base() { }
    protected SoftDeletableAggregateRoot(Guid id) : base(id) { }

    internal PendingDeletion PrepareDeletion(
        Guid? actorId,
        DateTimeOffset occurredAt,
        string? reason)
    {
        if (occurredAt == default || occurredAt == DateTimeOffset.MinValue)
            throw new BusinessRuleException(Common_InvalidDeletionTime, "Deleted timestamp must be a valid date.");

        var audit = PrepareAuditUpdate(actorId, occurredAt);
        var normalizedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        return new PendingDeletion(audit, actorId, occurredAt, normalizedReason);
    }

    internal void ApplyDeletion(PendingDeletion deletion)
    {
        DeletedAt = deletion.OccurredAt;
        DeletedBy = deletion.ActorId;
        DeleteReason = deletion.Reason;
        IsDeleted = true;
        ApplyAuditUpdate(deletion.Audit);
    }

    internal PendingRestore PrepareRestore(
        Guid? actorId,
        DateTimeOffset occurredAt)
    {
        if (occurredAt == default || occurredAt == DateTimeOffset.MinValue)
            throw new BusinessRuleException(Common_InvalidRestoreTime, "Restored timestamp must be a valid date.");

        var audit = PrepareAuditUpdate(actorId, occurredAt);
        return new PendingRestore(audit, actorId, occurredAt);
    }

    internal void ApplyRestore(PendingRestore restore)
    {
        DeletedAt = null;
        DeletedBy = null;
        DeleteReason = null;
        IsDeleted = false;
        RestoredAt = restore.OccurredAt;
        RestoredBy = restore.ActorId;
        ApplyAuditUpdate(restore.Audit);
    }

    protected void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new BusinessRuleException(Common_EntityAlreadyDeleted, $"{GetType().Name} with Id '{Id}' has been deleted and cannot be modified.");
    }
}
