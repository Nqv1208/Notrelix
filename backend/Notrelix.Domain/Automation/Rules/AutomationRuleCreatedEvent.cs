using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Automation.Rules;

public sealed record AutomationRuleCreatedEvent(
    Guid WorkspaceId,
    Guid RuleId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
