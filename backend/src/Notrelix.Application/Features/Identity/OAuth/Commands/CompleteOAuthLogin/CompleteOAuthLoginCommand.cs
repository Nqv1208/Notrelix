using Notrelix.Application.Common.Models;
using Notrelix.Application.Events.Identity;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLogin;

public sealed record CompleteOAuthLoginCommand
    : ICommand<Result<AuthResult>>,
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

public sealed class CompleteOAuthLoginCommandHandler
    : IRequestHandler<CompleteOAuthLoginCommand, Result<AuthResult>>
{
    private readonly IOAuthStateStore _stateStore;
    private readonly IOAuthProviderClient _providerClient;
    private readonly IOAuthOptionsProvider _optionsProvider;
    private readonly IIdentityDbContext _identityContext;
    private readonly IAccountDbContext _accountContext;
    private readonly IAuthSessionIssuer _sessionIssuer;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIntegrationEventCollector _integrationEventCollector;
    private readonly ILogger<CompleteOAuthLoginCommandHandler> _logger;

    public CompleteOAuthLoginCommandHandler(
        IOAuthStateStore stateStore,
        IOAuthProviderClient providerClient,
        IOAuthOptionsProvider optionsProvider,
        IIdentityDbContext identityContext,
        IAccountDbContext accountContext,
        IAuthSessionIssuer sessionIssuer,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IIntegrationEventCollector integrationEventCollector,
        ILogger<CompleteOAuthLoginCommandHandler> logger)
    {
        _stateStore = stateStore;
        _providerClient = providerClient;
        _optionsProvider = optionsProvider;
        _identityContext = identityContext;
        _accountContext = accountContext;
        _sessionIssuer = sessionIssuer;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
        _integrationEventCollector = integrationEventCollector;
        _logger = logger;
    }

    public async Task<Result<AuthResult>> Handle(
        CompleteOAuthLoginCommand request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Error))
        {
            _logger.LogInformation("OAuth callback error: {Error} ({ErrorDescription})",
                request.Error, request.ErrorDescription);
            return Result<AuthResult>.Failure("OAuth provider returned an error.");
        }

        var storedState = await _stateStore.ConsumeAsync(request.State, cancellationToken);
        if (storedState is null)
        {
            _logger.LogWarning("OAuth callback with invalid or expired state: {State}", request.State);
            return Result<AuthResult>.Failure("Invalid or expired OAuth state.");
        }

        if (storedState.Provider != request.Provider)
        {
            _logger.LogWarning("OAuth provider mismatch: stored={Stored}, request={Request}",
                storedState.Provider, request.Provider);
            return Result<AuthResult>.Failure("OAuth provider mismatch.");
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
            return Result<AuthResult>.Failure("Failed to authenticate with OAuth provider.");
        }

        var now = _dateTimeProvider.UtcNow;

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
            return Result<AuthResult>.Failure(
                "Cannot create account: email not verified by provider.");
        }

        var normalizedEmail = profile.Email.Trim().ToLowerInvariant();
        var existingUser = await _identityContext.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            return await LinkToExistingUser(existingUser, profile, now, cancellationToken);
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
            return Result<AuthResult>.Failure("User account not found.");
        }

        if (user.Status != UserStatus.Active)
        {
            _logger.LogWarning("OAuth login blocked: user {UserId} is {Status}", user.Id, user.Status);
            return Result<AuthResult>.Failure("Account is not active.");
        }

        user.RecordLogin(now);
        var authResult = await _sessionIssuer.IssueAsync(user, now, cancellationToken);

        _logger.LogInformation("OAuth login succeeded for existing account (user {UserId})", user.Id);
        return Result<AuthResult>.Success(authResult);
    }

    private async Task<Result<AuthResult>> LinkToExistingUser(
        User user, ExternalOAuthProfile profile, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (user.Status != UserStatus.Active)
        {
            _logger.LogWarning("OAuth link blocked: user {UserId} is {Status}", user.Id, user.Status);
            return Result<AuthResult>.Failure("Account is not active.");
        }

        user.LinkOAuthAccount(profile.Provider, profile.Subject,
            OAuthProfileSnapshot.Create(profile.Provider, 1, profile.RawProfile), null, now);
        var oauthAccount = user.OAuthAccounts.Last();
        _identityContext.OAuthAccounts.Add(oauthAccount);
        user.RecordLogin(now);

        var authResult = await _sessionIssuer.IssueAsync(user, now, cancellationToken);

        _logger.LogInformation("OAuth linked to existing user {UserId} via email", user.Id);
        return Result<AuthResult>.Success(authResult);
    }

    private async Task<Result<AuthResult>> CreateNewUser(
        ExternalOAuthProfile profile, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var displayName = ChooseDisplayName(profile);
        var email = profile.Email!;
        var sentinelHash = _passwordHasher.HashPassword("OAUTH_ONLY_" + Guid.CreateVersion7().ToString("N"));

        var user = User.Create(email, displayName, sentinelHash, now);
        _identityContext.Users.Add(user);

        var accountSlug = Slug.GenerateFromName($"{displayName}'s Account");
        var account = Account.Create(
            $"{displayName}'s Account",
            accountSlug.Value,
            AccountType.Personal,
            user.Id,
            now);
        _accountContext.Accounts.Add(account);

        var accountMember = AccountMember.Create(
            account.Id, user.Id, AccountRole.Owner, user.Id, now);
        _accountContext.AccountMembers.Add(accountMember);

        user.LinkOAuthAccount(profile.Provider, profile.Subject,
            OAuthProfileSnapshot.Create(profile.Provider, 1, profile.RawProfile), null, now);
        var oauthAccount = user.OAuthAccounts.Last();
        _identityContext.OAuthAccounts.Add(oauthAccount);

        _integrationEventCollector.Add(
            new IdentityRegistrationCompletedIntegrationEventV1(
                EventId: Guid.CreateVersion7(),
                UserId: user.Id,
                AccountId: account.Id,
                Email: user.Email.Value,
                DisplayName: user.Name,
                AccountName: account.Name,
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

    private static string ChooseDisplayName(ExternalOAuthProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.Name))
            return profile.Name.Trim();

        if (!string.IsNullOrWhiteSpace(profile.Email))
            return profile.Email.Split('@')[0];

        return "User";
    }
}
