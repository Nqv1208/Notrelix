using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using global::Notrelix.Application.Common.Events;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Domain.Workspaces.Invitations;
using global::Notrelix.Domain.Workspaces.Invitations.Events;
using global::Notrelix.Domain.Collaboration.Notifications;
using global::Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.Workspaces.Events;

public class MemberInvitedEventHandler : INotificationHandler<DomainEventNotification<WorkspaceInvitationCreatedEvent>>
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

    public async Task Handle(DomainEventNotification<WorkspaceInvitationCreatedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var invitation = await _context.WorkspaceInvitations
            .FirstOrDefaultAsync(i => i.Id == domainEvent.InvitationId, cancellationToken);

        if (invitation == null)
            return;

        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == domainEvent.WorkspaceId, cancellationToken);

        var workspaceName = workspace?.Name ?? "Workspace";
        var frontendUrl = _configuration["JwtSettings:Audience"] ?? "http://localhost:3000";
        var inviteLink = $"{frontendUrl.TrimEnd('/')}/invite/{invitation.Token}";
        var subject = $"Invitation to join workspace '{workspaceName}'";

        var emailBody = $@"
            <h3>You've been invited to '{workspaceName}'!</h3>
            <p>Role: <strong>{domainEvent.Role}</strong></p>
            <p><a href='{inviteLink}' style='display:inline-block;background-color:#0070f3;color:#fff;padding:8px 16px;text-decoration:none;border-radius:6px;'>Accept Invitation</a></p>";

        try
        {
            await _emailService.SendAsync(domainEvent.Email, subject, emailBody, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send invitation email: {ex.Message}");
        }

        var targetUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.Value == domainEvent.Email, cancellationToken);

        if (targetUser != null)
        {
            var dbNotification = Notification.Create(
                targetUser.Id,
                domainEvent.WorkspaceId,
                NotificationType.WorkspaceInvite,
                $"Invitation to '{workspaceName}'",
                $"You've been invited as {domainEvent.Role}",
                ResourceRef.Create("Workspace", domainEvent.WorkspaceId)
            );

            _context.Notifications.Add(dbNotification);
            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                await _notificationService.SendAsync(
                    targetUser.Id,
                    type: "workspace.invitation",
                    payload: $"{{\"invitationId\":\"{invitation.Id}\",\"workspaceId\":\"{domainEvent.WorkspaceId}\",\"workspaceName\":\"{workspaceName}\"}}",
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send realtime notification: {ex.Message}");
            }
        }
    }
}
