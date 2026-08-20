using Notrelix.Application.Features.Identity.Mfa.DTOs;

namespace Notrelix.Application.Features.Identity.OAuth.DTOs;

public sealed record OAuthLoginState(
    string State,
    string Nonce,
    string CodeVerifier,
    OAuthProvider Provider,
    string? ReturnUrl,
    DateTimeOffset ExpiresAt,
    OAuthFlowKind Flow = OAuthFlowKind.Login,
    Guid? BoundUserId = null,
    Guid? BoundSessionId = null,
    MfaChallengePurpose? StepUpPurpose = null);