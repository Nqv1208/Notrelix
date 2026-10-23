using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Members.Queries.GetMyWorkspaceMembership;

/// <summary>
/// The caller's own membership row in this Workspace: role, status, job title, version. It is the
/// guest-safe read the web shell uses for the viewer (WG-DEBT-002 item 6): every member, Guest
/// included, may read exactly their own row (<c>ViewWorkspace</c>); other people are never returned,
/// and the member list (<c>ViewMembers</c>) stays closed to a Guest.
/// </summary>
public record GetMyWorkspaceMembershipQuery(Guid WorkspaceId) : IQuery<Result<WorkspaceMemberDto>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    Guid IWorkspaceRequest.WorkspaceId => WorkspaceId;
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetMyWorkspaceMembershipQueryHandler : IRequestHandler<GetMyWorkspaceMembershipQuery, Result<WorkspaceMemberDto>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly IActorLookupService _actorLookup;
    private readonly IMemberPhoneLookup _phoneLookup;
    private readonly ICurrentRequestContext _requestContext;

    public GetMyWorkspaceMembershipQueryHandler(
        IWorkspaceDbContext context,
        IActorLookupService actorLookup,
        IMemberPhoneLookup phoneLookup,
        ICurrentRequestContext requestContext)
    {
        _context = context;
        _actorLookup = actorLookup;
        _phoneLookup = phoneLookup;
        _requestContext = requestContext;
    }

    public async Task<Result<WorkspaceMemberDto>> Handle(GetMyWorkspaceMembershipQuery request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        var member = await _context.WorkspaceMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WorkspaceId == request.WorkspaceId
                && m.UserId == userId
                && m.Status != WorkspaceMemberStatus.Removed, ct)
            ?? throw new NotFoundException(nameof(WorkspaceMember), userId);

        var actor = (await _actorLookup.FindManyAsync([userId], ct)).FirstOrDefault();

        // Same phone rule as the member list: shared only to a non-guest member who opted in.
        string? phone = null;
        if (member.IsInternalParticipant)
        {
            var phones = await _phoneLookup.FindSharedPhonesAsync([userId], ct);
            phones.TryGetValue(userId, out phone);
        }

        return Result<WorkspaceMemberDto>.Success(new WorkspaceMemberDto(
            member.UserId,
            actor?.Name ?? "Unknown",
            actor?.AvatarUrl,
            member.Role.ToString(),
            member.CreatedAt.DateTime,
            member.Status.ToString(),
            member.JobTitle,
            member.Version,
            phone));
    }
}
