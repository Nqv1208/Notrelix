namespace Notrelix.API.Options;

public sealed class SecurityHeaderOptions
{
    public const string SectionName = "SecurityHeaders";

    public bool EnableCsp { get; set; } = true;
    public string ContentSecurityPolicy { get; set; } =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "form-action 'self'; " +
        "base-uri 'self';";
}
