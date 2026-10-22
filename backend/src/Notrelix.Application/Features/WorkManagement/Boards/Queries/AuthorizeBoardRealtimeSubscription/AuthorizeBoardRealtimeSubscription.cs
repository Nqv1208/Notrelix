using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.AuthorizeBoardRealtimeSubscription;

/// <summary>
/// Authorizes a realtime subscription to a Board stream. It is decided by the
/// same permission and resource-scope gates as <c>GetBoardQuery</c> (ViewBoard on
/// the board resource, including Space, audience and grant facts); the tenant
/// binding is resolved by the pipeline from the board, never from the client.
/// </summary>
public record AuthorizeBoardRealtimeSubscriptionQuery(Guid BoardId)
    : IQuery<Result<RealtimeSubscriptionGrant>>, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IReadRequest
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public sealed class AuthorizeBoardRealtimeSubscriptionQueryHandler
    : IRequestHandler<AuthorizeBoardRealtimeSubscriptionQuery, Result<RealtimeSubscriptionGrant>>
{
    private readonly IExecutionContextReader _context;

    public AuthorizeBoardRealtimeSubscriptionQueryHandler(IExecutionContextReader context)
    {
        _context = context;
    }

    public Task<Result<RealtimeSubscriptionGrant>> Handle(
        AuthorizeBoardRealtimeSubscriptionQuery request, CancellationToken cancellationToken)
    {
        var snapshot = _context.Snapshot
            ?? throw new SecurityMisconfigurationException("Execution context is not resolved.");
        if (snapshot.AccountId is not { } accountId || snapshot.WorkspaceId is not { } workspaceId)
        {
            throw new SecurityMisconfigurationException("Realtime authorization requires a resolved tenant binding.");
        }

        return Task.FromResult(Result<RealtimeSubscriptionGrant>.Success(
            new RealtimeSubscriptionGrant(accountId, workspaceId)));
    }
}
