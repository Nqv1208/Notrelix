namespace Notrelix.Application.Features.Identity.Sso;

/// <summary>
/// The normalized reasons a company sign-in ends on the sign-in page. They are the only values the browser is
/// sent as <c>?error=</c> after the callback; the person sees a plain sentence for each and nothing about how
/// the failure happened. An inactive user, a failed code exchange and an untrusted ID token all read as
/// <see cref="Failed"/>.
/// </summary>
public static class SsoErrorCodes
{
    public const string Unavailable = "identity.sso.unavailable";
    public const string StateInvalid = "identity.sso.state-invalid";
    public const string Denied = "identity.sso.denied";
    public const string Failed = "identity.sso.failed";
    public const string EmailUnverified = "identity.sso.email-unverified";
    public const string DomainMismatch = "identity.sso.domain-mismatch";
    public const string IdentityConflict = "identity.sso.identity-conflict";
    public const string NotProvisioned = "identity.sso.not-provisioned";

    /// <summary>A password sign-in refused because the person's Account requires single sign-on.</summary>
    public const string SsoRequired = "identity.auth.sso-required";

    private static readonly HashSet<string> Known =
    [
        Unavailable, StateInvalid, Denied, Failed, EmailUnverified, DomainMismatch, IdentityConflict, NotProvisioned,
    ];

    /// <summary>The code of the first error when it is one of these, otherwise <see cref="Failed"/>.</summary>
    public static string Normalize(string? code) =>
        code is not null && Known.Contains(code) ? code : Failed;
}
