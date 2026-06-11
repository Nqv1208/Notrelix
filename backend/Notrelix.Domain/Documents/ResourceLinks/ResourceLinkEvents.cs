using Notrelix.Domain.Common;

namespace Notrelix.Domain.Documents.ResourceLinks;

public record ResourceLinkCreatedEvent(Guid SourceId, Guid TargetId, LinkType Type) : DomainRecordEvent;
public record ResourceLinkDeletedEvent(Guid LinkId) : DomainRecordEvent;
