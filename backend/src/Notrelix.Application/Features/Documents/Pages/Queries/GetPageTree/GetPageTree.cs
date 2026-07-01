using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.DTOs;

namespace Notrelix.Application.Features.Documents.Pages.Queries.GetPageTree;

public record GetPageTreeQuery(Guid WorkspaceId) : IQuery<Result<List<PageTreeItemDto>>>;

public class GetPageTreeQueryHandler : IRequestHandler<GetPageTreeQuery, Result<List<PageTreeItemDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetPageTreeQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<PageTreeItemDto>>> Handle(GetPageTreeQuery request, CancellationToken ct)
    {
        var pages = await _context.Pages.AsNoTracking()
            .Where(page => page.WorkspaceId == request.WorkspaceId && !page.IsDeleted && page.Status != PageStatus.Archived)
            .OrderBy(page => page.Title)
            .ToListAsync(ct);

        var parentIds = pages
            .Where(page => page.ParentId.HasValue)
            .Select(page => page.ParentId!.Value)
            .ToHashSet();

        return Result<List<PageTreeItemDto>>.Success(pages.Select(page => new PageTreeItemDto(
            page.Id,
            page.Title,
            page.Icon,
            page.ParentId,
            parentIds.Contains(page.Id)
        )).ToList());
    }
}
