using System.Text.Json;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;

namespace Notrelix.Application.Features.Identity.Auth.Commands.Logout;

public record LogoutCommand : ICommand<Result>, ITransactionalRequest, IGlobalRequest, IAnonymousRequest
{
    public required string RefreshToken { get; init; }
    public string? AccessToken { get; init; }
}

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IIdentityDbContext _context;
    private readonly IJwtBlacklistService _jwtBlacklist;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IIdentityDbContext context,
        IJwtBlacklistService jwtBlacklist,
        IDateTimeProvider dateTimeProvider,
        ILogger<LogoutCommandHandler> logger)
    {
        _context = context;
        _jwtBlacklist = jwtBlacklist;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    private static readonly TimeSpan SessionRevocationMarkerTtl = TimeSpan.FromHours(24);

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = RefreshTokenHash.Create(request.RefreshToken);
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.RefreshTokenHash.Hash == tokenHash.Hash, cancellationToken);

        if (session is not null)
        {
            var now = _dateTimeProvider.UtcNow;
            session.Revoke(now, SessionRevocationReasons.UserRequested);
            await _jwtBlacklist.RevokeSessionBeforeAsync(session.Id, now, SessionRevocationMarkerTtl);
        }

        if (!string.IsNullOrWhiteSpace(request.AccessToken))
        {
            var now = _dateTimeProvider.UtcNow;
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
