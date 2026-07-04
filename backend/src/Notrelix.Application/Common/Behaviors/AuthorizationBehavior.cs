namespace Notrelix.Application.Common.Behaviors;

public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> _logger;

    public AuthorizationBehavior(
        ICurrentUser currentUser,
        IPermissionService permissionService,
        ILogger<AuthorizationBehavior<TRequest, TResponse>> logger)
    {
        _currentUser = currentUser;
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IRequirePermission requirePermission)
        {
            var userId = _currentUser.UserId;
            if (userId == Guid.Empty)
            {
                throw new UnauthorizedAccessException("Authentication required.");
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
                // Security audit: log permission denial
                _logger.LogWarning(
                    "Permission denied: UserId={UserId} Action={Action} ResourceType={ResourceType} ResourceId={ResourceId} WorkspaceId={WorkspaceId} Reason={Reason}",
                    userId,
                    requirePermission.Action,
                    requirePermission.Resource.ResourceType,
                    requirePermission.Resource.ResourceId,
                    requirePermission.Resource.WorkspaceId,
                    decision.ReasonCode);

                if (decision.ReasonCode == "resource_not_found")
                {
                    throw new NotFoundException(requirePermission.Resource.ResourceType.ToString(), requirePermission.Resource.ResourceId);
                }
                throw new ForbiddenException("You do not have permission to perform this action.");
            }

            return await next();
        }

        return await next();
    }
}
