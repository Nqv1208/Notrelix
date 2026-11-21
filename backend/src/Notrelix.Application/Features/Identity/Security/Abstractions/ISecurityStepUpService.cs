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

    /// <summary>
    /// Completes an MFA challenge (TOTP or recovery code) and issues a purpose-bound proof,
    /// bound to <paramref name="target"/> when the purpose acts on one thing (see <see cref="StepUpTarget"/>).
    /// </summary>
    Task<Result<StepUpProofResult>> CompleteMfaAsync(
        Guid userId, Guid sessionId, StepUpPurpose purpose, string challengeToken, string code, string? target, CancellationToken ct);

    /// <summary>Verifies the current password and issues a purpose-bound proof (and target-bound when given).</summary>
    Task<Result<StepUpProofResult>> CompletePasswordAsync(
        Guid userId, Guid sessionId, StepUpPurpose purpose, string password, string? target, CancellationToken ct);

    /// <summary>Issues a purpose-bound proof (and target-bound when given) after successful OAuth re-authentication bound to the user.</summary>
    Task<Result<StepUpProofResult>> GrantOAuthProofAsync(
        Guid userId, Guid sessionId, StepUpPurpose purpose, string? target, CancellationToken ct);

    /// <summary>
    /// Consumes a step-up proof exactly once. Fails when the proof is missing, expired,
    /// already used, bound to a different user/session/purpose, or bound to a target (a
    /// target-bound proof is spent only through <see cref="ConsumeBoundAsync"/>).
    /// </summary>
    Task<Result> ConsumeAsync(
        string proofToken, Guid userId, Guid sessionId, StepUpPurpose purpose, CancellationToken ct);

    /// <summary>
    /// Consumes a step-up proof exactly once, and only when it was issued for exactly
    /// <paramref name="target"/>. A proof for another target, or for none, fails like a stale one (and is spent).
    /// </summary>
    Task<Result> ConsumeBoundAsync(
        string proofToken, Guid userId, Guid sessionId, StepUpPurpose purpose, string target, CancellationToken ct);
}