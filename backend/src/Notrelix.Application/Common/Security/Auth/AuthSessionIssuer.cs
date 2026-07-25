using Notrelix.Application.Features.Identity.Abstractions;

namespace Notrelix.Application.Common.Security.Auth;

public sealed class AuthSessionIssuer : IAuthSessionIssuer
{
    private readonly IJwtService _jwtService;
    private readonly IIdentityDbContext _identityContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuthSessionIssuer(
        IJwtService jwtService,
        IIdentityDbContext identityContext,
        IDateTimeProvider dateTimeProvider)
    {
        _jwtService = jwtService;
        _identityContext = identityContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public Task<AuthResult> IssueAsync(User user, DateTimeOffset now, CancellationToken ct)
    {
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var tokenHash = RefreshTokenHash.Create(refreshToken);

        var session = UserSession.Create(user.Id, tokenHash, now.AddDays(30), now);
        _identityContext.Sessions.Add(session);

        return Task.FromResult(new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = _dateTimeProvider.UtcNow.AddHours(1).UtcDateTime,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email.Value,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                EmailConfirmed = user.EmailConfirmed
            }
        });
    }
}
