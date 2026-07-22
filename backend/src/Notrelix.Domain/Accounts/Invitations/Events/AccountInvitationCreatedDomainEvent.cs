namespace Notrelix.Domain.Accounts.Invitations.Events;

public sealed record AccountInvitationCreatedDomainEvent(
    Guid InvitationId,
    Guid AccountId,
    string Email,
    AccountRole Role,
    Guid InvitedBy,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
