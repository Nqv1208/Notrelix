namespace Notrelix.API.Options;

public sealed class OAuthRedirectOptions
{
    public string FrontendSuccessUrl { get; init; } = "/auth/callback";
    public string FrontendFailureUrl { get; init; } = "/sign-in";

    /// <summary>Sign-in page that shows the second-factor step after an OAuth sign-in that needs MFA.</summary>
    public string FrontendMfaUrl { get; init; } = "/sign-in?step=mfa";
}
