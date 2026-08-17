using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Security.DTOs;

namespace Notrelix.Application.Features.Identity.Security.Abstractions;

/// <summary>
/// Canonical step-up verification boundary for security-sensitive operations
/// (DisableMfa, RegenerateRecoveryCodes, OAuth link/unlink, ChangePassword,
/// API token issuance...).
/// Complete* methods return a VERIFIED single-use proof token; ConsumeAsync
/// accepts ONLY verified proof-store tokens. An unverified MFA challenge token
/// can never authorize a sensitive mutation.
/// Proofs are single-use, short-lived, and bound to user + session + purpose.
/// Requires an authenticated session (sid claim) to bind a proof.
/// </summary>
public interface ISecurityStepUpService
{
    /// <summary>
    /// Returns the factor the user must satisfy (MFA challenge, password or OAuth re-authentication).
    /// When MFA is enrolled, a challenge token is issued by the caller's session.
    /// </summary>
    Task<Result<StepUpRequirementResult>> GetRequirementAsync(
        Guid userId, Guid sessionId, StepUpPurpose purpose, CancellationToken ct);

    /// <summary>Completes an MFA challenge (TOTP or recovery code) and issues a purpose-bound proof.</summary>
    Task<Result<StepUpProofResult>> CompleteMfaAsync(
        Guid userId, Guid sessionId, StepUpPurpose purpose, string challengeToken, string code, CancellationToken ct);

    /// <summary>Verifies the current password and issues a purpose-bound proof.</summary>
    Task<Result<StepUpProofResult>> CompletePasswordAsync(
        Guid userId, Guid sessionId, StepUpPurpose purpose, string password, CancellationToken ct);

    /// <summary>Issues a purpose-bound proof after successful OAuth re-authentication bound to the user.</summary>
    Task<Result<StepUpProofResult>> GrantOAuthProofAsync(
        Guid userId, Guid sessionId, StepUpPurpose purpose, CancellationToken ct);

    /// <summary>
    /// Consumes a step-up proof exactly once. Fails when the proof is missing, expired,
    /// already used, or bound to a different user/session/purpose.
    /// </summary>
    Task<Result> ConsumeAsync(
        string proofToken, Guid userId, Guid sessionId, StepUpPurpose purpose, CancellationToken ct);
}