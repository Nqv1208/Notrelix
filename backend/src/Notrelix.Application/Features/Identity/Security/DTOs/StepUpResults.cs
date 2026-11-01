namespace Notrelix.Application.Features.Identity.Security.DTOs;

/// <summary>Result of requesting step-up verification for a purpose. Tells the client which factor to satisfy.</summary>
public sealed record StepUpRequirementResult(
    StepUpRequiredFactor RequiredFactor,
    string? ChallengeToken,
    DateTimeOffset? ExpiresAt);

/// <summary>Single-use step-up proof bound to user, session and purpose.</summary>
public sealed record StepUpProofResult(
    string ProofToken,
    DateTimeOffset ExpiresAt);

/// <summary>What a fresh provider re-authentication granted: one proof, and for EnrollMfa a second (the activation spends its own).</summary>
public sealed record OAuthStepUpResult(StepUpProofResult Proof, StepUpProofResult? SpareProof);

/// <summary>The proof(s) of a provider re-authentication, handed to the page once.</summary>
public sealed record ProviderStepUpClaimResult(string ProofToken, string? SpareProofToken);
