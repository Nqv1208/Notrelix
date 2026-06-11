using Notrelix.Domain.Common;

namespace Notrelix.Domain.Documents.Templates;

public record PageTemplateCreatedEvent(Guid TemplateId, string Name) : DomainRecordEvent;
public record PageTemplatePublishedEvent(Guid TemplateId) : DomainRecordEvent;
