using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Common.Interfaces;
using TodoApp.Application.Common.Models;

namespace TodoApp.Application.Features.Auth.Commands.Logout;

public record LogoutCommand : IRequest<Result>
{
    public required string RefreshToken { get; init; }
    public string? AccessToken { get; init; }
}

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtBlacklistService _jwtBlacklist;

    public LogoutCommandHandler(IApplicationDbContext context, IJwtBlacklistService jwtBlacklist)
    {
        _context = context;
        _jwtBlacklist = jwtBlacklist;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.RefreshToken == request.RefreshToken, cancellationToken);

        if (session is not null)
        {
            session.Revoke();
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.AccessToken))
        {
            BlacklistAccessToken(request.AccessToken);
        }

        return Result.Success();
    }

    private async void BlacklistAccessToken(string token)
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

            var expTime = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
            if (expTime <= DateTime.UtcNow) return;

            var remaining = expTime - DateTime.UtcNow;
            await _jwtBlacklist.BlacklistAsync(jti, remaining);
        }
        catch
        {
            // Invalid token format — silently ignore
        }
    }
}
