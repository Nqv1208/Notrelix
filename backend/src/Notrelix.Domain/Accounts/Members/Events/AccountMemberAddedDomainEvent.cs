namespace Notrelix.Domain.Accounts.Members.Events;

[EventName("accounts.account-member-added")]
public sealed record AccountMemberAddedDomainEvent(
    Guid AccountId,
    Guid MemberId,
    Guid UserId,
    AccountRole Role,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
