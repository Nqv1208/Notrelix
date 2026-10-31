using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Application.Features.WorkManagement.Templates.DTOs;
using Notrelix.Application.Features.WorkManagement.Templates.Services;

namespace Notrelix.Application.Features.WorkManagement.Templates.Queries.ListBoardTemplates;

public record ListBoardTemplatesQuery(Guid WorkspaceId)
    : IQuery<Result<List<BoardTemplateDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class ListBoardTemplatesQueryHandler : IRequestHandler<ListBoardTemplatesQuery, Result<List<BoardTemplateDto>>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IIdentityUserFacts _identity;
    private readonly IWorkspaceAccessResolver _workspaceAccess;

    public ListBoardTemplatesQueryHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IIdentityUserFacts identity,
        IWorkspaceAccessResolver workspaceAccess)
    {
        _workspaceAccess = workspaceAccess;
        _context = context;
        _requestContext = requestContext;
        _identity = identity;
    }

    public async Task<Result<List<BoardTemplateDto>>> Handle(ListBoardTemplatesQuery request, CancellationToken ct)
    {
        // A draft or archived template is for those who manage templates (workspace Owner and Admin, the same
        // people the policy lets publish/archive/delete). Everyone else sees published ones only.
        var access = await _workspaceAccess.ResolveAsync(request.WorkspaceId, _requestContext.UserId, ct);
        var managesTemplates = access.IsOwnerOrAdmin;

        var templates = await _context.BoardTemplates.AsNoTracking()
            .Where(t => t.WorkspaceId == request.WorkspaceId)
            .Where(t => managesTemplates || t.Status == TemplateStatus.Published)
            .Select(t => new BoardTemplateDto(
                t.Id,
                t.Name,
                t.Description,
                t.Status.ToString(),
                null))
            .ToListAsync(ct);

        // The built-in starters come first; they are the same for every workspace and always published.
        var locale = await _identity.FindLocaleAsync(_requestContext.UserId, ct);
        var builtIns = BuiltInBoardTemplates.All(locale)
            .Select(t => new BoardTemplateDto(t.Id, t.Name, t.Description, TemplateStatus.Published.ToString(), t.Key));

        return Result<List<BoardTemplateDto>>.Success(builtIns.Concat(templates).ToList());
    }
}
