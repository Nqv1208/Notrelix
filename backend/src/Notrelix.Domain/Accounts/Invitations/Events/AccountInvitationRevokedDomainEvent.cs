namespace Notrelix.Domain.Accounts.Invitations.Events;

public sealed record AccountInvitationRevokedDomainEvent(
    Guid InvitationId,
    Guid AccountId,
    Guid RevokedBy,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
