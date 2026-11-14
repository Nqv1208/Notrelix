using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Realtime.Gateway;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Channels.Queries.AuthorizeChannelRealtimeSubscription;

/// <summary>
/// Authorizes a realtime subscription to a channel (UC-CHT-C01, C06, SYS-ADR-001 scope <c>channel</c>). The caller must be
/// an active member of a live, unarchived channel of the Workspace that the pipeline resolved from the channel. Every
/// other case, a missing channel included, is the same NotFound, so the gateway answers identically (no existence leak).
/// A public channel does not grant a subscription to someone who is not in it (acceptance: a non-member gets the same
/// error as an unknown space).
/// </summary>
public record AuthorizeChannelRealtimeSubscriptionQuery(Guid ChannelId)
    : IQuery<Result<RealtimeSubscriptionGrant>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    ResourceRef IResourceScopedRequest.Resource => ResourceRef.Create(ResourceKind.Create("chat.space"), ChannelId);
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef? IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("chat.space"), ChannelId);
}

public sealed class AuthorizeChannelRealtimeSubscriptionQueryHandler
    : IRequestHandler<AuthorizeChannelRealtimeSubscriptionQuery, Result<RealtimeSubscriptionGrant>>
{
    private readonly IExecutionContextReader _context;
    private readonly IChatDbContext _db;
    private readonly IWorkspaceMemberFacts _members;

    public AuthorizeChannelRealtimeSubscriptionQueryHandler(
        IExecutionContextReader context, IChatDbContext db, IWorkspaceMemberFacts members)
    {
        _context = context;
        _db = db;
        _members = members;
    }

    public async Task<Result<RealtimeSubscriptionGrant>> Handle(
        AuthorizeChannelRealtimeSubscriptionQuery request, CancellationToken cancellationToken)
    {
        var snapshot = _context.Snapshot
            ?? throw new SecurityMisconfigurationException("Execution context is not resolved.");
        if (snapshot.AccountId is not { } accountId || snapshot.WorkspaceId is not { } workspaceId || snapshot.UserId is not { } userId)
        {
            throw new SecurityMisconfigurationException("Realtime authorization requires a resolved tenant binding.");
        }

        await ChannelAccess.CallerAsync(_members, workspaceId, userId, cancellationToken);

        var live = await _db.ChatSpaces.AsNoTracking().AnyAsync(
            s => s.Id == request.ChannelId && s.WorkspaceId == workspaceId
                 && s.DeletedAt == null && s.ArchivedAt == null,
            cancellationToken);
        if (!live)
            throw new NotFoundException(nameof(Space), request.ChannelId);

        var member = await _db.ChatMemberships.AsNoTracking().AnyAsync(
            m => m.SpaceId == request.ChannelId && m.UserId == userId && m.LeftAt == null,
            cancellationToken);
        if (!member)
            throw new NotFoundException(nameof(Space), request.ChannelId);

        return Result<RealtimeSubscriptionGrant>.Success(new RealtimeSubscriptionGrant(accountId, workspaceId));
    }
}
