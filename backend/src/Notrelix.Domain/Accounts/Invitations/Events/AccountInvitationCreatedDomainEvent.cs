using Notrelix.Domain.Accounts.Members;
namespace Notrelix.Domain.Accounts.Invitations.Events;

[EventName("accounts.account-invitation-created")]
public sealed record AccountInvitationCreatedDomainEvent(
    Guid InvitationId,
    Guid AccountId,
    string Email,
    AccountRole Role,
    Guid InvitedBy,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
