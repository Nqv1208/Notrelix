using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Abstractions;

namespace Notrelix.Application.Features.Documents.Pages.Queries.GetPageHistory;

public record GetPageHistoryQuery(Guid PageId) : IQuery<Result<List<PageHistoryDto>>>;

public class GetPageHistoryQueryHandler : IRequestHandler<GetPageHistoryQuery, Result<List<PageHistoryDto>>>
{
    private readonly IDocumentDbContext _context;
    public GetPageHistoryQueryHandler(IDocumentDbContext context) => _context = context;

    public async Task<Result<List<PageHistoryDto>>> Handle(GetPageHistoryQuery request, CancellationToken ct)
    {
        var pageExists = await _context.Pages.AsNoTracking()
            .AnyAsync(page => page.Id == request.PageId && !page.IsDeleted, ct);
        if (pageExists == false) throw new NotFoundException(nameof(Page), request.PageId);

        return Result<List<PageHistoryDto>>.Success(new List<PageHistoryDto>());
    }
}
