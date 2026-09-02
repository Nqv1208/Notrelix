using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Members.Queries.GetWorkspaceMembers;

public record GetWorkspaceMembersQuery(Guid WorkspaceId) : IQuery<Result<List<WorkspaceMemberDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    Guid IWorkspaceRequest.WorkspaceId => WorkspaceId;
    PermissionAction IRequirePermission.Action => PermissionAction.ViewMembers;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetWorkspaceMembersQueryHandler : IRequestHandler<GetWorkspaceMembersQuery, Result<List<WorkspaceMemberDto>>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly IActorLookupService _actorLookup;

    public GetWorkspaceMembersQueryHandler(IWorkspaceDbContext context, IActorLookupService actorLookup)
    {
        _context = context;
        _actorLookup = actorLookup;
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
            .ToListAsync(ct);

        var userIds = members.Select(m => m.UserId).Distinct().ToList();
        var actors = await _actorLookup.FindManyAsync(userIds, ct);
        var actorMap = actors.ToDictionary(a => a.UserId);

        var result = members.Select(m =>
        {
            actorMap.TryGetValue(m.UserId, out var actor);
            return new WorkspaceMemberDto(
                m.UserId,
                actor?.Name ?? "Unknown",
                actor?.AvatarUrl,
                m.Role.ToString(),
                m.Status.ToString(),
                m.CreatedAt.DateTime
            );
        }).ToList();

        return Result<List<WorkspaceMemberDto>>.Success(result);
    }
}
