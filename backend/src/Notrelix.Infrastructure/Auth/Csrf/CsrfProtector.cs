namespace Notrelix.Infrastructure.Auth.Csrf;

/// <summary>
/// Double Submit Cookie CSRF protector per ADR-005.
/// The token reaches the client through the bootstrap response body; the
/// csrf_token cookie is HttpOnly and only participates in the fixed-time
/// equality comparison against the X-CSRF-Token header on unsafe requests.
/// </summary>
public sealed class CsrfProtector
{
    public const string CookieName = "csrf_token";
    public const string HeaderName = "X-CSRF-Token";
    private const int TokenLength = 32;

    private readonly IHostEnvironment _environment;

    public CsrfProtector(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenLength);
        return Convert.ToBase64String(bytes);
    }

    public void SetCookie(HttpContext context, string token)
    {
        var isProduction = _environment.IsProduction();

        var options = new CookieOptions
        {
            // The client receives the token from the bootstrap response body,
            // so the cookie never needs to be JavaScript-readable (ADR-005).
            HttpOnly = true,
            // SameSite=None lets the browser attach the cookie to legitimate
            // cross-origin mutations from the supported frontend topology;
            // it requires Secure and is therefore production-only, mirroring
            // the auth cookie policy.
            Secure = isProduction,
            SameSite = isProduction ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.FromHours(1),
        };

        context.Response.Cookies.Append(CookieName, token, options);
    }

    public bool Validate(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out var cookieToken))
            return false;

        if (!context.Request.Headers.TryGetValue(HeaderName, out var headerToken))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(cookieToken),
            Encoding.UTF8.GetBytes(headerToken.ToString()));
    }
}
