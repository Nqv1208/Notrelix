using System.Text.Json;
using Notrelix.Infrastructure.Data.Audit;

namespace Notrelix.API.Middleware;

/// <summary>
/// Logs security-relevant events: auth failures, rate limits, CSRF rejects.
/// Writes to the SecurityEvent table through <see cref="ISecurityAuditSink"/>: the response status is
/// only known after the use case returned, when the request's data session has already committed, so
/// the event is persisted by its own short transaction instead of the (closed) request session.
/// </summary>
public sealed class SecurityAuditMiddleware
{
    private static readonly TimeSpan PersistTimeout = TimeSpan.FromSeconds(5);

    private readonly RequestDelegate _next;

    public SecurityAuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        var statusCode = context.Response.StatusCode;

        if (!IsSecurityRelevant(statusCode, context.Request.Path))
            return;

        try
        {
            var sink = context.RequestServices.GetRequiredService<ISecurityAuditSink>();

            var eventType = ResolveEventType(statusCode, context.Request.Path);
            var severity = ResolveSeverity(statusCode);
            var outcome = statusCode switch
            {
                429 => "RateLimited",
                401 => "AuthenticationFailed",
                403 => "AuthorizationFailed",
                _ => "Observed"
            };

            var securityEvent = new SecurityEvent(
                workspaceId: null,
                userId: TryGetUserId(context),
                eventType: eventType,
                severity: severity,
                outcome: outcome,
                riskScore: ResolveRiskScore(statusCode),
                ipAddress: context.Connection.RemoteIpAddress?.ToString(),
                userAgent: context.Request.Headers.UserAgent.ToString(),
                deviceId: null,
                sessionId: null,
                resourceType: context.Request.Method,
                resourceId: null,
                correlationId: context.TraceIdentifier,
                metadataJson: CreateMetadata(statusCode, context.Request.Path, context.Request.Method),
                occurredAt: DateTimeOffset.UtcNow);

            // Bounded wait: auditing must neither hang nor crash the request. The sink logs its own failures.
            using var timeout = new CancellationTokenSource(PersistTimeout);
            await sink.WriteAsync(securityEvent, timeout.Token);
        }
        catch (Exception ex)
        {
            // Security audit must never crash the request pipeline
            context.RequestServices.GetService<ILogger<SecurityAuditMiddleware>>()
                ?.LogError(ex, "Security audit event could not be recorded");
        }
    }

    private static bool IsSecurityRelevant(int statusCode, PathString path) =>
        statusCode is 401 or 403 or 429
        || (statusCode >= 400 && SecuritySensitiveRoutes.IsSensitive(path));

    private static string ResolveEventType(int statusCode, PathString path) => statusCode switch
    {
        401 when SecuritySensitiveRoutes.IsSignInRoute(path) => "LoginFailed",
        401 => "UnauthorizedAccess",
        403 => "ForbiddenAccess",
        429 => "RateLimitExceeded",
        _ => "SecurityObservation"
    };

    private static string ResolveSeverity(int statusCode) => statusCode switch
    {
        401 => "Warning",
        403 => "Warning",
        429 => "Info",
        _ => "Info"
    };

    private static int ResolveRiskScore(int statusCode) => statusCode switch
    {
        401 => 60,
        403 => 40,
        429 => 30,
        _ => 10
    };

    private static Guid? TryGetUserId(HttpContext context)
    {
        var sub = context.User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }

    private static JsonDocument CreateMetadata(int statusCode, PathString path, string method)
    {
        var metadata = new
        {
            statusCode,
            path = path.Value,
            method = method
        };
        return JsonSerializer.SerializeToDocument(metadata);
    }
}

/// <summary>
/// The credential and security-sensitive routes whose failed (4xx) answers are audited even when
/// the status is not 401/403/429. Matches the versioned public route (<c>/api/v1/auth/...</c>);
/// the unversioned <c>/api/auth/...</c> spelling this list originally used never existed, so
/// real login failures were never classified.
/// </summary>
public static class SecuritySensitiveRoutes
{
    // Anonymous steps that establish a session: a 401 here is a failed sign-in.
    private static readonly HashSet<string> SignInRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "login",
        "register",
        "refresh",
        "forgot-password",
        "reset-password",
        "mfa/verify",
    };

    // Authenticated credential re-verification and credential changes.
    private static readonly HashSet<string> CredentialRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "change-password",
        "mfa/enrollment/verify",
        "mfa/disable",
        "mfa/recovery-codes/regenerate",
        "security/step-up/complete-mfa",
        "security/step-up/complete-password",
    };

    public static bool IsSensitive(PathString path) =>
        TryGetAuthRoute(path, out var route)
        && (SignInRoutes.Contains(route) || CredentialRoutes.Contains(route) || IsAnyOAuthCallback(route));

    public static bool IsSignInRoute(PathString path) =>
        TryGetAuthRoute(path, out var route) && (SignInRoutes.Contains(route) || IsSignInOAuthCallback(route));

    // "oauth/{provider}/callback" signs a user in; the link and step-up callbacks are credential
    // operations of an already signed-in user.
    private static bool IsSignInOAuthCallback(string route)
    {
        var segments = route.Split('/');
        return segments is ["oauth", _, var last] && last.Equals("callback", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAnyOAuthCallback(string route) =>
        route.StartsWith("oauth/", StringComparison.OrdinalIgnoreCase)
        && route.EndsWith("/callback", StringComparison.OrdinalIgnoreCase);

    // "/api/v1/auth/login" -> "login". Accepts any "/api/v{n}" version segment and a trailing slash.
    private static bool TryGetAuthRoute(PathString path, out string route)
    {
        route = string.Empty;
        var value = path.Value;
        if (string.IsNullOrEmpty(value)) return false;

        var span = value.AsSpan().TrimEnd('/');
        const string api = "/api/v";
        if (!span.StartsWith(api, StringComparison.OrdinalIgnoreCase)) return false;

        span = span[api.Length..];
        var slash = span.IndexOf('/');
        if (slash <= 0 || !span[..slash].ToString().All(char.IsAsciiDigit)) return false;

        span = span[slash..];
        const string auth = "/auth/";
        if (!span.StartsWith(auth, StringComparison.OrdinalIgnoreCase)) return false;

        route = span[auth.Length..].ToString();
        return route.Length > 0;
    }
}
