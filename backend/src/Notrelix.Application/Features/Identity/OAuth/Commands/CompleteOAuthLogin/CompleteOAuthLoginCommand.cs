using Notrelix.Application.Common.Models;
using Notrelix.Application.Events.Identity;
using Notrelix.Application.Features.Accounts.Public.PersonalAccountProvisioning;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Auth.Services;
using Notrelix.Application.Features.Identity.Registration.Ports;
using Notrelix.Application.Features.Identity.Registration.Services;
using Notrelix.Application.Features.Identity.Mfa;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Application.Common.Requests.Scoping;

namespace Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLogin;

public sealed record CompleteOAuthLoginCommand
    : ICommand<Result<AuthResult>>,
      IAnonymousRequest,
      IGlobalRequest,
      IWriteRequest
{
    public required OAuthProvider Provider { get; init; }
    public required string Code { get; init; }
    public required string State { get; init; }
    public string? Error { get; init; }
    public string? ErrorDescription { get; init; }
}

public sealed class CompleteOAuthLoginCommandHandler
    : IRequestHandler<CompleteOAuthLoginCommand, Result<AuthResult>>
{
    private readonly IOAuthStateStore _stateStore;
    private readonly IOAuthProviderClient _providerClient;
    private readonly IOAuthOptionsProvider _optionsProvider;
    private readonly IIdentityDbContext _identityContext;
    private readonly IAccountProvisioningActions _provisioningActions;
    private readonly IAuthSessionIssuer _sessionIssuer;
    private readonly IMfaChallengeStore _challengeStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIntegrationEventCollector _integrationEventCollector;
    private readonly ILogger<CompleteOAuthLoginCommandHandler> _logger;
    private readonly CredentialScope? _credentialScope;
    private readonly RegistrationScope? _registrationScope;
    private readonly IEmailRegistrationProbe? _registrationProbe;

    public CompleteOAuthLoginCommandHandler(
        IOAuthStateStore stateStore,
        IOAuthProviderClient providerClient,
        IOAuthOptionsProvider optionsProvider,
        IIdentityDbContext identityContext,
        IAccountProvisioningActions provisioningActions,
        IAuthSessionIssuer sessionIssuer,
        IMfaChallengeStore challengeStore,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IIntegrationEventCollector integrationEventCollector,
        ILogger<CompleteOAuthLoginCommandHandler> logger,
        CredentialScope? credentialScope = null,
        RegistrationScope? registrationScope = null,
        IEmailRegistrationProbe? registrationProbe = null)
    {
        _credentialScope = credentialScope;
        _registrationScope = registrationScope;
        _registrationProbe = registrationProbe;
        _stateStore = stateStore;
        _providerClient = providerClient;
        _optionsProvider = optionsProvider;
        _identityContext = identityContext;
        _provisioningActions = provisioningActions;
        _sessionIssuer = sessionIssuer;
        _challengeStore = challengeStore;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
        _integrationEventCollector = integrationEventCollector;
        _logger = logger;
    }

    public async Task<Result<AuthResult>> Handle(
        CompleteOAuthLoginCommand request,
        CancellationToken cancellationToken)
    {
        // The state is used up first, whatever the provider answered: a state works once.
        var storedState = await _stateStore.ConsumeAsync(request.State, cancellationToken);
        if (storedState is null)
        {
            _logger.LogWarning("OAuth callback with invalid or expired state");
            return Fail(OAuthLoginErrorCodes.StateInvalid, "Invalid or expired OAuth state.");
        }

        if (!string.IsNullOrWhiteSpace(request.Error))
        {
            _logger.LogInformation("OAuth callback error: {Error} ({ErrorDescription})",
                OAuthCallbackLog.Sanitize(request.Error), OAuthCallbackLog.Sanitize(request.ErrorDescription));
            return Fail(
                string.Equals(request.Error, "access_denied", StringComparison.Ordinal)
                    ? OAuthLoginErrorCodes.Denied
                    : OAuthLoginErrorCodes.Failed,
                "OAuth provider returned an error.");
        }

        if (storedState.Flow != OAuthFlowKind.Login)
        {
            _logger.LogWarning("OAuth state bound to {Flow} flow cannot complete a login", storedState.Flow);
            return Fail(OAuthLoginErrorCodes.StateInvalid, "OAuth state is not bound to a login flow.");
        }

        if (storedState.Provider != request.Provider)
        {
            _logger.LogWarning("OAuth provider mismatch: stored={Stored}, request={Request}",
                storedState.Provider, request.Provider);
            return Fail(OAuthLoginErrorCodes.Failed, "OAuth provider mismatch.");
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
            return Fail(OAuthLoginErrorCodes.Failed, "Failed to authenticate with OAuth provider.");
        }

        var now = _dateTimeProvider.UtcNow;

        // The credential scope is bound to the user this provider identity is linked to (nobody when unlinked).
        if (_credentialScope is not null)
            await _credentialScope.EnterByOAuthIdentityAsync(profile.Provider.ToString(), profile.Subject, cancellationToken);

        var existingAccount = await _identityContext.OAuthAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Provider == profile.Provider && a.ProviderId == profile.Subject,
                cancellationToken);

        if (existingAccount is not null)
        {
            return await LoginExistingOAuthAccount(existingAccount.UserId, now, cancellationToken);
        }

        if (!profile.EmailVerified || string.IsNullOrWhiteSpace(profile.Email))
        {
            _logger.LogInformation("OAuth email not verified for subject {Subject}", profile.Subject);
            return Fail(
                OAuthLoginErrorCodes.EmailUnverified, "Cannot create account: email not verified by provider.");
        }

        var normalizedEmail = profile.Email.Trim().ToLowerInvariant();
        var emailTaken = _registrationProbe is not null
            ? await _registrationProbe.IsRegisteredAsync(normalizedEmail, cancellationToken)
            : await _identityContext.Users
                .AsNoTracking()
                .AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (emailTaken)
        {
            _logger.LogWarning(
                "OAuth login blocked: subject {Subject} unlinked but its email already belongs to an account",
                profile.Subject);
            return Fail(
                OAuthLoginErrorCodes.EmailInUse,
                "An account with this email already exists. Sign in with your existing method, or link this provider from your signed-in account.");
        }

        return await CreateNewUser(profile, now, cancellationToken);
    }

    private async Task<Result<AuthResult>> LoginExistingOAuthAccount(
        Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var user = await _identityContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            _logger.LogError("OAuth account {UserId} has no matching user", userId);
            return Fail(OAuthLoginErrorCodes.Failed, "User account not found.");
        }

        if (user.Status != UserStatus.Active)
        {
            _logger.LogWarning("OAuth login blocked: user {UserId} is {Status}", user.Id, user.Status);
            return Fail(OAuthLoginErrorCodes.Failed, "Account is not active.");
        }

        var mfaEnabled = await _identityContext.UserMfaMethods
            .AnyAsync(m => m.UserId == user.Id && m.Status == MfaMethodStatus.Active, cancellationToken);

        if (mfaEnabled)
        {
            var (token, payload) = await MfaChallengeFactory.CreateAsync(
                _challengeStore, user.Id, MfaChallengePurpose.OAuthLogin, now, cancellationToken);

            _logger.LogInformation("OAuth login requires MFA challenge for {UserId}", user.Id);

            return Result<AuthResult>.Success(new AuthResult
            {
                MfaRequired = true,
                MfaChallengeToken = token,
                MfaMethod = nameof(MfaMethodType.AuthenticatorApp),
                MfaExpiresAt = payload.ExpiresAt.UtcDateTime
            });
        }

        user.RecordLogin(now);
        var authResult = await _sessionIssuer.IssueAsync(user, now, cancellationToken);

        _logger.LogInformation("OAuth login succeeded for existing account (user {UserId})", user.Id);
        return Result<AuthResult>.Success(authResult);
    }

    private async Task<Result<AuthResult>> CreateNewUser(
        ExternalOAuthProfile profile, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var displayName = ChooseDisplayName(profile);
        var email = profile.Email!;
        var sentinelHash = _passwordHasher.HashPassword("OAUTH_ONLY_" + Guid.CreateVersion7().ToString("N"));

        var user = User.Create(email, displayName, sentinelHash, now, hasPasswordCredential: false);
        if (_registrationScope is not null)
            await _registrationScope.EnterAsync(user.Id, cancellationToken);
        _identityContext.Users.Add(user);

        var accountName = $"{displayName}'s Account";
        var provisioning = await _provisioningActions.ProvisionPersonalAccountAsync(
            user.Id,
            displayName,
            now,
            cancellationToken);
        var accountId = provisioning.AccountId;

        user.LinkOAuthAccount(profile.Provider, profile.Subject,
            OAuthProfileSnapshot.Create(profile.Provider, 1, profile.RawProfile), null, user.Id, now);
        var oauthAccount = user.OAuthAccounts.Last();
        _identityContext.OAuthAccounts.Add(oauthAccount);

        _integrationEventCollector.Add(
            new IdentityRegistrationCompletedIntegrationEventV1(
                EventId: Guid.CreateVersion7(),
                UserId: user.Id,
                AccountId: accountId,
                Email: user.Email.Value,
                DisplayName: user.Name,
                AccountName: accountName,
                CorrelationId: Guid.CreateVersion7(),
                ActorUserId: user.Id,
                SourceEventId: null,
                CausationId: null,
                OccurredAt: now));

        user.RecordLogin(now);
        var authResult = await _sessionIssuer.IssueAsync(user, now, cancellationToken);

        _logger.LogInformation("New user {UserId} created via OAuth ({Provider})", user.Id, profile.Provider);
        return Result<AuthResult>.Success(authResult);
    }

    private static Result<AuthResult> Fail(string code, string message) =>
        Result<AuthResult>.Failure(new ApplicationError(code, message, ApplicationErrorType.Authentication));

    private static string ChooseDisplayName(ExternalOAuthProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.Name))
            return profile.Name.Trim();

        if (!string.IsNullOrWhiteSpace(profile.Email))
            return profile.Email.Split('@')[0];

        return "User";
    }
}
