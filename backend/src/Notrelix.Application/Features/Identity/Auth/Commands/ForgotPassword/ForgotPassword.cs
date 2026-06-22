using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Email;

namespace Notrelix.Application.Features.Identity.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand : ICommand<Result>
{
    public required string Email { get; init; }
}

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IOtpService _otpService;
    private readonly IRateLimitService _rateLimitService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IApplicationDbContext context,
        IOtpService otpService,
        IRateLimitService rateLimitService,
        IEmailService emailService,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _context = context;
        _otpService = otpService;
        _rateLimitService = rateLimitService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var isLimited = await _rateLimitService.IsRateLimitedAsync(
            "forgot-password", email, maxAttempts: 3, window: TimeSpan.FromHours(1));

        if (isLimited)
        {
            return Result.Failure("Too many requests. Please try again later.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant(), cancellationToken);

        // Always return success to prevent email enumeration
        if (user is null)
        {
            _logger.LogInformation("Forgot password requested for non-existent email: {Email}", email);
            return Result.Success();
        }

        var code = await _otpService.GenerateAsync("forgot-password", email);

        try
        {
            var html = EmailTemplateService.ForgotPasswordOtp(user.Name, code);
            await _emailService.SendAsync(email, "Reset your password — Notrelix", html, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send forgot password email to {Email}", email);
            return Result.Failure("Failed to send email. Please try again.");
        }

        return Result.Success();
    }
}
