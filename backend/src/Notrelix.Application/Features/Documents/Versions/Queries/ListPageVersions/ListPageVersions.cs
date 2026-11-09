using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Common;
using Notrelix.Application.Features.Documents.DTOs;

namespace Notrelix.Application.Features.Documents.Versions.Queries.ListPageVersions;

/// <summary>
/// The history of a page, newest first, as a cursor page (default 50, at most 200). Pass <c>NextCursor</c> back as
/// <c>Cursor</c> for the older entries. Seeing the history is seeing the page (ViewPage).
/// </summary>
public record ListPageVersionsQuery(Guid PageId, string? Cursor = null, int? Limit = null) : IQuery<Result<CursorPage<PageVersionDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewPage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class ListPageVersionsQueryHandler : IRequestHandler<ListPageVersionsQuery, Result<CursorPage<PageVersionDto>>>
{
    private readonly IDocumentDbContext _context;

    public ListPageVersionsQueryHandler(IDocumentDbContext context) => _context = context;

    public async Task<Result<CursorPage<PageVersionDto>>> Handle(ListPageVersionsQuery request, CancellationToken ct)
    {
        var pageExists = await _context.Pages.AsNoTracking()
            .AnyAsync(page => page.Id == request.PageId && page.DeletedAt == null, ct);
        if (!pageExists) throw new NotFoundException(nameof(Page), request.PageId);

        var limit = DocumentCursor.ClampLimit(request.Limit);
        var query = _context.DocumentVersions.AsNoTracking().Where(v => v.PageId == request.PageId);
        if (DocumentCursor.TryDecodeNumber(request.Cursor, out var before))
            query = query.Where(v => v.VersionNumber < before);

        // The list never loads the captured content: only the columns an entry is listed by.
        var rows = await query
            .OrderByDescending(v => v.VersionNumber)
            .Take(limit + 1)
            .Select(v => new { v.Id, v.PageId, v.VersionNumber, v.Kind, v.ChangeSummary, v.CreatedBy, v.CreatedAt })
            .ToListAsync(ct);

        var hasMore = rows.Count > limit;
        var kept = hasMore ? rows.Take(limit).ToList() : rows;
        var nextCursor = hasMore ? DocumentCursor.EncodeNumber(kept[^1].VersionNumber) : null;

        var items = kept
            .Select(v => new PageVersionDto(v.Id, v.PageId, v.VersionNumber, DocumentDtoMapper.KindName(v.Kind), v.ChangeSummary, v.CreatedBy, v.CreatedAt.UtcDateTime))
            .ToArray();
        return Result<CursorPage<PageVersionDto>>.Success(new CursorPage<PageVersionDto>(items, nextCursor));
    }
}
