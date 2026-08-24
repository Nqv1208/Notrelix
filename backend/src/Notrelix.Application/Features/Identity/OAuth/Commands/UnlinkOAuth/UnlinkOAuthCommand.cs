using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Identity;

namespace Notrelix.Application.Features.Identity.OAuth.Commands.UnlinkOAuth;

public sealed record UnlinkOAuthCommand
    : ICommand<Result>,
      IAuthenticatedRequest,
      IGlobalRequest,
      IWriteRequest
{
    public required OAuthProvider Provider { get; init; }

    /// <summary>Single-use step-up proof for the UnlinkOAuth purpose (TOTP, recovery code, password or OAuth re-authentication).</summary>
    public required string StepUpToken { get; init; }
}

public sealed class UnlinkOAuthCommandHandler
    : IRequestHandler<UnlinkOAuthCommand, Result>
{
    private readonly IIdentityDbContext _identityContext;
    private readonly ICurrentRequestContext _currentUser;
    private readonly ISecurityStepUpService _stepUpService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<UnlinkOAuthCommandHandler> _logger;

    public UnlinkOAuthCommandHandler(
        IIdentityDbContext identityContext,
        ICurrentRequestContext currentUser,
        ISecurityStepUpService stepUpService,
        IDateTimeProvider dateTimeProvider,
        ILogger<UnlinkOAuthCommandHandler> logger)
    {
        _identityContext = identityContext;
        _currentUser = currentUser;
        _stepUpService = stepUpService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result> Handle(UnlinkOAuthCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var stepUp = await ConsumeStepUpAsync(userId, request.StepUpToken, cancellationToken);
        if (!stepUp.Succeeded)
        {
            return stepUp;
        }

        var user = await _identityContext.Users
            .Include(u => u.OAuthAccounts)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

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

    private async Task<Result> ConsumeStepUpAsync(Guid userId, string stepUpToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(stepUpToken))
        {
            return Result.Failure(new ApplicationError(
                "identity.security.step-up-required",
                "Strong verification is required for this action.",
                ApplicationErrorType.PreconditionFailed));
        }

        if (_currentUser.SessionId is not { } sessionId)
        {
            return Result.Failure(new ApplicationError(
                "identity.security.step-up-required",
                "Strong verification is required for this action.",
                ApplicationErrorType.PreconditionFailed));
        }

        return await _stepUpService.ConsumeAsync(
            stepUpToken, userId, sessionId, StepUpPurpose.UnlinkOAuth, cancellationToken);
    }
}
