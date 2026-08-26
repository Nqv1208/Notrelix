using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;

namespace Notrelix.Application.Features.Identity.OAuth.Commands.StartOAuthLink;

public sealed record StartOAuthLinkCommand
    : ICommand<Result<OAuthLoginStartResult>>,
      IAuthenticatedRequest, INoDataRequest,
      IGlobalRequest
{
    public required OAuthProvider Provider { get; init; }
    public string? ReturnUrl { get; init; }

    /// <summary>Single-use step-up proof for the LinkOAuth purpose (TOTP, recovery code, password or OAuth re-authentication).</summary>
    public required string StepUpToken { get; init; }
}

public sealed class StartOAuthLinkCommandHandler
    : IRequestHandler<StartOAuthLinkCommand, Result<OAuthLoginStartResult>>
{
    private readonly IOAuthOptionsProvider _optionsProvider;
    private readonly IOAuthProviderClient _providerClient;
    private readonly IOAuthStateStore _stateStore;
    private readonly ICurrentRequestContext _currentUser;
    private readonly ISecurityStepUpService _stepUpService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public StartOAuthLinkCommandHandler(
        IOAuthOptionsProvider optionsProvider,
        IOAuthProviderClient providerClient,
        IOAuthStateStore stateStore,
        ICurrentRequestContext currentUser,
        ISecurityStepUpService stepUpService,
        IDateTimeProvider dateTimeProvider)
    {
        _optionsProvider = optionsProvider;
        _providerClient = providerClient;
        _stateStore = stateStore;
        _currentUser = currentUser;
        _stepUpService = stepUpService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<OAuthLoginStartResult>> Handle(
        StartOAuthLinkCommand request,
        CancellationToken cancellationToken)
    {
        if (!_optionsProvider.IsProviderEnabled(request.Provider))
        {
            return Result<OAuthLoginStartResult>.Failure($"OAuth provider {request.Provider} is not enabled");
        }

        var stepUp = await ConsumeStepUpAsync(_currentUser.UserId, request.StepUpToken, cancellationToken);
        if (!stepUp.Succeeded)
        {
            return Result<OAuthLoginStartResult>.Failure(stepUp.TypedErrors);
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
        var loginState = new OAuthLoginState(
            state, nonce, codeVerifier, request.Provider, request.ReturnUrl, expiresAt,
            OAuthFlowKind.Link, _currentUser.UserId);

        await _stateStore.StoreAsync(loginState, TimeSpan.FromMinutes(10), cancellationToken);

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
            stepUpToken, userId, sessionId, StepUpPurpose.LinkOAuth, cancellationToken);
    }
}
