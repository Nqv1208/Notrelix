using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Application.Common.Security;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetWorkspaceBySlug;

public record GetWorkspaceBySlugQuery(Guid WorkspaceId, string Slug) : IQuery<Result<WorkspaceDto>>, IAuthorizeableRequest
{
    ResourceType IAuthorizeableRequest.ResourceType => ResourceType.Workspace;
    Guid IAuthorizeableRequest.ResourceId => WorkspaceId;
    PermissionAction IAuthorizeableRequest.Action => PermissionAction.ViewWorkspace;
}

public class GetWorkspaceBySlugQueryHandler : IRequestHandler<GetWorkspaceBySlugQuery, Result<WorkspaceDto>>
{
    private readonly IApplicationDbContext _context;

    public GetWorkspaceBySlugQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<WorkspaceDto>> Handle(GetWorkspaceBySlugQuery request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Slug == request.Slug && w.Status == WorkspaceStatus.Active && !w.IsDeleted, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.Slug);

        var memberCount = await _context.WorkspaceMembers
            .CountAsync(m => m.WorkspaceId == workspace.Id, ct);

        return Result<WorkspaceDto>.Success(new WorkspaceDto(
            workspace.Id,
            workspace.Name,
            workspace.Slug,
            workspace.Description,
            workspace.IsPersonal,
            "free",
            null,
            null,
            null,
            false,
            memberCount,
            workspace.CreatedAt.DateTime,
            null
        ));
    }
}
