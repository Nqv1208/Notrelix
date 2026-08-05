
namespace Notrelix.Application.Common.Security;

public sealed record PermissionContext(
    Guid UserId,
    Guid AccountId,
    Guid? WorkspaceId,
    ResourceKind ResourceKind,
    Guid? ResourceId,
    PermissionAction Action,
    PermissionScope Scope,
    Dictionary<string, object>? Attributes = null
);
