using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Invitations.Queries.ListResourceInvitations;

/// <summary>One pending resource invitation of a Board or Page (UC-WM-A20).</summary>
public sealed record ResourceInvitationDto(
    Guid Id,
    string Email,
    string Level,
    Guid InvitedBy,
    string InvitedByName,
    DateTimeOffset ExpiresAt,
    bool IssuedByYou);

/// <summary>
/// The pending resource invitations of one Board or Page (UC-WM-A20): stored, not yet accepted, not expired,
/// for this exact resource. Authorized as manage permission on the resource, so only a manager of the Board
/// reads the list. <c>IssuedByYou</c> tells the caller which rows they may resend or revoke.
/// </summary>
public sealed record ListResourceInvitationsQuery(string ResourceKind, Guid ResourceId)
    : IQuery<Result<List<ResourceInvitationDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => ResourceKind switch
    {
        WorkspaceInvitationResourceGrant.BoardKind => PermissionAction.ManageBoardPermission,
        WorkspaceInvitationResourceGrant.PageKind => PermissionAction.ManagePagePermission,
        _ => PermissionAction.ManageWorkspace
    };

    ResourceRef IResourceScopedRequest.Resource => ResourceRef.Create(
        global::Notrelix.Domain.SharedKernel.ResourceKind.Create(ResourceKind), ResourceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(
        global::Notrelix.Domain.SharedKernel.ResourceKind.Create(ResourceKind), ResourceId);
}

public sealed class ListResourceInvitationsQueryHandler : IRequestHandler<ListResourceInvitationsQuery, Result<List<ResourceInvitationDto>>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActorLookupService _actors;

    public ListResourceInvitationsQueryHandler(
        IWorkspaceDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IActorLookupService actors)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _actors = actors;
    }

    public async Task<Result<List<ResourceInvitationDto>>> Handle(ListResourceInvitationsQuery request, CancellationToken ct)
    {
        var workspaceId = _requestContext.RequireWorkspaceId();
        var actorId = _requestContext.UserId;
        var now = _dateTimeProvider.UtcNow;

        var rows = await _context.WorkspaceInvitations
            .AsNoTracking()
            .Where(i => i.WorkspaceId == workspaceId
                        && i.Status == WorkspaceInvitationStatus.Pending
                        && i.ExpiresAt > now
                        && i.ResourceGrant != null
                        && i.ResourceGrant.ResourceKind == request.ResourceKind
                        && i.ResourceGrant.ResourceId == request.ResourceId)
            .OrderByDescending(i => i.Id)
            .Select(i => new { i.Id, i.Email, Level = i.ResourceGrant!.Level, i.InvitedBy, i.ExpiresAt })
            .ToListAsync(ct);

        var names = rows.Count == 0
            ? new Dictionary<Guid, ActorSnapshot>()
            : (await _actors.FindManyAsync(rows.Select(r => r.InvitedBy).Distinct().ToList(), ct)).ToDictionary(a => a.UserId);

        var result = rows
            .Select(r => new ResourceInvitationDto(
                r.Id,
                r.Email,
                r.Level,
                r.InvitedBy,
                names.TryGetValue(r.InvitedBy, out var actor) ? actor.Name : "Unknown",
                r.ExpiresAt,
                r.InvitedBy == actorId))
            .ToList();

        return Result<List<ResourceInvitationDto>>.Success(result);
    }
}
