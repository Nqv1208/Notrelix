namespace Notrelix.Application.Features.Identity.OAuth.DTOs;

public sealed record OAuthCodeRedemptionRequest(
    string Code,
    string? CodeVerifier,
    string? Nonce,
    string RedirectUri);
