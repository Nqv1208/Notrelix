using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Governance.Permissions;

public class ResourcePermission : AggregateRoot
{
    public ResourceType ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public PermissionSubjectType SubjectType { get; private set; }
    public Guid SubjectId { get; private set; }
    public PermissionLevel Level { get; private set; }

    private ResourcePermission() : base() { }

    public static ResourcePermission Grant(
        ResourceType resourceType, 
        Guid resourceId, 
        PermissionSubjectType subjectType, 
        Guid subjectId, 
        PermissionLevel level,
        Guid grantedBy)
    {
        Guard.NotEmpty(resourceId);
        Guard.NotEmpty(subjectId);

        var permission = new ResourcePermission
        {
            ResourceType = resourceType,
            ResourceId = resourceId,
            SubjectType = subjectType,
            SubjectId = subjectId,
            Level = level
        };

        permission.SetAuditOnCreate(grantedBy);
        permission.AddDomainEvent(new ResourcePermissionGrantedEvent(
            permission.Id, resourceType, resourceId, subjectType, subjectId, level, grantedBy));

        return permission;
    }

    public void ChangeLevel(PermissionLevel newLevel, Guid updatedBy)
    {
        EnsureNotDeleted();
        if (Level == newLevel) return;

        var oldLevel = Level;
        Level = newLevel;
        SetAuditOnUpdate(updatedBy);
        
        AddDomainEvent(new ResourcePermissionLevelChangedEvent(Id, oldLevel, newLevel, updatedBy));
    }

    public void Revoke(Guid revokedBy)
    {
        EnsureNotDeleted();
        SoftDelete(revokedBy, DateTimeOffset.UtcNow);
        AddDomainEvent(new ResourcePermissionRevokedEvent(Id, revokedBy));
    }
}
