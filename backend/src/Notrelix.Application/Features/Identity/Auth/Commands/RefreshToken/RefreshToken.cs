using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Auth.Services;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Domain.Identity.Security;

namespace Notrelix.Application.Features.Identity.Auth.Commands.RefreshToken;

/// <summary>
/// Rotates the refresh credential of one session. The session keeps its id for its whole life, so a
/// step-up proof and the "this device" mark survive a refresh. The credential that was just replaced
/// answers with the new one for <see cref="SessionLifetimePolicy.RotationGrace"/> (two tabs refreshing
/// together, a lost response retried); after that, presenting it again is treated as theft and ends the
/// session. A session ends 30 days after its last refresh and 90 days after it began.
/// </summary>
public record RefreshTokenCommand : ICommand<Result<AuthResult>>, IWriteRequest, IGlobalRequest, IAnonymousRequest
{
    public required string RefreshToken { get; init; }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResult>>
{
    private static readonly TimeSpan SessionRevocationMarkerTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan RacePoll = TimeSpan.FromMilliseconds(100);
    private const int RacePolls = 20;

    private readonly IIdentityDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IJwtBlacklistService _jwtBlacklist;
    private readonly IRefreshRotationLedger _ledger;
    private readonly ISecurityActivityRecorder _activity;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CredentialScope? _credentialScope;

    public RefreshTokenCommandHandler(
        IIdentityDbContext context,
        IJwtService jwtService,
        IJwtBlacklistService jwtBlacklist,
        IRefreshRotationLedger ledger,
        ISecurityActivityRecorder activity,
        IDateTimeProvider dateTimeProvider,
        CredentialScope? credentialScope = null)
    {
        _credentialScope = credentialScope;
        _context = context;
        _jwtService = jwtService;
        _jwtBlacklist = jwtBlacklist;
        _ledger = ledger;
        _activity = activity;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AuthResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var presentedHash = RefreshTokenHash.Create(request.RefreshToken).Hash;

        // The credential scope is bound to the owner of the session this refresh token names, whether the
        // token is the session's current one or the one its last rotation replaced.
        if (_credentialScope is not null)
            await _credentialScope.EnterByRefreshTokenAsync(presentedHash, cancellationToken);

        var session = await _context.Sessions
            .FirstOrDefaultAsync(s =>
                s.RefreshTokenHash.Hash == presentedHash ||
                s.PreviousRefreshTokenHash == presentedHash,
                cancellationToken);

        if (session is null)
        {
            return Result<AuthResult>.Failure(InvalidRefreshTokenError);
        }

        if (session.RefreshTokenHash.Hash != presentedHash)
        {
            return await HandleReplacedCredentialAsync(session, request.RefreshToken, presentedHash, now, cancellationToken);
        }

        if (session.Status != SessionStatus.Active || session.ExpiresAt <= now)
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

        // Two requests presenting one credential at the same moment: one rotates, the other is served
        // the new credential once that rotation is committed.
        if (!await _ledger.TryClaimAsync(presentedHash, cancellationToken))
        {
            return await ServeRacedRotationAsync(request.RefreshToken, presentedHash, cancellationToken);
        }

        var newRefreshToken = _jwtService.GenerateRefreshToken();
        session.Rotate(RefreshTokenHash.Create(newRefreshToken), now);
        await _ledger.RecordSuccessorAsync(request.RefreshToken, newRefreshToken, cancellationToken);

        return Result<AuthResult>.Success(BuildResult(user, session.Id, newRefreshToken));
    }

    /// <summary>The presented credential is the one the session's last rotation replaced.</summary>
    private async Task<Result<AuthResult>> HandleReplacedCredentialAsync(
        UserSession session, string presentedToken, string presentedHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // A session that already ended has nothing left to protect.
        if (session.Status != SessionStatus.Active)
        {
            return Result<AuthResult>.Failure(InvalidRefreshTokenError);
        }

        if (session.IsWithinRotationGrace(presentedHash, now))
        {
            return await ServeGraceAsync(
                session.UserId, session.Id, session.RefreshTokenHash.Hash, presentedToken, cancellationToken);
        }

        // Outside the window a replaced credential can only be a copy someone kept: the person and the
        // presenter cannot both be its only holder, so the session ends for both.
        session.Revoke(now, SessionRevocationReasons.RefreshTokenReuse);
        await _jwtBlacklist.RevokeSessionBeforeAsync(session.Id, now, SessionRevocationMarkerTtl);
        _activity.Record(session.UserId, SecurityActivityType.SessionRevoked, now, session.Id);

        return Result<AuthResult>.Failure(InvalidRefreshTokenError);
    }

    private async Task<Result<AuthResult>> ServeRacedRotationAsync(string presentedToken, string presentedHash, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < RacePolls; attempt++)
        {
            await Task.Delay(RacePoll, cancellationToken);

            // Not tracked: the session read above is cached by the context and would not show the commit.
            var rotated = await _context.Sessions
                .AsNoTracking()
                .Where(s => s.PreviousRefreshTokenHash == presentedHash && s.Status == SessionStatus.Active)
                .Select(s => new { s.Id, s.UserId, CurrentHash = s.RefreshTokenHash.Hash })
                .FirstOrDefaultAsync(cancellationToken);

            if (rotated is not null)
            {
                return await ServeGraceAsync(rotated.UserId, rotated.Id, rotated.CurrentHash, presentedToken, cancellationToken);
            }
        }

        return Result<AuthResult>.Failure(InvalidRefreshTokenError);
    }

    /// <summary>Answers a retry with the credential that replaced the presented one, if it is still the current one.</summary>
    private async Task<Result<AuthResult>> ServeGraceAsync(
        Guid userId, Guid sessionId, string currentHash, string presentedToken, CancellationToken cancellationToken)
    {
        var successor = await _ledger.RecallSuccessorAsync(presentedToken, cancellationToken);
        if (successor is null || RefreshTokenHash.Create(successor).Hash != currentHash)
        {
            return Result<AuthResult>.Failure(InvalidRefreshTokenError);
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null || user.Status is not UserStatus.Active)
        {
            return Result<AuthResult>.Failure(InvalidRefreshTokenError);
        }

        return Result<AuthResult>.Success(BuildResult(user, sessionId, successor));
    }

    private AuthResult BuildResult(User user, Guid sessionId, string refreshToken) => new()
    {
        AccessToken = _jwtService.GenerateAccessToken(user, sessionId),
        RefreshToken = refreshToken,
        ExpiresAt = _dateTimeProvider.UtcNow.UtcDateTime.AddHours(1),
        User = new UserDto
        {
            Id = user.Id,
            Email = user.Email.Value,
            Name = user.Name,
            AvatarUrl = user.AvatarUrl,
            EmailConfirmed = user.EmailConfirmed
        }
    };

    private static readonly ApplicationError InvalidRefreshTokenError = new(
        "identity.auth.invalid-refresh-token",
        "Refresh token is invalid or expired",
        ApplicationErrorType.Authentication);
}
