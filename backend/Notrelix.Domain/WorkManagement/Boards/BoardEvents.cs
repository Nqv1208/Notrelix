using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Boards;

public record BoardCreatedEvent(Guid WorkspaceId, Guid BoardId, string Title, Guid CreatedBy) : DomainRecordEvent;
public record BoardRenamedEvent(Guid BoardId, string OldTitle, string NewTitle, Guid UpdatedBy) : DomainRecordEvent;
public record BoardVisibilityChangedEvent(Guid BoardId, BoardVisibility OldVisibility, BoardVisibility NewVisibility, Guid UpdatedBy) : DomainRecordEvent;
public record BoardArchivedEvent(Guid BoardId, Guid ArchivedBy) : DomainRecordEvent;
public record BoardSoftDeletedEvent(Guid BoardId, Guid DeletedBy) : DomainRecordEvent;
public record BoardRestoredEvent(Guid BoardId, Guid RestoredBy) : DomainRecordEvent;
