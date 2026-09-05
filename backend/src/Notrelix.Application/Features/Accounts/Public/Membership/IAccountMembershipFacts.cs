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
}
