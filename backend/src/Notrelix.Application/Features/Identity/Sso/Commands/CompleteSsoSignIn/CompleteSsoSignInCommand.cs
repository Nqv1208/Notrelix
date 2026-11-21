using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Accounts.Public.Security;
using Notrelix.Application.Features.Accounts.Public.Sso;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Auth.Services;
using Notrelix.Application.Features.Identity.Mfa;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Application.Features.Identity.OAuth;
using Notrelix.Application.Features.Identity.Registration.Services;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.Sso;

namespace Notrelix.Application.Features.Identity.Sso.Commands.CompleteSsoSignIn;

/// <summary>What the browser continues with after the company sign-in: the same outcomes as any sign-in, and the safe path it asked to return to.</summary>
public sealed record SsoSignInResult(AuthResult Auth, string? ReturnUrl);

public sealed record CompleteSsoSignInCommand
    : ICommand<Result<SsoSignInResult>>, IAnonymousRequest, IGlobalRequest, IWriteRequest
{
    public string? Code { get; init; }
    public string? State { get; init; }
    public string? Error { get; init; }
    public string? ErrorDescription { get; init; }

    /// <summary>The value read from the browser's binding cookie; null when the browser sent none.</summary>
    public string? BrowserBinding { get; init; }
}

/// <summary>
/// Finishes a company sign-in. The state is used up first; the client secret is read before any other scope is entered;
/// the ID token is validated before one claim is believed; and the person is then found, linked or provisioned strictly
/// in that order (known provider identity, existing user with the same verified email, just-in-time creation).
/// After that the Account's MFA rules apply exactly as after a password or Google sign-in.
/// </summary>
public sealed class CompleteSsoSignInCommandHandler : IRequestHandler<CompleteSsoSignInCommand, Result<SsoSignInResult>>
{
    private readonly ISsoStateStore _stateStore;
    private readonly IAccountSsoFacts _facts;
    private readonly ISsoCodeRedeemer _redeemer;
    private readonly IIdentityDbContext _context;
    private readonly SsoPendingWrites _pendingWrites;
    private readonly IAuthSessionIssuer _sessionIssuer;
    private readonly IMfaChallengeStore _challengeStore;
    private readonly IAccountSecurityPolicyFacts _securityPolicy;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<CompleteSsoSignInCommandHandler> _logger;
    private readonly CredentialScope? _credentialScope;
    private readonly RegistrationScope? _registrationScope;

    public CompleteSsoSignInCommandHandler(
        ISsoStateStore stateStore,
        IAccountSsoFacts facts,
        ISsoCodeRedeemer redeemer,
        IIdentityDbContext context,
        SsoPendingWrites pendingWrites,
        IAuthSessionIssuer sessionIssuer,
        IMfaChallengeStore challengeStore,
        IAccountSecurityPolicyFacts securityPolicy,
        IPasswordHasher passwordHasher,
        IDateTimeProvider clock,
        ILogger<CompleteSsoSignInCommandHandler> logger,
        CredentialScope? credentialScope = null,
        RegistrationScope? registrationScope = null)
    {
        _stateStore = stateStore;
        _facts = facts;
        _redeemer = redeemer;
        _context = context;
        _pendingWrites = pendingWrites;
        _sessionIssuer = sessionIssuer;
        _challengeStore = challengeStore;
        _securityPolicy = securityPolicy;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _logger = logger;
        _credentialScope = credentialScope;
        _registrationScope = registrationScope;
    }

