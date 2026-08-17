namespace Notrelix.Application.Features.Identity.Security.DTOs;

/// <summary>
/// Transient Redis payload behind a verified single-use step-up proof token.
/// A proof is issued only after a factor has been verified for User + Session + Purpose,
/// and only a verified proof may authorize a sensitive mutation.
/// </summary>
public sealed record StepUpProofPayload(
    Guid UserId,
    Guid SessionId,
    StepUpPurpose Purpose,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt);
