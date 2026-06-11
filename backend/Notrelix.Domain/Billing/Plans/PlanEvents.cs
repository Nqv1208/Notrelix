using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Plans;

public record PlanCreatedEvent(Guid PlanId, string Name) : DomainRecordEvent;
