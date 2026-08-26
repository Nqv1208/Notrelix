using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;

namespace Notrelix.Application.Features.Identity.Auth.Commands.RefreshToken;

public record RefreshTokenCommand : ICommand<Result<AuthResult>>, IWriteRequest, IGlobalRequest, IAnonymousRequest
{
    public required string RefreshToken { get; init; }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResult>>
{
    private readonly IIdentityDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IClientMetadata _clientMetadata;

    public RefreshTokenCommandHandler(
        IIdentityDbContext context,
        IJwtService jwtService,
        IDateTimeProvider dateTimeProvider,
        IClientMetadata clientMetadata)
    {
        _context = context;
        _jwtService = jwtService;
        _dateTimeProvider = dateTimeProvider;
        _clientMetadata = clientMetadata;
    }

    public async Task<Result<AuthResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var tokenHash = RefreshTokenHash.Create(request.RefreshToken);

        var session = await _context.Sessions
            .FirstOrDefaultAsync(s =>
                s.RefreshTokenHash.Hash == tokenHash.Hash &&
                s.Status == SessionStatus.Active &&
                s.ExpiresAt > now,
                cancellationToken);

        if (session is null)
        {
            return Result<AuthResult>.Failure(InvalidRefreshTokenError);
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == session.UserId, cancellationToken);

        if (user is null)
        {
            return Result<AuthResult>.Failure(InvalidRefreshTokenError);
        }

        if (user.Status is not UserStatus.Active)
        {
            session.Revoke(now);
            return Result<AuthResult>.Failure(InvalidRefreshTokenError);
        }

        session.Revoke(now);

        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var newTokenHash = RefreshTokenHash.Create(newRefreshToken);

        var newSession = UserSession.Create(user.Id, newTokenHash, now.AddDays(30), now,
            _clientMetadata.IpAddress, _clientMetadata.UserAgent);
        _context.Sessions.Add(newSession);

        var accessToken = _jwtService.GenerateAccessToken(user, newSession.Id);

        return Result<AuthResult>.Success(new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = _dateTimeProvider.UtcNow.UtcDateTime.AddHours(1),
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

    private static readonly ApplicationError InvalidRefreshTokenError = new(
        "identity.auth.invalid-refresh-token",
        "Refresh token is invalid or expired",
        ApplicationErrorType.Authentication);
}
