using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Mfa;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Domain.Identity.Mfa;

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
    private readonly IAuthSessionIssuer _sessionIssuer;
    private readonly IMfaChallengeStore _challengeStore;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IIdentityDbContext context,
        IPasswordHasher passwordHasher,
        IAuthSessionIssuer sessionIssuer,
        IMfaChallengeStore challengeStore,
        IDateTimeProvider dateTimeProvider,
        ILogger<LoginCommandHandler> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _sessionIssuer = sessionIssuer;
        _challengeStore = challengeStore;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<AuthResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        var passwordHash = user is { HasPasswordCredential: true } ? user.PasswordHash : DummyPasswordHash;
        var passwordValid = _passwordHasher.VerifyPassword(request.Password, passwordHash);

        if (user is null || !passwordValid)
        {
            _logger.LogInformation("Login failed: invalid credentials for {NormalizedEmail}", normalizedEmail);
            return Result<AuthResult>.Failure("Invalid email or password");
        }

        if (user.Status is not UserStatus.Active)
        {
            _logger.LogWarning("Login blocked: non-active account {UserId} (status {UserStatus})", user.Id, user.Status);
            return Result<AuthResult>.Failure("Invalid email or password");
        }

        var now = _dateTimeProvider.UtcNow;

        var mfaEnabled = await _context.UserMfaMethods
            .AnyAsync(m => m.UserId == user.Id && m.Status == MfaMethodStatus.Active, cancellationToken);

        if (mfaEnabled)
        {
            var (token, payload) = await MfaChallengeFactory.CreateAsync(
                _challengeStore, user.Id, MfaChallengePurpose.PasswordLogin, now, cancellationToken);

            _logger.LogInformation("Login requires MFA challenge for {UserId} ({NormalizedEmail})",
                user.Id, normalizedEmail);

            return Result<AuthResult>.Success(new AuthResult
            {
                MfaRequired = true,
                MfaChallengeToken = token,
                MfaMethod = nameof(MfaMethodType.AuthenticatorApp),
                MfaExpiresAt = payload.ExpiresAt.UtcDateTime
            });
        }

        user.RecordLogin(now);

        _logger.LogInformation("Login succeeded for {UserId} ({NormalizedEmail})", user.Id, normalizedEmail);

        var authResult = await _sessionIssuer.IssueAsync(user, now, cancellationToken);
        return Result<AuthResult>.Success(authResult);
    }

    private const string DummyPasswordHash = "$2b$12$l21rZMRnrPl/Lfm2kVzYOuxlKgQzbwMzEvK7cOBZI40eJ42/FIuh2";
}