    public async Task<Result<SsoSignInResult>> Handle(CompleteSsoSignInCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        // The state is used up first, whatever the provider answered: a state works once.
        var state = string.IsNullOrWhiteSpace(request.State)
            ? null
            : await _stateStore.ConsumeAsync(request.State, cancellationToken);
        if (state is null || state.ExpiresAt <= now)
        {
            _logger.LogWarning("SSO callback with an unknown, used or expired state");
            return Fail(SsoErrorCodes.StateInvalid);
        }

        // The state belongs to the browser that started it: a callback opened by anyone else (login CSRF) is refused
        // exactly like an unknown state, and the state is already used up.
        if (!SsoBinding.Matches(request.BrowserBinding, state.BindingHash))
        {
            _logger.LogWarning("SSO callback refused: the browser is not the one that started the sign-in");
            return Fail(SsoErrorCodes.StateInvalid);
        }

        if (!string.IsNullOrWhiteSpace(request.Error))
        {
            _logger.LogInformation("SSO callback error: {Error} ({ErrorDescription})",
                OAuthCallbackLog.Sanitize(request.Error), OAuthCallbackLog.Sanitize(request.ErrorDescription));
            return Fail(string.Equals(request.Error, "access_denied", StringComparison.Ordinal)
                ? SsoErrorCodes.Denied
                : SsoErrorCodes.Failed);
        }

        if (string.IsNullOrWhiteSpace(request.Code))
            return Fail(SsoErrorCodes.Failed);

        // The client secret is read in the database's SSO callback scope, which replaces any earlier scope:
        // it comes before the credential or registration scope is entered.
        var provider = await _facts.GetCallbackProviderAsync(state.AccountId, cancellationToken);
        if (provider is null || !string.Equals(provider.Issuer, state.Issuer, StringComparison.Ordinal))
        {
            _logger.LogWarning("SSO callback refused for account {AccountId}: provider missing or changed since the start", state.AccountId);
            return Fail(SsoErrorCodes.Failed);
        }

        SsoIdentityClaims claims;
        try
        {
            claims = await _redeemer.RedeemAsync(
                new SsoCodeRedemption(provider.Issuer, provider.ClientId, provider.ClientSecret, request.Code, state.CodeVerifier, state.RedirectUri, state.Nonce),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Only the failed check or the exception type is logged: never the code, a token or the secret.
            _logger.LogWarning("SSO code redemption failed for account {AccountId}: {Reason}", state.AccountId,
                ex is SsoRedemptionException ? ex.Message : ex.GetType().Name);
            return Fail(SsoErrorCodes.Failed);
        }

        if (!claims.EmailVerified || string.IsNullOrWhiteSpace(claims.Email))
        {
            _logger.LogInformation("SSO sign-in refused for account {AccountId}: the provider did not verify the email", state.AccountId);
            return Fail(SsoErrorCodes.EmailUnverified);
        }

        var email = claims.Email.Trim().ToLowerInvariant();
        if (!await _facts.IsEmailDomainVerifiedAsync(state.AccountId, email, cancellationToken))
        {
            _logger.LogWarning("SSO sign-in refused for account {AccountId}: the email's domain is not verified for it", state.AccountId);
            return Fail(SsoErrorCodes.DomainMismatch);
        }

        // (a) A subject this Account already knows signs in as its user.
        if (_credentialScope is not null)
            await _credentialScope.EnterBySsoIdentityAsync(state.AccountId, claims.Subject, cancellationToken);
        var known = await _context.SsoIdentities.AsNoTracking()
            .FirstOrDefaultAsync(i => i.AccountId == state.AccountId && i.Subject == claims.Subject, cancellationToken);
        if (known is not null)
        {
            var linked = await _context.Users.FirstOrDefaultAsync(u => u.Id == known.UserId, cancellationToken);
            return linked is null ? Fail(SsoErrorCodes.Failed) : await FinishAsync(linked, state, now, cancellationToken);
        }

        // (b) An existing user whose VERIFIED email is this one is linked; an unverified address is never merged.
        if (_credentialScope is not null)
            await _credentialScope.EnterByEmailAsync(email, cancellationToken);
        var existing = await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == email, cancellationToken);
        if (existing is not null)
        {
            if (!existing.EmailConfirmed
                || await _context.SsoIdentities.AnyAsync(i => i.UserId == existing.Id && i.AccountId == state.AccountId, cancellationToken))
            {
                _logger.LogWarning("SSO sign-in refused for account {AccountId}: the email belongs to a user that cannot be linked", state.AccountId);
                return Fail(SsoErrorCodes.IdentityConflict);
            }

            if (existing.Status != UserStatus.Active)
                return Fail(SsoErrorCodes.Failed);

            _context.SsoIdentities.Add(SsoIdentity.Create(state.AccountId, provider.Issuer, claims.Subject, existing.Id, now));
            if (provider.JitProvisioning)
                await _facts.JoinAsMemberAsync(state.AccountId, existing.Id, email, now, cancellationToken);
            await _pendingWrites.StoreNowAsync(cancellationToken);
            _logger.LogInformation("SSO identity linked to user {UserId} for account {AccountId}", existing.Id, state.AccountId);
            return await FinishAsync(existing, state, now, cancellationToken);
        }

        // (c) Nobody has this email: a Member is created only when the Owner switched provisioning on.
        if (!provider.JitProvisioning)
        {
            _logger.LogInformation("SSO sign-in refused for account {AccountId}: unknown person and provisioning is off", state.AccountId);
            return Fail(SsoErrorCodes.NotProvisioned);
        }

        var user = await ProvisionAsync(state.AccountId, provider.Issuer, claims, email, now, cancellationToken);
        return await FinishAsync(user, state, now, cancellationToken);
    }

