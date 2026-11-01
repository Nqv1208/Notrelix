using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Public.PageViewAccess;

namespace Notrelix.Application.Features.Documents.Pages.Queries.GetPageBreadcrumb;

/// <summary>
/// The path of ancestor pages from the root down to the page. Each ancestor is shown only if the caller may
/// see it (<c>ViewPage</c>); ancestors the caller cannot see, or that are gone, are one "unavailable" crumb
/// with no title, icon or id, so a hidden ancestor is never disclosed.
/// </summary>
public record GetPageBreadcrumbQuery(Guid PageId) : IQuery<Result<List<PageBreadcrumbDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewPage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class GetPageBreadcrumbQueryHandler : IRequestHandler<GetPageBreadcrumbQuery, Result<List<PageBreadcrumbDto>>>
{
    /// <summary>Stops a walk through damaged data (a cycle) after this many pages.</summary>
    private const int WalkLimit = 64;

    private readonly IDocumentDbContext _context;
    private readonly IPageViewAccess _viewAccess;
    private readonly ICurrentRequestContext _requestContext;

    public GetPageBreadcrumbQueryHandler(IDocumentDbContext context, IPageViewAccess viewAccess, ICurrentRequestContext requestContext)
    {
        _context = context;
        _viewAccess = viewAccess;
        _requestContext = requestContext;
    }

    public async Task<Result<List<PageBreadcrumbDto>>> Handle(GetPageBreadcrumbQuery request, CancellationToken ct)
    {
        var workspaceId = _requestContext.RequireWorkspaceId();
        var accountId = _requestContext.RequireAccountId();
        var userId = _requestContext.UserId;

        var page = await FindAsync(workspaceId, request.PageId, ct)
            ?? throw new NotFoundException(nameof(Page), request.PageId);

        // Climb one parent at a time: the path is as long as the tree is deep, never the whole workspace.
        var ancestors = new List<PageBreadcrumbDto>();
        var seen = new HashSet<Guid> { page.Id };
        var parentId = page.ParentId;
        while (parentId is { } id && seen.Count < WalkLimit)
        {
            var parent = seen.Add(id) ? await FindAsync(workspaceId, id, ct) : null;
            if (parent is null)
            {
                ancestors.Add(Unavailable);
                break;
            }

            ancestors.Add(await _viewAccess.CanViewAsync(accountId, workspaceId, parent.Id, userId, ct)
                ? new PageBreadcrumbDto(parent.Id, parent.Title, parent.Icon)
                : Unavailable);
            parentId = parent.ParentId;
        }

        // Root first; a run of hidden ancestors is one crumb.
        var breadcrumb = new List<PageBreadcrumbDto>();
        foreach (var crumb in Enumerable.Reverse(ancestors))
            if (!(crumb.Unavailable && breadcrumb.Count > 0 && breadcrumb[^1].Unavailable)) breadcrumb.Add(crumb);
        breadcrumb.Add(new PageBreadcrumbDto(page.Id, page.Title, page.Icon));
        return Result<List<PageBreadcrumbDto>>.Success(breadcrumb);
    }

    private static PageBreadcrumbDto Unavailable { get; } = new(null, null, null, Unavailable: true);

    private Task<PageRow?> FindAsync(Guid workspaceId, Guid pageId, CancellationToken ct) =>
        _context.Pages.AsNoTracking()
            .Where(page => page.Id == pageId && page.WorkspaceId == workspaceId && page.DeletedAt == null)
            .Select(page => new PageRow(page.Id, page.ParentId, page.Title, page.Icon))
            .FirstOrDefaultAsync(ct);

    private sealed record PageRow(Guid Id, Guid? ParentId, string Title, string Icon);
}
