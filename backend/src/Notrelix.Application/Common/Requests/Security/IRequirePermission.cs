namespace Notrelix.Application.Common.Requests;

public interface IRequirePermission
{
    PermissionAction Action { get; }
    ResourceRef Resource { get; }
}
