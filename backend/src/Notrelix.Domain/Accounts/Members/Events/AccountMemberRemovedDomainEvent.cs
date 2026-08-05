namespace Notrelix.Domain.Accounts.Members.Events;

[EventName("accounts.account-member-removed")]
public sealed record AccountMemberRemovedDomainEvent(
    Guid AccountId,
    Guid MemberId,
    Guid UserId,
    Guid ActorId,
    DateTimeOffset OccurredAt,
    string? Reason
) : AccountScopedDomainEvent(AccountId, OccurredAt);
