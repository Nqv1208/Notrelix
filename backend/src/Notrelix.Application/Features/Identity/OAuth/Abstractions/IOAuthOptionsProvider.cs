namespace Notrelix.Application.Features.Identity.OAuth.Abstractions;

public interface IOAuthOptionsProvider
{
    bool IsProviderEnabled(OAuthProvider provider);
    string GetRedirectUri(OAuthProvider provider);
    string GetFrontendSuccessUrl();
    string GetFrontendFailureUrl();
    string[] GetAllowedReturnOrigins();
}
