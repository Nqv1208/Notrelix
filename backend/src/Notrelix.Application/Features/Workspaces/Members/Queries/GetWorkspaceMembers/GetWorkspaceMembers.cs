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
    private readonly IMemberPhoneLookup _phoneLookup;
    private readonly ICurrentRequestContext _requestContext;

    public GetWorkspaceMembersQueryHandler(
        IWorkspaceDbContext context,
        IActorLookupService actorLookup,
        IMemberPhoneLookup phoneLookup,
        ICurrentRequestContext requestContext)
    {
        _context = context;
        _actorLookup = actorLookup;
        _phoneLookup = phoneLookup;
        _requestContext = requestContext;
    }

    public async Task<Result<List<WorkspaceMemberDto>>> Handle(GetWorkspaceMembersQuery request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && w.DeletedAt == null, ct);

        if (!workspaceExists)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var members = await _context.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.WorkspaceId == request.WorkspaceId && m.Status != WorkspaceMemberStatus.Removed)
            .ToListAsync(ct);

        var userIds = members.Select(m => m.UserId).Distinct().ToList();
        var actors = await _actorLookup.FindManyAsync(userIds, ct);
        var actorMap = actors.ToDictionary(a => a.UserId);

        // A phone is visible only to a non-guest member of this Workspace and only for people who
        // opted in (D15). A guest reader gets none; a guest subject's phone is not shared either.
        var reader = members.FirstOrDefault(m => m.UserId == _requestContext.UserId);
        var phoneCandidates = reader is { IsInternalParticipant: true }
            ? members.Where(m => m.IsInternalParticipant).Select(m => m.UserId).ToList()
            : [];
        var phones = await _phoneLookup.FindSharedPhonesAsync(request.WorkspaceId, phoneCandidates, ct);

        var result = members.Select(m =>
        {
            actorMap.TryGetValue(m.UserId, out var actor);
            phones.TryGetValue(m.UserId, out var phone);
            return new WorkspaceMemberDto(
                m.UserId,
                actor?.Name ?? "Unknown",
                actor?.AvatarUrl,
                m.Role.ToString(),
                m.CreatedAt.UtcDateTime,
                m.Status.ToString(),
                m.JobTitle,
                m.Version,
                phone
            );
        }).ToList();

        return Result<List<WorkspaceMemberDto>>.Success(result);
    }
}
