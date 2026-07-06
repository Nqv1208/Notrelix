namespace Notrelix.Domain.Billing.Usage;

public class FeatureUsageLedger : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string FeatureCode { get; private set; } = null!;
    public decimal Delta { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? ReferenceResource { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    private FeatureUsageLedger() : base() { }

    public static FeatureUsageLedger Create(
        Guid accountId,
        Guid workspaceId,
        string featureCode,
        decimal delta,
        Guid? actorUserId,
        string? referenceResource,
        string? note,
        DateTimeOffset occurredAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(featureCode);

        return new FeatureUsageLedger
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            FeatureCode = featureCode.Trim().ToUpperInvariant(),
            Delta = delta,
            ActorUserId = actorUserId,
            ReferenceResource = referenceResource,
            Note = note,
            OccurredAt = occurredAt
        };
    }
}
