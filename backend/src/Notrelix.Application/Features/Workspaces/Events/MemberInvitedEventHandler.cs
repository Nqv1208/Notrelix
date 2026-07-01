using global::Notrelix.Application.Common.Events;

namespace Notrelix.Application.Features.Workspaces.Events;

[Obsolete("This handler is dead code because WorkspaceInvitationCreatedDomainEvent is dispatched via Outbox mode. Email/notification delivery should be implemented through an outbox consumer.")]
public class MemberInvitedEventHandler : INotificationHandler<DomainEventNotification<WorkspaceInvitationCreatedDomainEvent>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public MemberInvitedEventHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task Handle(DomainEventNotification<WorkspaceInvitationCreatedDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var invitation = await _context.WorkspaceInvitations
            .FirstOrDefaultAsync(i => i.Id == domainEvent.InvitationId, cancellationToken);

        if (invitation == null)
            return;

        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == domainEvent.WorkspaceId, cancellationToken);

        var workspaceName = workspace?.Name ?? "Workspace";
        var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
        var inviteLink = $"{frontendUrl.TrimEnd('/')}/invite/{invitation.Token.Value}";
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
    }
}
