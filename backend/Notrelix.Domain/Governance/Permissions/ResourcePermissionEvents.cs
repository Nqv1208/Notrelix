using Notrelix.Domain.Common;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Domain.Governance.Permissions;

public record ResourcePermissionGrantedEvent(Guid PermissionId, ResourceType ResourceType, Guid ResourceId, PermissionSubjectType SubjectType, Guid SubjectId, PermissionLevel Level, Guid GrantedBy) : DomainRecordEvent;
public record ResourcePermissionLevelChangedEvent(Guid PermissionId, PermissionLevel OldLevel, PermissionLevel NewLevel, Guid UpdatedBy) : DomainRecordEvent;
public record ResourcePermissionRevokedEvent(Guid PermissionId, Guid RevokedBy) : DomainRecordEvent;
public record FieldPermissionGrantedEvent(Guid FieldId, PermissionSubjectType SubjectType, Guid SubjectId, PermissionLevel Level, Guid GrantedBy) : DomainRecordEvent;
public record FieldPermissionRevokedEvent(Guid FieldId, PermissionSubjectType SubjectType, Guid SubjectId, Guid RevokedBy) : DomainRecordEvent;
