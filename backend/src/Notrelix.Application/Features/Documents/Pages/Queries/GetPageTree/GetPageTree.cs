using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.Common;
using global::Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Governance.Public.Visibility;

namespace Notrelix.Application.Features.Documents.Pages.Queries.GetPageTree;

/// <summary>
/// The active pages of one Workspace the caller may see, sorted by title (case and marks ignored). The tree is
/// read whole while the caller sees at most <see cref="WholeTreeLimit"/> pages; beyond that it is read one
/// level at a time: without <paramref name="ParentId"/> only the root pages come back, with it the visible
/// children of that page. Either way a node says whether it has visible children.
/// </summary>
public record GetPageTreeQuery(Guid WorkspaceId, Guid? ParentId = null) : IQuery<Result<List<PageTreeItemDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    public const int WholeTreeLimit = 2000;

    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class GetPageTreeQueryHandler : IRequestHandler<GetPageTreeQuery, Result<List<PageTreeItemDto>>>
{
    private readonly IDocumentDbContext _context;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public GetPageTreeQueryHandler(IDocumentDbContext context, IResourceVisibility visibility, ICurrentRequestContext requestContext)
    {
        _context = context;
        _visibility = visibility;
        _requestContext = requestContext;
    }

    public async Task<Result<List<PageTreeItemDto>>> Handle(GetPageTreeQuery request, CancellationToken ct)
    {
        var visible = await _visibility.VisiblePageIdsAsync(request.WorkspaceId, _requestContext.UserId, ct);
        var visibleSet = visible.ToHashSet();

        // A parent the caller cannot see answers like a page that does not exist.
        if (request.ParentId is { } parentId && !visibleSet.Contains(parentId))
            throw new NotFoundException(nameof(Page), parentId);
        if (visible.Count == 0) return Result<List<PageTreeItemDto>>.Success([]);

        var whole = request.ParentId is null && visible.Count <= GetPageTreeQuery.WholeTreeLimit;
        var query = _context.Pages.AsNoTracking()
            .Where(page => page.WorkspaceId == request.WorkspaceId && page.DeletedAt == null && page.Status != PageStatus.Archived)
            .Where(page => visible.Contains(page.Id));
        if (request.ParentId is { } parent)
            query = query.Where(page => page.ParentId == parent);
        else if (!whole)
            query = query.Where(page => page.ParentId == null || !visible.Contains(page.ParentId.Value));

        var pages = (await query.ToListAsync(ct))
            .OrderBy(page => PageTitleOrder.Key(page.Title), StringComparer.Ordinal)
            .ThenBy(page => page.Id)
            .ToList();

        HashSet<Guid> withChildren;
        if (whole)
        {
            withChildren = pages.Where(page => page.ParentId.HasValue).Select(page => page.ParentId!.Value).ToHashSet();
        }
        else
        {
            var ids = pages.Select(page => page.Id).ToList();
            withChildren = (await _context.Pages.AsNoTracking()
                .Where(page => page.WorkspaceId == request.WorkspaceId && page.DeletedAt == null && page.Status != PageStatus.Archived)
                .Where(page => visible.Contains(page.Id) && page.ParentId != null && ids.Contains(page.ParentId.Value))
                .Select(page => page.ParentId!.Value)
                .Distinct()
                .ToListAsync(ct)).ToHashSet();
        }

        var spaces = (await _visibility.VisibleSpaceIdsAsync(request.WorkspaceId, _requestContext.UserId, ct)).ToHashSet();

        // A visible page under a parent the caller cannot read is presented as a
        // root: the hidden parent's id is never disclosed (GOV-029, BE-SEC-028).
        return Result<List<PageTreeItemDto>>.Success(pages.Select(page => new PageTreeItemDto(
            page.Id,
            page.Title,
            page.Icon,
            request.ParentId ?? (page.ParentId is { } parentOfPage && visibleSet.Contains(parentOfPage) ? parentOfPage : null),
            withChildren.Contains(page.Id),
            page.Version,
            page.SpaceId is { } spaceId && spaces.Contains(spaceId) ? spaceId : null
        )).ToList());
    }
}
