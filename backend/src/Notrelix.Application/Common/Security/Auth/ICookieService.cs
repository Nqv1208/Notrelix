namespace Notrelix.Application.Common.Security.Auth
{
    public interface ICookieService
    {
        /// <summary>Cookie that carries an MFA challenge across the OAuth redirect (never in a URL).</summary>
        const string MfaChallengeCookieName = "mfaChallenge";

        /// <summary>Only the MFA endpoints receive the challenge cookie.</summary>
        const string MfaChallengeCookiePath = "/api/v1/auth/mfa";

        /// <summary>Upper bound of the cookie lifetime, whatever the challenge lifetime is.</summary>
        static readonly TimeSpan MfaChallengeCookieMaxLifetime = TimeSpan.FromMinutes(5);

        void SetTokenCookie(string accesToken, string refreshToken);
        void DeleteTokenCookie();

        /// <summary>
        /// Stores the MFA challenge token in a short-lived httpOnly, path-scoped, SameSite=Lax cookie
        /// (Secure when the request is HTTPS). The lifetime never exceeds the challenge itself.
        /// </summary>
        void SetMfaChallengeCookie(string challengeToken, DateTimeOffset challengeExpiresAt);

        string? GetMfaChallengeCookie();

        void DeleteMfaChallengeCookie();
    }
}
