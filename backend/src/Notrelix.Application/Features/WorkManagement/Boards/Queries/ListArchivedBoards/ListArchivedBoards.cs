using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.ListArchivedBoards;

/// <summary>
/// The archived boards of one Workspace that the caller may see, newest first. An archived board keeps
/// its items and reads like any other, so each one is limited by the same single-read decision as the
/// active list (GOV-029): a board the caller cannot see is absent, never marked.
/// </summary>
public record ListArchivedBoardsQuery(Guid WorkspaceId)
    : IQuery<Result<List<RetiredBoardDto>>>, IRequirePermission, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class ListArchivedBoardsQueryHandler : IRequestHandler<ListArchivedBoardsQuery, Result<List<RetiredBoardDto>>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public ListArchivedBoardsQueryHandler(
        IWorkManagementDbContext context,
        IResourceVisibility visibility,
        ICurrentRequestContext requestContext)
    {
        _context = context;
        _visibility = visibility;
        _requestContext = requestContext;
    }

    public async Task<Result<List<RetiredBoardDto>>> Handle(ListArchivedBoardsQuery request, CancellationToken ct)
    {
        var visible = await _visibility.VisibleBoardIdsAsync(request.WorkspaceId, _requestContext.UserId, ct);
        if (visible.Count == 0) return Result<List<RetiredBoardDto>>.Success([]);

        var spaces = (await _visibility.VisibleSpaceIdsAsync(request.WorkspaceId, _requestContext.UserId, ct)).ToHashSet();
        var boards = await _context.Boards.AsNoTracking()
            .Where(b => b.WorkspaceId == request.WorkspaceId && b.IsArchived && visible.Contains(b.Id))
            .OrderByDescending(b => b.UpdatedAt)
            .ToListAsync(ct);

        return Result<List<RetiredBoardDto>>.Success(boards
            .Select(b => new RetiredBoardDto(
                b.Id, b.WorkspaceId, b.Title, b.Background, b.Visibility.ToString(), b.Version,
                b.SpaceId is { } spaceId && spaces.Contains(spaceId) ? spaceId : null,
                string.IsNullOrWhiteSpace(b.ItemKeyPrefix) ? null : b.ItemKeyPrefix,
                (b.UpdatedAt ?? b.CreatedAt).UtcDateTime,
                null))
            .ToList());
    }
}
