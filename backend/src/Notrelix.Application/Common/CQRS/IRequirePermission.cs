namespace Notrelix.Application.Common.CQRS;

public interface IRequirePermission
{
    PermissionAction Action { get; }
    ResourceRef Resource { get; }
}
