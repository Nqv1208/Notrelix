using System.Text.Json;
using Notrelix.Infrastructure.Data.Abstractions;
using Notrelix.Infrastructure.Data.Audit;

namespace Notrelix.API.Middleware;

/// <summary>
/// Logs security-relevant events: auth failures, rate limits, CSRF rejects.
/// Writes to SecurityEvent table. Persisted by the request data session's
/// SaveChangesAsync when the pipeline opens a transactional session.
/// </summary>
public sealed class SecurityAuditMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly HashSet<string> SecuritySensitivePaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh-token",
        "/api/auth/forgot-password",
        "/api/auth/reset-password",
        "/api/auth/complete-oauth",
    };

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
            var auditContext = context.RequestServices.GetRequiredService<IAuditDbContext>();

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
                metadataJson: CreateMetadata(statusCode, context.Request.Path),
                occurredAt: DateTimeOffset.UtcNow);

            auditContext.EnterpriseSecurityEvents.Add(securityEvent);
        }
        catch
        {
            // Security audit must never crash the request pipeline
        }
    }

    private static bool IsSecurityRelevant(int statusCode, PathString path) =>
        statusCode is 401 or 403 or 429
        || (statusCode >= 400 && SecuritySensitivePaths.Contains(path));

    private static string ResolveEventType(int statusCode, PathString path) => statusCode switch
    {
        401 when SecuritySensitivePaths.Contains(path) => "LoginFailed",
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

    private static JsonDocument CreateMetadata(int statusCode, PathString path)
    {
        var metadata = new
        {
            statusCode,
            path = path.Value,
            method = ""
        };
        return JsonSerializer.SerializeToDocument(metadata);
    }
}
