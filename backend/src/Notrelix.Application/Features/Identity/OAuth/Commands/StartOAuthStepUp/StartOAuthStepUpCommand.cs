using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Security;
using Notrelix.Application.Features.Identity.Security.DTOs;

namespace Notrelix.Application.Features.Identity.OAuth.Commands.StartOAuthStepUp;

/// <summary>
/// Starts an OAuth re-authentication flow bound to the current user, session and
/// step-up purpose. Completing it grants a step-up proof (no new session is issued).
/// </summary>
public sealed record StartOAuthStepUpCommand
    : ICommand<Result<OAuthLoginStartResult>>,
      IAuthenticatedRequest,
      IGlobalRequest,
      ITransactionalRequest
{
    public required OAuthProvider Provider { get; init; }
    public required StepUpPurpose Purpose { get; init; }
    public string? ReturnUrl { get; init; }
}

public sealed class StartOAuthStepUpCommandHandler
    : IRequestHandler<StartOAuthStepUpCommand, Result<OAuthLoginStartResult>>
{
    private readonly IOAuthOptionsProvider _optionsProvider;
    private readonly IOAuthProviderClient _providerClient;
    private readonly IOAuthStateStore _stateStore;
    private readonly IIdentityDbContext _identityContext;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public StartOAuthStepUpCommandHandler(
        IOAuthOptionsProvider optionsProvider,
        IOAuthProviderClient providerClient,
        IOAuthStateStore stateStore,
        IIdentityDbContext identityContext,
        ICurrentRequestContext currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _optionsProvider = optionsProvider;
        _providerClient = providerClient;
        _stateStore = stateStore;
        _identityContext = identityContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<OAuthLoginStartResult>> Handle(
        StartOAuthStepUpCommand request,
        CancellationToken cancellationToken)
    {
        if (!_optionsProvider.IsProviderEnabled(request.Provider))
        {
            return Result<OAuthLoginStartResult>.Failure($"OAuth provider {request.Provider} is not enabled");
        }

        var userId = _currentUser.UserId;

        if (_currentUser.SessionId is not { } sessionId)
        {
            return Result<OAuthLoginStartResult>.Failure(new ApplicationError(
                "identity.security.step-up-required",
                "Strong verification is required for this action.",
                ApplicationErrorType.PreconditionFailed));
        }

        var providerLinked = await _identityContext.OAuthAccounts
            .AsNoTracking()
            .AnyAsync(a => a.UserId == userId && a.Provider == request.Provider, cancellationToken);

        if (!providerLinked)
        {
            return Result<OAuthLoginStartResult>.Failure(new ApplicationError(
                "identity.oauth.provider-not-linked",
                "This provider is not linked to your account.",
                ApplicationErrorType.Conflict));
        }

        var state = GenerateCryptographicValue();
        var nonce = GenerateCryptographicValue();

        string? codeVerifier = null;
        string? codeChallenge = null;

        if (request.Provider == OAuthProvider.Google)
        {
            codeVerifier = GenerateCryptographicValue();
            codeChallenge = ComputeCodeChallenge(codeVerifier);
        }

        var redirectUri = _optionsProvider.GetRedirectUri(request.Provider);

        var authRequest = new OAuthAuthorizationRequest(
            redirectUri, state, nonce, codeChallenge, "S256");

        var urlResult = await _providerClient.BuildAuthorizationUrlAsync(
            request.Provider, authRequest, cancellationToken);

        var expiresAt = _dateTimeProvider.UtcNow.AddMinutes(10);
        var stepUpState = new OAuthLoginState(
            state, nonce, codeVerifier, request.Provider, request.ReturnUrl, expiresAt,
            OAuthFlowKind.StepUp, userId, sessionId,
            StepUpPurposeMapping.ToChallengePurpose(request.Purpose));

        await _stateStore.StoreAsync(stepUpState, TimeSpan.FromMinutes(10), cancellationToken);

        return Result<OAuthLoginStartResult>.Success(
            new OAuthLoginStartResult(urlResult.AuthorizationUrl));
    }

    private static string GenerateCryptographicValue()
    {
        var bytes = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string ComputeCodeChallenge(string codeVerifier)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(
            System.Text.Encoding.ASCII.GetBytes(codeVerifier));
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}