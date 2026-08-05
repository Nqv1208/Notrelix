using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Invitations.Queries.GetWorkspaceInvitations;

public record GetWorkspaceInvitationsQuery(Guid WorkspaceId) : IQuery<Result<List<WorkspaceInvitationDto>>>, IWorkspaceRequest, IRequirePermission
{
    Guid IWorkspaceRequest.WorkspaceId => WorkspaceId;
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetWorkspaceInvitationsQueryHandler : IRequestHandler<GetWorkspaceInvitationsQuery, Result<List<WorkspaceInvitationDto>>>
{
    private readonly IWorkspaceDbContext _context;

    public GetWorkspaceInvitationsQueryHandler(IWorkspaceDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<WorkspaceInvitationDto>>> Handle(GetWorkspaceInvitationsQuery request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && !w.IsDeleted, ct);

        if (!workspaceExists)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var invitations = await _context.WorkspaceInvitations
            .AsNoTracking()
            .Where(invitation => invitation.WorkspaceId == request.WorkspaceId)
            .OrderByDescending(invitation => invitation.CreatedAt)
            .ToListAsync(ct);

        var result = invitations.Select(invitation => new WorkspaceInvitationDto(
            invitation.Id,
            invitation.Email,
            invitation.Role.ToString(),
            invitation.ExpiresAt.DateTime,
            invitation.Status == WorkspaceInvitationStatus.Accepted,
            invitation.CreatedAt.DateTime
        )).ToList();

        return Result<List<WorkspaceInvitationDto>>.Success(result);
    }
}
