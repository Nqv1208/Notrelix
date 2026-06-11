using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Entitlements;

public record EntitlementChangedEvent(Guid WorkspaceId, string FeatureCode, int NewLimit) : DomainRecordEvent;
public record EntitlementRevokedEvent(Guid WorkspaceId, string FeatureCode) : DomainRecordEvent;
