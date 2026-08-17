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