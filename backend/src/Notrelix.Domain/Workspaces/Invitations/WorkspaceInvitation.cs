namespace Notrelix.Domain.Workspaces.Invitations;

public class WorkspaceInvitation : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Email { get; private set; } = null!;
    public WorkspaceRole Role { get; private set; }
    public InvitationTokenHash Token { get; private set; } = null!;
    public WorkspaceInvitationStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public Guid InvitedBy { get; private set; }

    private WorkspaceInvitation() : base() { }

    public static WorkspaceInvitation Create(
        Guid accountId,
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

        var emailValue = SharedKernel.Email.Create(email);

        if (expiry is not null && expiry <= TimeSpan.Zero)
            throw new BusinessRuleException("Invitation expiry must be greater than zero.");

        var invitation = new WorkspaceInvitation
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Email = emailValue.Value,
            Role = role,
            Token = token,
            Status = WorkspaceInvitationStatus.Pending,
            ExpiresAt = createdAt.Add(expiry ?? TimeSpan.FromDays(7)),
            InvitedBy = invitedBy
        };

        invitation.SetAuditOnCreate(invitedBy, createdAt);
        invitation.AddDomainEvent(new WorkspaceInvitationCreatedDomainEvent(
            accountId, invitation.Id, workspaceId, invitation.Email, role, invitedBy, createdAt));

        return invitation;
    }

    public void Accept(Guid acceptedUserId, DateTimeOffset acceptedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(acceptedUserId);

        if (Status != WorkspaceInvitationStatus.Pending)
            throw new BusinessRuleException("Invitation is not pending.");

        if (acceptedAt >= ExpiresAt)
            throw new BusinessRuleException("Invitation has expired.");

        Status = WorkspaceInvitationStatus.Accepted;
        SetAuditOnUpdate(acceptedUserId, acceptedAt);

        AddDomainEvent(new WorkspaceInvitationAcceptedDomainEvent(
            AccountId, Id, WorkspaceId, acceptedUserId, acceptedUserId, acceptedAt));
    }

    public void Expire(DateTimeOffset expiredAt)
    {
        EnsureNotDeleted();

        if (Status != WorkspaceInvitationStatus.Pending) return;

        Status = WorkspaceInvitationStatus.Expired;
        SetAuditOnUpdate(null, expiredAt);

        AddDomainEvent(new WorkspaceInvitationExpiredDomainEvent(
            AccountId, Id, WorkspaceId, expiredAt));
    }

    public void Revoke(Guid revokedBy, DateTimeOffset revokedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(revokedBy);

        if (Status != WorkspaceInvitationStatus.Pending) return;

        if (revokedAt >= ExpiresAt)
            throw new BusinessRuleException("Invitation has expired.");

        Status = WorkspaceInvitationStatus.Revoked;
        SetAuditOnUpdate(revokedBy, revokedAt);

        AddDomainEvent(new WorkspaceInvitationRevokedDomainEvent(
            AccountId, Id, WorkspaceId, revokedBy, revokedAt));
    }
}
