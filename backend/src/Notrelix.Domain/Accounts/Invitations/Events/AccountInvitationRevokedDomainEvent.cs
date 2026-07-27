namespace Notrelix.Domain.Accounts.Invitations.Events;

[EventName("accounts.account-invitation-revoked")]
public sealed record AccountInvitationRevokedDomainEvent(
    Guid InvitationId,
    Guid AccountId,
    Guid RevokedBy,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
