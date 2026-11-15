using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.Common;
using global::Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Abstractions;

namespace Notrelix.Application.Features.Documents.Blocks.Queries.GetPageBlocks;

/// <summary>
/// The live blocks of one page in order, as a cursor page of at most 1 000 (LD14). Pass <c>NextCursor</c> back as
/// <c>Cursor</c> for the next window.
/// </summary>
public record GetPageBlocksQuery(Guid PageId, string? Cursor = null, int? Limit = null) : IQuery<Result<CursorPage<BlockDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewPage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class GetPageBlocksQueryHandler : IRequestHandler<GetPageBlocksQuery, Result<CursorPage<BlockDto>>>
{
    private readonly IDocumentDbContext _context;
    public GetPageBlocksQueryHandler(IDocumentDbContext context) => _context = context;

    public async Task<Result<CursorPage<BlockDto>>> Handle(GetPageBlocksQuery request, CancellationToken ct)
    {
        var pageExists = await _context.Pages.AsNoTracking()
            .AnyAsync(page => page.Id == request.PageId && page.DeletedAt == null, ct);
        if (!pageExists) throw new NotFoundException(nameof(Page), request.PageId);

        var blockEntities = await _context.Blocks.AsNoTracking()
            .Where(block => block.PageId == request.PageId && block.DeletedAt == null)
            .ToListAsync(ct);

        // A fractional index orders by ordinal comparison, which the column collation does not
        // guarantee, so order and cursor are applied here (a page holds at most 5 000 live blocks).
        // shortcut: the whole bounded set is read for every window, when pages may exceed 5 000 push the keyset into SQL with a C collation.
        var ordered = blockEntities.OrderBy(block => block.Position.Value, StringComparer.Ordinal).AsEnumerable();
        if (DocumentCursor.TryDecodePosition(request.Cursor, out var after))
            ordered = ordered.Where(block => string.CompareOrdinal(block.Position.Value, after) > 0);

        var limit = DocumentCursor.ClampBlockLimit(request.Limit);
        var window = ordered.Take(limit + 1).ToList();
        var hasMore = window.Count > limit;
        if (hasMore) window.RemoveAt(limit);
        return Result<CursorPage<BlockDto>>.Success(new CursorPage<BlockDto>(
            window.Select(DocumentDtoMapper.ToBlockDto).ToList(),
            hasMore ? DocumentCursor.EncodePosition(window[^1].Position.Value) : null));
    }
}
