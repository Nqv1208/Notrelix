namespace Notrelix.Application.Features.Identity.Security.DTOs;

/// <summary>
/// Transient Redis payload behind a verified single-use step-up proof token.
/// A proof is issued only after a factor has been verified for User + Session + Purpose,
/// and only a verified proof may authorize a sensitive mutation.
/// <paramref name="Target"/> names what the proof is for when the purpose acts on one thing (see
/// <see cref="StepUpTarget"/>); a proof with a target authorizes only that.
/// </summary>
public sealed record StepUpProofPayload(
    Guid UserId,
    Guid SessionId,
    StepUpPurpose Purpose,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    string? Target = null);
