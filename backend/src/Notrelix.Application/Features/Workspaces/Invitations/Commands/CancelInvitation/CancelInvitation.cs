using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.CancelInvitation;

public record CancelInvitationCommand(
    Guid WorkspaceId,
    Guid InvitationId
) : ICommand<Result>, ITransactionalRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.InviteMember;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Workspace, WorkspaceId, WorkspaceId);
}

public class CancelInvitationCommandHandler : IRequestHandler<CancelInvitationCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CancelInvitationCommandHandler(
        IWorkspaceDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(CancelInvitationCommand request, CancellationToken ct)
    {
        var invitation = await _context.WorkspaceInvitations
            .FirstOrDefaultAsync(i => i.Id == request.InvitationId && i.WorkspaceId == request.WorkspaceId, ct);

        if (invitation is null)
            throw new NotFoundException(nameof(WorkspaceInvitation), request.InvitationId);

        var now = _dateTimeProvider.UtcNow;
        if (now >= invitation.ExpiresAt)
            invitation.Expire(now);
        else
            invitation.Revoke(_requestContext.UserId, now);

        return Result.Success();
    }
}
