using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Templates;

public record AutomationTemplateCreatedEvent(Guid TemplateId, string Name) : DomainRecordEvent;
public record AutomationTemplatePublishedEvent(Guid TemplateId) : DomainRecordEvent;
