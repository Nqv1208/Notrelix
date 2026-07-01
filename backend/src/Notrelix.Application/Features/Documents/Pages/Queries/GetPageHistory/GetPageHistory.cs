using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.DTOs;

namespace Notrelix.Application.Features.Documents.Pages.Queries.GetPageHistory;

public record GetPageHistoryQuery(Guid PageId) : IQuery<Result<List<PageHistoryDto>>>;

public class GetPageHistoryQueryHandler : IRequestHandler<GetPageHistoryQuery, Result<List<PageHistoryDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetPageHistoryQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<PageHistoryDto>>> Handle(GetPageHistoryQuery request, CancellationToken ct)
    {
        var pageExists = await _context.Pages.AsNoTracking()
            .AnyAsync(page => page.Id == request.PageId && !page.IsDeleted, ct);
        if (pageExists == false) throw new NotFoundException(nameof(Page), request.PageId);

        return Result<List<PageHistoryDto>>.Success(new List<PageHistoryDto>());
    }
}
