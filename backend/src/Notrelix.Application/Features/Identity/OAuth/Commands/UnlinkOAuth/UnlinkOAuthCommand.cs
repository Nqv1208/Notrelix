using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Domain.Identity;

namespace Notrelix.Application.Features.Identity.OAuth.Commands.UnlinkOAuth;

public sealed record UnlinkOAuthCommand
    : ICommand<Result>,
      IAuthenticatedRequest,
      IGlobalRequest,
      ITransactionalRequest
{
    public required OAuthProvider Provider { get; init; }
}

public sealed class UnlinkOAuthCommandHandler
    : IRequestHandler<UnlinkOAuthCommand, Result>
{
    private readonly IIdentityDbContext _identityContext;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<UnlinkOAuthCommandHandler> _logger;

    public UnlinkOAuthCommandHandler(
        IIdentityDbContext identityContext,
        ICurrentRequestContext currentUser,
        IDateTimeProvider dateTimeProvider,
        ILogger<UnlinkOAuthCommandHandler> logger)
    {
        _identityContext = identityContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result> Handle(UnlinkOAuthCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityContext.Users
            .Include(u => u.OAuthAccounts)
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(new ApplicationError(
                "identity.auth.user-not-found",
                "User not found.",
                ApplicationErrorType.NotFound));
        }

        if (user.Status != UserStatus.Active)
        {
            _logger.LogWarning("OAuth unlink blocked: user {UserId} is {Status}", user.Id, user.Status);
            return Result.Failure(new ApplicationError(
                "identity.auth.account-not-active",
                "Account is not active.",
                ApplicationErrorType.Conflict));
        }

        var accountToUnlink = user.OAuthAccounts.FirstOrDefault(a => a.Provider == request.Provider);

        try
        {
            user.UnlinkOAuthAccount(request.Provider, user.Id, _dateTimeProvider.UtcNow);
        }
        catch (Notrelix.Domain.Common.Exceptions.BusinessRuleException ex)
            when (ex.RuleCode == IdentityRuleCodes.Identity_User_LastPrimaryAuthMethod)
        {
            _logger.LogWarning(
                "OAuth unlink rejected for user {UserId}: last primary auth method",
                user.Id);

            return Result.Failure(new ApplicationError(
                "identity.auth.last-primary-auth-method",
                "Cannot unlink the last authentication method.",
                ApplicationErrorType.Conflict));
        }

        if (accountToUnlink is not null)
        {
            _identityContext.OAuthAccounts.Remove(accountToUnlink);
        }

        _logger.LogInformation("OAuth provider {Provider} unlinked from user {UserId}", request.Provider, user.Id);
        return Result.Success();
    }
}
