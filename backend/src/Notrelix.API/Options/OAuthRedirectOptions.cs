namespace Notrelix.API.Options;

public sealed class OAuthRedirectOptions
{
    public string FrontendSuccessUrl { get; init; } = "/auth/callback";
    public string FrontendFailureUrl { get; init; } = "/login";
}
