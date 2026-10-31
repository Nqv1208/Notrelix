namespace Notrelix.Application.Features.Identity.Auth.Services;

/// <summary>
/// Short-lived coordination for refresh rotation. The first request to present a refresh credential
/// rotates it; a request that presents the same credential while that rotation is under way, or within
/// the grace window after it, receives the same new credential instead of being refused. Only the
/// credential's digest is a key; the new credential itself is held for the grace window, because the
/// database keeps digests only, and it is held encrypted under a key derived from the replaced
/// credential, so only a presenter of that credential can read it back.
/// </summary>
public interface IRefreshRotationLedger
{
    /// <returns><c>true</c> for the first claim of this credential: that request does the rotation.</returns>
    Task<bool> TryClaimAsync(string presentedHash, CancellationToken ct = default);

    /// <summary>Frees the claim of a credential whose rotation never landed, so a retry can take it.</summary>
    Task ReleaseClaimAsync(string presentedHash, CancellationToken ct = default);

    /// <summary>Holds the credential that replaced <paramref name="presentedRefreshToken"/> for the grace window.</summary>
    Task RecordSuccessorAsync(string presentedRefreshToken, string newRefreshToken, CancellationToken ct = default);

    /// <returns>
    /// The credential that replaced <paramref name="presentedRefreshToken"/>, or <c>null</c> when none is held
    /// or the held value cannot be read with it.
    /// </returns>
    Task<string?> RecallSuccessorAsync(string presentedRefreshToken, CancellationToken ct = default);
}
