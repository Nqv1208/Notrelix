using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.Sessions;

namespace Notrelix.Application.Features.Identity.Auth.Commands.ChangePassword;

public record ChangePasswordCommand : ICommand<Result>, ITransactionalRequest, IGlobalRequest, IAuthenticatedRequest
{
    public required string CurrentPassword { get; init; }
    public required string NewPassword { get; init; }

    /// <summary>
    /// Optional single-use step-up proof (purpose ChangePassword). Required when
    /// the user has an active MFA method; ignored otherwise.
    /// </summary>
    public string? StepUpToken { get; init; }
}

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtBlacklistService _jwtBlacklist;
    private readonly IEmailService _emailService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISecurityStepUpService _stepUpService;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    private const string DummyPasswordHash = "$2b$12$l21rZMRnrPl/Lfm2kVzYOuxlKgQzbwMzEvK7cOBZI40eJ42/FIuh2";
    private static readonly TimeSpan RevocationWatermarkTtl = TimeSpan.FromHours(24);

    public ChangePasswordCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IPasswordHasher passwordHasher,
        IJwtBlacklistService jwtBlacklist,
        IEmailService emailService,
        IDateTimeProvider dateTimeProvider,
        ILogger<ChangePasswordCommandHandler> logger,
        ISecurityStepUpService stepUpService)
    {
        _context = context;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _jwtBlacklist = jwtBlacklist;
        _emailService = emailService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _stepUpService = stepUpService;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(new ApplicationError(
                "identity.auth.user-not-found",
                "User not found.",
                ApplicationErrorType.NotFound));
        }

        var currentPasswordValid = _passwordHasher.VerifyPassword(
            request.CurrentPassword,
            user.PasswordHash ?? DummyPasswordHash);

        if (!currentPasswordValid)
        {
            return Result.Failure(new ApplicationError(
                "identity.auth.invalid-current-password",
                "Current password is incorrect.",
                ApplicationErrorType.Validation));
        }

        var hasActiveMfa = await _context.UserMfaMethods
            .AnyAsync(m => m.UserId == user.Id && m.Status == MfaMethodStatus.Active, cancellationToken);

        if (hasActiveMfa)
        {
            var stepUp = await ConsumeStepUpAsync(user.Id, request.StepUpToken, cancellationToken);
            if (!stepUp.Succeeded)
            {
                return stepUp;
            }
        }

        var now = _dateTimeProvider.UtcNow;
        var hash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatePassword(hash, user.Id, now);

        var activeSessions = await _context.Sessions
            .Where(s => s.UserId == user.Id && s.Status == SessionStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.Revoke(now, SessionRevocationReasons.PasswordChanged);
        }

        await _jwtBlacklist.RevokeUserBeforeAsync(user.Id, now, RevocationWatermarkTtl);

        try
        {
            var html = EmailTemplateService.PasswordChanged(user.Name);
            await _emailService.SendAsync(user.Email.Value, "Password changed — Notrelix", html, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password changed notification to {UserId}", user.Id);
        }

        return Result.Success();
    }

    private async Task<Result> ConsumeStepUpAsync(Guid userId, string? stepUpToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(stepUpToken))
        {
            return Result.Failure(new ApplicationError(
                "identity.security.step-up-required",
                "Strong verification is required for this action.",
                ApplicationErrorType.PreconditionFailed));
        }

        if (_currentUser.SessionId is not { } sessionId)
        {
            return Result.Failure(new ApplicationError(
                "identity.security.step-up-required",
                "Strong verification is required for this action.",
                ApplicationErrorType.PreconditionFailed));
        }

        return await _stepUpService.ConsumeAsync(
            stepUpToken, userId, sessionId, StepUpPurpose.ChangePassword, cancellationToken);
    }
}
