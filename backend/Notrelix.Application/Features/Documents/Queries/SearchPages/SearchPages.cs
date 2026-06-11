using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Document.Common;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Document.Queries.SearchPages;

public record SearchPagesQuery(Guid WorkspaceId, string Query) : IRequest<Result<List<PageDto>>>;

public class SearchPagesQueryHandler : IRequestHandler<SearchPagesQuery, Result<List<PageDto>>>
{
    private readonly IApplicationDbContext _context;
    public SearchPagesQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<PageDto>>> Handle(SearchPagesQuery request, CancellationToken ct)
    {
        var query = request.Query.Trim().ToLower();
        if (query.Length < 2) return Result<List<PageDto>>.Success([]);

        var pageEntities = await _context.Pages.AsNoTracking()
            .Where(page => page.WorkspaceId == request.WorkspaceId && !page.IsDeleted && !page.IsArchived)
            .Where(page => page.Title.ToLower().Contains(query))
            .OrderBy(page => page.Title)
            .Take(25)
            .ToListAsync(ct);

        return Result<List<PageDto>>.Success(pageEntities.Select(DocumentDtoMapper.ToPageDto).ToList());
    }
}
