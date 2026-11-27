using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.Application.Features.Documents.Pages.Queries.AuthorizePageRealtimeSubscription;

/// <summary>
/// Authorizes a realtime subscription to a Page stream (UC-COL-E01, UC-DCT-K01). It is decided by the same permission
/// and resource-scope gates as <c>GetPageBlocksQuery</c> (ViewPage on the page resource, including Space, audience
/// and share facts); the tenant binding is resolved by the pipeline from the page, never from the client.
/// </summary>
public record AuthorizePageRealtimeSubscriptionQuery(Guid PageId)
    : IQuery<Result<RealtimeSubscriptionGrant>>, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IReadRequest
{
    public PermissionAction Action => PermissionAction.ViewPage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public sealed class AuthorizePageRealtimeSubscriptionQueryHandler
    : IRequestHandler<AuthorizePageRealtimeSubscriptionQuery, Result<RealtimeSubscriptionGrant>>
{
    private readonly IExecutionContextReader _context;

    public AuthorizePageRealtimeSubscriptionQueryHandler(IExecutionContextReader context)
    {
        _context = context;
    }

    public Task<Result<RealtimeSubscriptionGrant>> Handle(
        AuthorizePageRealtimeSubscriptionQuery request, CancellationToken cancellationToken)
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
