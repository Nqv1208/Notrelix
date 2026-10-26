using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Common;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Versions.Services;
using Notrelix.Domain.Documents.Versions;

namespace Notrelix.Application.Features.Documents.Versions.Queries.ListPageVersions;

/// <summary>The history of a page, newest first. Seeing the history is seeing the page (ViewPage).</summary>
public record ListPageVersionsQuery(Guid PageId) : IQuery<Result<List<PageVersionDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewPage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class ListPageVersionsQueryHandler : IRequestHandler<ListPageVersionsQuery, Result<List<PageVersionDto>>>
{
    private readonly IDocumentDbContext _context;

    public ListPageVersionsQueryHandler(IDocumentDbContext context) => _context = context;

    public async Task<Result<List<PageVersionDto>>> Handle(ListPageVersionsQuery request, CancellationToken ct)
    {
        var pageExists = await _context.Pages.AsNoTracking()
            .AnyAsync(page => page.Id == request.PageId && page.DeletedAt == null, ct);
        if (!pageExists) throw new NotFoundException(nameof(Page), request.PageId);

        // The list never loads the captured content: only the columns an entry is listed by.
        var rows = await _context.DocumentVersions.AsNoTracking()
            .Where(v => v.PageId == request.PageId)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new { v.Id, v.PageId, v.VersionNumber, v.Kind, v.ChangeSummary, v.CreatedBy, v.CreatedAt })
            .ToListAsync(ct);

        return Result<List<PageVersionDto>>.Success(rows
            .Select(v => new PageVersionDto(v.Id, v.PageId, v.VersionNumber, DocumentDtoMapper.KindName(v.Kind), v.ChangeSummary, v.CreatedBy, v.CreatedAt.DateTime))
            .ToList());
    }
}
