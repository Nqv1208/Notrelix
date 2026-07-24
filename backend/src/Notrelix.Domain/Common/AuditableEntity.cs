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

    protected void SetAuditOnCreate(Guid? createdBy, DateTimeOffset createdAt)
    {
        if (createdAt == default || createdAt == DateTimeOffset.MinValue)
            throw new BusinessRuleException(
                BusinessRuleCodes.Common_Audit_InvalidTimestamp,
                "Created timestamp must be a valid date.");

        if (CreatedAt != default)
            throw new BusinessRuleException(
                BusinessRuleCodes.Common_Audit_CreatedAtAlreadySet,
                "CreatedAt has already been set and cannot be changed.");

        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    protected void SetAuditOnUpdate(Guid? updatedBy, DateTimeOffset updatedAt)
    {
        if (updatedAt == default || updatedAt == DateTimeOffset.MinValue)
            throw new BusinessRuleException(
                BusinessRuleCodes.Common_Audit_InvalidTimestamp,
                "Updated timestamp must be a valid date.");

        if (CreatedAt != default && updatedAt < CreatedAt)
            throw new BusinessRuleException(
                BusinessRuleCodes.Common_Audit_UpdatedAtBeforeCreatedAt,
                "Updated timestamp cannot be earlier than created timestamp.");

        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }
}
