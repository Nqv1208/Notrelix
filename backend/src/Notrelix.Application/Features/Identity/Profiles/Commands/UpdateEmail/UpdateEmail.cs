using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Verification.Abstractions;

namespace Notrelix.Application.Features.Identity.Profiles.Commands.UpdateEmail;

public sealed record UpdateEmailCommand(string Email)
    : ICommand<Result<UpdateEmailResultDto>>,
      IAuthenticatedRequest,
      IWriteRequest,
      IGlobalRequest;

public sealed record UpdateEmailResultDto(
    bool EmailConfirmed,
    bool SessionRefreshRequired);

public sealed class UpdateEmailCommandHandler
    : IRequestHandler<UpdateEmailCommand, Result<UpdateEmailResultDto>>
{
    private readonly IIdentityDbContext _identityContext;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IEmailVerificationTokenIssuer _tokenIssuer;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateEmailCommandHandler(
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

    public async Task<Result<UpdateEmailResultDto>> Handle(
        UpdateEmailCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _identityContext.Users
            .FirstOrDefaultAsync(
                x => x.Id == _requestContext.UserId,
                cancellationToken);

        if (user is null)
            return Result<UpdateEmailResultDto>.Failure("User not found.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailInUse = await _identityContext.Users
            .AnyAsync(
                x => x.NormalizedEmail == normalizedEmail && x.Id != user.Id,
                cancellationToken);

        if (emailInUse)
            return Result<UpdateEmailResultDto>.Failure("Email is already in use.");

        var now = _dateTimeProvider.UtcNow;
        user.UpdateEmail(normalizedEmail, _requestContext.UserId, now);
        await _tokenIssuer.IssueAsync(user, user.Id, now, cancellationToken);

        return Result<UpdateEmailResultDto>.Success(
            new UpdateEmailResultDto(
                EmailConfirmed: false,
                SessionRefreshRequired: true));
    }
}
