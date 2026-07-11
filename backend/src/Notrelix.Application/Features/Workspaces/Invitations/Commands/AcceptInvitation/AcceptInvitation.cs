using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;

public record AcceptInvitationResultDto(string WorkspaceSlug, Guid WorkspaceId);

public record AcceptInvitationCommand(string Token) : ICommand<Result<AcceptInvitationResultDto>>, ITransactionalRequest;

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, Result<AcceptInvitationResultDto>>
{
    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly IActorLookupService _actorLookup;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AcceptInvitationCommandHandler(
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

    public async Task<Result<AcceptInvitationResultDto>> Handle(AcceptInvitationCommand request, CancellationToken ct)
    {
        if (!_requestContext.IsAuthenticated || _requestContext.UserId == Guid.Empty)
            return Result<AcceptInvitationResultDto>.Failure("Bạn cần đăng nhập để thực hiện hành động này.");

        var tokenHash = InvitationTokenHash.Create(request.Token);
        var invitation = await _workspaceContext.WorkspaceInvitations
            .FirstOrDefaultAsync(i => i.Token == tokenHash, ct);

        if (invitation == null)
            throw new NotFoundException(nameof(WorkspaceInvitation), request.Token);

        var workspace = await _workspaceContext.Workspaces.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == invitation.WorkspaceId, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), invitation.WorkspaceId);

        var now = _dateTimeProvider.UtcNow;

        if (now >= invitation.ExpiresAt)
            return Result<AcceptInvitationResultDto>.Failure("Lời mời đã hết hạn.");

        if (invitation.Status != WorkspaceInvitationStatus.Pending)
            return Result<AcceptInvitationResultDto>.Failure("Lời mời này không còn hiệu lực.");

        var user = await _actorLookup.FindAsync(_requestContext.UserId, ct);

        if (user == null)
            return Result<AcceptInvitationResultDto>.Failure("Không tìm thấy thông tin tài khoản người dùng hiện tại.");

        // TODO: Email validation against invitation email is not yet implemented.
        // IActorLookupService does not expose email. The invitation token binds to the email,
        // but if a token is forwarded, a different user could accept. An IIdentityUserLookupService
        // port should be introduced to check: currentUser.Email == invitation.Email.
        // This is a P1 requirement before Invitations slice is considered complete.

        var isAlreadyMember = await _workspaceContext.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == invitation.WorkspaceId && m.UserId == _requestContext.UserId, ct);

        if (isAlreadyMember)
        {
            invitation.Accept(_requestContext.UserId, now);
            return Result<AcceptInvitationResultDto>.Success(new AcceptInvitationResultDto(workspace.Slug, invitation.WorkspaceId));
        }

        invitation.Accept(_requestContext.UserId, now);

        var member = WorkspaceMember.Create(workspace.AccountId, invitation.WorkspaceId, _requestContext.UserId, invitation.Role, invitation.InvitedBy, now);
        _workspaceContext.WorkspaceMembers.Add(member);

        return Result<AcceptInvitationResultDto>.Success(new AcceptInvitationResultDto(workspace.Slug, invitation.WorkspaceId));
    }
}
