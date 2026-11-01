using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Identity.Security;

namespace Notrelix.Application.Features.Identity.Sessions.Commands.RevokeOtherSessions;

/// <summary>
/// Revokes every active session of the current user except the session that
/// issued the request (identified by the sid claim). Fail-closed when the
/// current session cannot be bound. Needs a single-use step-up proof for the
/// RevokeOtherSessions purpose (owner decision 48); with no other active
/// session it is a no-op that consumes no proof and records nothing.
/// </summary>
public sealed record RevokeOtherSessionsCommand
    : ICommand<Result<RevokeOtherSessionsResult>>,
      IWriteRequest,
      IGlobalRequest,
      IAuthenticatedRequest
{
    /// <summary>Single-use step-up proof for the RevokeOtherSessions purpose.</summary>
    public required string StepUpToken { get; init; }
}

/// <summary>How many other sessions this call ended.</summary>
public sealed record RevokeOtherSessionsResult(int RevokedCount);

public sealed class RevokeOtherSessionsCommandHandler
    : IRequestHandler<RevokeOtherSessionsCommand, Result<RevokeOtherSessionsResult>>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IJwtBlacklistService _jwtBlacklist;
    private readonly ISecurityStepUpService _stepUpService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISecurityActivityRecorder _activity;
    private readonly ILogger<RevokeOtherSessionsCommandHandler> _logger;

    private static readonly TimeSpan SessionRevocationMarkerTtl = TimeSpan.FromHours(24);

    public RevokeOtherSessionsCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IJwtBlacklistService jwtBlacklist,
        ISecurityStepUpService stepUpService,
        IDateTimeProvider dateTimeProvider,
        ISecurityActivityRecorder activity,
        ILogger<RevokeOtherSessionsCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _jwtBlacklist = jwtBlacklist;
        _stepUpService = stepUpService;
        _dateTimeProvider = dateTimeProvider;
        _activity = activity;
        _logger = logger;
    }

    public async Task<Result<RevokeOtherSessionsResult>> Handle(
        RevokeOtherSessionsCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        if (_currentUser.SessionId is not { } currentSessionId)
        {
            return Result<RevokeOtherSessionsResult>.Failure(new ApplicationError(
                "identity.session.not-bound",
                "Current session is not bound to this request. Refresh and retry.",
                ApplicationErrorType.Conflict));
        }

        var now = _dateTimeProvider.UtcNow;

        var sessions = await _context.Sessions
            .Where(s =>
                s.UserId == userId &&
                s.Id != currentSessionId &&
                s.Status == SessionStatus.Active &&
                s.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        if (sessions.Count == 0)
        {
            return Result<RevokeOtherSessionsResult>.Success(new RevokeOtherSessionsResult(0));
        }

        var stepUp = await ConsumeStepUpAsync(userId, currentSessionId, request.StepUpToken, cancellationToken);
        if (!stepUp.Succeeded)
        {
            return Result<RevokeOtherSessionsResult>.Failure(stepUp.TypedErrors);
        }

        foreach (var session in sessions)
        {
            session.Revoke(now, SessionRevocationReasons.UserRevokedOtherSessions);
            await _jwtBlacklist.RevokeSessionBeforeAsync(session.Id, now, SessionRevocationMarkerTtl);
        }

        _activity.Record(userId, SecurityActivityType.OtherSessionsRevoked, now, currentSessionId);

        _logger.LogInformation("Revoked {Count} other sessions for user {UserId}", sessions.Count, userId);

        return Result<RevokeOtherSessionsResult>.Success(new RevokeOtherSessionsResult(sessions.Count));
    }

    private async Task<Result> ConsumeStepUpAsync(
        Guid userId, Guid sessionId, string stepUpToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(stepUpToken))
        {
            return Result.Failure(new ApplicationError(
                "identity.security.step-up-required",
                "Strong verification is required for this action.",
                ApplicationErrorType.PreconditionFailed));
        }

        return await _stepUpService.ConsumeAsync(
            stepUpToken, userId, sessionId, StepUpPurpose.RevokeOtherSessions, cancellationToken);
    }
}
