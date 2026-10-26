using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.Common;
using global::Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Abstractions;

namespace Notrelix.Application.Features.Documents.Blocks.Queries.GetPageBlocks;

public record GetPageBlocksQuery(Guid PageId) : IQuery<Result<List<BlockDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewPage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class GetPageBlocksQueryHandler : IRequestHandler<GetPageBlocksQuery, Result<List<BlockDto>>>
{
    private readonly IDocumentDbContext _context;
    public GetPageBlocksQueryHandler(IDocumentDbContext context) => _context = context;

    public async Task<Result<List<BlockDto>>> Handle(GetPageBlocksQuery request, CancellationToken ct)
    {
        var pageExists = await _context.Pages.AsNoTracking()
            .AnyAsync(page => page.Id == request.PageId && page.DeletedAt == null, ct);
        if (!pageExists) throw new NotFoundException(nameof(Page), request.PageId);

        var blockEntities = await _context.Blocks.AsNoTracking()
            .Where(block => block.PageId == request.PageId && block.DeletedAt == null)
            .ToListAsync(ct);

        // A fractional index orders by ordinal comparison, which the column collation does not
        // guarantee, so the order is applied here (a page holds a bounded number of blocks).
        return Result<List<BlockDto>>.Success(blockEntities
            .OrderBy(block => block.Position.Value, StringComparer.Ordinal)
            .Select(DocumentDtoMapper.ToBlockDto)
            .ToList());
    }
}
