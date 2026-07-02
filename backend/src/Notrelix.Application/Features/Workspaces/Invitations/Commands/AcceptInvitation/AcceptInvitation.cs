using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;

public record AcceptInvitationResultDto(string WorkspaceSlug, Guid WorkspaceId);

public record AcceptInvitationCommand(string Token) : ICommand<Result<AcceptInvitationResultDto>>, ITransactionalRequest;

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, Result<AcceptInvitationResultDto>>
{
    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly IActorLookupService _actorLookup;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenantContext _tenant;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AcceptInvitationCommandHandler(
        IWorkspaceDbContext workspaceContext,
        IActorLookupService actorLookup,
        ICurrentUser currentUser,
        ICurrentTenantContext tenant,
        IDateTimeProvider dateTimeProvider)
    {
        _workspaceContext = workspaceContext;
        _actorLookup = actorLookup;
        _currentUser = currentUser;
        _tenant = tenant;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AcceptInvitationResultDto>> Handle(AcceptInvitationCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
            return Result<AcceptInvitationResultDto>.Failure("Bạn cần đăng nhập để thực hiện hành động này.");

        var tokenHash = InvitationTokenHash.Create(request.Token);
        var invitation = await _workspaceContext.WorkspaceInvitations
            .FirstOrDefaultAsync(i => i.Token == tokenHash, ct);

        if (invitation == null)
            throw new NotFoundException(nameof(WorkspaceInvitation), request.Token);

        var workspace = await _workspaceContext.Workspaces.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == invitation.WorkspaceId, ct);

        var now = _dateTimeProvider.UtcNow;

        if (now >= invitation.ExpiresAt)
            return Result<AcceptInvitationResultDto>.Failure("Lời mời đã hết hạn.");

        if (invitation.Status != WorkspaceInvitationStatus.Pending)
            return Result<AcceptInvitationResultDto>.Failure("Lời mời này không còn hiệu lực.");

        var user = await _actorLookup.FindAsync(_currentUser.UserId, ct);

        if (user == null)
            return Result<AcceptInvitationResultDto>.Failure("Không tìm thấy thông tin tài khoản người dùng hiện tại.");

        // Note: Email validation against invitation email cannot be done via IActorLookupService
        // since it doesn't expose email. The invitation token already binds to the email.
        // For stricter email validation, an IAccountLookupService port could be introduced.

        var isAlreadyMember = await _workspaceContext.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == invitation.WorkspaceId && m.UserId == _currentUser.UserId, ct);

        if (isAlreadyMember)
        {
            invitation.Accept(_currentUser.UserId, now);
            return Result<AcceptInvitationResultDto>.Success(new AcceptInvitationResultDto(workspace?.Slug ?? "", invitation.WorkspaceId));
        }

        invitation.Accept(_currentUser.UserId, now);

        var member = WorkspaceMember.Create(_tenant.RequireAccountId(), invitation.WorkspaceId, _currentUser.UserId, invitation.Role, invitation.InvitedBy, now);
        _workspaceContext.WorkspaceMembers.Add(member);

        return Result<AcceptInvitationResultDto>.Success(new AcceptInvitationResultDto(workspace?.Slug ?? "", invitation.WorkspaceId));
    }
}
