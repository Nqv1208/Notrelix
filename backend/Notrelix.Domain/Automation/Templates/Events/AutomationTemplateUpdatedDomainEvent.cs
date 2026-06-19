using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Templates.Events;

public sealed record AutomationTemplateUpdatedDomainEvent(
    Guid TemplateId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, null, null);
