using Notrelix.Application.Features.Identity.OAuth.DTOs;

namespace Notrelix.Application.Features.Identity.OAuth.Abstractions;

public interface IOAuthProviderClient
{
    Task<OAuthAuthorizationUrlResult> BuildAuthorizationUrlAsync(
        OAuthProvider provider,
        OAuthAuthorizationRequest request,
        CancellationToken ct);

    Task<ExternalOAuthProfile> RedeemCodeAsync(
        OAuthProvider provider,
        OAuthCodeRedemptionRequest request,
        CancellationToken ct);
}
