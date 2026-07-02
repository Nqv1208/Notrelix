namespace Notrelix.Application.Common.Behaviors;

public class WorkspaceContextBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentAccount _currentAccount;
    private readonly ICurrentWorkspace _currentWorkspace;
    private readonly IWorkspacePermissionService _workspacePermissionService;

    public WorkspaceContextBehavior(
        ICurrentUser currentUser,
        ICurrentAccount currentAccount,
        ICurrentWorkspace currentWorkspace,
        IWorkspacePermissionService workspacePermissionService)
    {
        _currentUser = currentUser;
        _currentAccount = currentAccount;
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

            if (!_currentAccount.AccountId.HasValue)
            {
                throw new UnauthorizedAccessException("Account context required.");
            }

            _currentWorkspace.SetWorkspace(_currentAccount.AccountId.Value, workspaceId);

            var canView = await _workspacePermissionService.CanViewWorkspaceAsync(workspaceId, _currentUser.UserId, cancellationToken);
            if (!canView)
            {
                throw new ForbiddenException("Access to workspace denied.");
            }
        }

        return await next();
    }
}
