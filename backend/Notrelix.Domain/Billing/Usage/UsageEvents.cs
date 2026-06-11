using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Usage;

public record UsageMetricIncreasedEvent(Guid WorkspaceId, UsageMetricKey Key, int Amount) : DomainRecordEvent;
public record UsageLimitExceededEvent(Guid WorkspaceId, UsageMetricKey Key) : DomainRecordEvent;
