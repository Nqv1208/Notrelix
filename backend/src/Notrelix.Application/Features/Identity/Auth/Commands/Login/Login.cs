using Notrelix.Application.Common.Requests.Scoping;
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
    private readonly IAuthSessionIssuer _sessionIssuer;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IIdentityDbContext context,
        IPasswordHasher passwordHasher,
        IAuthSessionIssuer sessionIssuer,
        IDateTimeProvider dateTimeProvider,
        ILogger<LoginCommandHandler> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _sessionIssuer = sessionIssuer;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<AuthResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        var passwordValid = _passwordHasher.VerifyPassword(request.Password, user?.PasswordHash ?? DummyPasswordHash);

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
        user.RecordLogin(now);

        _logger.LogInformation("Login succeeded for {UserId} ({NormalizedEmail})", user.Id, normalizedEmail);

        var authResult = await _sessionIssuer.IssueAsync(user, now, cancellationToken);
        return Result<AuthResult>.Success(authResult);
    }

    private const string DummyPasswordHash = "$2b$12$l21rZMRnrPl/Lfm2kVzYOuxlKgQzbwMzEvK7cOBZI40eJ42/FIuh2";
}
