using Microsoft.Extensions.Options;
using Notrelix.Infrastructure.Auth.Csrf;

namespace Notrelix.API.Middleware;

/// <summary>
/// Fail-closed startup validation for browser CSRF enforcement (ADR-005).
/// Enforcement may be disabled only in the Development and Testing
/// environments; Staging, Production and any other environment must enforce.
/// When enforced, the cookie/header names must be non-empty and the credentialed
/// CORS origin allow-list must be explicit (no wildcard, absolute http/https
/// origins) so the double-submit pair can only be sent from known frontends.
/// </summary>
public sealed class CsrfOptionsValidator : IValidateOptions<CsrfOptions>
{
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public CsrfOptionsValidator(IHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    public ValidateOptionsResult Validate(string? name, CsrfOptions options)
    {
        if (!options.Enabled)
        {
            return _environment.IsDevelopment() || _environment.IsEnvironment("Testing")
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(
                    "Security:Csrf:Enabled=false is permitted only in Development/Testing; " +
                    $"environment '{_environment.EnvironmentName}' must enforce CSRF (ADR-005).");
        }

        if (string.IsNullOrWhiteSpace(CsrfProtector.CookieName))
            return ValidateOptionsResult.Fail("CSRF cookie name must not be empty.");

        if (string.IsNullOrWhiteSpace(CsrfProtector.HeaderName))
            return ValidateOptionsResult.Fail("CSRF header name must not be empty.");

        var origins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length == 0)
            return ValidateOptionsResult.Fail(
                "Cors:AllowedOrigins must list at least one origin when CSRF enforcement is enabled.");

        foreach (var origin in origins)
        {
            if (origin == "*")
                return ValidateOptionsResult.Fail(
                    "Cors:AllowedOrigins must not contain '*' when CSRF enforcement is enabled.");

            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return ValidateOptionsResult.Fail(
                    $"Cors:AllowedOrigins entry '{origin}' must be an absolute http(s) origin.");
        }

        return ValidateOptionsResult.Success;
    }
}
