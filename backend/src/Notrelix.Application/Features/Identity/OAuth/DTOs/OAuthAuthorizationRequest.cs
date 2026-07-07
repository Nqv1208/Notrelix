namespace Notrelix.Application.Features.Identity.OAuth.DTOs;

public sealed record OAuthAuthorizationRequest(
    string RedirectUri,
    string State,
    string? Nonce,
    string? CodeChallenge,
    string? CodeChallengeMethod);
