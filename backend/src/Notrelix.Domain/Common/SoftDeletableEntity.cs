using static Notrelix.Domain.Common.Exceptions.CommonRuleCodes;

namespace Notrelix.Domain.Common;

/// <summary>
/// Soft-delete capability for non-root child entities (e.g., ItemDependency).
/// Aggregate roots should use SoftDeletableAggregateRoot instead.
/// </summary>
public abstract class SoftDeletableEntity : AuditableEntity
{
    public bool IsDeleted => DeletedAt.HasValue;
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
    public string? DeleteReason { get; private set; }
    public DateTimeOffset? RestoredAt { get; private set; }
    public Guid? RestoredBy { get; private set; }

    protected SoftDeletableEntity() : base() { }
    protected SoftDeletableEntity(Guid id) : base(id) { }

    protected bool MarkDeleted(Guid? deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (deletedAt == default || deletedAt == DateTimeOffset.MinValue)
            throw new BusinessRuleException(Common_InvalidDeletionTime, "Deleted timestamp must be a valid date.");

        if (IsDeleted) return false;

        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        DeleteReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
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
