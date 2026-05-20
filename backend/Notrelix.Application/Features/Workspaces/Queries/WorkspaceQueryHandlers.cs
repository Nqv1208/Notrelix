using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Entities.Workspaces;

namespace Notrelix.Application.Features.Workspaces.Queries;

// ── GetWorkspaceBySlug ───────────────────────────────────────
public class GetWorkspaceBySlugQueryHandler : IRequestHandler<GetWorkspaceBySlugQuery, Result<WorkspaceDto>>
{
    private readonly IApplicationDbContext _context;

    public GetWorkspaceBySlugQueryHandler(IApplicationDbContext context) => _context = context;

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
            workspace.CreatedAt
        ));
    }
}

// ── GetUserWorkspaces ────────────────────────────────────────
public class GetUserWorkspacesQueryHandler : IRequestHandler<GetUserWorkspacesQuery, Result<List<WorkspaceDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetUserWorkspacesQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<WorkspaceDto>>> Handle(GetUserWorkspacesQuery request, CancellationToken ct)
    {
        if (request.UserId == Guid.Empty)
            return Result<List<WorkspaceDto>>.Failure("User is not authenticated");

        var workspaces = await _context.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.UserId == request.UserId)
            .Join(_context.Workspaces.AsNoTracking(),
                member => member.WorkspaceId,
                workspace => workspace.Id,
                (_, workspace) => workspace)
            .Where(workspace => !workspace.IsArchived)
            .OrderBy(workspace => workspace.Name)
            .ToListAsync(ct);

        var workspaceIds = workspaces.Select(workspace => workspace.Id).ToList();
        var memberCounts = workspaceIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _context.WorkspaceMembers
                .AsNoTracking()
                .Where(member => workspaceIds.Contains(member.WorkspaceId))
                .GroupBy(member => member.WorkspaceId)
                .Select(group => new { WorkspaceId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.WorkspaceId, item => item.Count, ct);

        var result = workspaces.Select(w => new WorkspaceDto(
            w.Id, w.Name, w.Slug, w.Description, w.IsPersonal,
            w.Plan.ToString(), w.Icon.Type.ToString(), w.Icon.Value,
            w.CoverUrl, w.IsArchived, memberCounts.GetValueOrDefault(w.Id), w.CreatedAt
        )).ToList();

        return Result<List<WorkspaceDto>>.Success(result);
    }
}

// ── GetWorkspaceMembersBySlug ────────────────────────────────
public class GetWorkspaceMembersBySlugQueryHandler : IRequestHandler<GetWorkspaceMembersBySlugQuery, Result<List<WorkspaceMemberDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetWorkspaceMembersBySlugQueryHandler(IApplicationDbContext context) => _context = context;

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

// ── GetWorkspaceActivityBySlug ───────────────────────────────
public class GetWorkspaceActivityBySlugQueryHandler : IRequestHandler<GetWorkspaceActivityBySlugQuery, Result<object>>
{
    private readonly IApplicationDbContext _context;

    public GetWorkspaceActivityBySlugQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<object>> Handle(GetWorkspaceActivityBySlugQuery request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Slug == request.Slug, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.Slug);

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
