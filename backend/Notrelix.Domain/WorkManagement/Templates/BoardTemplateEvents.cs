using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Templates;

public record BoardTemplateCreatedEvent(Guid TemplateId, string Name) : DomainRecordEvent;
