namespace Notrelix.Application.Features.Identity.Auth.Services;

/// <summary>
/// Short-lived coordination for refresh rotation. The first request to present a refresh credential
/// rotates it; a request that presents the same credential while that rotation is under way, or within
/// the grace window after it, receives the same new credential instead of being refused. Only the
/// credential's digest is a key; the new credential itself is held for the grace window, because the
/// database keeps digests only.
/// </summary>
public interface IRefreshRotationLedger
{
    /// <returns><c>true</c> for the first claim of this credential: that request does the rotation.</returns>
    Task<bool> TryClaimAsync(string presentedHash, CancellationToken ct = default);

    /// <summary>Holds the credential that replaced <paramref name="presentedHash"/> for the grace window.</summary>
    Task RecordSuccessorAsync(string presentedHash, string newRefreshToken, CancellationToken ct = default);

    /// <returns>The credential that replaced <paramref name="presentedHash"/>, or <c>null</c> when none is held.</returns>
    Task<string?> RecallSuccessorAsync(string presentedHash, CancellationToken ct = default);
}
