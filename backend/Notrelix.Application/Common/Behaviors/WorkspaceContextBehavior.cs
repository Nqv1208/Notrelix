using MediatR;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.CQRS;
using Notrelix.Application.Common.Exceptions;

namespace Notrelix.Application.Common.Behaviors;

public class WorkspaceContextBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentWorkspace _currentWorkspace;
    private readonly IWorkspacePermissionService _workspacePermissionService;

    public WorkspaceContextBehavior(
        ICurrentUser currentUser,
        ICurrentWorkspace currentWorkspace,
        IWorkspacePermissionService workspacePermissionService)
    {
        _currentUser = currentUser;
        _currentWorkspace = currentWorkspace;
        _workspacePermissionService = workspacePermissionService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IWorkspaceRequest workspaceRequest)
        {
            var workspaceId = workspaceRequest.WorkspaceId;

            if (workspaceId == Guid.Empty)
            {
                throw new ForbiddenException("Invalid workspace context.");
            }

            if (_currentUser.UserId == Guid.Empty)
            {
                throw new UnauthorizedAccessException("Authentication required.");
            }

            _currentWorkspace.SetWorkspace(workspaceId);

            var canView = await _workspacePermissionService.CanViewWorkspaceAsync(workspaceId, _currentUser.UserId, cancellationToken);
            if (!canView)
            {
                throw new ForbiddenException("Access to workspace denied.");
            }
        }

        return await next();
    }
}
