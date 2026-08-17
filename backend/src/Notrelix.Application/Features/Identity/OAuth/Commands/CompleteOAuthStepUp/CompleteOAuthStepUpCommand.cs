using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Security;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;

namespace Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthStepUp;

/// <summary>
/// Completes an OAuth re-authentication (step-up) callback. Never issues a new
/// session; it only grants a purpose-bound step-up proof for the bound user.
/// </summary>
public sealed record CompleteOAuthStepUpCommand
    : ICommand<Result<StepUpProofResult>>,
      IAnonymousRequest,
      IGlobalRequest,
      ITransactionalRequest
{
    public required OAuthProvider Provider { get; init; }
    public required string Code { get; init; }
    public required string State { get; init; }
    public string? Error { get; init; }
    public string? ErrorDescription { get; init; }
}

public sealed class CompleteOAuthStepUpCommandHandler
    : IRequestHandler<CompleteOAuthStepUpCommand, Result<StepUpProofResult>>
{
    private readonly IOAuthStateStore _stateStore;
    private readonly IOAuthProviderClient _providerClient;
    private readonly IOAuthOptionsProvider _optionsProvider;
    private readonly IIdentityDbContext _identityContext;
    private readonly ISecurityStepUpService _stepUpService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CompleteOAuthStepUpCommandHandler> _logger;

    public CompleteOAuthStepUpCommandHandler(
        IOAuthStateStore stateStore,
        IOAuthProviderClient providerClient,
        IOAuthOptionsProvider optionsProvider,
        IIdentityDbContext identityContext,
        ISecurityStepUpService stepUpService,
        IDateTimeProvider dateTimeProvider,
        ILogger<CompleteOAuthStepUpCommandHandler> logger)
    {
        _stateStore = stateStore;
        _providerClient = providerClient;
        _optionsProvider = optionsProvider;
        _identityContext = identityContext;
        _stepUpService = stepUpService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<StepUpProofResult>> Handle(
        CompleteOAuthStepUpCommand request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Error))
        {
            _logger.LogInformation("OAuth step-up callback error: {Error} ({ErrorDescription})",
                request.Error, request.ErrorDescription);
            return Result<StepUpProofResult>.Failure("OAuth provider returned an error.");
        }

        var storedState = await _stateStore.ConsumeAsync(request.State, cancellationToken);
        if (storedState is null || storedState.Flow != OAuthFlowKind.StepUp)
        {
            _logger.LogWarning("OAuth step-up with invalid or expired state");
            return Result<StepUpProofResult>.Failure("Invalid or expired OAuth state.");
        }

        if (storedState.Provider != request.Provider)
        {
            _logger.LogWarning("OAuth provider mismatch: stored={Stored}, request={Request}",
                storedState.Provider, request.Provider);
            return Result<StepUpProofResult>.Failure("OAuth provider mismatch.");
        }

        if (storedState.BoundUserId is not { } userId
            || storedState.BoundSessionId is not { } sessionId
            || storedState.StepUpPurpose is not { } stepUpPurpose)
        {
            _logger.LogWarning("OAuth step-up state missing user/session/purpose binding");
            return Result<StepUpProofResult>.Failure("Invalid or expired OAuth state.");
        }

        var user = await _identityContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null || user.Status != UserStatus.Active)
        {
            _logger.LogWarning("OAuth step-up blocked: user {UserId} is {Status}", userId, user?.Status);
            return Result<StepUpProofResult>.Failure("Account is not active.");
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
            return Result<StepUpProofResult>.Failure("Failed to authenticate with OAuth provider.");
        }

        var accountLinked = await _identityContext.OAuthAccounts
            .AsNoTracking()
            .AnyAsync(a =>
                a.UserId == userId &&
                a.Provider == profile.Provider &&
                a.ProviderId == profile.Subject,
                cancellationToken);

        if (!accountLinked)
        {
            _logger.LogWarning(
                "OAuth step-up rejected: subject {Subject} is not linked to user {UserId}",
                profile.Subject, userId);
            return Result<StepUpProofResult>.Failure("Unable to verify identity with the OAuth provider.");
        }

        var purpose = StepUpPurposeMapping.FromChallengePurpose(stepUpPurpose);
        var proof = await _stepUpService.GrantOAuthProofAsync(userId, sessionId, purpose, cancellationToken);

        _logger.LogInformation("OAuth step-up completed for {UserId} (purpose {Purpose})", userId, purpose);

        return proof;
    }
}