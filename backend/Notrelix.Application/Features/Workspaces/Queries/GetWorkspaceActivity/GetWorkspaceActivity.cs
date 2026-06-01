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

namespace Notrelix.Application.Features.Workspaces.Queries.GetWorkspaceActivity;

public record GetWorkspaceActivityQuery(Guid WorkspaceId, int Page = 1, int PageSize = 20) : IRequest<Result<object>>;

public class GetWorkspaceActivityQueryHandler : IRequestHandler<GetWorkspaceActivityQuery, Result<object>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetWorkspaceActivityQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<object>> Handle(GetWorkspaceActivityQuery request, CancellationToken ct)
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

        var total = await _context.ActivityLogs
            .CountAsync(a => a.WorkspaceId == request.WorkspaceId, ct);

        var logs = await _context.ActivityLogs
            .AsNoTracking()
            .Where(a => a.WorkspaceId == request.WorkspaceId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new
            {
                a.Id,
                a.ActorId,
                a.Action,
                ResourceType = a.ResourceType.ToString(),
                a.ResourceId,
                a.ResourceTitle,
                a.CreatedAt
            })
            .ToListAsync(ct);

        return Result<object>.Success(new { data = logs, total, page = request.Page, pageSize = request.PageSize });
    }
}