    /// <summary>
    /// A new person: no password, email confirmed (the provider verified it and the Account verified the domain), a
    /// Member of the Account and nothing else: no personal Account and no workspace.
    /// </summary>
    private async Task<User> ProvisionAsync(
        Guid accountId, string issuer, SsoIdentityClaims claims, string email, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var displayName = string.IsNullOrWhiteSpace(claims.Name) ? email.Split('@')[0] : claims.Name.Trim();
        if (displayName.Length > User.MaxNameLength)
            displayName = displayName[..User.MaxNameLength];

        var user = User.Create(email, displayName, _passwordHasher.HashPassword("SSO_ONLY_" + Guid.CreateVersion7().ToString("N")), now, hasPasswordCredential: false);
        user.ConfirmEmail(null, now);

        if (_registrationScope is not null)
            await _registrationScope.EnterAsync(user.Id, cancellationToken);
        _context.Users.Add(user);
        _context.SsoIdentities.Add(SsoIdentity.Create(accountId, issuer, claims.Subject, user.Id, now));

        // The Account side joins the stored person: it checks the confirmed email against the verified domain.
        await _pendingWrites.StoreNowAsync(cancellationToken);
        if (!await _facts.JoinAsMemberAsync(accountId, user.Id, email, now, cancellationToken))
            throw new InvalidOperationException("The person could not be joined to the Account after the provider and the domain were verified.");
        await _pendingWrites.StoreNowAsync(cancellationToken);

        // From here the person is an existing user: the Account's rules are read in that user's own scope.
        if (_credentialScope is not null)
            await _credentialScope.EnterByEmailAsync(email, cancellationToken);
        _logger.LogInformation("SSO person provisioned as an Account Member");
        return user;
    }

    private async Task<Result<SsoSignInResult>> FinishAsync(
        User user, SsoLoginState state, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (user.Status != UserStatus.Active)
        {
            _logger.LogWarning("SSO sign-in blocked: user {UserId} is {Status}", user.Id, user.Status);
            return Fail(SsoErrorCodes.Failed);
        }

        var returnUrl = SafeReturnPath.OrNull(state.ReturnUrl);

        var mfaEnabled = await _context.UserMfaMethods
            .AnyAsync(m => m.UserId == user.Id && m.Status == MfaMethodStatus.Active, cancellationToken);
        if (mfaEnabled)
        {
            var (token, payload) = await MfaChallengeFactory.CreateAsync(
                _challengeStore, user.Id, MfaChallengePurpose.OAuthLogin, now, cancellationToken);
            return Success(new AuthResult
            {
                MfaRequired = true,
                MfaChallengeToken = token,
                MfaMethod = nameof(MfaMethodType.AuthenticatorApp),
                MfaExpiresAt = payload.ExpiresAt.UtcDateTime,
            }, returnUrl);
        }

        // The Account's MFA policy still applies: a person without a factor only gets the step that enrols one.
        if (await _securityPolicy.IsMfaRequiredForPersonAsync(user.Id, cancellationToken))
            return Success(await RequiredMfaEnrollment.ChallengeAsync(_challengeStore, user.Id, now, cancellationToken), returnUrl);

        user.RecordLogin(now);
        var auth = await _sessionIssuer.IssueAsync(user, now, cancellationToken);
        _logger.LogInformation("SSO sign-in succeeded for user {UserId}", user.Id);
        return Success(auth, returnUrl);
    }

    private static Result<SsoSignInResult> Success(AuthResult auth, string? returnUrl) =>
        Result<SsoSignInResult>.Success(new SsoSignInResult(auth, returnUrl));

    private static Result<SsoSignInResult> Fail(string code) =>
        Result<SsoSignInResult>.Failure(new ApplicationError(code, "Company sign-in did not complete.", ApplicationErrorType.Authentication));
}

public sealed class CompleteSsoSignInCommandValidator : AbstractValidator<CompleteSsoSignInCommand>
{
    public CompleteSsoSignInCommandValidator()
    {
        RuleFor(x => x.State).MaximumLength(512);
        RuleFor(x => x.Code).MaximumLength(4096);
    }
}
