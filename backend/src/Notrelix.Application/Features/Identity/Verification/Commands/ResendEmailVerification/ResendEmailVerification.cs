using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Verification.Abstractions;

namespace Notrelix.Application.Features.Identity.Verification.Commands.ResendEmailVerification;

public sealed record ResendEmailVerificationCommand(string Email)
    : ICommand<Result>, IAnonymousRequest, IGlobalRequest, IWriteRequest;

public sealed class ResendEmailVerificationCommandHandler
    : IRequestHandler<ResendEmailVerificationCommand, Result>
{
    private readonly IIdentityDbContext _identityContext;
    private readonly IEmailVerificationTokenIssuer _tokenIssuer;
    private readonly IRateLimitService _rateLimitService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ResendEmailVerificationCommandHandler(
        IIdentityDbContext identityContext,
        IEmailVerificationTokenIssuer tokenIssuer,
        IRateLimitService rateLimitService,
        IDateTimeProvider dateTimeProvider)
    {
        _identityContext = identityContext;
        _tokenIssuer = tokenIssuer;
        _rateLimitService = rateLimitService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(
        ResendEmailVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var isLimited = await _rateLimitService.IsRateLimitedAsync(
            "email-verification-resend",
            email,
            maxAttempts: 5,
            window: TimeSpan.FromMinutes(15));

        if (isLimited)
            return Result.Success();

        var user = await _identityContext.Users
            .FirstOrDefaultAsync(x => x.NormalizedEmail == email, cancellationToken);

        if (user is null || user.EmailConfirmed)
            return Result.Success();

        await _tokenIssuer.IssueAsync(
            user,
            user.Id,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        return Result.Success();
    }
}
