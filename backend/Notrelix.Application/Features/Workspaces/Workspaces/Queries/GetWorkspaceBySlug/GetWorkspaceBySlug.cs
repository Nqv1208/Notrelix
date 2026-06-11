using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

using global::Notrelix.Application.Common.Security;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetWorkspaceBySlug;

public record GetWorkspaceBySlugQuery(Guid WorkspaceId, string Slug) : IRequest<Result<WorkspaceDto>>, IAuthorizeableRequest
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
            .FirstOrDefaultAsync(w => w.Slug == request.Slug && !w.IsArchived, ct);

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
            workspace.Plan.ToString(),
            workspace.Icon.Type.ToString(),
            workspace.Icon.Value,
            workspace.CoverUrl,
            workspace.IsArchived,
            memberCount,
            workspace.CreatedAt,
            workspace.Settings
        ));
    }
}
