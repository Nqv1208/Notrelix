namespace Notrelix.Domain.Accounts.Members.Events;

[EventName("accounts.account-member-activated")]
public sealed record AccountMemberActivatedDomainEvent(
    Guid AccountId,
    Guid MemberId,
    Guid UserId,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
