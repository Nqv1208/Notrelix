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

namespace Notrelix.Application.Features.Workspaces.Queries.GetWorkspaceActivityBySlug;

public record GetWorkspaceActivityBySlugQuery(string Slug, int Page = 1, int PageSize = 20) : IRequest<Result<object>>;

public class GetWorkspaceActivityBySlugQueryHandler : IRequestHandler<GetWorkspaceActivityBySlugQuery, Result<object>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetWorkspaceActivityBySlugQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<object>> Handle(GetWorkspaceActivityBySlugQuery request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Slug == request.Slug, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.Slug);

        var isMember = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspace.Id && m.UserId == _currentUser.UserId, ct);

        if (!isMember)
            throw new ForbiddenException("Bạn không phải thành viên của workspace này.");

        var total = await _context.ActivityLogs
            .CountAsync(a => a.WorkspaceId == workspace.Id, ct);

        var logs = await _context.ActivityLogs
            .AsNoTracking()
            .Where(a => a.WorkspaceId == workspace.Id)
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
