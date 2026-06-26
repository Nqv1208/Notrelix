namespace Notrelix.Domain.Common;

public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAt { get; protected set; }
    public Guid? CreatedBy { get; protected set; }
    public DateTimeOffset? UpdatedAt { get; protected set; }
    public Guid? UpdatedBy { get; protected set; }

    protected AuditableEntity() : base()
    {
    }

    protected AuditableEntity(Guid id) : base(id)
    {
    }

    public void SetAuditOnCreate(Guid? createdBy, DateTimeOffset createdAt)
    {
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public void SetAuditOnUpdate(Guid? updatedBy, DateTimeOffset updatedAt)
    {
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }
}
