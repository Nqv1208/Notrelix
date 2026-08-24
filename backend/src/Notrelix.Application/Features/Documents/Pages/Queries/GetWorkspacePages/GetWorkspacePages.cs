using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.Common;
using global::Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Abstractions;

namespace Notrelix.Application.Features.Documents.Pages.Queries.GetWorkspacePages;

public record GetWorkspacePagesQuery(Guid WorkspaceId) : IQuery<Result<List<PageDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class GetWorkspacePagesQueryHandler : IRequestHandler<GetWorkspacePagesQuery, Result<List<PageDto>>>
{
    private readonly IDocumentDbContext _context;
    private readonly IResourceReferenceResolver _resourceResolver;
    public GetWorkspacePagesQueryHandler(IDocumentDbContext context, IResourceReferenceResolver resourceResolver)
    {
        _context = context;
        _resourceResolver = resourceResolver;
    }

    public async Task<Result<List<PageDto>>> Handle(GetWorkspacePagesQuery request, CancellationToken ct)
    {
        // Workspace existence is verified by checking workspace-scoped resource access at a higher layer.
        // Pages are filtered by WorkspaceId directly.

        var pageEntities = await _context.Pages.AsNoTracking()
            .Where(page => page.WorkspaceId == request.WorkspaceId && !page.IsDeleted && page.Status != PageStatus.Archived)
            .OrderBy(page => page.Title)
            .ToListAsync(ct);

        return Result<List<PageDto>>.Success(pageEntities.Select(DocumentDtoMapper.ToPageDto).ToList());
    }
}
