namespace Notrelix.Domain.Accounts.Invitations.Events;

[EventName("accounts.account-invitation-accepted")]
public sealed record AccountInvitationAcceptedDomainEvent(
    Guid InvitationId,
    Guid AccountId,
    Guid AcceptedByUserId,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
