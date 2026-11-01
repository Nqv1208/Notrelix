namespace Notrelix.Application.Features.Accounts.Public.Membership;

/// <summary>
/// Producer-owned public query surface for stable Accounts membership
/// admission facts. Returns null when the account does not exist.
/// </summary>
public interface IAccountMembershipFacts
{
    Task<AccountMembershipAdmissionFact?> GetAdmissionAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    /// <summary>
    /// True only when the user holds an active membership in exactly this account.
    /// A missing user, a user of another account and an inactive member are indistinguishable.
    /// </summary>
    Task<bool> IsActiveMemberAsync(
        Guid accountId,
        Guid userId,
        CancellationToken cancellationToken);
}
