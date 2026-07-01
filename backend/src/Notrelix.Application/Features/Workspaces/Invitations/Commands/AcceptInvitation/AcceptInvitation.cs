using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;

public record AcceptInvitationResultDto(string WorkspaceSlug, Guid WorkspaceId);

public record AcceptInvitationCommand(string Token) : ICommand<Result<AcceptInvitationResultDto>>, ITransactionalRequest;

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, Result<AcceptInvitationResultDto>>
{
    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly IIdentityDbContext _identityContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentAccount _currentAccount;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AcceptInvitationCommandHandler(
        IWorkspaceDbContext workspaceContext,
        IIdentityDbContext identityContext,
        ICurrentUser currentUser,
        ICurrentAccount currentAccount,
        IDateTimeProvider dateTimeProvider)
    {
        _workspaceContext = workspaceContext;
        _identityContext = identityContext;
        _currentUser = currentUser;
        _currentAccount = currentAccount;
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

        var user = await _identityContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, ct);

        if (user == null)
            return Result<AcceptInvitationResultDto>.Failure("Không tìm thấy thông tin tài khoản người dùng hiện tại.");

        if (user.Email.Value.Trim().ToLowerInvariant() != invitation.Email)
            return Result<AcceptInvitationResultDto>.Failure($"Lời mời này chỉ dành cho địa chỉ email '{invitation.Email}'. Tài khoản hiện tại của bạn đăng ký bằng '{user.Email.Value}'.");

        var isAlreadyMember = await _workspaceContext.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == invitation.WorkspaceId && m.UserId == _currentUser.UserId, ct);

        if (isAlreadyMember)
        {
            invitation.Accept(_currentUser.UserId, now);
            return Result<AcceptInvitationResultDto>.Success(new AcceptInvitationResultDto(workspace?.Slug ?? "", invitation.WorkspaceId));
        }

        invitation.Accept(_currentUser.UserId, now);

        var member = WorkspaceMember.Create(_currentAccount.AccountId ?? Guid.Empty, invitation.WorkspaceId, _currentUser.UserId, invitation.Role, invitation.InvitedBy, now);
        _workspaceContext.WorkspaceMembers.Add(member);

        return Result<AcceptInvitationResultDto>.Success(new AcceptInvitationResultDto(workspace?.Slug ?? "", invitation.WorkspaceId));
    }
}
