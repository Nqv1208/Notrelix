using Notrelix.Domain.Common;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Workspaces.Invitations;

public class WorkspaceInvitation : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public string Email { get; private set; } = null!;
    public WorkspaceRole Role { get; private set; }
    public InvitationTokenHash Token { get; private set; } = null!;
    public WorkspaceInvitationStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public Guid InvitedBy { get; private set; }

    private WorkspaceInvitation() : base() { }

    public static WorkspaceInvitation Create(
        Guid workspaceId, 
        string email, 
        WorkspaceRole role, 
        InvitationTokenHash token, 
        Guid invitedBy,
        DateTimeOffset createdAt,
        TimeSpan? expiry = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(email);
        Guard.NotNull(token);
        Guard.NotEmpty(invitedBy);

        var invitation = new WorkspaceInvitation
        {
            WorkspaceId = workspaceId,
            Email = email.Trim().ToLowerInvariant(),
            Role = role,
            Token = token,
            Status = WorkspaceInvitationStatus.Pending,
            ExpiresAt = createdAt.Add(expiry ?? TimeSpan.FromDays(7)),
            InvitedBy = invitedBy
        };

        invitation.SetAuditOnCreate(invitedBy, createdAt);
        invitation.AddDomainEvent(new WorkspaceInvitationCreatedEvent(invitation.Id, workspaceId, invitation.Email, role, invitedBy, createdAt));

        return invitation;
    }

    public void Accept(Guid userId, DateTimeOffset acceptedAt)
    {
        if (Status != WorkspaceInvitationStatus.Pending)
            throw new BusinessRuleException("Invitation is not pending.");
        
        if (ExpiresAt < acceptedAt)
        {
            Status = WorkspaceInvitationStatus.Expired;
            AddDomainEvent(new WorkspaceInvitationExpiredEvent(Id, WorkspaceId, acceptedAt));
            throw new BusinessRuleException("Invitation has expired.");
        }

        Status = WorkspaceInvitationStatus.Accepted;
        SetAuditOnUpdate(userId, acceptedAt);
        AddDomainEvent(new WorkspaceInvitationAcceptedEvent(Id, WorkspaceId, userId, userId, acceptedAt));
    }

    public void Revoke(Guid revokedBy, DateTimeOffset revokedAt)
    {
        if (Status != WorkspaceInvitationStatus.Pending) return;

        Status = WorkspaceInvitationStatus.Revoked;
        SetAuditOnUpdate(revokedBy, revokedAt);
        AddDomainEvent(new WorkspaceInvitationRevokedEvent(Id, WorkspaceId, revokedBy, revokedAt));
    }
}
