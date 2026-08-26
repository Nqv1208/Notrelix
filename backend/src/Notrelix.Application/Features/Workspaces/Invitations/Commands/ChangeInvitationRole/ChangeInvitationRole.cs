using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.ChangeInvitationRole;

public record ChangeInvitationRoleCommand(
    Guid WorkspaceId,
    Guid InvitationId,
    WorkspaceRole NewRole
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.InviteMember;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
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
