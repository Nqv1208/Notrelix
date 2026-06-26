using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;

public record AcceptInvitationResultDto(string WorkspaceSlug, Guid WorkspaceId);

public record AcceptInvitationCommand(string Token) : ICommand<Result<AcceptInvitationResultDto>>, ITransactionalRequest;

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, Result<AcceptInvitationResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AcceptInvitationCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AcceptInvitationResultDto>> Handle(AcceptInvitationCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
            return Result<AcceptInvitationResultDto>.Failure("Bạn cần đăng nhập để thực hiện hành động này.");

        var tokenHash = InvitationTokenHash.Create(request.Token);
        var invitation = await _context.WorkspaceInvitations
            .FirstOrDefaultAsync(i => i.Token == tokenHash, ct);

        if (invitation == null)
            throw new NotFoundException(nameof(WorkspaceInvitation), request.Token);

        var workspace = await _context.Workspaces.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == invitation.WorkspaceId, ct);

        var now = _dateTimeProvider.UtcNow;

        if (now >= invitation.ExpiresAt)
            return Result<AcceptInvitationResultDto>.Failure("Lời mời đã hết hạn.");

        if (invitation.Status != WorkspaceInvitationStatus.Pending)
            return Result<AcceptInvitationResultDto>.Failure("Lời mời này không còn hiệu lực.");

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, ct);

        if (user == null)
            return Result<AcceptInvitationResultDto>.Failure("Không tìm thấy thông tin tài khoản người dùng hiện tại.");

        if (user.Email.Value.Trim().ToLowerInvariant() != invitation.Email)
            return Result<AcceptInvitationResultDto>.Failure($"Lời mời này chỉ dành cho địa chỉ email '{invitation.Email}'. Tài khoản hiện tại của bạn đăng ký bằng '{user.Email.Value}'.");

        var isAlreadyMember = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == invitation.WorkspaceId && m.UserId == _currentUser.UserId, ct);

        if (isAlreadyMember)
        {
            invitation.Accept(_currentUser.UserId, now);
            return Result<AcceptInvitationResultDto>.Success(new AcceptInvitationResultDto(workspace?.Slug ?? "", invitation.WorkspaceId));
        }

        invitation.Accept(_currentUser.UserId, now);

        var member = WorkspaceMember.Create(invitation.WorkspaceId, _currentUser.UserId, invitation.Role, invitation.InvitedBy, now);
        _context.WorkspaceMembers.Add(member);

        return Result<AcceptInvitationResultDto>.Success(new AcceptInvitationResultDto(workspace?.Slug ?? "", invitation.WorkspaceId));
    }
}
