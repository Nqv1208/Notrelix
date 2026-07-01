namespace Notrelix.Domain.Accounts.Members.Events;

public sealed record AccountMemberActivatedDomainEvent(
    Guid AccountId,
    Guid MemberId,
    Guid UserId,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt, ActorId);
