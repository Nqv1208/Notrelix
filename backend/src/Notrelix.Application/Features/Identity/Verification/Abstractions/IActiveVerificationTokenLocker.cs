using Notrelix.Domain.Identity.Tokens;

namespace Notrelix.Application.Features.Identity.Verification.Abstractions;

/// <summary>
/// Locks and returns active email verification tokens for a user.
/// Implemented in Infrastructure using FOR UPDATE to prevent concurrent issuance.
/// </summary>
public interface IActiveVerificationTokenLocker
{
    Task<IReadOnlyList<EmailVerificationToken>> LockActiveTokensAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
