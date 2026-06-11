using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Templates;

public record ItemTemplateCreatedEvent(Guid TemplateId, string Name) : DomainRecordEvent;
