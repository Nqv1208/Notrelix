using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Identity.Security.Services;
using Notrelix.Domain.Identity.Security;

namespace Notrelix.Application.Features.Identity.Auth.Commands.SetFirstPassword;

/// <summary>
/// Gives a provider-only person a password. <paramref name="StepUpToken"/> is the single-use proof of purpose
/// ChangePassword (their linked provider, or a code while two-factor is on): a session alone may not do this.
/// </summary>
public sealed record SetFirstPasswordCommand(string NewPassword, string StepUpToken = "")
    : ICommand<Result>, IWriteRequest, IGlobalRequest, IAuthenticatedRequest;

public sealed class SetFirstPasswordCommandValidator : AbstractValidator<SetFirstPasswordCommand>
{
    public SetFirstPasswordCommandValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(PasswordPolicy.MinimumLength)
                .WithMessage($"Password must be at least {PasswordPolicy.MinimumLength} characters");
    }
}

public sealed class SetFirstPasswordCommandHandler : IRequestHandler<SetFirstPasswordCommand, Result>
{
    private static readonly TimeSpan SessionMarkerTtl = TimeSpan.FromHours(24);

    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtBlacklistService _jwtBlacklist;
    private readonly IEmailService _emailService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISecurityStepUpService _stepUpService;
    private readonly ISecurityActivityRecorder _activity;
    private readonly ILogger<SetFirstPasswordCommandHandler> _logger;

    public SetFirstPasswordCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IPasswordHasher passwordHasher,
        IJwtBlacklistService jwtBlacklist,
        IEmailService emailService,
        IDateTimeProvider dateTimeProvider,
        ISecurityStepUpService stepUpService,
        ISecurityActivityRecorder activity,
        ILogger<SetFirstPasswordCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _jwtBlacklist = jwtBlacklist;
        _emailService = emailService;
        _dateTimeProvider = dateTimeProvider;
        _stepUpService = stepUpService;
        _activity = activity;
        _logger = logger;
    }

    public async Task<Result> Handle(SetFirstPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(SessionErrors.SessionUserMissing);

        // A person who has a password changes it through the change flow, which asks for the current one.
        if (user.HasPasswordCredential)
        {
            return Result.Failure(new ApplicationError(
                "identity.auth.password-already-set",
                "A password is already set. Change it instead.",
                ApplicationErrorType.Conflict));
        }

        if (string.IsNullOrWhiteSpace(request.StepUpToken) || _currentUser.SessionId is not { } sessionId)
        {
            return Result.Failure(new ApplicationError(
                "identity.security.step-up-required",
                "Strong verification is required for this action.",
                ApplicationErrorType.PreconditionFailed));
        }

        var stepUp = await _stepUpService.ConsumeAsync(
            request.StepUpToken, user.Id, sessionId, StepUpPurpose.ChangePassword, cancellationToken);
        if (!stepUp.Succeeded)
            return stepUp;

        var now = _dateTimeProvider.UtcNow;
        user.UpdatePassword(_passwordHasher.HashPassword(request.NewPassword), user.Id, now);
        await PasswordChangeRecorder.RecordAsync(_context, user.Id, now, cancellationToken);
        _activity.Record(user.Id, SecurityActivityType.PasswordChanged, now, sessionId);

        // This device stays signed in (the person just proved themselves); every other one ends.
        var others = await _context.Sessions
            .Where(s => s.UserId == user.Id && s.Id != sessionId && s.Status == SessionStatus.Active)
            .ToListAsync(cancellationToken);
        foreach (var session in others)
        {
            session.Revoke(now, SessionRevocationReasons.PasswordChanged);
            await _jwtBlacklist.RevokeSessionBeforeAsync(session.Id, now, SessionMarkerTtl);
        }

        try
        {
            await _emailService.SendAsync(
                user.Email.Value, "Password changed — Notrelix", EmailTemplateService.PasswordChanged(user.Name), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password changed notification to {UserId}", user.Id);
        }

        return Result.Success();
    }
}
