using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Verification.Abstractions;

namespace Notrelix.Application.Features.Identity.Verification.Commands.RequestEmailVerification;

public sealed record RequestEmailVerificationCommand()
    : ICommand<Result>, IAuthenticatedRequest, ITransactionalRequest;

public sealed record RequestEmailVerificationResult(
    Guid TokenId,
    DateTimeOffset ExpiresAt
);

public sealed class RequestEmailVerificationCommandHandler
    : IRequestHandler<RequestEmailVerificationCommand, Result>
{
    private readonly IIdentityDbContext _identityContext;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IEmailVerificationTokenIssuer _tokenIssuer;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RequestEmailVerificationCommandHandler(
        IIdentityDbContext identityContext,
        ICurrentRequestContext requestContext,
        IEmailVerificationTokenIssuer tokenIssuer,
        IDateTimeProvider dateTimeProvider)
    {
        _identityContext = identityContext;
        _requestContext = requestContext;
        _tokenIssuer = tokenIssuer;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(
        RequestEmailVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _requestContext.UserId;

        var user = await _identityContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Result.Failure("User not found.");

        if (user.EmailConfirmed)
            return Result.Failure("Email is already confirmed.");

        var now = _dateTimeProvider.UtcNow;

        await _tokenIssuer.IssueAsync(user, userId, now, cancellationToken);

        return Result.Success();
    }
}
