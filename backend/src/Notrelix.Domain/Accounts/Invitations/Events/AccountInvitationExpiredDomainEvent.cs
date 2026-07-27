namespace Notrelix.Domain.Accounts.Invitations.Events;

[EventName("accounts.account-invitation-expired")]
public sealed record AccountInvitationExpiredDomainEvent(
    Guid InvitationId,
    Guid AccountId,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
