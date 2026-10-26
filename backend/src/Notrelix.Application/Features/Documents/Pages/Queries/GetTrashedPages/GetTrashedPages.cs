using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.Common;
using global::Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Governance.Public.Visibility;

namespace Notrelix.Application.Features.Documents.Pages.Queries.GetTrashedPages;

/// <summary>The pages in the trash of one Workspace that the caller may see.</summary>
public record GetTrashedPagesQuery(Guid WorkspaceId) : IQuery<Result<List<PageDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class GetTrashedPagesQueryHandler : IRequestHandler<GetTrashedPagesQuery, Result<List<PageDto>>>
{
    private readonly IDocumentDbContext _context;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public GetTrashedPagesQueryHandler(IDocumentDbContext context, IResourceVisibility visibility, ICurrentRequestContext requestContext)
    {
        _context = context;
        _visibility = visibility;
        _requestContext = requestContext;
    }

    public async Task<Result<List<PageDto>>> Handle(GetTrashedPagesQuery request, CancellationToken ct)
    {
        // Same per-page ViewPage decision as the page list, applied to the trash.
        var visible = await _visibility.VisibleTrashedPageIdsAsync(request.WorkspaceId, _requestContext.UserId, ct);
        if (visible.Count == 0) return Result<List<PageDto>>.Success([]);

        var pages = await _context.Pages.AsNoTracking()
            .Where(page => page.WorkspaceId == request.WorkspaceId && page.DeletedAt == null && page.Status == PageStatus.Archived)
            .Where(page => visible.Contains(page.Id))
            .OrderByDescending(page => page.UpdatedAt)
            .ToListAsync(ct);

        var spaces = (await _visibility.VisibleSpaceIdsAsync(request.WorkspaceId, _requestContext.UserId, ct)).ToHashSet();
        return Result<List<PageDto>>.Success(pages.Select(page => DocumentDtoMapper.ToPageDto(page, spaces)).ToList());
    }
}
