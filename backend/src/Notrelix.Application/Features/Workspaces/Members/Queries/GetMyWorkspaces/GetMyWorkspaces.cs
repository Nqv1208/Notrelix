using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Public.DefaultWorkspace;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Members.Queries.GetMyWorkspaces;

/// <summary>
/// The Workspaces the caller takes part in, one row each with the caller's own role, the default
/// flag and whether the caller may leave. The subject is always the principal: only the caller's own
/// active memberships of active Workspaces are read, so another person's memberships and Workspaces
/// the caller left, was removed from or is suspended in never appear. Counts come from the same
/// single query (no per-row lookups); the default Workspace is the Identity-owned choice.
/// </summary>
public record GetMyWorkspacesQuery : IQuery<Result<List<AccountWorkspaceDto>>>, IAuthenticatedRequest, IReadRequest, IGlobalRequest;

public class GetMyWorkspacesQueryHandler : IRequestHandler<GetMyWorkspacesQuery, Result<List<AccountWorkspaceDto>>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IIdentityDefaultWorkspace _defaultWorkspace;

    public GetMyWorkspacesQueryHandler(
        IWorkspaceDbContext context,
        ICurrentRequestContext requestContext,
        IIdentityDefaultWorkspace defaultWorkspace)
    {
        _context = context;
        _requestContext = requestContext;
        _defaultWorkspace = defaultWorkspace;
    }

    public async Task<Result<List<AccountWorkspaceDto>>> Handle(GetMyWorkspacesQuery request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        if (userId == Guid.Empty)
            return Result<List<AccountWorkspaceDto>>.Failure("User is not authenticated");

        // A global request carries no Workspace, and the EF tenant filter of the workspace-scoped sets
        // keys on the request Workspace, so it would hide every row. The subject is fixed to the
        // principal below; the filters are lifted only to read across the caller's own Workspaces, and
        // the soft-delete conditions the filters would add are stated explicitly.
        var rows = await (
            from member in _context.WorkspaceMembers.IgnoreQueryFilters().AsNoTracking()
            join workspace in _context.Workspaces.IgnoreQueryFilters().AsNoTracking() on member.WorkspaceId equals workspace.Id
            where member.UserId == userId
                && member.Status == WorkspaceMemberStatus.Active
                && workspace.DeletedAt == null
                && workspace.Status == WorkspaceStatus.Active
            orderby workspace.Name, workspace.Id
            select new
            {
                workspace.Id,
                workspace.Name,
                workspace.Slug,
                member.Role,
                MemberCount = _context.WorkspaceMembers.IgnoreQueryFilters().Count(other =>
                    other.WorkspaceId == workspace.Id && other.Status == WorkspaceMemberStatus.Active),
                DepartmentCount = _context.Spaces.IgnoreQueryFilters().Count(space =>
                    space.WorkspaceId == workspace.Id
                    && space.DeletedAt == null
                    && space.SpaceType == SpaceType.Department
                    && space.Status == SpaceStatus.Active),
                ActiveOwnerCount = _context.WorkspaceMembers.IgnoreQueryFilters().Count(other =>
                    other.WorkspaceId == workspace.Id
                    && other.Role == WorkspaceRole.Owner
                    && other.Status == WorkspaceMemberStatus.Active),
            }).ToListAsync(ct);

        var defaultId = await _defaultWorkspace.GetAsync(userId, ct);

        var result = rows.Select(row => new AccountWorkspaceDto(
            row.Id,
            row.Name,
            row.Slug,
            row.Role.ToString(),
            row.Role == WorkspaceRole.Guest ? null : row.MemberCount,
            row.Role is WorkspaceRole.Owner or WorkspaceRole.Admin ? row.DepartmentCount : null,
            // A default the caller cannot open is not in the list, so it can never be flagged here.
            defaultId == row.Id,
            row.Role != WorkspaceRole.Owner || row.ActiveOwnerCount > 1)).ToList();

        return Result<List<AccountWorkspaceDto>>.Success(result);
    }
}
