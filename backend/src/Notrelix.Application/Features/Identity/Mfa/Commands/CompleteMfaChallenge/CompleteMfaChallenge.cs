using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;

namespace Notrelix.Application.Features.Identity.Mfa.Commands.CompleteMfaChallenge;

public sealed record CompleteMfaChallengeCommand
    : ICommand<Result<AuthResult>>,
      ITransactionalRequest,
      IGlobalRequest,
      IAnonymousRequest
{
    public required string ChallengeToken { get; init; }
    public required string Code { get; init; }
}

public sealed class CompleteMfaChallengeCommandHandler
    : IRequestHandler<CompleteMfaChallengeCommand, Result<AuthResult>>
{
    private readonly IIdentityDbContext _context;
    private readonly IMfaChallengeStore _challengeStore;
    private readonly IMfaCodeVerifier _codeVerifier;
    private readonly IAuthSessionIssuer _sessionIssuer;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CompleteMfaChallengeCommandHandler> _logger;

    public CompleteMfaChallengeCommandHandler(
        IIdentityDbContext context,
        IMfaChallengeStore challengeStore,
        IMfaCodeVerifier codeVerifier,
        IAuthSessionIssuer sessionIssuer,
        IDateTimeProvider dateTimeProvider,
        ILogger<CompleteMfaChallengeCommandHandler> logger)
    {
        _context = context;
        _challengeStore = challengeStore;
        _codeVerifier = codeVerifier;
        _sessionIssuer = sessionIssuer;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<AuthResult>> Handle(
        CompleteMfaChallengeCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        var challenge = await _challengeStore.ConsumeAsync(request.ChallengeToken, cancellationToken);
        if (challenge is null)
        {
            return Result<AuthResult>.Failure(new ApplicationError(
                "identity.mfa.challenge-invalid",
                "MFA challenge is invalid or expired.",
                ApplicationErrorType.Validation));
        }

        if (challenge.ExpiresAt < now)
        {
            return Result<AuthResult>.Failure(new ApplicationError(
                "identity.mfa.challenge-invalid",
                "MFA challenge is invalid or expired.",
                ApplicationErrorType.Validation));
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == challenge.UserId, cancellationToken);

        if (user is null || user.Status != UserStatus.Active)
        {
            return Result<AuthResult>.Failure(new ApplicationError(
                "identity.mfa.account-inactive",
                "Account is not active.",
                ApplicationErrorType.Conflict));
        }

        var verified = await _codeVerifier.VerifyAsync(user.Id, request.Code, now, cancellationToken);

        if (!verified)
        {
            _logger.LogWarning("MFA challenge failed verification for {UserId} (purpose {Purpose})",
                user.Id, challenge.Purpose);
            return Result<AuthResult>.Failure(new ApplicationError(
                "identity.mfa.invalid-code",
                "Invalid MFA code.",
                ApplicationErrorType.Validation));
        }

        user.RecordLogin(now);
        var authResult = await _sessionIssuer.IssueAsync(user, now, cancellationToken);

        _logger.LogInformation("MFA challenge completed for {UserId} (purpose {Purpose})",
            user.Id, challenge.Purpose);

        return Result<AuthResult>.Success(authResult);
    }
}