namespace Notrelix.Domain.Accounts.Members.Events;

[EventName("accounts.account-member-restored")]
public sealed record AccountMemberRestoredDomainEvent(
    Guid AccountId,
    Guid MemberId,
    Guid UserId,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
