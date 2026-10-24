namespace Notrelix.Application.Features.Accounts.Public.Domains;

/// <summary>
/// Producer-owned public query surface for the verified-domain facts of an
/// Account (ACC-010). Consumers never read Accounts tables.
/// </summary>
public interface IAccountDomainFacts
{
    /// <summary>
    /// The Account's primary verified domain: the verified domain that was verified first (ties broken by
    /// name). Null when the Account has no verified domain or does not exist. Pending and rejected domains
    /// never qualify.
    /// </summary>
    Task<string?> GetPrimaryVerifiedDomainAsync(Guid accountId, CancellationToken cancellationToken);
}
