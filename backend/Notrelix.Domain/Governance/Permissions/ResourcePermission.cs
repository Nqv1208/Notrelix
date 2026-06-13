using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Governance.Permissions;

public class ResourcePermission : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public ResourceType ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public PermissionSubjectType SubjectType { get; private set; }
    public Guid SubjectId { get; private set; }
    public PermissionLevel Level { get; private set; }
    public PermissionEffect Effect { get; private set; } = PermissionEffect.Allow;
    public string ConditionJson { get; private set; } = "{}";
    public int Priority { get; private set; } = 100;

    private ResourcePermission() : base() { }

    public static ResourcePermission Grant(
        Guid workspaceId,
        ResourceType resourceType, 
        Guid resourceId, 
        PermissionSubjectType subjectType, 
        Guid subjectId, 
        PermissionLevel level,
        Guid grantedBy,
        DateTimeOffset grantedAt,
        PermissionEffect effect = PermissionEffect.Allow,
        string? conditionJson = null,
        int priority = 100)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(resourceId);
        Guard.NotEmpty(subjectId);

        var permission = new ResourcePermission
        {
            WorkspaceId = workspaceId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            SubjectType = subjectType,
            SubjectId = subjectId,
            Level = level,
            Effect = effect,
            ConditionJson = conditionJson ?? "{}",
            Priority = priority
        };

        permission.SetAuditOnCreate(grantedBy, grantedAt);
        permission.AddDomainEvent(new ResourcePermissionGrantedEvent(
            workspaceId, permission.Id, resourceType, resourceId, subjectType, subjectId, level, grantedBy, grantedAt));

        return permission;
    }

    public void ChangeLevel(PermissionLevel newLevel, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Level == newLevel) return;

        var oldLevel = Level;
        Level = newLevel;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new ResourcePermissionLevelChangedEvent(WorkspaceId, Id, ResourceType, ResourceId, SubjectType, SubjectId, oldLevel, newLevel, updatedBy, updatedAt));
    }

    public void Revoke(Guid revokedBy, DateTimeOffset revokedAt)
    {
        EnsureNotDeleted();
        SoftDelete(revokedBy, revokedAt);
        AddDomainEvent(new ResourcePermissionRevokedEvent(WorkspaceId, Id, ResourceType, ResourceId, SubjectType, SubjectId, revokedBy, revokedAt));
    }
}
