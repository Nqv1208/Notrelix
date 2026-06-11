using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Document.Common;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Document.Queries.GetPageTree;

public record GetPageTreeQuery(Guid WorkspaceId) : IRequest<Result<List<PageTreeItemDto>>>;

public class GetPageTreeQueryHandler : IRequestHandler<GetPageTreeQuery, Result<List<PageTreeItemDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetPageTreeQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<PageTreeItemDto>>> Handle(GetPageTreeQuery request, CancellationToken ct)
    {
        var pages = await _context.Pages.AsNoTracking()
            .Where(page => page.WorkspaceId == request.WorkspaceId && !page.IsDeleted && !page.IsArchived)
            .OrderBy(page => page.Depth)
            .ThenBy(page => page.Position)
            .ThenBy(page => page.Title)
            .ToListAsync(ct);

        var parentIds = pages
            .Where(page => page.ParentId.HasValue)
            .Select(page => page.ParentId!.Value)
            .ToHashSet();

        return Result<List<PageTreeItemDto>>.Success(pages.Select(page => new PageTreeItemDto(
            page.Id,
            page.Title,
            page.IconType,
            page.IconValue,
            page.ParentId,
            page.Position,
            page.Depth,
            parentIds.Contains(page.Id)
        )).ToList());
    }
}
