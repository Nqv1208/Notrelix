using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Common.Behaviors;

public class WorkspaceContextBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentWorkspace _currentWorkspace;
    private readonly IWorkspaceDbContext _workspaceDbContext;
    private readonly IWorkspacePermissionService _workspacePermissionService;

    public WorkspaceContextBehavior(
        ICurrentUser currentUser,
        ICurrentWorkspace currentWorkspace,
        IWorkspaceDbContext workspaceDbContext,
        IWorkspacePermissionService workspacePermissionService)
    {
        _currentUser = currentUser;
        _currentWorkspace = currentWorkspace;
        _workspaceDbContext = workspaceDbContext;
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

            // Resolve AccountId from DB — single source of truth
            var workspace = await _workspaceDbContext.Workspaces
                .Where(w => w.Id == workspaceId)
                .Select(w => new { w.AccountId, w.Status })
                .FirstOrDefaultAsync(cancellationToken);

            if (workspace is null)
            {
                throw new NotFoundException(nameof(Workspace), workspaceId);
            }

            _currentWorkspace.SetWorkspace(workspace.AccountId, workspaceId);

            var canView = await _workspacePermissionService.CanViewWorkspaceAsync(workspaceId, _currentUser.UserId, cancellationToken);
            if (!canView)
            {
                throw new ForbiddenException("Access to workspace denied.");
            }
        }

        return await next();
    }
}
