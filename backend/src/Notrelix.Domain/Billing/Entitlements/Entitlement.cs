using Notrelix.Domain.Billing.Entitlements.Events;
using Notrelix.Domain.Billing.Plans;
namespace Notrelix.Domain.Billing.Entitlements;

public class Entitlement : AggregateRoot, IAccountScoped
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
        Guard.NotEmpty(actorUserId);

        if (newLimit < 0)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Entitlement_LimitCannotBeNegative, "Entitlement limit cannot be negative.");

        if (Status != EntitlementStatus.Active)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Entitlement_CannotChangeLimitOfNonActive, "Cannot change the limit of a non-active entitlement.");

        if (Limit == newLimit) return;

        var oldLimit = Limit;
        var pending = PrepareAuditUpdate(actorUserId, occurredAt);
        Limit = newLimit;
        ApplyAuditUpdate(pending);
        IncrementVersion();

        RaiseDomainEvent(new EntitlementLimitChangedDomainEvent(
            AccountId, WorkspaceId, Id, Feature.Code, oldLimit, newLimit, occurredAt));
    }

    public void Disable(Guid actorUserId, DateTimeOffset occurredAt)
    {
        Guard.NotEmpty(actorUserId);

        if (Status == EntitlementStatus.Revoked)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Entitlement_CannotDisableRevoked, "Cannot disable a revoked entitlement.");

        if (Status == EntitlementStatus.Disabled) return;

        var pending = PrepareAuditUpdate(actorUserId, occurredAt);
        Status = EntitlementStatus.Disabled;
        ApplyAuditUpdate(pending);
        IncrementVersion();

        RaiseDomainEvent(new EntitlementDisabledDomainEvent(
            AccountId, WorkspaceId, Id, Feature.Code, occurredAt));
    }

    public void Revoke(Guid actorUserId, DateTimeOffset occurredAt)
    {
        Guard.NotEmpty(actorUserId);

        if (Status == EntitlementStatus.Revoked) return;

        var pending = PrepareAuditUpdate(actorUserId, occurredAt);
        Status = EntitlementStatus.Revoked;
        RevokedAt = occurredAt;
        RevokedBy = actorUserId;
        ApplyAuditUpdate(pending);
        IncrementVersion();

        RaiseDomainEvent(new EntitlementRevokedDomainEvent(
            AccountId, WorkspaceId, Id, Feature.Code, occurredAt));
    }

    public void MarkExpired(DateTimeOffset occurredAt)
    {
        if (Status == EntitlementStatus.Expired) return;

        if (Status == EntitlementStatus.Revoked)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Entitlement_CannotExpireRevoked, "Cannot expire a revoked entitlement.");

        var pending = PrepareAuditUpdate(null, occurredAt);
        Status = EntitlementStatus.Expired;
        ApplyAuditUpdate(pending);
        IncrementVersion();

        RaiseDomainEvent(new EntitlementExpiredDomainEvent(
            AccountId, WorkspaceId, Id, Feature.Code, occurredAt));
    }

    public bool IsActiveAt(DateTimeOffset now)
    {
        if (Status != EntitlementStatus.Active) return false;
        if (ExpiresAt.HasValue && ExpiresAt.Value <= now) return false;
        return true;
    }
}
