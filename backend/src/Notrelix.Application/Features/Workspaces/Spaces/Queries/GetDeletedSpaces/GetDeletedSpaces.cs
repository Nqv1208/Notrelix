using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Spaces.Queries.GetDeletedSpaces;

/// <summary>A deleted Space inside its restore window.</summary>
public record DeletedSpaceDto(
    Guid Id,
    string Name,
    Guid? ParentSpaceId,
    DateTime DeletedAt,
    DateTime RestorableUntil);

/// <summary>
/// The deleted Spaces of one Workspace that can still be restored, newest first. Restore is a
/// workspace-level ManageSpaces decision (workspace Owner or Admin), so the list asks for the same one.
/// </summary>
public record GetDeletedSpacesQuery(Guid WorkspaceId)
    : IQuery<Result<List<DeletedSpaceDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ManageSpaces;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetDeletedSpacesQueryHandler : IRequestHandler<GetDeletedSpacesQuery, Result<List<DeletedSpaceDto>>>
{
    /// <summary>shortcut: the newest 200 are listed, not paged; add a cursor when a workspace deletes more in 30 days.</summary>
    internal const int MaxListed = 200;

    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public GetDeletedSpacesQueryHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider clock)
    {
        _context = context;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result<List<DeletedSpaceDto>>> Handle(GetDeletedSpacesQuery request, CancellationToken ct)
    {
        var accountId = _requestContext.RequireAccountId();
        var since = _clock.UtcNow - Space.RestoreWindow;

        // Bypassing the soft-delete filter also drops its tenant predicate, so the Account and the
        // Workspace are applied here.
        var rows = await _context.Spaces
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.AccountId == accountId && s.WorkspaceId == request.WorkspaceId
                        && s.DeletedAt != null && s.DeletedAt >= since)
            .OrderByDescending(s => s.DeletedAt)
            .Take(MaxListed)
            .ToListAsync(ct);

        return Result<List<DeletedSpaceDto>>.Success(rows
            .Select(s => new DeletedSpaceDto(
                s.Id, s.Name, s.ParentSpaceId, s.DeletedAt!.Value.UtcDateTime, s.RestorableUntil!.Value.UtcDateTime))
            .ToList());
    }
}
