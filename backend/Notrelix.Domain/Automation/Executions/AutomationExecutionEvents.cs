using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Executions;

public record AutomationExecutionStartedEvent(Guid ExecutionId, Guid RuleId) : DomainRecordEvent;
public record AutomationExecutionSucceededEvent(Guid ExecutionId, Guid RuleId) : DomainRecordEvent;
public record AutomationExecutionFailedEvent(Guid ExecutionId, Guid RuleId, string Error) : DomainRecordEvent;
public record AutomationExecutionCancelledEvent(Guid ExecutionId, Guid RuleId, Guid CancelledBy) : DomainRecordEvent;
