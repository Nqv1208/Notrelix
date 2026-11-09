using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.Common;
using global::Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Governance.Public.Visibility;

namespace Notrelix.Application.Features.Documents.Pages.Queries.GetWorkspacePages;

/// <summary>
/// The active pages of one Workspace the caller may see, as a cursor page in id order (default 50, at most 200).
/// Pass <c>NextCursor</c> back as <c>Cursor</c> for the next page; a client that shows the whole list sorts it by title.
/// </summary>
public record GetWorkspacePagesQuery(Guid WorkspaceId, string? Cursor = null, int? Limit = null) : IQuery<Result<CursorPage<PageDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class GetWorkspacePagesQueryHandler : IRequestHandler<GetWorkspacePagesQuery, Result<CursorPage<PageDto>>>
{
    private readonly IDocumentDbContext _context;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public GetWorkspacePagesQueryHandler(IDocumentDbContext context, IResourceVisibility visibility, ICurrentRequestContext requestContext)
    {
        _context = context;
        _visibility = visibility;
        _requestContext = requestContext;
    }

    public async Task<Result<CursorPage<PageDto>>> Handle(GetWorkspacePagesQuery request, CancellationToken ct)
    {
        // The pipeline authorized ViewWorkspace; each Page still needs the
        // single-read ViewPage decision, applied in the datastore (GOV-029).
        var visible = await _visibility.VisiblePageIdsAsync(request.WorkspaceId, _requestContext.UserId, ct);
        if (visible.Count == 0) return Result<CursorPage<PageDto>>.Success(new CursorPage<PageDto>([], null));

        var limit = DocumentCursor.ClampLimit(request.Limit);
        var query = _context.Pages.AsNoTracking()
            .Where(page => page.WorkspaceId == request.WorkspaceId && page.DeletedAt == null && page.Status != PageStatus.Archived)
            .Where(page => visible.Contains(page.Id));

        if (DocumentCursor.TryDecodeId(request.Cursor, out var after))
            query = query.Where(page => page.Id > after);

        var rows = await query.OrderBy(page => page.Id).Take(limit + 1).ToListAsync(ct);
        var hasMore = rows.Count > limit;
        var pages = hasMore ? rows.Take(limit).ToList() : rows;
        var nextCursor = hasMore ? DocumentCursor.EncodeId(pages[^1].Id) : null;

        // The parent's title is disclosed only for a parent in the same visible list, so it is looked up among the visible pages.
        var parentIds = pages.Where(page => page.ParentId is not null).Select(page => page.ParentId!.Value).Distinct().ToList();
        var parentTitles = parentIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await _context.Pages.AsNoTracking()
                .Where(page => page.WorkspaceId == request.WorkspaceId && page.DeletedAt == null && page.Status != PageStatus.Archived)
                .Where(page => visible.Contains(page.Id) && parentIds.Contains(page.Id))
                .Select(page => new { page.Id, page.Title })
                .ToListAsync(ct))
                .ToDictionary(page => page.Id, page => page.Title);

        var spaces = (await _visibility.VisibleSpaceIdsAsync(request.WorkspaceId, _requestContext.UserId, ct)).ToHashSet();
        var items = pages
            .Select(page => DocumentDtoMapper.ToPageDto(page, spaces) with
            {
                ParentTitle = page.ParentId is { } parentId && parentTitles.TryGetValue(parentId, out var title) ? title : null
            })
            .ToList();
        return Result<CursorPage<PageDto>>.Success(new CursorPage<PageDto>(items, nextCursor));
    }
}
