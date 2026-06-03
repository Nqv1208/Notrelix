using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using global::Notrelix.Application.Common.Events;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Events.Workspace;

namespace Notrelix.Application.Features.Workspaces.Events;

public class MemberInvitedEventHandler : INotificationHandler<DomainEventNotification<MemberInvitedEvent>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _configuration;

    public MemberInvitedEventHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        INotificationService notificationService,
        IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _notificationService = notificationService;
        _configuration = configuration;
    }

    public async Task Handle(DomainEventNotification<MemberInvitedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        // Truy vấn invitation đang chờ để lấy Token và tên Workspace
        var invitation = await _context.WorkspaceInvitations
            .Include(i => i.Workspace)
            .FirstOrDefaultAsync(i => i.WorkspaceId == domainEvent.WorkspaceId 
                                   && i.Email == domainEvent.Email 
                                   && i.AcceptedAt == null 
                                   && i.ExpiresAt > DateTime.UtcNow, cancellationToken);

        if (invitation == null)
            return;

        // 1. Gửi Email lời mời kèm link chứa Token
        var frontendUrl = _configuration["JwtSettings:Audience"] ?? "http://localhost:3000";
        var inviteLink = $"{frontendUrl.TrimEnd('/')}/invite/{invitation.Token}";
        var workspaceName = invitation.Workspace?.Name ?? "Workspace";
        var subject = $"Lời mời tham gia Workspace '{workspaceName}'";
        
        var emailBody = $@"
            <h3>Bạn đã được mời tham gia Workspace '{workspaceName}'!</h3>
            <p>Vai trò được mời: <strong>{domainEvent.Role}</strong></p>
            <p>Để chấp nhận lời mời và tham gia làm việc cùng đội ngũ, vui lòng nhấp vào liên kết bên dưới:</p>
            <p><a href='{inviteLink}' style='display:inline-block;background-color:#0070f3;color:#fff;padding:8px 16px;text-decoration:none;border-radius:6px;'>Chấp nhận lời mời</a></p>
            <p>Nếu liên kết trên không hoạt động, bạn có thể copy URL này và dán vào trình duyệt: {inviteLink}</p>
            <p>Lời mời này sẽ hết hạn vào {invitation.ExpiresAt.ToLocalTime():g}.</p>
        ";

        try
        {
            await _emailService.SendAsync(domainEvent.Email, subject, emailBody, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log lỗi gửi email nhưng vẫn cho phép luồng chạy tiếp
            Console.WriteLine($"Lỗi gửi email invitation: {ex.Message}");
        }

        // 2. Kiểm tra xem người nhận đã có tài khoản trong hệ thống chưa để gửi Notification realtime
        var targetUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.Value.ToLower() == domainEvent.Email.ToLower(), cancellationToken);

        if (targetUser != null)
        {
            // Tạo Notification lưu DB
            var dbNotification = Notification.Create(
                domainEvent.WorkspaceId,
                targetUser.Id,
                type: "invitation",
                actorId: domainEvent.InvitedBy,
                payload: $"{{\"invitationId\":\"{invitation.Id}\",\"token\":\"{invitation.Token}\",\"workspaceName\":\"{workspaceName}\"}}"
            );

            _context.Notifications.Add(dbNotification);
            await _context.SaveChangesAsync(cancellationToken);

            // Gửi thông báo đẩy realtime qua Redis pub/sub
            try
            {
                await _notificationService.SendAsync(
                    targetUser.Id,
                    type: "invitation",
                    payload: $"{{\"invitationId\":\"{invitation.Id}\",\"token\":\"{invitation.Token}\",\"workspaceId\":\"{domainEvent.WorkspaceId}\",\"workspaceName\":\"{workspaceName}\"}}",
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi realtime notification: {ex.Message}");
            }
        }
    }
}
