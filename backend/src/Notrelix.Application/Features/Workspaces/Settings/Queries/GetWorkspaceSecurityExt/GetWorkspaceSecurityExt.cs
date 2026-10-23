using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Application.Features.Workspaces.Settings.Queries.GetWorkspaceSecurityExt;

/// <summary>
/// The read-only extras of the "sharing and security" page (CTR-GAP-WS-SECURITY-EXT): the active Guest
/// count, the default Guest expiry offered for new entries (GOV-031) and the instant the sharing policy
/// turned share links off (GOV-032). The Guest count is derived from membership, so it needs the same
/// right as the member list (<c>ViewMembers</c>): a Guest does not enumerate the workspace (GOV-009).
/// </summary>
public record GetWorkspaceSecurityExtQuery(
    Guid WorkspaceId
) : IQuery<Result<WorkspaceSecurityExtDto>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewMembers;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

/// <param name="GuestCount">Active members whose Workspace role is Guest.</param>
/// <param name="GuestExpiryDays">Default Guest expiry in days (30, 90 or 180), or null for none.</param>
/// <param name="CompanyDomain">
/// Always null for now: the verified company domain is owned by Accounts (ACC-010) and no Workspace fact
/// exists yet (open decision), so the page hides the row.
/// </param>
/// <param name="LinksOffSince">When share links are off, the instant the policy turned them off (the
/// Workspace creation instant if it never changed); null while links are on.</param>
public record WorkspaceSecurityExtDto(
    int GuestCount,
    int? GuestExpiryDays,
    string? CompanyDomain,
    DateTimeOffset? LinksOffSince
);

public class GetWorkspaceSecurityExtQueryHandler : IRequestHandler<GetWorkspaceSecurityExtQuery, Result<WorkspaceSecurityExtDto>>
{
    private readonly IWorkspaceDbContext _context;

    public GetWorkspaceSecurityExtQueryHandler(IWorkspaceDbContext context)
    {
        _context = context;
    }

    public async Task<Result<WorkspaceSecurityExtDto>> Handle(GetWorkspaceSecurityExtQuery request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, ct)
            ?? throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var guestCount = await _context.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.WorkspaceId == request.WorkspaceId)
            .Where(WorkspaceMemberRules.ActiveGuest)
            .CountAsync(ct);

        var settings = workspace.Settings;
        DateTimeOffset? linksOffSince = settings.AllowPublicSharing
            ? null
            : settings.PublicSharingChangedAt ?? workspace.CreatedAt;

        return Result<WorkspaceSecurityExtDto>.Success(new WorkspaceSecurityExtDto(
            guestCount,
            settings.GuestDefaultExpiryDays,
            null,
            linksOffSince));
    }
}
