namespace Notrelix.Domain.Accounts.Invitations.Events;

public sealed record AccountInvitationAcceptedDomainEvent(
    Guid InvitationId,
    Guid AccountId,
    Guid AcceptedByUserId,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt, ActorId);
