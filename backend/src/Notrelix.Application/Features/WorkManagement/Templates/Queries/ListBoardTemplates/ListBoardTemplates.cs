using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Templates.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Templates.Queries.ListBoardTemplates;

public record ListBoardTemplatesQuery(Guid WorkspaceId)
    : IQuery<Result<List<BoardTemplateDto>>>, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class ListBoardTemplatesQueryHandler : IRequestHandler<ListBoardTemplatesQuery, Result<List<BoardTemplateDto>>>
{
    private readonly IWorkManagementDbContext _context;

    public ListBoardTemplatesQueryHandler(IWorkManagementDbContext context) => _context = context;

    public async Task<Result<List<BoardTemplateDto>>> Handle(ListBoardTemplatesQuery request, CancellationToken ct)
    {
        var templates = await _context.BoardTemplates.AsNoTracking()
            .Where(t => t.WorkspaceId == request.WorkspaceId)
            .Select(t => new BoardTemplateDto(
                t.Id,
                t.Name,
                t.Description,
                t.Status.ToString()))
            .ToListAsync(ct);

        return Result<List<BoardTemplateDto>>.Success(templates);
    }
}
