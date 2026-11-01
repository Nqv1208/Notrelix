using Notrelix.Application.Features.Operators.Abstractions;

namespace Notrelix.Application.Features.Operators.Auth;

public sealed record OperatorProfileDto(Guid Id, string Email, string DisplayName);

public sealed record OperatorSessionDto(string AccessToken, DateTimeOffset ExpiresAt, OperatorProfileDto Operator);

/// <summary>
/// Signs a platform operator in with a password. The answer to a wrong email, a wrong password, a disabled
/// operator and a locked-out operator is the same, so the console reveals nothing about which operators exist.
/// Five wrong passwords lock the operator for fifteen minutes.
/// </summary>
public sealed class OperatorSignInService
{
    private const string Rejected = "The email or password is not correct.";

    // A real hash of a random string, made once: an unknown email costs one verification, like a known one.
    private static string? _dummyHash;

    private readonly IOperatorsDbContext _context;
    private readonly IPasswordHasher _hasher;
    private readonly IOperatorTokenIssuer _tokens;
    private readonly IDateTimeProvider _clock;

    public OperatorSignInService(
        IOperatorsDbContext context, IPasswordHasher hasher, IOperatorTokenIssuer tokens, IDateTimeProvider clock)
    {
        _context = context;
        _hasher = hasher;
        _tokens = tokens;
        _clock = clock;
        _dummyHash ??= hasher.HashPassword(Guid.NewGuid().ToString("N"));
    }

    public async Task<OperatorSessionDto> SignInAsync(string? email, string? password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password) || email.Length > 320 || password.Length > 256)
            throw new UnauthorizedException(Rejected);

        var normalized = email.Trim().ToLowerInvariant();
        var now = _clock.UtcNow;
        var operatorAccount = await _context.OperatorAccounts.FirstOrDefaultAsync(o => o.Email == normalized, cancellationToken);

        if (operatorAccount is null)
        {
            _hasher.VerifyPassword(password, _dummyHash!);
            throw new UnauthorizedException(Rejected);
        }

        if (!operatorAccount.CanAttemptSignIn(now))
        {
            _hasher.VerifyPassword(password, _dummyHash!);
            throw new UnauthorizedException(Rejected);
        }

        if (!_hasher.VerifyPassword(password, operatorAccount.PasswordHash))
        {
            operatorAccount.RecordFailedSignIn(now);
            await _context.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException(Rejected);
        }

        operatorAccount.RecordSignedIn(now);
        await _context.SaveChangesAsync(cancellationToken);

        var token = _tokens.Issue(operatorAccount, now);
        return new OperatorSessionDto(
            token.Value,
            token.ExpiresAt,
            new OperatorProfileDto(operatorAccount.Id, operatorAccount.Email, operatorAccount.DisplayName));
    }
}
