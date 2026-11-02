namespace Notrelix.Infrastructure.Auth.Jwt;

/// <summary>Custom JWT claim names used by the bearer access token.</summary>
public static class JwtClaimNames
{
    /// <summary>Session id the access token was issued for. Enables per-session revocation of both refresh and access tokens.</summary>
    public const string SessionId = "sid";

    /// <summary>
    /// Issue instant in Unix milliseconds. The standard <c>iat</c> claim is whole seconds, too coarse to compare
    /// with the millisecond revocation watermark, so a token issued just after a revocation would be refused.
    /// </summary>
    public const string IssuedAtMilliseconds = "iat_ms";
}
