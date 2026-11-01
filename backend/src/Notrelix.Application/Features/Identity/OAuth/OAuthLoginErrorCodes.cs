namespace Notrelix.Application.Features.Identity.OAuth;

/// <summary>
/// The normalized reasons a provider sign-in ends on the sign-in page. They are the only values the
/// browser is ever sent as <c>?error=</c>; the person sees a plain sentence for each and nothing about
/// how the failure happened. An inactive account, a failed code exchange and an untrusted ID token all
/// read as <see cref="Failed"/>.
/// </summary>
public static class OAuthLoginErrorCodes
{
    public const string Failed = "identity.oauth.failed";
    public const string ProviderUnavailable = "identity.oauth.provider-unavailable";
    /// <summary>The state is unknown, expired or already used.</summary>
    public const string StateInvalid = "identity.oauth.state-invalid";
    /// <summary>The person stopped at the provider.</summary>
    public const string Denied = "identity.oauth.denied";
    public const string EmailUnverified = "identity.oauth.email-unverified";
    /// <summary>The verified email belongs to an account that is not linked to this provider identity.</summary>
    public const string EmailInUse = "identity.oauth.email-in-use";
    /// <summary>The account has a second factor: it signs in with its password, then the code.</summary>
    public const string MfaRequired = "identity.oauth.mfa-required";

    private static readonly HashSet<string> Known =
    [
        Failed, ProviderUnavailable, StateInvalid, Denied, EmailUnverified, EmailInUse, MfaRequired,
    ];

    /// <summary>The code of the first error when it is one of these, otherwise <see cref="Failed"/>.</summary>
    public static string Normalize(string? code) =>
        code is not null && Known.Contains(code) ? code : Failed;
}
