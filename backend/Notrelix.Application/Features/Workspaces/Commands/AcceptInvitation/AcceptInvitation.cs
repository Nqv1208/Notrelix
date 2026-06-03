using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Workspaces.Commands.AcceptInvitation;

public record AcceptInvitationResultDto(string WorkspaceSlug, Guid WorkspaceId);

public record AcceptInvitationCommand(string Token) : IRequest<Result<AcceptInvitationResultDto>>;

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, Result<AcceptInvitationResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public AcceptInvitationCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<AcceptInvitationResultDto>> Handle(AcceptInvitationCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
            return Result<AcceptInvitationResultDto>.Failure("Bạn cần đăng nhập để thực hiện hành động này.");

        // Tìm lời mời theo token
        var invitation = await _context.WorkspaceInvitations
            .Include(i => i.Workspace)
            .FirstOrDefaultAsync(i => i.Token == request.Token, ct);

        if (invitation == null)
            throw new NotFoundException(nameof(WorkspaceInvitation), request.Token);

        // Kiểm tra tính hợp lệ (hết hạn, đã accept)
        if (invitation.IsExpired)
            return Result<AcceptInvitationResultDto>.Failure("Lời mời đã hết hạn.");

        if (invitation.IsAccepted)
            return Result<AcceptInvitationResultDto>.Failure("Lời mời này đã được chấp nhận trước đó.");

        // Kiểm tra email khớp với email của user hiện tại
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, ct);

        if (user == null)
            return Result<AcceptInvitationResultDto>.Failure("Không tìm thấy thông tin tài khoản người dùng hiện tại.");

        if (user.Email.Value.Trim().ToLowerInvariant() != invitation.Email)
            return Result<AcceptInvitationResultDto>.Failure($"Lời mời này chỉ dành cho địa chỉ email '{invitation.Email}'. Tài khoản hiện tại của bạn đăng ký bằng '{user.Email.Value}'.");

        // Kiểm tra xem người dùng đã là thành viên trong workspace này chưa
        var isAlreadyMember = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == invitation.WorkspaceId && m.UserId == _currentUser.UserId, ct);

        if (isAlreadyMember)
        {
            // Nếu đã là thành viên rồi, đánh dấu accepted lời mời luôn để đóng
            invitation.Accept();
            await _context.SaveChangesAsync(ct);
            return Result<AcceptInvitationResultDto>.Success(new AcceptInvitationResultDto(invitation.Workspace.Slug, invitation.WorkspaceId));
        }

        // Chấp nhận lời mời
        invitation.Accept();

        // Tạo thành viên WorkspaceMember mới
        var member = WorkspaceMember.Create(invitation.WorkspaceId, _currentUser.UserId, invitation.Role, invitation.InvitedBy);
        _context.WorkspaceMembers.Add(member);

        await _context.SaveChangesAsync(ct);

        return Result<AcceptInvitationResultDto>.Success(new AcceptInvitationResultDto(invitation.Workspace.Slug, invitation.WorkspaceId));
    }
}
