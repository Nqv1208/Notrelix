using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.Common;
using global::Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Governance.Public.Visibility;

namespace Notrelix.Application.Features.Documents.Pages.Queries.GetWorkspacePages;

public record GetWorkspacePagesQuery(Guid WorkspaceId) : IQuery<Result<List<PageDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class GetWorkspacePagesQueryHandler : IRequestHandler<GetWorkspacePagesQuery, Result<List<PageDto>>>
{
    private readonly IDocumentDbContext _context;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public GetWorkspacePagesQueryHandler(IDocumentDbContext context, IResourceVisibility visibility, ICurrentRequestContext requestContext)
    {
        _context = context;
        _visibility = visibility;
        _requestContext = requestContext;
    }

    public async Task<Result<List<PageDto>>> Handle(GetWorkspacePagesQuery request, CancellationToken ct)
    {
        // The pipeline authorized ViewWorkspace; each Page still needs the
        // single-read ViewPage decision, applied in the datastore (GOV-029).
        var visible = await _visibility.VisiblePageIdsAsync(request.WorkspaceId, _requestContext.UserId, ct);
        if (visible.Count == 0) return Result<List<PageDto>>.Success([]);

        var pageEntities = await _context.Pages.AsNoTracking()
            .Where(page => page.WorkspaceId == request.WorkspaceId && page.DeletedAt == null && page.Status != PageStatus.Archived)
            .Where(page => visible.Contains(page.Id))
            .OrderBy(page => page.Title)
            .ToListAsync(ct);

        var spaces = (await _visibility.VisibleSpaceIdsAsync(request.WorkspaceId, _requestContext.UserId, ct)).ToHashSet();
        return Result<List<PageDto>>.Success(pageEntities.Select(page => DocumentDtoMapper.ToPageDto(page, spaces)).ToList());
    }
}
