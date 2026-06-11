using Notrelix.Domain.Common;

namespace Notrelix.Domain.Documents.Versions;

public record DocumentVersionCreatedEvent(Guid PageId, int VersionNumber) : DomainRecordEvent;
public record DocumentVersionRestoredEvent(Guid PageId, int VersionNumber) : DomainRecordEvent;
