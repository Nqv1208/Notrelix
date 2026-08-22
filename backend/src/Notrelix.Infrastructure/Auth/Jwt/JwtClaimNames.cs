namespace Notrelix.Infrastructure.Auth.Jwt;

/// <summary>Custom JWT claim names used by the bearer access token.</summary>
public static class JwtClaimNames
{
    /// <summary>Session id the access token was issued for. Enables per-session revocation of both refresh and access tokens.</summary>
    public const string SessionId = "sid";
}
