using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Governance.Public.Visibility;

namespace Notrelix.Application.Features.Documents.Pages.Queries.GetPageTree;

public record GetPageTreeQuery(Guid WorkspaceId) : IQuery<Result<List<PageTreeItemDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
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
        if (visible.Count == 0) return Result<List<PageTreeItemDto>>.Success([]);

        var pages = await _context.Pages.AsNoTracking()
            .Where(page => page.WorkspaceId == request.WorkspaceId && page.DeletedAt == null && page.Status != PageStatus.Archived)
            .Where(page => visible.Contains(page.Id))
            .OrderBy(page => page.Title)
            .ToListAsync(ct);

        var visibleIds = pages.Select(page => page.Id).ToHashSet();
        var spaces = (await _visibility.VisibleSpaceIdsAsync(request.WorkspaceId, _requestContext.UserId, ct)).ToHashSet();
        var parentIds = pages
            .Where(page => page.ParentId.HasValue)
            .Select(page => page.ParentId!.Value)
            .ToHashSet();

        // A visible page under a parent the caller cannot read is presented as a
        // root: the hidden parent's id is never disclosed (GOV-029, BE-SEC-028).
        return Result<List<PageTreeItemDto>>.Success(pages.Select(page => new PageTreeItemDto(
            page.Id,
            page.Title,
            page.Icon,
            page.ParentId is { } parentId && visibleIds.Contains(parentId) ? parentId : null,
            parentIds.Contains(page.Id),
            page.SpaceId is { } spaceId && spaces.Contains(spaceId) ? spaceId : null
        )).ToList());
    }
}
