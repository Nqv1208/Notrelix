using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Application.Common.Requests.Scoping;

namespace Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLink;

public sealed record OAuthLinkResult(
    OAuthProvider Provider,
    string ProviderId,
    bool AlreadyLinked);

public sealed record CompleteOAuthLinkCommand
    : ICommand<Result<OAuthLinkResult>>,
      IAuthenticatedRequest,
      IGlobalRequest,
      IWriteRequest
{
    public required OAuthProvider Provider { get; init; }
    public required string Code { get; init; }
    public required string State { get; init; }
    public string? Error { get; init; }
    public string? ErrorDescription { get; init; }
}

public sealed class CompleteOAuthLinkCommandHandler
    : IRequestHandler<CompleteOAuthLinkCommand, Result<OAuthLinkResult>>
{
    private readonly IOAuthStateStore _stateStore;
    private readonly IOAuthProviderClient _providerClient;
    private readonly IOAuthOptionsProvider _optionsProvider;
    private readonly IIdentityDbContext _identityContext;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CompleteOAuthLinkCommandHandler> _logger;

    public CompleteOAuthLinkCommandHandler(
        IOAuthStateStore stateStore,
        IOAuthProviderClient providerClient,
        IOAuthOptionsProvider optionsProvider,
        IIdentityDbContext identityContext,
        ICurrentRequestContext currentUser,
        IDateTimeProvider dateTimeProvider,
        ILogger<CompleteOAuthLinkCommandHandler> logger)
    {
        _stateStore = stateStore;
        _providerClient = providerClient;
        _optionsProvider = optionsProvider;
        _identityContext = identityContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<OAuthLinkResult>> Handle(
        CompleteOAuthLinkCommand request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Error))
        {
            _logger.LogInformation("OAuth link callback error: {Error} ({ErrorDescription})",
                request.Error, request.ErrorDescription);
            return Result<OAuthLinkResult>.Failure("OAuth provider returned an error.");
        }

        var storedState = await _stateStore.ConsumeAsync(request.State, cancellationToken);
        if (storedState is null)
        {
            _logger.LogWarning("OAuth link callback with invalid or expired state: {State}", request.State);
            return Result<OAuthLinkResult>.Failure("Invalid or expired OAuth state.");
        }

        if (storedState.Flow != OAuthFlowKind.Link)
        {
            _logger.LogWarning("OAuth state bound to {Flow} flow cannot complete a link", storedState.Flow);
            return Result<OAuthLinkResult>.Failure("OAuth state is not bound to a link flow.");
        }

        if (storedState.BoundUserId != _currentUser.UserId)
        {
            _logger.LogWarning(
                "OAuth link state bound to user {BoundUser} attempted by user {CurrentUser}",
                storedState.BoundUserId, _currentUser.UserId);
            return Result<OAuthLinkResult>.Failure("OAuth state is bound to another user.");
        }

        if (storedState.Provider != request.Provider)
        {
            _logger.LogWarning("OAuth provider mismatch: stored={Stored}, request={Request}",
                storedState.Provider, request.Provider);
            return Result<OAuthLinkResult>.Failure("OAuth provider mismatch.");
        }

        var redirectUri = _optionsProvider.GetRedirectUri(request.Provider);
        var redemptionRequest = new OAuthCodeRedemptionRequest(
            request.Code, storedState.CodeVerifier, storedState.Nonce, redirectUri);

        ExternalOAuthProfile profile;
        try
        {
            profile = await _providerClient.RedeemCodeAsync(
                request.Provider, redemptionRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth code redemption failed for provider {Provider}", request.Provider);
            return Result<OAuthLinkResult>.Failure("Failed to authenticate with OAuth provider.");
        }

        var now = _dateTimeProvider.UtcNow;
        var user = await _identityContext.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user is null)
        {
            return Result<OAuthLinkResult>.Failure("User not found.");
        }

        if (user.Status != UserStatus.Active)
        {
            _logger.LogWarning("OAuth link blocked: user {UserId} is {Status}", user.Id, user.Status);
            return Result<OAuthLinkResult>.Failure("Account is not active.");
        }

        var existingAccount = await _identityContext.OAuthAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Provider == profile.Provider && a.ProviderId == profile.Subject,
                cancellationToken);

        if (existingAccount is not null)
        {
            if (existingAccount.UserId == user.Id)
            {
                _logger.LogInformation("OAuth provider {Provider} already linked to user {UserId}", profile.Provider, user.Id);
                return Result<OAuthLinkResult>.Success(
                    new OAuthLinkResult(profile.Provider, profile.Subject, AlreadyLinked: true));
            }

            _logger.LogWarning(
                "OAuth link rejected: provider subject {Subject} already linked to another user {OtherUser}",
                profile.Subject, existingAccount.UserId);
            return Result<OAuthLinkResult>.Failure(
                "This provider identity is already linked to another account.");
        }

        user.LinkOAuthAccount(profile.Provider, profile.Subject,
            OAuthProfileSnapshot.Create(profile.Provider, 1, profile.RawProfile), null, user.Id, now);
        var oauthAccount = user.OAuthAccounts.Last();
        _identityContext.OAuthAccounts.Add(oauthAccount);

        _logger.LogInformation("OAuth provider {Provider} linked to user {UserId}", profile.Provider, user.Id);
        return Result<OAuthLinkResult>.Success(
            new OAuthLinkResult(profile.Provider, profile.Subject, AlreadyLinked: false));
    }
}
