using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.Common;
using global::Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Abstractions;

namespace Notrelix.Application.Features.Documents.Blocks.Queries.GetPageBlocks;

public record GetPageBlocksQuery(Guid PageId) : IQuery<Result<List<BlockDto>>>, IResourceScopedRequest
{
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Page, PageId);
}

public class GetPageBlocksQueryHandler : IRequestHandler<GetPageBlocksQuery, Result<List<BlockDto>>>
{
    private readonly IDocumentDbContext _context;
    public GetPageBlocksQueryHandler(IDocumentDbContext context) => _context = context;

    public async Task<Result<List<BlockDto>>> Handle(GetPageBlocksQuery request, CancellationToken ct)
    {
        var pageExists = await _context.Pages.AsNoTracking()
            .AnyAsync(page => page.Id == request.PageId && !page.IsDeleted, ct);
        if (!pageExists) throw new NotFoundException(nameof(Page), request.PageId);

        var blockEntities = await _context.Blocks.AsNoTracking()
            .Where(block => block.PageId == request.PageId && !block.IsDeleted)
            .OrderBy(block => block.Position)
            .ToListAsync(ct);

        return Result<List<BlockDto>>.Success(blockEntities.Select(DocumentDtoMapper.ToBlockDto).ToList());
    }
}
