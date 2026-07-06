using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Abstractions;
namespace Notrelix.Application.Features.Workspaces.Members.Queries.GetWorkspaceMembersBySlug;

public record GetWorkspaceMembersBySlugQuery(Guid WorkspaceId, string Slug) : IQuery<Result<List<WorkspaceMemberDto>>>, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewMembers;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceType.Workspace, WorkspaceId, WorkspaceId);
}

public class GetWorkspaceMembersBySlugQueryHandler : IRequestHandler<GetWorkspaceMembersBySlugQuery, Result<List<WorkspaceMemberDto>>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly IActorLookupService _actorLookup;

    public GetWorkspaceMembersBySlugQueryHandler(IWorkspaceDbContext context, IActorLookupService actorLookup)
    {
        _context = context;
        _actorLookup = actorLookup;
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
                m.CreatedAt.DateTime
            );
        }).ToList();

        return Result<List<WorkspaceMemberDto>>.Success(result);
    }
}
