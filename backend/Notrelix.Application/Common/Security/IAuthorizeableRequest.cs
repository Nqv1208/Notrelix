
namespace Notrelix.Application.Common.Security;

[Obsolete("Use IRequirePermission instead. This interface will be removed after Application permission migration.")]
public interface IAuthorizeableRequest
{
    Guid WorkspaceId { get; }
    ResourceType ResourceType { get; }
    Guid ResourceId { get; }
    PermissionAction Action { get; }
}
