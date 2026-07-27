using static Notrelix.Domain.Common.Exceptions.CommonRuleCodes;

namespace Notrelix.Domain.Common;

/// <summary>
/// Aggregate root with soft-delete lifecycle (Version + deletion).
/// Use this for aggregates that have public SoftDelete/Restore with business logic.
/// </summary>
public abstract class SoftDeletableAggregateRoot : AggregateRoot
{
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
    public string? DeleteReason { get; private set; }
    public DateTimeOffset? RestoredAt { get; private set; }
    public Guid? RestoredBy { get; private set; }

    protected SoftDeletableAggregateRoot() : base() { }
    protected SoftDeletableAggregateRoot(Guid id) : base(id) { }

    protected bool MarkDeleted(Guid? deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (deletedAt == default || deletedAt == DateTimeOffset.MinValue)
            throw new BusinessRuleException(Common_InvalidDeletionTime, "Deleted timestamp must be a valid date.");

        if (IsDeleted) return false;

        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        DeleteReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        IsDeleted = true;
        return true;
    }

    protected bool MarkRestored(Guid? restoredBy, DateTimeOffset restoredAt)
    {
        if (restoredAt == default || restoredAt == DateTimeOffset.MinValue)
            throw new BusinessRuleException(Common_InvalidRestoreTime, "Restored timestamp must be a valid date.");

        if (!IsDeleted) return false;

        DeletedAt = null;
        DeletedBy = null;
        DeleteReason = null;
        IsDeleted = false;
        RestoredAt = restoredAt;
        RestoredBy = restoredBy;
        return true;
    }

    protected void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new BusinessRuleException(Common_EntityAlreadyDeleted, $"{GetType().Name} with Id '{Id}' has been deleted and cannot be modified.");
    }
}
