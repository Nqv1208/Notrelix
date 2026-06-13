using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Billing.Events;
using Notrelix.Domain.Billing.Plans;

namespace Notrelix.Domain.Billing.Usage;

public class WorkspaceFeatureUsage : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public FeatureCode Feature { get; private set; } = null!;
    public decimal CurrentUsage { get; private set; }
    public decimal? HardLimit { get; private set; }
    public decimal? SoftLimit { get; private set; }
    public bool OverageAllowed { get; private set; }
    public string ResetPeriod { get; private set; } = "None";
    public DateTimeOffset? LastResetAt { get; private set; }

    private WorkspaceFeatureUsage() : base() { }

    public static WorkspaceFeatureUsage Create(
        Guid workspaceId,
        FeatureCode feature,
        decimal currentUsage,
        decimal? hardLimit,
        decimal? softLimit,
        bool overageAllowed = false,
        string resetPeriod = "None")
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(feature);

        return new WorkspaceFeatureUsage
        {
            WorkspaceId = workspaceId,
            Feature = feature,
            CurrentUsage = currentUsage,
            HardLimit = hardLimit,
            SoftLimit = softLimit,
            OverageAllowed = overageAllowed,
            ResetPeriod = resetPeriod
        };
    }

    public void Consume(decimal amount, Guid actorUserId, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        if (amount < 0)
            throw new BusinessRuleException("Amount to consume must be positive.");

        if (HardLimit.HasValue && !OverageAllowed && CurrentUsage + amount > HardLimit.Value)
        {
            AddDomainEvent(new QuotaExceededDomainEvent(WorkspaceId, Feature.Code, HardLimit.Value, occurredAt));
            throw new BusinessRuleException($"Feature usage limit exceeded for '{Feature.Code}'. Limit: {HardLimit.Value}, Requested total: {CurrentUsage + amount}.");
        }

        var oldUsage = CurrentUsage;
        CurrentUsage += amount;
        IncrementVersion();

        AddDomainEvent(new FeatureUsageConsumedDomainEvent(WorkspaceId, Feature.Code, amount, actorUserId, occurredAt));
    }

    public void Release(decimal amount, Guid actorUserId, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        if (amount < 0)
            throw new BusinessRuleException("Amount to release must be positive.");

        if (CurrentUsage - amount < 0)
            throw new BusinessRuleException("Usage cannot be released below zero.");

        CurrentUsage -= amount;
        IncrementVersion();

        AddDomainEvent(new FeatureUsageReleasedDomainEvent(WorkspaceId, Feature.Code, amount, actorUserId, occurredAt));
    }

    public void Reset(DateTimeOffset resetAt, Guid actorUserId)
    {
        EnsureNotDeleted();
        CurrentUsage = 0;
        LastResetAt = resetAt;
        IncrementVersion();
    }
}
