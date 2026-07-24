using static Notrelix.Domain.Common.Exceptions.BusinessRuleCodes;

namespace Notrelix.Domain.Common;

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

    /// <summary>
    /// Marks the entity as deleted. Returns false if already deleted (no-op).
    /// Only changes deletion state — concrete aggregates own audit, version, and events.
    /// </summary>
    protected bool MarkDeleted(Guid? deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (deletedAt == default || deletedAt == DateTimeOffset.MinValue)
            throw new BusinessRuleException(Common_EntityHasBeenDeleted, "Deleted timestamp must be a valid date.");

        if (IsDeleted) return false;

        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        DeleteReason = reason;
        return true;
    }

    /// <summary>
    /// Marks the entity as restored. Returns false if not deleted (no-op).
    /// Only changes deletion state — concrete aggregates own audit, version, and events.
    /// </summary>
    protected bool MarkRestored(Guid? restoredBy, DateTimeOffset restoredAt)
    {
        if (restoredAt == default || restoredAt == DateTimeOffset.MinValue)
            throw new BusinessRuleException(Common_EntityHasBeenDeleted, "Restored timestamp must be a valid date.");

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
            throw new BusinessRuleException(Common_EntityHasBeenDeleted, $"{GetType().Name} with Id '{Id}' has been deleted and cannot be modified.");
    }
}
