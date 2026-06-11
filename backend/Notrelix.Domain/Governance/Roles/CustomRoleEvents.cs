using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Roles;

public record CustomRoleCreatedEvent(Guid RoleId, Guid WorkspaceId, string Name, Guid CreatedBy) : DomainRecordEvent;
public record CustomRoleUpdatedEvent(Guid RoleId, Guid UpdatedBy) : DomainRecordEvent;
public record CustomRoleAssignedEvent(Guid RoleId, Guid MemberId, Guid AssignedBy) : DomainRecordEvent;
public record CustomRoleRevokedEvent(Guid RoleId, Guid MemberId, Guid RevokedBy) : DomainRecordEvent;
