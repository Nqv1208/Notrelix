using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Email;

namespace Notrelix.Application.Features.Identity.Auth.Commands.ResetPassword;

public record ResetPasswordCommand : ICommand<Result>, ITransactionalRequest
{
    public required string Email { get; init; }
    public required string Code { get; init; }
    public required string NewPassword { get; init; }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IOtpService _otpService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IApplicationDbContext context,
        IOtpService otpService,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _context = context;
        _otpService = otpService;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var attempts = await _otpService.GetAttemptsAsync("forgot-password", email);
        if (attempts >= 5)
        {
            return Result.Failure("Too many failed attempts. Please request a new code.");
        }

        var isValid = await _otpService.ValidateAsync("forgot-password", email, request.Code);
        if (!isValid)
        {
            return Result.Failure("Invalid or expired code");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant(), cancellationToken);

        if (user is null)
        {
            return Result.Failure("User not found");
        }

        var hash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatePassword(hash, DateTimeOffset.UtcNow);

        var activeSessions = await _context.Sessions
            .Where(s => s.UserId == user.Id && s.Status == SessionStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.Revoke(DateTimeOffset.UtcNow);
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
