using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Rules;

public record AutomationRuleCreatedEvent(Guid WorkspaceId, Guid RuleId, string Name, Guid CreatedBy) : DomainRecordEvent;
public record AutomationRuleEnabledEvent(Guid RuleId, Guid UpdatedBy) : DomainRecordEvent;
public record AutomationRuleDisabledEvent(Guid RuleId, Guid UpdatedBy) : DomainRecordEvent;
public record AutomationRuleDeletedEvent(Guid RuleId, Guid DeletedBy) : DomainRecordEvent;
