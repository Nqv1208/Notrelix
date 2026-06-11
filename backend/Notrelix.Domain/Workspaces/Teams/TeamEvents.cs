using Notrelix.Domain.Common;

namespace Notrelix.Domain.Workspaces.Teams;

public record TeamCreatedEvent(Guid TeamId, Guid WorkspaceId, string Name, Guid CreatedBy) : DomainRecordEvent;
public record TeamRenamedEvent(Guid TeamId, string OldName, string NewName, Guid UpdatedBy) : DomainRecordEvent;
public record TeamMemberAddedEvent(Guid TeamId, Guid UserId, TeamMemberRole Role, Guid AddedBy) : DomainRecordEvent;
public record TeamMemberRemovedEvent(Guid TeamId, Guid UserId, Guid RemovedBy) : DomainRecordEvent;
public record TeamArchivedEvent(Guid TeamId, Guid ArchivedBy) : DomainRecordEvent;
