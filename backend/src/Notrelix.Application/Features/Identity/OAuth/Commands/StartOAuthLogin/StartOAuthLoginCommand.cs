using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Application.Common.Requests.Scoping;

namespace Notrelix.Application.Features.Identity.OAuth.Commands.StartOAuthLogin;

public sealed record StartOAuthLoginCommand
    : ICommand<Result<OAuthLoginStartResult>>,
      IAnonymousRequest,
      IGlobalRequest
{
    public required OAuthProvider Provider { get; init; }
    public string? ReturnUrl { get; init; }
}

public sealed class StartOAuthLoginCommandHandler
    : IRequestHandler<StartOAuthLoginCommand, Result<OAuthLoginStartResult>>
{
    private readonly IOAuthOptionsProvider _optionsProvider;
    private readonly IOAuthProviderClient _providerClient;
    private readonly IOAuthStateStore _stateStore;
    private readonly IDateTimeProvider _dateTimeProvider;

    public StartOAuthLoginCommandHandler(
        IOAuthOptionsProvider optionsProvider,
        IOAuthProviderClient providerClient,
        IOAuthStateStore stateStore,
        IDateTimeProvider dateTimeProvider)
    {
        _optionsProvider = optionsProvider;
        _providerClient = providerClient;
        _stateStore = stateStore;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<OAuthLoginStartResult>> Handle(
        StartOAuthLoginCommand request,
        CancellationToken cancellationToken)
    {
        if (!_optionsProvider.IsProviderEnabled(request.Provider))
        {
            return Result<OAuthLoginStartResult>.Failure($"OAuth provider {request.Provider} is not enabled");
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
            state, nonce, codeVerifier, request.Provider, request.ReturnUrl, expiresAt);

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
}
