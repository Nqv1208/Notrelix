using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.ChangeInvitationRole;

/// <summary>
/// Changes the role a pending invitation offers. Guarded by the invitation's expected version, so a role
/// chosen on a stale row (the invitation was resent, changed or answered meanwhile) is refused.
/// </summary>
public record ChangeInvitationRoleCommand(
    Guid WorkspaceId,
    Guid InvitationId,
    WorkspaceRole NewRole,
    long ExpectedVersion
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.InviteMember;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    // The concurrency authority is the WorkspaceInvitation; authorization stays on the Workspace.
    ResourceRef IExpectedVersionRequest.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace-invitation"), InvitationId, WorkspaceId);
}

public class ChangeInvitationRoleCommandHandler : IRequestHandler<ChangeInvitationRoleCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ChangeInvitationRoleCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ChangeInvitationRoleCommand request, CancellationToken ct)
    {
        var invitation = await _context.WorkspaceInvitations
            .FirstOrDefaultAsync(i => i.Id == request.InvitationId && i.WorkspaceId == request.WorkspaceId, ct);

        if (invitation is null)
            throw new NotFoundException(nameof(WorkspaceInvitation), request.InvitationId);

        invitation.ChangeRole(request.NewRole, _requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
