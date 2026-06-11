using Notrelix.Domain.Common;

namespace Notrelix.Domain.Workspaces.Members;

public record WorkspaceMemberAddedEvent(Guid WorkspaceId, Guid MemberId, Guid UserId, WorkspaceRole Role, Guid AddedBy) : DomainRecordEvent;
public record WorkspaceMemberRemovedEvent(Guid WorkspaceId, Guid MemberId, Guid RemovedBy) : DomainRecordEvent;
public record WorkspaceMemberRoleChangedEvent(Guid WorkspaceId, Guid MemberId, WorkspaceRole OldRole, WorkspaceRole NewRole, Guid UpdatedBy) : DomainRecordEvent;
public record WorkspaceMemberSuspendedEvent(Guid WorkspaceId, Guid MemberId, Guid SuspendedBy) : DomainRecordEvent;
public record WorkspaceMemberActivatedEvent(Guid WorkspaceId, Guid MemberId, Guid ActivatedBy) : DomainRecordEvent;
