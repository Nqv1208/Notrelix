using Notrelix.Domain.Common;

namespace Notrelix.Domain.Workspaces.Workspaces;

public record WorkspaceCreatedEvent(Guid WorkspaceId, string Name, string Slug, Guid CreatedBy) : DomainRecordEvent;
public record WorkspaceRenamedEvent(Guid WorkspaceId, string OldName, string NewName, Guid UpdatedBy) : DomainRecordEvent;
public record WorkspaceArchivedEvent(Guid WorkspaceId, Guid ArchivedBy) : DomainRecordEvent;
public record WorkspaceSoftDeletedEvent(Guid WorkspaceId, Guid DeletedBy) : DomainRecordEvent;
public record WorkspaceRestoredEvent(Guid WorkspaceId, Guid RestoredBy) : DomainRecordEvent;
