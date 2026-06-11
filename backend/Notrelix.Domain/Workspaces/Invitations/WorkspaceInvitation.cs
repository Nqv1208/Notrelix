using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Workspaces.Invitations;

public class WorkspaceInvitation : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public string Email { get; private set; } = null!;
    public WorkspaceRole Role { get; private set; }
    public string Token { get; private set; } = null!;
    public WorkspaceInvitationStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public Guid InvitedBy { get; private set; }

    private WorkspaceInvitation() : base() { }

    public static WorkspaceInvitation Create(
        Guid workspaceId, 
        string email, 
        WorkspaceRole role, 
        string token, 
        Guid invitedBy,
        TimeSpan? expiry = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(email);
        Guard.NotNullOrWhiteSpace(token);
        Guard.NotEmpty(invitedBy);

        var invitation = new WorkspaceInvitation
        {
            WorkspaceId = workspaceId,
            Email = email.Trim().ToLowerInvariant(),
            Role = role,
            Token = token,
            Status = WorkspaceInvitationStatus.Pending,
            ExpiresAt = DateTimeOffset.UtcNow.Add(expiry ?? TimeSpan.FromDays(7)),
            InvitedBy = invitedBy
        };

        invitation.SetAuditOnCreate(invitedBy);
        invitation.AddDomainEvent(new WorkspaceInvitationCreatedEvent(invitation.Id, workspaceId, invitation.Email, role, invitedBy));

        return invitation;
    }

    public void Accept(Guid userId)
    {
        if (Status != WorkspaceInvitationStatus.Pending)
            throw new BusinessRuleException("Invitation is not pending.");
        
        if (ExpiresAt < DateTimeOffset.UtcNow)
        {
            Status = WorkspaceInvitationStatus.Expired;
            throw new BusinessRuleException("Invitation has expired.");
        }

        Status = WorkspaceInvitationStatus.Accepted;
        AddDomainEvent(new WorkspaceInvitationAcceptedEvent(Id, WorkspaceId, userId, userId));
    }

    public void Revoke(Guid revokedBy)
    {
        if (Status != WorkspaceInvitationStatus.Pending) return;

        Status = WorkspaceInvitationStatus.Revoked;
        SetAuditOnUpdate(revokedBy);
    }
}
