using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetUserWorkspaces;

public record GetUserWorkspacesQuery
    : IQuery<Result<List<WorkspaceDto>>>,
      IAuthenticatedRequest,
      IGlobalRequest;

public class GetUserWorkspacesQueryHandler : IRequestHandler<GetUserWorkspacesQuery, Result<List<WorkspaceDto>>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;

    public GetUserWorkspacesQueryHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext)
    {
        _context = context;
        _requestContext = requestContext;
    }

    public async Task<Result<List<WorkspaceDto>>> Handle(GetUserWorkspacesQuery request, CancellationToken ct)
    {
        if (_requestContext.UserId == Guid.Empty)
            return Result<List<WorkspaceDto>>.Failure("User is not authenticated");

        var workspaces = await _context.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.UserId == _requestContext.UserId)
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
