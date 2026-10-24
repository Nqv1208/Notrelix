using Notrelix.Application.Features.Identity.Public.Facts;

namespace Notrelix.Application.Features.Identity.Public.Queries;

/// <summary>
/// Producer-owned public query surface for stable Identity user facts.
/// Returns null when the user does not exist.
/// </summary>
public interface IIdentityUserFacts
{
    Task<IdentityUserFact?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sign-in emails of the given users (lower-case, trimmed); users that do not exist are omitted.
    /// An email is personal data: the caller owns the visibility rule and must never call this for a
    /// reader the policy does not allow.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> FindEmailsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Whether a sign-in identity exists for the email (case-insensitive, trimmed). Only a caller that
    /// already holds proof of the address (for example a one-time invitation token delivered to it) may
    /// expose the answer; it must never back an anonymous "does this account exist" lookup.
    /// </summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);
}
