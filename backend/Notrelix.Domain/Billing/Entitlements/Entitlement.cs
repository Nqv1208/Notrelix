using Notrelix.Domain.Common;
using Notrelix.Domain.Billing.Plans;

namespace Notrelix.Domain.Billing.Entitlements;

public class Entitlement : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public FeatureCode Feature { get; private set; } = null!;
    public int Limit { get; private set; }
    public EntitlementSource Source { get; private set; }
    public EntitlementStatus Status { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    private Entitlement() : base() { }

    public static Entitlement Create(Guid workspaceId, FeatureCode feature, int limit, EntitlementSource source, DateTimeOffset? expiresAt = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(feature);

        return new Entitlement
        {
            WorkspaceId = workspaceId,
            Feature = feature,
            Limit = limit,
            Source = source,
            Status = EntitlementStatus.Active,
            ExpiresAt = expiresAt
        };
    }
}
