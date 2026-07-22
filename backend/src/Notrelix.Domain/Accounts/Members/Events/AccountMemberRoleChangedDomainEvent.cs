namespace Notrelix.Domain.Accounts.Members.Events;

public sealed record AccountMemberRoleChangedDomainEvent(
    Guid AccountId,
    Guid MemberId,
    Guid UserId,
    AccountRole OldRole,
    AccountRole NewRole,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
