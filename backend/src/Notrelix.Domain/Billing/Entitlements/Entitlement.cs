using Notrelix.Domain.Billing.Entitlements.Events;
using Notrelix.Domain.Billing.Plans;
namespace Notrelix.Domain.Billing.Entitlements;

public class Entitlement : SoftDeletableAggregateRoot, IAccountScoped
{
    public Guid AccountId { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public EntitlementTargetScope TargetScope { get; private set; } = EntitlementTargetScope.Account;
    public Guid? TargetWorkspaceId { get; private set; }
    public FeatureCode Feature { get; private set; } = null!;
    public int Limit { get; private set; }
    public EntitlementSource Source { get; private set; }
    public EntitlementStatus Status { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? RevokedBy { get; private set; }

    private Entitlement() : base() { }

    public static Entitlement Create(
        Guid accountId,
        FeatureCode feature,
        int limit,
        EntitlementSource source,
        DateTimeOffset createdAt,
        EntitlementTargetScope targetScope = EntitlementTargetScope.Account,
        Guid? targetWorkspaceId = null,
        DateTimeOffset? expiresAt = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotNull(feature);

        if (limit < 0)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Entitlement_LimitCannotBeNegative, "Entitlement limit cannot be negative.");

        if (targetScope == EntitlementTargetScope.Workspace && targetWorkspaceId is null)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Entitlement_WorkspaceScopedRequiresTarget, "Workspace-scoped entitlement requires a target workspace id.");

        if (targetScope == EntitlementTargetScope.Account && targetWorkspaceId is not null)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Entitlement_AccountScopedMustNotSpecifyTarget, "Account-scoped entitlement must not specify a target workspace id.");

        var entitlement = new Entitlement
        {
            AccountId = accountId,
            WorkspaceId = targetWorkspaceId,
            TargetScope = targetScope,
            TargetWorkspaceId = targetWorkspaceId,
            Feature = feature,
            Limit = limit,
            Source = source,
            Status = EntitlementStatus.Active,
            ExpiresAt = expiresAt
        };

        entitlement.RaiseDomainEvent(new EntitlementGrantedDomainEvent(
            accountId, targetWorkspaceId, entitlement.Id, feature.Code, limit, createdAt));
        return entitlement;
    }

    public void ChangeLimit(int newLimit, Guid actorUserId, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(actorUserId);

        if (newLimit < 0)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Entitlement_LimitCannotBeNegative, "Entitlement limit cannot be negative.");

        if (Status != EntitlementStatus.Active)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Entitlement_CannotChangeLimitOfNonActive, "Cannot change the limit of a non-active entitlement.");

        if (Limit == newLimit) return;

        var oldLimit = Limit;
        Limit = newLimit;
        SetAuditOnUpdate(actorUserId, occurredAt);
        IncrementVersion();

        RaiseDomainEvent(new EntitlementLimitChangedDomainEvent(
            AccountId, WorkspaceId, Id, Feature.Code, oldLimit, newLimit, occurredAt));
    }

    public void Disable(Guid actorUserId, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(actorUserId);

        if (Status == EntitlementStatus.Revoked)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Entitlement_CannotDisableRevoked, "Cannot disable a revoked entitlement.");

        if (Status == EntitlementStatus.Disabled) return;

        Status = EntitlementStatus.Disabled;
        SetAuditOnUpdate(actorUserId, occurredAt);
        IncrementVersion();

        RaiseDomainEvent(new EntitlementDisabledDomainEvent(
            AccountId, WorkspaceId, Id, Feature.Code, occurredAt));
    }

    public void Revoke(Guid actorUserId, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(actorUserId);

        if (Status == EntitlementStatus.Revoked) return;

        Status = EntitlementStatus.Revoked;
        RevokedAt = occurredAt;
        RevokedBy = actorUserId;
        SetAuditOnUpdate(actorUserId, occurredAt);
        IncrementVersion();

        RaiseDomainEvent(new EntitlementRevokedDomainEvent(
            AccountId, WorkspaceId, Id, Feature.Code, occurredAt));
    }

    public void MarkExpired(DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();

        if (Status == EntitlementStatus.Expired) return;

        if (Status == EntitlementStatus.Revoked)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Entitlement_CannotExpireRevoked, "Cannot expire a revoked entitlement.");

        Status = EntitlementStatus.Expired;
        SetAuditOnUpdate(null, occurredAt);
        IncrementVersion();

        RaiseDomainEvent(new EntitlementExpiredDomainEvent(
            AccountId, WorkspaceId, Id, Feature.Code, occurredAt));
    }

    public bool IsActiveAt(DateTimeOffset now)
    {
        if (IsDeleted) return false;
        if (Status != EntitlementStatus.Active) return false;
        if (ExpiresAt.HasValue && ExpiresAt.Value <= now) return false;
        return true;
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        IncrementVersion();
        RaiseDomainEvent(new EntitlementSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, Feature.Code, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        Status = EntitlementStatus.Active;
        IncrementVersion();
        RaiseDomainEvent(new EntitlementRestoredDomainEvent(AccountId, WorkspaceId, Id, Feature.Code, restoredBy, restoredAt));
    }
}
