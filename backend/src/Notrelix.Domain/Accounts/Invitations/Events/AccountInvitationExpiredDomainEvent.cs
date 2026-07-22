namespace Notrelix.Domain.Accounts.Invitations.Events;

public sealed record AccountInvitationExpiredDomainEvent(
    Guid InvitationId,
    Guid AccountId,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
