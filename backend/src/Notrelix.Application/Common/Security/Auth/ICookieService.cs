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

        /// <summary>Cookie that carries a provider step-up proof across the OAuth redirect (never in a URL).</summary>
        const string StepUpProofCookieName = "stepUpProof";

        /// <summary>Only the step-up endpoints receive the proof cookie.</summary>
        const string StepUpProofCookiePath = "/api/v1/auth/security/step-up";

        /// <summary>Cookie that ties a provider sign-in's state to the browser that started it (login CSRF defence).</summary>
        const string OAuthBindingCookieName = "oauthBinding";

        /// <summary>
        /// Only the provider sign-in endpoints receive the binding cookie. The provider segment of the callback
        /// address is matched case-insensitively by the route but cookie paths are case-sensitive, so the path stops at the OAuth root.
        /// </summary>
        const string OAuthBindingCookiePath = "/api/v1/auth/oauth";

        /// <summary>The binding lives as long as the sign-in state does.</summary>
        static readonly TimeSpan OAuthBindingCookieLifetime = TimeSpan.FromMinutes(10);

        void SetTokenCookie(string accesToken, string refreshToken);
        void DeleteTokenCookie();

        /// <summary>
        /// Stores the MFA challenge token in a short-lived httpOnly, path-scoped, SameSite=Lax cookie
        /// (Secure when the request is HTTPS). The lifetime never exceeds the challenge itself.
        /// </summary>
        void SetMfaChallengeCookie(string challengeToken, DateTimeOffset challengeExpiresAt);

        string? GetMfaChallengeCookie();

        void DeleteMfaChallengeCookie();

        /// <summary>
        /// Holds the proof(s) a provider re-authentication granted, in a httpOnly, path-scoped, SameSite=Lax
        /// cookie that lives no longer than the proof itself. One claim reads it and deletes it.
        /// </summary>
        void SetStepUpProofCookie(string value, DateTimeOffset proofExpiresAt);

        string? GetStepUpProofCookie();

        void DeleteStepUpProofCookie();

        /// <summary>httpOnly, path-scoped, SameSite=Lax (the callback is a top-level GET from the provider), Secure when the request is HTTPS.</summary>
        void SetOAuthBindingCookie(string binding);

        string? GetOAuthBindingCookie();

        void DeleteOAuthBindingCookie();
    }
}
