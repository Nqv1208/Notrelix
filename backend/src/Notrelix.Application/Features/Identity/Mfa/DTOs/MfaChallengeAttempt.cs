namespace Notrelix.Application.Features.Identity.Mfa.DTOs;

/// <summary>
/// Result of recording one verification attempt against a challenge.
/// The attempt budget is CUMULATIVE for the challenge lifetime (bounded by
/// the challenge TTL) and is never reset by wall-clock windows: when attempts
/// exceed <c>maxAttempts</c> the challenge is atomically invalidated.
/// </summary>
public sealed record MfaChallengeAttempt(int Attempts, bool Exceeded);