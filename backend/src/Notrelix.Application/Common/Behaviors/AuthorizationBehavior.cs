using MediatR;
using Notrelix.Application.Common.Security;

namespace Notrelix.Application.Common.Behaviors;

public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionService _permissionService;

    public AuthorizationBehavior(ICurrentUser currentUser, IPermissionService permissionService)
    {
        _currentUser = currentUser;
        _permissionService = permissionService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IRequirePermission requirePermission)
        {
            var userId = _currentUser.UserId;
            if (userId == Guid.Empty)
            {
                throw new UnauthorizedAccessException("Bạn cần đăng nhập để thực hiện hành động này.");
            }

            var decision = await _permissionService.EvaluateAsync(
                new PermissionContext(
                    userId,
                    requirePermission.Resource.WorkspaceId ?? Guid.Empty,
                    requirePermission.Resource.ResourceType,
                    requirePermission.Resource.ResourceId,
                    requirePermission.Action),
                cancellationToken);

            if (!decision.IsAllowed)
            {
                if (decision.ReasonCode == "resource_not_found")
                {
                    throw new NotFoundException(requirePermission.Resource.ResourceType.ToString(), requirePermission.Resource.ResourceId);
                }
                throw new ForbiddenException("Bạn không có quyền thực hiện hành động này.");
            }

            return await next();
        }

        return await next();
    }
}
