using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Automation.Rules.Events;

public sealed record AutomationRuleRestoredEvent(
    Guid WorkspaceId,
    Guid RuleId,
    string Name,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
