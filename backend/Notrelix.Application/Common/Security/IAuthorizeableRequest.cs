
namespace Notrelix.Application.Common.Security;

public interface IAuthorizeableRequest
{
    Guid WorkspaceId { get; }
    ResourceType ResourceType { get; }
    Guid ResourceId { get; }
    PermissionAction Action { get; }
}
