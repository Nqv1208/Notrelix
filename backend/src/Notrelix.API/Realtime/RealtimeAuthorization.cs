using System.Security.Claims;

using MediatR;

using Notrelix.Application.Common.Realtime.Gateway;
using Notrelix.Application.Common.Security.Auth;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.AuthorizeBoardRealtimeSubscription;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.AuthorizeWorkspaceRealtimeSubscription;
using Notrelix.Infrastructure.Auth.Jwt;

namespace Notrelix.API.Realtime;

/// <summary>The authenticated identity of one gateway connection, captured at upgrade time.</summary>
public sealed record RealtimePrincipal(
    Guid UserId,
    string Email,
    string Name,
    string? TokenId,
    Guid? SessionId,
    DateTimeOffset? IssuedAt,
    DateTimeOffset? ExpiresAt,
    Guid CorrelationId)
{
    public static RealtimePrincipal? From(ClaimsPrincipal user, Guid correlationId)
    {
        var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId) || userId == Guid.Empty)
        {
            return null;
        }

        return new RealtimePrincipal(
            userId,
            user.FindFirstValue("email") ?? user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            user.FindFirstValue("name") ?? user.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            user.FindFirstValue("jti"),
            Guid.TryParse(user.FindFirstValue(JwtClaimNames.SessionId), out var sid) ? sid : null,
            ReadUnix(user, "iat"),
            ReadUnix(user, "exp"),
            correlationId);
    }

    private static DateTimeOffset? ReadUnix(ClaimsPrincipal user, string claim)
        => long.TryParse(user.FindFirstValue(claim), out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
}

public enum RealtimeAuthorizationOutcome
{
    Granted,
    /// <summary>Forbidden or not found; deliberately indistinguishable so existence does not leak.</summary>
    Denied,
    Unauthenticated,
    /// <summary>The authorization system failed; callers must fail closed.</summary>
    Unavailable,
}

public sealed record RealtimeAuthorizationResult(RealtimeAuthorizationOutcome Outcome, RealtimeSubscriptionGrant? Grant = null)
{
    public static RealtimeAuthorizationResult Granted(RealtimeSubscriptionGrant grant) => new(RealtimeAuthorizationOutcome.Granted, grant);
    public static readonly RealtimeAuthorizationResult Denied = new(RealtimeAuthorizationOutcome.Denied);
    public static readonly RealtimeAuthorizationResult Unauthenticated = new(RealtimeAuthorizationOutcome.Unauthenticated);
    public static readonly RealtimeAuthorizationResult Unavailable = new(RealtimeAuthorizationOutcome.Unavailable);
}

/// <summary>Decides whether a connection principal may receive the stream of a scope.</summary>
public interface IRealtimeSubscriptionAuthorizer
{
    Task<RealtimeAuthorizationResult> AuthorizeAsync(
        RealtimePrincipal principal, RealtimeScope scope, CancellationToken cancellationToken);
}

/// <summary>
/// Authorizes a subscription by running the owning context's authorization query
/// through the Application pipeline in a fresh scope, so realtime access is decided
/// by exactly the execution-context, membership, permission and resource-scope gates
/// the matching REST read uses. Nothing is trusted from the client except the scope id.
/// </summary>
public sealed class MediatorRealtimeSubscriptionAuthorizer : IRealtimeSubscriptionAuthorizer
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<MediatorRealtimeSubscriptionAuthorizer> _logger;

    public MediatorRealtimeSubscriptionAuthorizer(
        IServiceScopeFactory scopes,
        ILogger<MediatorRealtimeSubscriptionAuthorizer> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    public async Task<RealtimeAuthorizationResult> AuthorizeAsync(
        RealtimePrincipal principal, RealtimeScope scope, CancellationToken cancellationToken)
    {
        try
        {
            await using var serviceScope = _scopes.CreateAsyncScope();
            var services = serviceScope.ServiceProvider;

            var execution = services.GetRequiredService<IExecutionContextAccessor>();
            execution.SetUser(principal.UserId, principal.Email, principal.Name);
            execution.SetCorrelation(principal.CorrelationId);
            services.GetRequiredService<ICurrentTenantContext>().SetUser(principal.UserId);

            var sender = services.GetRequiredService<ISender>();
            var result = scope.Kind switch
            {
                RealtimeScopeKind.Workspace => await sender.Send(
                    new AuthorizeWorkspaceRealtimeSubscriptionQuery(scope.Id), cancellationToken),
                RealtimeScopeKind.Board => await sender.Send(
                    new AuthorizeBoardRealtimeSubscriptionQuery(scope.Id), cancellationToken),
                _ => null,
            };

            return result is { Succeeded: true, Data: { } grant }
                ? RealtimeAuthorizationResult.Granted(grant)
                : RealtimeAuthorizationResult.Denied;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is ForbiddenException or NotFoundException)
        {
            return RealtimeAuthorizationResult.Denied;
        }
        catch (UnauthorizedException)
        {
            return RealtimeAuthorizationResult.Unauthenticated;
        }
        catch (Exception ex)
        {
            // Fail closed. Only the exception type is logged: messages may name resources.
            _logger.LogError(
                "Realtime subscription authorization failed for scope {ScopeKind} {ScopeId}: {ExceptionType}",
                scope.WireKind, scope.Id, ex.GetType().Name);
            return RealtimeAuthorizationResult.Unavailable;
        }
    }
}

/// <summary>Re-validates the connection credential against revocation state.</summary>
public interface IRealtimeCredentialValidator
{
    Task<bool> IsStillValidAsync(RealtimePrincipal principal, CancellationToken cancellationToken);
}

/// <summary>
/// Applies the same revocation rules the bearer handler applies when a token is
/// validated (token blacklist, user and session revoked-before), so a logout,
/// password change or session revoke ends realtime within one reauthorization interval.
/// </summary>
public sealed class JwtRealtimeCredentialValidator : IRealtimeCredentialValidator
{
    private readonly IJwtBlacklistService _blacklist;
    private readonly TimeProvider _clock;

    public JwtRealtimeCredentialValidator(IJwtBlacklistService blacklist, TimeProvider clock)
    {
        _blacklist = blacklist;
        _clock = clock;
    }

    public async Task<bool> IsStillValidAsync(RealtimePrincipal principal, CancellationToken cancellationToken)
    {
        if (principal.ExpiresAt is { } expiresAt && expiresAt <= _clock.GetUtcNow())
        {
            return false;
        }

        if (principal.TokenId is { } jti && await _blacklist.IsBlacklistedAsync(jti))
        {
            return false;
        }

        var userRevokedBefore = await _blacklist.GetUserRevokedBeforeAsync(principal.UserId);
        if (AccessTokenRevocationEvaluator.ShouldReject(principal.IssuedAt, userRevokedBefore))
        {
            return false;
        }

        if (principal.SessionId is { } sessionId)
        {
            var sessionRevokedBefore = await _blacklist.GetSessionRevokedBeforeAsync(sessionId);
            if (AccessTokenRevocationEvaluator.ShouldReject(principal.IssuedAt, sessionRevokedBefore))
            {
                return false;
            }
        }

        return true;
    }
}
