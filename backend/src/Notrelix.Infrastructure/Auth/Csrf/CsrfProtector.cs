namespace Notrelix.Infrastructure.Auth.Csrf;

/// <summary>
/// Double Submit Cookie CSRF protector.
/// Generates a random token, sets it as a cookie, and validates it on state-changing requests.
/// </summary>
public sealed class CsrfProtector
{
    private const string CookieName = "csrf_token";
    private const string HeaderName = "X-CSRF-Token";
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
        var options = new CookieOptions
        {
            HttpOnly = false, // JavaScript must read this for Double Submit
            Secure = _environment.IsProduction(),
            SameSite = SameSiteMode.Strict,
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

    public bool IsStateChangingMethod(string method) =>
        method is "POST" or "PUT" or "PATCH" or "DELETE";
}
