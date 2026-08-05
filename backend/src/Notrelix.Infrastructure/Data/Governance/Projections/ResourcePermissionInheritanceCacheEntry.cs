namespace Notrelix.Infrastructure.Data.Governance.Projections;

public sealed class ResourcePermissionInheritanceCacheEntry
{
    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string ResourceKind { get; private set; } = null!;
    public Guid ResourceId { get; private set; }
    public string? ParentResourceType { get; private set; }
    public Guid? ParentResourceId { get; private set; }
    public string SubjectType { get; private set; } = null!;
    public Guid? SubjectId { get; private set; }
    public string? SubjectKey { get; private set; }
    public string Action { get; private set; } = null!;
    public string Effect { get; private set; } = null!;
    public string? PermissionLevel { get; private set; }
    public string? SourceType { get; private set; }
    public Guid? SourceId { get; private set; }
    public string? InheritedFromResourceType { get; private set; }
    public Guid? InheritedFromResourceId { get; private set; }
    public long CacheVersion { get; private set; }
    public string ComputedPermissionsJson { get; private set; } = null!;
    public DateTimeOffset ComputedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    private ResourcePermissionInheritanceCacheEntry() { }

    public static ResourcePermissionInheritanceCacheEntry Create(
        Guid id,
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string? parentResourceType,
        Guid? parentResourceId,
        string subjectType,
        Guid? subjectId,
        string? subjectKey,
        string action,
        string effect,
        string? permissionLevel,
        string? sourceType,
        Guid? sourceId,
        string? inheritedFromResourceType,
        Guid? inheritedFromResourceId,
        long cacheVersion,
        string computedPermissionsJson,
        DateTimeOffset computedAt)
    {
        return new ResourcePermissionInheritanceCacheEntry
        {
            Id = id,
            WorkspaceId = workspaceId,
            ResourceKind = resourceType,
            ResourceId = resourceId,
            ParentResourceType = parentResourceType,
            ParentResourceId = parentResourceId,
            SubjectType = subjectType,
            SubjectId = subjectId,
            SubjectKey = subjectKey,
            Action = action,
            Effect = effect,
            PermissionLevel = permissionLevel,
            SourceType = sourceType,
            SourceId = sourceId,
            InheritedFromResourceType = inheritedFromResourceType,
            InheritedFromResourceId = inheritedFromResourceId,
            CacheVersion = cacheVersion,
            ComputedPermissionsJson = computedPermissionsJson,
            ComputedAt = computedAt,
        };
    }
}
