using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Auth.Services;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.Commands.CompleteMfaChallenge;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Domain.Identity.Mfa;

namespace Notrelix.Application.Features.Identity.Mfa.Commands.RequiredEnrollment;

/// <summary>
/// Starts the authenticator the person's Account requires, during sign-in and before any session exists.
/// The enrollment token from the sign-in answer is the only credential: it names the person, it is accepted
/// by nothing else, and the secret it yields is useless without finishing through the same token.
/// </summary>
public sealed record StartRequiredMfaEnrollmentCommand
    : ICommand<Result<MfaEnrollmentStartResult>>,
      IWriteRequest,
      IGlobalRequest,
      IAnonymousRequest
{
    public required string EnrollmentToken { get; init; }
}

public sealed class StartRequiredMfaEnrollmentCommandHandler
    : IRequestHandler<StartRequiredMfaEnrollmentCommand, Result<MfaEnrollmentStartResult>>
{
    private readonly IIdentityDbContext _context;
    private readonly IMfaChallengeStore _challengeStore;
    private readonly IMfaTotpService _totp;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CredentialScope? _credentialScope;

    public StartRequiredMfaEnrollmentCommandHandler(
        IIdentityDbContext context,
        IMfaChallengeStore challengeStore,
        IMfaTotpService totp,
        IDateTimeProvider dateTimeProvider,
        CredentialScope? credentialScope = null)
    {
        _context = context;
        _challengeStore = challengeStore;
        _totp = totp;
        _dateTimeProvider = dateTimeProvider;
        _credentialScope = credentialScope;
    }

    public async Task<Result<MfaEnrollmentStartResult>> Handle(
        StartRequiredMfaEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        var challenge = await _challengeStore.PeekAsync(request.EnrollmentToken, cancellationToken);
        if (challenge is null
            || challenge.ExpiresAt < now
            || challenge.Purpose != MfaChallengePurpose.RequiredEnrollment)
        {
            return Invalid();
        }

        if (_credentialScope is not null)
            await _credentialScope.EnterForServerBoundUserAsync(challenge.UserId, cancellationToken);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == challenge.UserId, cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
            return Invalid();

        var hasActiveMethod = await _context.UserMfaMethods
            .AnyAsync(m => m.UserId == user.Id && m.Status == MfaMethodStatus.Active, cancellationToken);
        if (hasActiveMethod)
            return Invalid();

        var abandoned = await _context.UserMfaMethods
            .Where(m => m.UserId == user.Id && m.Status == MfaMethodStatus.PendingVerification)
            .ToListAsync(cancellationToken);
        foreach (var pending in abandoned)
        {
            pending.Disable(now);
        }

        var secret = _totp.GenerateSecretKey();
        var method = UserMfaMethod.Create(
            user.Id,
            MfaMethodType.AuthenticatorApp,
            now,
            SecretRef.Create(_totp.ProtectSecret(secret)));
        _context.UserMfaMethods.Add(method);

        return Result<MfaEnrollmentStartResult>.Success(new MfaEnrollmentStartResult(
            method.Id,
            secret,
            _totp.BuildOtpAuthUri(secret, user.Email.Value, MfaPolicy.TotpIssuer)));
    }

    private static Result<MfaEnrollmentStartResult> Invalid() =>
        Result<MfaEnrollmentStartResult>.Failure(CompleteMfaChallengeErrors.ChallengeInvalid);
}
