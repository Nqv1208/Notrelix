using Notrelix.Domain.Common;

namespace Notrelix.Domain.Workspaces.Spaces;

public record SpaceCreatedEvent(Guid SpaceId, Guid WorkspaceId, string Name, Guid CreatedBy) : DomainRecordEvent;
public record SpaceMovedEvent(Guid SpaceId, Guid OldWorkspaceId, Guid NewWorkspaceId, Guid MovedBy) : DomainRecordEvent;
public record SpaceRenamedEvent(Guid SpaceId, string OldName, string NewName, Guid UpdatedBy) : DomainRecordEvent;
public record SpaceArchivedEvent(Guid SpaceId, Guid ArchivedBy) : DomainRecordEvent;
public record SpaceSoftDeletedEvent(Guid SpaceId, Guid DeletedBy) : DomainRecordEvent;
