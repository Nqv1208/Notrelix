using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.DTOs;

namespace Notrelix.Application.Features.Documents.Pages.Queries.GetPageBreadcrumb;

public record GetPageBreadcrumbQuery(Guid PageId) : IQuery<Result<List<PageBreadcrumbDto>>>;

public class GetPageBreadcrumbQueryHandler : IRequestHandler<GetPageBreadcrumbQuery, Result<List<PageBreadcrumbDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetPageBreadcrumbQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<PageBreadcrumbDto>>> Handle(GetPageBreadcrumbQuery request, CancellationToken ct)
    {
        var pages = await _context.Pages.AsNoTracking()
            .Where(page => !page.IsDeleted)
            .Select(page => new { page.Id, page.ParentId, page.Title, page.Icon })
            .ToListAsync(ct);

        var byId = pages.ToDictionary(page => page.Id);
        if (!byId.ContainsKey(request.PageId)) throw new NotFoundException(nameof(Page), request.PageId);

        var breadcrumb = new List<PageBreadcrumbDto>();
        var currentId = request.PageId;
        while (byId.TryGetValue(currentId, out var current))
        {
            breadcrumb.Add(new PageBreadcrumbDto(current.Id, current.Title, current.Icon));
            if (!current.ParentId.HasValue) break;
            currentId = current.ParentId.Value;
        }

        breadcrumb.Reverse();
        return Result<List<PageBreadcrumbDto>>.Success(breadcrumb);
    }
}
