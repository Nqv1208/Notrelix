using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Application.Features.Governance.Public.Visibility;

namespace Notrelix.Application.Features.Documents.Pages.Queries.GetPageBacklinks;

/// <summary>The pages that mention a page, limited to the ones the caller may see.</summary>
public record GetPageBacklinksQuery(Guid PageId) : IQuery<Result<PageBacklinksDto>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewPage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class GetPageBacklinksQueryHandler : IRequestHandler<GetPageBacklinksQuery, Result<PageBacklinksDto>>
{
    private readonly IDocumentDbContext _context;
    private readonly IPageBacklinkReader _reader;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public GetPageBacklinksQueryHandler(
        IDocumentDbContext context,
        IPageBacklinkReader reader,
        IResourceVisibility visibility,
        ICurrentRequestContext requestContext)
    {
        _context = context;
        _reader = reader;
        _visibility = visibility;
        _requestContext = requestContext;
    }

    public async Task<Result<PageBacklinksDto>> Handle(GetPageBacklinksQuery request, CancellationToken ct)
    {
        var workspaceId = await _context.Pages.AsNoTracking()
            .Where(page => page.Id == request.PageId && page.DeletedAt == null)
            .Select(page => (Guid?)page.WorkspaceId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(Page), request.PageId);

        var mentioning = await _reader.FindMentioningPageIdsAsync(workspaceId, request.PageId, ct);
        if (mentioning.Count == 0) return Result<PageBacklinksDto>.Success(new PageBacklinksDto([]));

        // Each mentioning page still needs the single-read ViewPage decision:
        // a backlink never discloses a page the caller cannot open.
        var visible = (await _visibility.VisiblePageIdsAsync(workspaceId, _requestContext.UserId, ct)).ToHashSet();
        var readable = mentioning.Where(visible.Contains).ToList();

        var spaces = (await _visibility.VisibleSpaceIdsAsync(workspaceId, _requestContext.UserId, ct)).ToHashSet();
        var pages = await _context.Pages.AsNoTracking()
            .Where(page => readable.Contains(page.Id) && page.DeletedAt == null && page.Status != PageStatus.Archived)
            .OrderBy(page => page.Title)
            .Select(page => new { page.Id, page.Title, page.Icon, page.SpaceId })
            .ToListAsync(ct);

        return Result<PageBacklinksDto>.Success(new PageBacklinksDto(pages
            .Select(page => new PageBacklinkDto(page.Id, page.Title, page.Icon, page.SpaceId is { } space && spaces.Contains(space) ? space : null))
            .ToList()));
    }
}
