using Notrelix.Domain.Common;

namespace Notrelix.Domain.Documents.Pages;

public record PageCreatedEvent(Guid WorkspaceId, Guid PageId, string Title, Guid CreatedBy) : DomainRecordEvent;
public record PageRenamedEvent(Guid PageId, string OldTitle, string NewTitle, Guid UpdatedBy) : DomainRecordEvent;
public record PageMovedEvent(Guid PageId, Guid? OldParentId, Guid? NewParentId, Guid UpdatedBy) : DomainRecordEvent;
public record PageArchivedEvent(Guid PageId, Guid ArchivedBy) : DomainRecordEvent;
public record PageSoftDeletedEvent(Guid PageId, Guid DeletedBy) : DomainRecordEvent;
public record PageRestoredEvent(Guid PageId, Guid RestoredBy) : DomainRecordEvent;
