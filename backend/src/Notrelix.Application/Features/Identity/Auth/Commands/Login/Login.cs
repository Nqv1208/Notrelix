using Notrelix.Application.Common.CQRS.Scoping;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Abstractions;

namespace Notrelix.Application.Features.Identity.Auth.Commands.Login;

public record LoginCommand
    : ICommand<Result<AuthResult>>,
      IAnonymousRequest,
      IGlobalRequest,
      ITransactionalRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResult>>
{
    private readonly IIdentityDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IIdentityDbContext context,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IDateTimeProvider dateTimeProvider,
        ILogger<LoginCommandHandler> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<AuthResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            _logger.LogInformation("Login failed: user not found for email {NormalizedEmail}", normalizedEmail);
            return Result<AuthResult>.Failure("Invalid email or password");
        }

        if (user.Status == UserStatus.Inactive)
        {
            _logger.LogWarning("Login blocked: inactive account {UserId} ({NormalizedEmail})", user.Id, normalizedEmail);
            return Result<AuthResult>.Failure("Account has been deactivated");
        }

        if (user.Status == UserStatus.Suspended)
        {
            _logger.LogWarning("Login blocked: suspended account {UserId} ({NormalizedEmail})", user.Id, normalizedEmail);
            return Result<AuthResult>.Failure("Account has been suspended");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogInformation("Login failed: invalid password for {UserId} ({NormalizedEmail})", user.Id, normalizedEmail);
            return Result<AuthResult>.Failure("Invalid email or password");
        }

        var now = _dateTimeProvider.UtcNow;
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var tokenHash = RefreshTokenHash.Create(refreshToken);

        var session = UserSession.Create(user.Id, tokenHash, now.AddDays(30), now);
        _context.Sessions.Add(session);

        user.RecordLogin(now);

        _logger.LogInformation("Login succeeded for {UserId} ({NormalizedEmail})", user.Id, normalizedEmail);

        return Result<AuthResult>.Success(new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = _dateTimeProvider.UtcNow.AddHours(1).UtcDateTime,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email.Value,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl
            }
        });
    }
}
