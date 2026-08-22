using Microsoft.Extensions.Options;
using Notrelix.API.ErrorHandling;
using Notrelix.Infrastructure.Auth.Csrf;

namespace Notrelix.API.Middleware;

/// <summary>
/// Browser CSRF protection per ADR-005.
/// Validates the Double Submit pair (csrf_token cookie + X-CSRF-Token header)
/// on unsafe requests that rely on ambient browser credentials. Token issuance
/// happens only through the bootstrap endpoint, never implicitly here.
/// Feature-flagged via Security:Csrf:Enabled.
/// </summary>
public sealed class CsrfValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CsrfProtector _protector;
    private readonly ICsrfApplicabilityClassifier _classifier;
    private readonly CsrfOptions _options;

    public CsrfValidationMiddleware(
        RequestDelegate next,
        CsrfProtector protector,
        ICsrfApplicabilityClassifier classifier,
        IOptions<CsrfOptions> options)
    {
        _next = next;
        _protector = protector;
        _classifier = classifier;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        if (_classifier.IsBrowserCsrfApplicable(context.Request)
            && !_protector.Validate(context))
        {
            await ProblemDetailsWriter.WriteCsrfForbiddenAsync(context, context.RequestAborted);
            return;
        }

        await _next(context);
    }
}

public sealed class CsrfOptions
{
    public bool Enabled { get; init; }
}
