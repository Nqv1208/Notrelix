using Notrelix.Domain.Billing.Plans.Events;
namespace Notrelix.Domain.Billing.Plans;

public class PlanLimit : Entity
{
    public Guid PlanId { get; private set; }
    public FeatureCode Feature { get; private set; } = null!;
    public int Limit { get; private set; }

    private PlanLimit() : base() { }

    public static PlanLimit Create(Guid planId, FeatureCode feature, int limit)
    {
        Guard.NotEmpty(planId);
        Guard.NotNull(feature);

        if (limit < 0)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Plan_LimitCannotBeNegative, "Plan limit cannot be negative.");

        return new PlanLimit
        {
            PlanId = planId,
            Feature = feature,
            Limit = limit
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
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Money Price { get; private set; } = null!;
    public BillingPeriod Period { get; private set; }
    public PlanStatus Status { get; private set; }

    private readonly List<PlanLimit> _limits = new();
    public IReadOnlyCollection<PlanLimit> Limits => _limits.AsReadOnly();

    private Plan() : base() { }

    public static Plan Create(string name, Money price, BillingPeriod period, DateTimeOffset createdAt)
    {
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 100);
        Guard.NotNull(price);

        var plan = new Plan
        {
            Name = name.Trim(),
            Price = price,
            Period = period,
            Status = PlanStatus.Active
        };

        plan.SetAuditOnCreate(null, createdAt);
        plan.RaiseDomainEvent(new PlanCreatedDomainEvent(plan.Id, plan.Name, createdAt));
        return plan;
    }

    public void AddLimit(FeatureCode feature, int limit, DateTimeOffset occurredAt)
    {
        if (_limits.Any(l => l.Feature == feature))
            throw new BusinessRuleException(BillingRuleCodes.Billing_Plan_FeatureAlreadyAdded, $"Feature '{feature}' is already added to this plan.");

        _limits.Add(PlanLimit.Create(Id, feature, limit));
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
