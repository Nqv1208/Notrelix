using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Common;

public abstract class SoftDeletableEntity : AuditableEntity
{
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
    public string? DeleteReason { get; private set; }
    public DateTimeOffset? RestoredAt { get; private set; }
    public Guid? RestoredBy { get; private set; }
    
    protected SoftDeletableEntity() : base() { }
    protected SoftDeletableEntity(Guid id) : base(id) { }
    
    public virtual void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        DeleteReason = reason;
    }
    
    public virtual void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        DeleteReason = null;
        RestoredAt = restoredAt;
        RestoredBy = restoredBy;
        UpdatedBy = restoredBy;
        UpdatedAt = restoredAt;
    }
    
    protected void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException($"{GetType().Name} with Id '{Id}' has been deleted and cannot be modified.");
    }
}
