using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetDeletedWorkspaces;

/// <summary>
/// The "Recently deleted" list (owner decision 2026-11-01): the workspaces of the selected Account that the
/// caller owns and that were deleted inside the 30-day restore window. Anyone else gets an empty list, and
/// the list is the only way back to a deleted workspace, since every other request on it is NotFound.
/// </summary>
public record GetDeletedWorkspacesQuery : IQuery<Result<List<DeletedWorkspaceDto>>>, IAuthenticatedRequest, IReadRequest, IAccountRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef? IRequirePermission.Resource => null;
}

public class GetDeletedWorkspacesQueryHandler : IRequestHandler<GetDeletedWorkspacesQuery, Result<List<DeletedWorkspaceDto>>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetDeletedWorkspacesQueryHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<List<DeletedWorkspaceDto>>> Handle(GetDeletedWorkspacesQuery request, CancellationToken ct)
    {
        var accountId = _requestContext.RequireAccountId();
        var userId = _requestContext.UserId;
        var windowStart = _dateTimeProvider.UtcNow - Workspace.RestoreWindow;

        // Account request: no Workspace is selected, so the EF tenant filter of the workspace-scoped sets would
        // hide every row. The subject (the caller as an active Owner), the Account and the window are explicit,
        // and RLS shows a deleted workspace only to its Owner.
        var rows = await _context.WorkspaceMembers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(m => m.AccountId == accountId && m.UserId == userId
                        && m.Role == WorkspaceRole.Owner && m.Status == WorkspaceMemberStatus.Active)
            .Join(_context.Workspaces.IgnoreQueryFilters().AsNoTracking(),
                member => member.WorkspaceId,
                workspace => workspace.Id,
                (_, workspace) => workspace)
            .Where(w => w.AccountId == accountId && w.DeletedAt != null && w.DeletedAt >= windowStart)
            .OrderByDescending(w => w.DeletedAt)
            .ToListAsync(ct);

        var result = rows
            .Select(w => new DeletedWorkspaceDto(
                w.Id, w.Name, w.Slug, w.DeletedAt!.Value.UtcDateTime, w.PurgeAfter!.Value.UtcDateTime, w.Version))
            .ToList();

        return Result<List<DeletedWorkspaceDto>>.Success(result);
    }
}
