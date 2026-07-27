namespace Notrelix.Domain.Accounts.Members.Events;

[EventName("accounts.account-member-role-changed")]
public sealed record AccountMemberRoleChangedDomainEvent(
    Guid AccountId,
    Guid MemberId,
    Guid UserId,
    AccountRole OldRole,
    AccountRole NewRole,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
