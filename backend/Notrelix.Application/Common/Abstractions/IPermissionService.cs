using Notrelix.Application.Common.Security;

namespace Notrelix.Application.Common.Abstractions;

public interface IPermissionService
{
    Task<PermissionDecision> EvaluateAsync(
        PermissionContext context,
        CancellationToken cancellationToken = default);

    Task EnsureAllowedAsync(
        PermissionContext context,
        CancellationToken cancellationToken = default);

    Task<bool> AuthorizeAsync(
        Guid userId,
        Guid workspaceId,
        ResourceType resourceType,
        Guid resourceId,
        PermissionAction action,
        CancellationToken cancellationToken = default);

    Task<bool> AuthorizeWorkspaceAsync(
        Guid userId,
        Guid workspaceId,
        PermissionAction action,
        CancellationToken cancellationToken = default);

    Task<bool> HasPermissionAsync(
        Guid userId,
        Guid workspaceId,
        ResourceType resourceType,
        Guid? resourceId,
        PermissionAction action,
        CancellationToken cancellationToken = default);
}
