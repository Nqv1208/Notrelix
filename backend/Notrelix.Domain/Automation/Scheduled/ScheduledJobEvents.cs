using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Scheduled;

public record ScheduledJobCreatedEvent(Guid JobId, Guid RuleId) : DomainRecordEvent;
public record ScheduledJobPausedEvent(Guid JobId) : DomainRecordEvent;
