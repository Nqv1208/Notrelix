using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Workspaces.Workspaces;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetUserWorkspaces;

public record GetUserWorkspacesQuery(Guid UserId) : IQuery<Result<List<WorkspaceDto>>>;

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
            .Where(workspace => workspace.Status == WorkspaceStatus.Active && workspace.DeletedAt == null)
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
            "free", null, null,
            null, w.Status == WorkspaceStatus.Archived, memberCounts.GetValueOrDefault(w.Id), w.CreatedAt.DateTime,
            null
        )).ToList();

        return Result<List<WorkspaceDto>>.Success(result);
    }
}
