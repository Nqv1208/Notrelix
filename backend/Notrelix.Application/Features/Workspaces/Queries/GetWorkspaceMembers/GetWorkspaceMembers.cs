using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;

namespace Notrelix.Application.Features.Workspaces.Queries.GetWorkspaceMembers;

public record GetWorkspaceMembersQuery(Guid WorkspaceId) : IRequest<Result<List<WorkspaceMemberDto>>>;

public class GetWorkspaceMembersQueryHandler : IRequestHandler<GetWorkspaceMembersQuery, Result<List<WorkspaceMemberDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetWorkspaceMembersQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<List<WorkspaceMemberDto>>> Handle(GetWorkspaceMembersQuery request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WorkspaceId && !w.IsArchived, ct);

        if (!workspaceExists)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var isMember = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == request.WorkspaceId && m.UserId == _currentUser.UserId, ct);

        if (!isMember)
            throw new ForbiddenException("Bạn không phải thành viên của workspace này.");

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
                    m.JoinedAt
                ))
            .ToListAsync(ct);

        return Result<List<WorkspaceMemberDto>>.Success(members);
    }
}
