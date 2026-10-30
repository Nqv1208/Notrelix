using System.Text.Json;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;

namespace Notrelix.Application.Features.Identity.Auth.Commands.Logout;

public record LogoutCommand : ICommand<Result>, IWriteRequest, IGlobalRequest, IAuthenticatedRequest
{
    public required string RefreshToken { get; init; }
    public string? AccessToken { get; init; }
}

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IJwtBlacklistService _jwtBlacklist;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IJwtBlacklistService jwtBlacklist,
        IDateTimeProvider dateTimeProvider,
        ILogger<LogoutCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _jwtBlacklist = jwtBlacklist;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    private static readonly TimeSpan SessionRevocationMarkerTtl = TimeSpan.FromHours(24);

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var now = _dateTimeProvider.UtcNow;

        // This device only: the session the access credential names and the one the refresh
        // cookie holds, both the caller's own. Another person's cookie never ends their session.
        var currentSessionId = _currentUser.SessionId ?? Guid.Empty;
        var refreshHash = string.IsNullOrWhiteSpace(request.RefreshToken)
            ? string.Empty
            : RefreshTokenHash.Create(request.RefreshToken).Hash;

        var sessions = await _context.Sessions
            .Where(s =>
                s.UserId == userId &&
                s.Status == SessionStatus.Active &&
                (s.Id == currentSessionId || s.RefreshTokenHash.Hash == refreshHash))
            .ToListAsync(cancellationToken);

        var endedIds = new HashSet<Guid>();
        foreach (var session in sessions)
        {
            session.Revoke(now, SessionRevocationReasons.UserRequested);
            endedIds.Add(session.Id);
        }

        // A second sign-out finds nothing active and changes nothing; the marker is
        // still set so the access credential of this device stays refused.
        if (currentSessionId != Guid.Empty)
        {
            endedIds.Add(currentSessionId);
        }

        foreach (var sessionId in endedIds)
        {
            await _jwtBlacklist.RevokeSessionBeforeAsync(sessionId, now, SessionRevocationMarkerTtl);
        }

        if (!string.IsNullOrWhiteSpace(request.AccessToken))
        {
            await BlacklistAccessTokenAsync(request.AccessToken, now);
        }

        return Result.Success();
    }

    private async Task BlacklistAccessTokenAsync(string token, DateTimeOffset now)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return;

            var payload = parts[1];
            var remainder = payload.Length % 4;
            var padded = remainder switch
            {
                2 => payload + "==",
                3 => payload + "=",
                _ => payload
            };

            var json = Convert.FromBase64String(padded);
            var claims = JsonSerializer.Deserialize<JsonElement>(json);

            var jti = claims.GetProperty("jti").GetString();
            var exp = claims.GetProperty("exp").GetInt64();

            if (jti is null) return;

            var expTime = DateTimeOffset.FromUnixTimeSeconds(exp);
            if (expTime <= now) return;

            var remaining = expTime - now;
            await _jwtBlacklist.BlacklistAsync(jti, remaining);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to blacklist access token during logout");
        }
    }
}
