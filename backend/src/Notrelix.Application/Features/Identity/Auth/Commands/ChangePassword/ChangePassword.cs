using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;

namespace Notrelix.Application.Features.Identity.Auth.Commands.ChangePassword;

public record ChangePasswordCommand : ICommand<Result>, ITransactionalRequest, IGlobalRequest, IAuthenticatedRequest
{
    public required string CurrentPassword { get; init; }
    public required string NewPassword { get; init; }
}

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtBlacklistService _jwtBlacklist;
    private readonly IEmailService _emailService;
    private readonly IDateTimeProvider _dateTimeProvider;
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
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _jwtBlacklist = jwtBlacklist;
        _emailService = emailService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
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
}
