namespace Notrelix.Application.Common.Requests.Security;

public interface IRequirePermission
{
    PermissionAction Action { get; }
    ResourceRef? Resource { get; }
}
