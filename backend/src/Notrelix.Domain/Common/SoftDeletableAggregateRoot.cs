using static Notrelix.Domain.Common.Exceptions.CommonRuleCodes;

namespace Notrelix.Domain.Common;

public abstract class SoftDeletableAggregateRoot : AggregateRoot
{
    public readonly record struct PendingDeletion(
        PendingAuditUpdate Audit,
        Guid? ActorId,
        DateTimeOffset OccurredAt,
        string? Reason);

    public readonly record struct PendingRestore(
        PendingAuditUpdate Audit,
        Guid? ActorId,
        DateTimeOffset OccurredAt);

    // DeletedAt is the single persisted soft-delete fact; IsDeleted is derived so
    // lifecycle guards hold after EF reloads an entity. _isDeleted mirrors it only
    // for mappings that still store an is_deleted column (EF binds it by name).
#pragma warning disable CS0414 // read by EF through its backing-field convention, not by code
    private bool _isDeleted;
#pragma warning restore CS0414

    public bool IsDeleted => DeletedAt is not null;
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
    public string? DeleteReason { get; private set; }

    protected SoftDeletableAggregateRoot() : base() { }
    protected SoftDeletableAggregateRoot(Guid id) : base(id) { }

    protected PendingDeletion PrepareDeletion(
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

    protected void ApplyDeletion(PendingDeletion deletion)
    {
        DeletedAt = deletion.OccurredAt;
        DeletedBy = deletion.ActorId;
        DeleteReason = deletion.Reason;
        _isDeleted = true;
        ApplyAuditUpdate(deletion.Audit);
    }

    protected PendingRestore PrepareRestore(
        Guid? actorId,
        DateTimeOffset occurredAt)
    {
        if (occurredAt == default || occurredAt == DateTimeOffset.MinValue)
            throw new BusinessRuleException(Common_InvalidRestoreTime, "Restored timestamp must be a valid date.");

        var audit = PrepareAuditUpdate(actorId, occurredAt);
        return new PendingRestore(audit, actorId, occurredAt);
    }

    protected void ApplyRestore(PendingRestore restore)
    {
        DeletedAt = null;
        DeletedBy = null;
        DeleteReason = null;
        _isDeleted = false;
        ApplyAuditUpdate(restore.Audit);
    }

    protected void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new BusinessRuleException(Common_EntityAlreadyDeleted, $"{GetType().Name} with Id '{Id}' has been deleted and cannot be modified.");
    }

    protected void EnsureDeleted()
    {
        if (!IsDeleted)
            throw new BusinessRuleException(Common_EntityNotDeleted, $"{GetType().Name} with Id '{Id}' is not deleted.");
    }
}
