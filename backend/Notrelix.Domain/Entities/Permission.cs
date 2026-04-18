using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Entities;

// Entity đại diện cho quyền truy cập vào resource
public class Permission : AuditableEntity
{
    public Guid WorkspaceId { get; private set; }
    public ResourceType ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public SubjectType SubjectType { get; private set; }
    public Guid SubjectId { get; private set; } // UserId hoặc RoleId
    public PermissionLevel Level { get; private set; }
    public Guid? GrantedBy { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    private Permission() : base() { }

    public static Permission Create(
        Guid workspaceId,
        ResourceType resourceType,
        Guid resourceId,
        SubjectType subjectType,
        Guid subjectId,
        PermissionLevel level,
        Guid? grantedBy = null,
        DateTime? expiresAt = null)
    {
        return new Permission
        {
            WorkspaceId = workspaceId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            SubjectType = subjectType,
            SubjectId = subjectId,
            Level = level,
            GrantedBy = grantedBy,
            ExpiresAt = expiresAt
        };
    }

    public static Permission CreateForUser(
        Guid workspaceId,
        ResourceType resourceType,
        Guid resourceId,
        Guid userId,
        PermissionLevel level)
    {
        return Create(workspaceId, resourceType, resourceId, SubjectType.User, userId, level);
    }

    public static Permission CreateForRole(
        Guid workspaceId,
        ResourceType resourceType,
        Guid resourceId,
        Guid roleId,
        PermissionLevel level)
    {
        return Create(workspaceId, resourceType, resourceId, SubjectType.Role, roleId, level);
    }

    public void UpdateLevel(PermissionLevel newLevel)
    {
        Level = newLevel;
    }

    public bool CanRead => Level >= PermissionLevel.Read;
    public bool CanWrite => Level >= PermissionLevel.Write;
    public bool CanAdmin => Level >= PermissionLevel.Admin;
}
