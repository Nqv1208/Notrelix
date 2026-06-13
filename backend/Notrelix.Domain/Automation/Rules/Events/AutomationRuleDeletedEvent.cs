using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Automation.Rules.Events;

public sealed record AutomationRuleDeletedEvent(
    Guid WorkspaceId,
    Guid RuleId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
