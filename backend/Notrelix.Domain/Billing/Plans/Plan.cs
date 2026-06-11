using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

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
            throw new BusinessRuleException("Plan limit cannot be negative.");

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
            throw new BusinessRuleException("Plan limit cannot be negative.");
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
        Guard.NotNull(price);

        var plan = new Plan
        {
            Name = name.Trim(),
            Price = price,
            Period = period,
            Status = PlanStatus.Active
        };

        plan.SetAuditOnCreate(Guid.Empty, createdAt);
        plan.AddDomainEvent(new PlanCreatedEvent(plan.Id, plan.Name, createdAt));
        return plan;
    }

    public void AddLimit(FeatureCode feature, int limit)
    {
        if (_limits.Any(l => l.Feature == feature))
            throw new BusinessRuleException($"Feature '{feature}' is already added to this plan.");

        _limits.Add(PlanLimit.Create(Id, feature, limit));
    }

    public void UpdateDescription(string description)
    {
        Description = description?.Trim();
    }

    public void Archive()
    {
        if (Status == PlanStatus.Archived) return;
        Status = PlanStatus.Archived;
    }

    public void Deprecate()
    {
        if (Status == PlanStatus.Deprecated) return;
        Status = PlanStatus.Deprecated;
    }
}
