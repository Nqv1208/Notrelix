using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

using global::Notrelix.Application.Common.Security;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetWorkspaceMembersBySlug;

public record GetWorkspaceMembersBySlugQuery(Guid WorkspaceId, string Slug) : IRequest<Result<List<WorkspaceMemberDto>>>, IAuthorizeableRequest
{
    ResourceType IAuthorizeableRequest.ResourceType => ResourceType.Workspace;
    Guid IAuthorizeableRequest.ResourceId => WorkspaceId;
    PermissionAction IAuthorizeableRequest.Action => PermissionAction.ViewMembers;
}

public class GetWorkspaceMembersBySlugQueryHandler : IRequestHandler<GetWorkspaceMembersBySlugQuery, Result<List<WorkspaceMemberDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetWorkspaceMembersBySlugQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<WorkspaceMemberDto>>> Handle(GetWorkspaceMembersBySlugQuery request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Slug == request.Slug, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.Slug);

        var members = await _context.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspace.Id)
            .Join(_context.Users.AsNoTracking(),
                m => m.UserId,
                u => u.Id,
                (m, u) => new WorkspaceMemberDto(
                    m.UserId,
                    u.Name,
                    u.AvatarUrl,
                    m.Role.ToString(),
                    m.JoinedAt
                ))
            .ToListAsync(ct);

        return Result<List<WorkspaceMemberDto>>.Success(members);
    }
}
