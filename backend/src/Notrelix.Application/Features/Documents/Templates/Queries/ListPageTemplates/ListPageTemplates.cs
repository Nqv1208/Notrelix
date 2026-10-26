using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Templates.Common;
using Notrelix.Domain.Documents.Templates;

namespace Notrelix.Application.Features.Documents.Templates.Queries.ListPageTemplates;

/// <summary>The published templates of a workspace, by name. Every member who can see the workspace can pick one.</summary>
public record ListPageTemplatesQuery(Guid WorkspaceId) : IQuery<Result<List<PageTemplateDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class ListPageTemplatesQueryHandler : IRequestHandler<ListPageTemplatesQuery, Result<List<PageTemplateDto>>>
{
    private readonly IDocumentDbContext _context;

    public ListPageTemplatesQueryHandler(IDocumentDbContext context) => _context = context;

    public async Task<Result<List<PageTemplateDto>>> Handle(ListPageTemplatesQuery request, CancellationToken ct)
    {
        var templates = await _context.PageTemplates.AsNoTracking()
            .Where(t => t.WorkspaceId == request.WorkspaceId && t.Status == PageTemplateStatus.Published)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
        return Result<List<PageTemplateDto>>.Success(templates.Select(PageTemplateMapper.ToDto).ToList());
    }
}
