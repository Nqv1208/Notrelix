using static Notrelix.Domain.Common.Exceptions.CommonRuleCodes;

namespace Notrelix.Domain.Common;

public abstract class AuditableEntity : Entity
{
    internal readonly record struct PendingAuditUpdate(
        Guid? ActorId,
        DateTimeOffset OccurredAt);

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
                Common_Audit_InvalidTimestamp,
                "Created timestamp must be a valid date.");

        if (createdBy.HasValue && createdBy.Value == Guid.Empty)
            throw new BusinessRuleException(
                Common_Audit_EmptyActor,
                "CreatedBy actor cannot be Guid.Empty.");

        if (CreatedAt != default)
            throw new BusinessRuleException(
                Common_Audit_CreatedAtAlreadySet,
                "CreatedAt has already been set and cannot be changed.");

        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    internal PendingAuditUpdate PrepareAuditUpdate(
        Guid? actorId,
        DateTimeOffset occurredAt)
    {
        ValidateAuditUpdate(actorId, occurredAt);
        return new PendingAuditUpdate(actorId, occurredAt);
    }

    internal void ApplyAuditUpdate(PendingAuditUpdate update)
    {
        UpdatedBy = update.ActorId;
        UpdatedAt = update.OccurredAt;
    }

    private void SetAuditOnUpdate(Guid? updatedBy, DateTimeOffset updatedAt)
    {
        ValidateAuditUpdate(updatedBy, updatedAt);
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

    private void ValidateAuditUpdate(Guid? actorId, DateTimeOffset occurredAt)
    {
        if (occurredAt == default || occurredAt == DateTimeOffset.MinValue)
            throw new BusinessRuleException(
                Common_Audit_InvalidTimestamp,
                "Updated timestamp must be a valid date.");

        if (actorId.HasValue && actorId.Value == Guid.Empty)
            throw new BusinessRuleException(
                Common_Audit_EmptyActor,
                "UpdatedBy actor cannot be Guid.Empty.");

        if (CreatedAt != default && occurredAt < CreatedAt)
            throw new BusinessRuleException(
                Common_Audit_UpdatedAtBeforeCreatedAt,
                "Updated timestamp cannot be earlier than created timestamp.");

        if (UpdatedAt.HasValue && occurredAt < UpdatedAt.Value)
            throw new BusinessRuleException(
                Common_Audit_UpdatedAtRegression,
                "Updated timestamp cannot be earlier than previous UpdatedAt.");
    }
}
