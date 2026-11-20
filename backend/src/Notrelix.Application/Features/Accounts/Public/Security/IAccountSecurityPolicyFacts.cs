namespace Notrelix.Application.Features.Accounts.Public.Security;

/// <summary>
/// Producer-owned public query surface for the security policy facts of an Account. Consumers never read
/// Accounts tables.
/// </summary>
public interface IAccountSecurityPolicyFacts
{
    /// <summary>True when the Account requires every member to hold an active MFA factor. False when it does not exist.</summary>
    Task<bool> IsMfaRequiredAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>
    /// True when any Account the person is an active member of requires MFA (the stricter reading: one
    /// requiring Account is enough, whatever the person's other Accounts say).
    /// </summary>
    Task<bool> IsMfaRequiredForPersonAsync(Guid userId, CancellationToken cancellationToken);
}
