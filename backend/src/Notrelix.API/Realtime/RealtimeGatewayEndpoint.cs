using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Notrelix.API.Endpoints;

using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.API.Realtime;

/// <summary>
/// Decides whether a WebSocket upgrade is allowed from an Origin. Browsers always
/// send Origin on WebSocket upgrades and cannot add an anti-CSRF header, so an
/// exact allow-list is the cross-site WebSocket hijacking defense. A request with
/// no Origin is not a browser request and still has to authenticate.
/// </summary>
public sealed class RealtimeOriginPolicy
{
    private readonly HashSet<string> _allowed;
    private readonly bool _allowAny;

    public RealtimeOriginPolicy(IEnumerable<string> allowedOrigins, bool allowWildcard)
    {
        var origins = allowedOrigins.Where(o => !string.IsNullOrWhiteSpace(o)).Select(Normalize).ToList();
        _allowAny = allowWildcard && origins.Contains("*");
        _allowed = new HashSet<string>(origins.Where(o => o != "*"), StringComparer.OrdinalIgnoreCase);
    }

    public bool IsAllowed(string? origin)
    {
        if (string.IsNullOrEmpty(origin))
        {
            return true;
        }

        return _allowAny || _allowed.Contains(Normalize(origin));
    }

    private static string Normalize(string origin) => origin.Trim().TrimEnd('/');
}

/// <summary>Bounds concurrent gateway connections per user on this instance.</summary>
public sealed class RealtimeConnectionLimiter
{
    private readonly ConcurrentDictionary<Guid, int> _counts = new();

    public IDisposable? TryAcquire(Guid userId, int max)
    {
        var count = _counts.AddOrUpdate(userId, 1, (_, current) => current + 1);
        if (count > max)
        {
            Release(userId);
            return null;
        }

        return new Lease(this, userId);
    }

    public int Count(Guid userId) => _counts.TryGetValue(userId, out var value) ? value : 0;

    private void Release(Guid userId)
    {
        while (true)
        {
            if (!_counts.TryGetValue(userId, out var current))
            {
                return;
            }

            if (current <= 1)
            {
                if (_counts.TryRemove(new KeyValuePair<Guid, int>(userId, current)))
                {
                    return;
                }
            }
            else if (_counts.TryUpdate(userId, current - 1, current))
            {
                return;
            }
        }
    }

    private sealed class Lease(RealtimeConnectionLimiter owner, Guid userId) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                owner.Release(userId);
            }
        }
    }
}

/// <summary>Thin HTTP edge of the realtime gateway: origin, authentication, limits, upgrade.</summary>
public static class RealtimeGatewayEndpoint
{
    public static IEndpointRouteBuilder MapRealtimeGateway(this IEndpointRouteBuilder app)
    {
        var options = app.ServiceProvider.GetRequiredService<IOptions<RealtimeGatewayOptions>>().Value;
        if (!options.Enabled)
        {
            return app;
        }

        app.MapAuthenticatedGet(options.Path, HandleAsync)
            .WithName("RealtimeGateway")
            .WithSummary("Realtime WebSocket gateway (notify-only; see RealtimeProtocol)")
            .ExcludeFromDescription();
        return app;
    }

    private static async Task<IResult> HandleAsync(
        HttpContext context,
        IOptions<RealtimeGatewayOptions> optionsAccessor,
        RealtimeOriginPolicy originPolicy,
        RealtimeConnectionLimiter limiter,
        IRealtimeSubscriptionAuthorizer authorizer,
        IRealtimeCredentialValidator credentials,
        IRealtimeNotificationSource source,
        ICurrentCredentialContext credentialContext,
        IExecutionContextReader executionContext,
        ILoggerFactory loggerFactory)
    {
        var options = optionsAccessor.Value;

        if (!originPolicy.IsAllowed(context.Request.Headers.Origin.ToString()))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            return Results.StatusCode(StatusCodes.Status426UpgradeRequired);
        }

        // Automation API tokens are bound to one workspace and are not realtime credentials.
        if (credentialContext.Kind == CredentialKind.ApiToken)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var correlationId = executionContext.CorrelationId;
        var principal = RealtimePrincipal.From(context.User, correlationId);
        if (principal is null)
        {
            return Results.StatusCode(StatusCodes.Status401Unauthorized);
        }

        using var lease = limiter.TryAcquire(principal.UserId, options.MaxConnectionsPerUser);
        if (lease is null)
        {
            return Results.StatusCode(StatusCodes.Status429TooManyRequests);
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        var connection = new RealtimeConnection(
            socket, principal, options, authorizer, credentials, source,
            loggerFactory.CreateLogger<RealtimeConnection>());
        await connection.RunAsync(context.RequestAborted).ConfigureAwait(false);

        // The response is complete once the socket closes.
        return Results.Empty;
    }
}
