using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Auth.Services;
using Notrelix.Application.Features.Identity.Auth.Throttling;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.Services;
using Notrelix.Domain.Identity.Security;

namespace Notrelix.Application.Features.Identity.Auth.Commands.ResetPassword;

public record ResetPasswordCommand : ICommand<Result>, IWriteRequest, IGlobalRequest, IAnonymousRequest
{
    public required string Email { get; init; }
    public required string Code { get; init; }
    public required string NewPassword { get; init; }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IIdentityDbContext _context;
    private readonly IOtpService _otpService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IJwtBlacklistService _jwtBlacklist;
    private readonly ILoginThrottle _throttle;
    private readonly ISecurityActivityRecorder _activity;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;
    private readonly CredentialScope? _credentialScope;

    // Same watermark lifetime as change-password: longer than any access token.
    private static readonly TimeSpan RevocationWatermarkTtl = TimeSpan.FromHours(24);

    public ResetPasswordCommandHandler(
        IIdentityDbContext context,
        IOtpService otpService,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IDateTimeProvider dateTimeProvider,
        IJwtBlacklistService jwtBlacklist,
        ILoginThrottle throttle,
        ISecurityActivityRecorder activity,
        ILogger<ResetPasswordCommandHandler> logger,
        CredentialScope? credentialScope = null)
    {
        _credentialScope = credentialScope;
        _context = context;
        _otpService = otpService;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _dateTimeProvider = dateTimeProvider;
        _jwtBlacklist = jwtBlacklist;
        _throttle = throttle;
        _activity = activity;
        _logger = logger;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var attempts = await _otpService.GetAttemptsAsync("forgot-password", email);
        if (attempts >= 5)
        {
            return Result.Failure(new ApplicationError("identity.auth.too-many-attempts", "Too many failed attempts. Please request a new code.", ApplicationErrorType.BusinessRule));
        }

        var isValid = await _otpService.ValidateAsync("forgot-password", email, request.Code);
        if (!isValid)
        {
            return Result.Failure(new ApplicationError("identity.auth.invalid-code", "Invalid or expired code.", ApplicationErrorType.Validation));
        }

        // The emailed code was just verified for this address; the scope is bound to the user it names.
        if (_credentialScope is not null)
            await _credentialScope.EnterByEmailAsync(email, cancellationToken);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToLowerInvariant(), cancellationToken);

        if (user is null)
        {
            return Result.Failure(new ApplicationError("identity.auth.user-not-found", "User not found.", ApplicationErrorType.NotFound));
        }

        var now = _dateTimeProvider.UtcNow;
        var hash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatePassword(hash, user.Id, now);
        await PasswordChangeRecorder.RecordAsync(_context, user.Id, now, cancellationToken);
        _activity.Record(user.Id, SecurityActivityType.PasswordReset, now);

        var activeSessions = await _context.Sessions
            .Where(s => s.UserId == user.Id && s.Status == SessionStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.Revoke(now, SessionRevocationReasons.PasswordReset);
        }

        // Access tokens already issued stay valid until they expire unless the user watermark rejects
        // them, exactly as after a password change.
        await _jwtBlacklist.RevokeUserBeforeAsync(user.Id, now, RevocationWatermarkTtl);

        // Resetting through the emailed code proves control of the account: lift its sign-in lockout.
        // Only the account counter is cleared; the per-IP counter and IP lock stay (they protect
        // other accounts from the same client).
        await _throttle.ClearSubjectAsync(LoginThrottleScope.PasswordLogin, email, cancellationToken);

        try
        {
            var html = EmailTemplateService.PasswordChanged(user.Name);
            await _emailService.SendAsync(email, "Password changed — Notrelix", html, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password changed notification to {Email}", email);
        }

        return Result.Success();
    }
}
