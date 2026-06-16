using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Application.Common.Security;
using global::Notrelix.Domain.Workspaces.Workspaces;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetWorkspaceMembers;

public record GetWorkspaceMembersQuery(Guid WorkspaceId) : IRequest<Result<List<WorkspaceMemberDto>>>, IAuthorizeableRequest
{
    ResourceType IAuthorizeableRequest.ResourceType => ResourceType.Workspace;
    Guid IAuthorizeableRequest.ResourceId => WorkspaceId;
    PermissionAction IAuthorizeableRequest.Action => PermissionAction.ViewMembers;
}

public class GetWorkspaceMembersQueryHandler : IRequestHandler<GetWorkspaceMembersQuery, Result<List<WorkspaceMemberDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetWorkspaceMembersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<WorkspaceMemberDto>>> Handle(GetWorkspaceMembersQuery request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && !w.IsDeleted, ct);

        if (!workspaceExists)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var members = await _context.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.WorkspaceId == request.WorkspaceId)
            .Join(_context.Users.AsNoTracking(),
                m => m.UserId,
                u => u.Id,
                (m, u) => new WorkspaceMemberDto(
                    m.UserId,
                    u.Name,
                    u.AvatarUrl,
                    m.Role.ToString(),
                    m.CreatedAt.DateTime
                ))
            .ToListAsync(ct);

        return Result<List<WorkspaceMemberDto>>.Success(members);
    }
}
