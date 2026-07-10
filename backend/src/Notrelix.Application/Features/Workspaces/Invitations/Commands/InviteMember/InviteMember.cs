using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;

public record InviteMemberCommand(
    Guid WorkspaceId,
    string Email,
    WorkspaceRole Role
) : ICommand<Result<Guid>>, ITransactionalRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.InviteMember;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceType.Workspace, WorkspaceId, WorkspaceId);
}

public class InviteMemberCommandHandler : IRequestHandler<InviteMemberCommand, Result<Guid>>
{
    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly IActorLookupService _actorLookup;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public InviteMemberCommandHandler(
        IWorkspaceDbContext workspaceContext,
        IActorLookupService actorLookup,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _workspaceContext = workspaceContext;
        _actorLookup = actorLookup;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(InviteMemberCommand request, CancellationToken ct)
    {
        var workspace = await _workspaceContext.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && !w.IsDeleted, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var cleanEmail = request.Email.Trim().ToLowerInvariant();
        var now = _dateTimeProvider.UtcNow;

        // Check if user with this email exists via actor lookup
        // We cannot look up by email directly; the invitation flow does not require the user to exist yet.
        // The InviteMemberCommand checks for existing membership using workspace members only.

        var isAlreadyMember = await _workspaceContext.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == request.WorkspaceId, ct);

        // Note: We can't check if the target user is already a member without their UserId.
        // The email-based invite flow creates an invitation; duplicate-membership is checked at Accept time.
        // For a stricter check, a IUserLookupByEmailService port could be introduced in the future.

        var hasActiveInvitation = await _workspaceContext.WorkspaceInvitations
            .AnyAsync(i => i.WorkspaceId == request.WorkspaceId
                           && i.Email == cleanEmail
                           && i.Status == WorkspaceInvitationStatus.Pending
                           && i.ExpiresAt > now, ct);

        if (hasActiveInvitation)
            return Result<Guid>.Failure("Đã có một lời mời đang chờ xử lý dành cho email này.");

        var token = InvitationTokenHash.Create(Guid.NewGuid().ToString("N"));
        var invitation = WorkspaceInvitation.Create(workspace.AccountId, request.WorkspaceId, cleanEmail, request.Role, token, _requestContext.UserId, now);

        _workspaceContext.WorkspaceInvitations.Add(invitation);
        return Result<Guid>.Success(invitation.Id);
    }
}
