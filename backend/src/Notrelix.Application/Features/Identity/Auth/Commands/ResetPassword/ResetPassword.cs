using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;

namespace Notrelix.Application.Features.Identity.Auth.Commands.ResetPassword;

public record ResetPasswordCommand : ICommand<Result>, ITransactionalRequest, IGlobalRequest, IAnonymousRequest
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
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IIdentityDbContext context,
        IOtpService otpService,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IDateTimeProvider dateTimeProvider,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _context = context;
        _otpService = otpService;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _dateTimeProvider = dateTimeProvider;
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

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToLowerInvariant(), cancellationToken);

        if (user is null)
        {
            return Result.Failure(new ApplicationError("identity.auth.user-not-found", "User not found.", ApplicationErrorType.NotFound));
        }

        var now = _dateTimeProvider.UtcNow;
        var hash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatePassword(hash, user.Id, now);

        var activeSessions = await _context.Sessions
            .Where(s => s.UserId == user.Id && s.Status == SessionStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.Revoke(now);
        }

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
