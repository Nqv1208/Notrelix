
namespace Notrelix.Application.Common.Security;

public sealed record PermissionContext(
    Guid UserId,
    Guid? WorkspaceId,
    ResourceType ResourceType,
    Guid? ResourceId,
    PermissionAction Action,
    PermissionScope Scope,
    Dictionary<string, object>? Attributes = null
);
