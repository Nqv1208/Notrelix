namespace Notrelix.Domain.Common;

public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAt { get; protected set; }
    public Guid? CreatedBy { get; protected set; }
    public DateTimeOffset? UpdatedAt { get; protected set; }
    public Guid? UpdatedBy { get; protected set; }
    
    protected AuditableEntity() : base() 
    { 
        CreatedAt = DateTimeOffset.UtcNow; 
    }
    
    protected AuditableEntity(Guid id) : base(id) 
    { 
        CreatedAt = DateTimeOffset.UtcNow; 
    }
    
    public void SetAuditOnCreate(Guid createdBy) 
    { 
        CreatedBy = createdBy; 
        CreatedAt = DateTimeOffset.UtcNow; 
    }
    
    public void SetAuditOnUpdate(Guid updatedBy) 
    { 
        UpdatedBy = updatedBy; 
        UpdatedAt = DateTimeOffset.UtcNow; 
    }
}
