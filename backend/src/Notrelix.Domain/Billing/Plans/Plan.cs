using Notrelix.Domain.Billing.Plans.Events;
using Notrelix.Domain.Billing.Subscriptions;
namespace Notrelix.Domain.Billing.Plans;

public class PlanLimit : Entity
{
    public Guid PlanId { get; private set; }
    public FeatureCode Feature { get; private set; } = null!;
    public int Limit { get; private set; }

    /// <summary>The capability has no cap on this plan; <see cref="Limit"/> is then zero and means nothing.</summary>
    public bool IsUnlimited { get; private set; }

    private PlanLimit() : base() { }

    public static PlanLimit Create(Guid planId, FeatureCode feature, int limit, bool isUnlimited = false)
    {
        Guard.NotEmpty(planId);
        Guard.NotNull(feature);

        if (limit < 0)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Plan_LimitCannotBeNegative, "Plan limit cannot be negative.");

        return new PlanLimit
        {
            PlanId = planId,
            Feature = feature,
            Limit = isUnlimited ? 0 : limit,
            IsUnlimited = isUnlimited
        };
    }

    public void UpdateLimit(int newLimit)
    {
        if (newLimit < 0)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Plan_LimitCannotBeNegative, "Plan limit cannot be negative.");
        Limit = newLimit;
    }
}

public class Plan : AggregateRoot
{
    /// <summary>The plan family (free, pro, ...); every revision of one family shares it.</summary>
    public string FamilyKey { get; private set; } = null!;

    /// <summary>The revision of the family, starting at 1. A revision is immutable once created: a change is a new revision.</summary>
    public int Revision { get; private set; }

    /// <summary>The subscription tier a subscription on this plan reports.</summary>
    public SubscriptionTier Tier { get; private set; }

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Money Price { get; private set; } = null!;
    public BillingPeriod Period { get; private set; }
    public PlanStatus Status { get; private set; }

    private readonly List<PlanLimit> _limits = new();
    public IReadOnlyCollection<PlanLimit> Limits => _limits.AsReadOnly();

    private Plan() : base() { }
    private Plan(Guid id) : base(id) { }

    public static Plan Create(
        string familyKey,
        int revision,
        string name,
        SubscriptionTier tier,
        Money price,
        BillingPeriod period,
        DateTimeOffset createdAt,
        Guid? planId = null)
    {
        Guard.NotNullOrWhiteSpace(familyKey);
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 100);
        Guard.NotNull(price);

        var key = familyKey.Trim().ToLowerInvariant();
        if (key.Length > 64 || !System.Text.RegularExpressions.Regex.IsMatch(key, "^[a-z][a-z0-9-]*$"))
            throw new BusinessRuleException(BillingRuleCodes.Billing_Plan_FamilyKeyInvalid, "The plan family key is lowercase letters, digits and dashes, starting with a letter, at most 64 characters.");
        if (revision < 1)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Plan_RevisionInvalid, "The plan revision starts at 1.");
        if (price.Amount < 0)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Plan_PriceCannotBeNegative, "Plan price cannot be negative.");

        // A fixed id is for the platform's baseline catalog, so subscriptions that referenced a plan by
        // identity before the catalog moved to the database keep resolving.
        var plan = planId is { } fixedId ? new Plan(fixedId) : new Plan();
        plan.FamilyKey = key;
        plan.Revision = revision;
        plan.Tier = tier;
        plan.Name = name.Trim();
        plan.Price = price;
        plan.Period = period;
        plan.Status = PlanStatus.Active;

        plan.SetAuditOnCreate(null, createdAt);
        plan.RaiseDomainEvent(new PlanCreatedDomainEvent(plan.Id, plan.Name, createdAt));
        return plan;
    }

    public void AddLimit(FeatureCode feature, int limit, DateTimeOffset occurredAt, bool isUnlimited = false)
    {
        if (_limits.Any(l => l.Feature == feature))
            throw new BusinessRuleException(BillingRuleCodes.Billing_Plan_FeatureAlreadyAdded, $"Feature '{feature.Code}' is already added to this plan.");

        _limits.Add(PlanLimit.Create(Id, feature, limit, isUnlimited));
        IncrementVersion();
        RaiseDomainEvent(new PlanLimitAddedDomainEvent(Id, feature, limit, occurredAt));
    }

    public void UpdateDescription(string? description, DateTimeOffset updatedAt)
    {
        Description = description?.Trim();
        IncrementVersion();
        RaiseDomainEvent(new PlanDescriptionUpdatedDomainEvent(Id, description, updatedAt));
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        if (Status == PlanStatus.Archived) return;
        Status = PlanStatus.Archived;
        IncrementVersion();
        RaiseDomainEvent(new PlanArchivedDomainEvent(Id, archivedAt));
    }

    public void Deprecate(DateTimeOffset deprecatedAt)
    {
        if (Status == PlanStatus.Deprecated) return;
        Status = PlanStatus.Deprecated;
        IncrementVersion();
        RaiseDomainEvent(new PlanDeprecatedDomainEvent(Id, deprecatedAt));
    }
}
