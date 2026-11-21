using System.Security.Cryptography;
using System.Text;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Oidc;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Accounts.Public.Sso;
using Notrelix.Application.Features.Identity.OAuth;

namespace Notrelix.Application.Features.Identity.Sso.Commands.StartSsoSignIn;

public sealed record SsoStartResult(string AuthorizationUrl);

/// <summary>
/// Starts a company sign-in for an email: routes it to the Account that verified its domain, reads the provider's
/// discovery document and answers the address the browser continues at. <see cref="RedirectUri"/> is this
/// deployment's one callback address, supplied by the endpoint.
/// </summary>
public sealed record StartSsoSignInCommand
    : ICommand<Result<SsoStartResult>>, IAnonymousRequest, IGlobalRequest, IReadRequest
{
    public required string Email { get; init; }
    public string? ReturnUrl { get; init; }
    public required string RedirectUri { get; init; }
}

public sealed class StartSsoSignInCommandHandler : IRequestHandler<StartSsoSignInCommand, Result<SsoStartResult>>
{
    public static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

    private readonly IAccountSsoFacts _facts;
    private readonly IOidcDiscoveryClient _discovery;
    private readonly ISsoStateStore _stateStore;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<StartSsoSignInCommandHandler> _logger;

    public StartSsoSignInCommandHandler(
        IAccountSsoFacts facts,
        IOidcDiscoveryClient discovery,
        ISsoStateStore stateStore,
        IDateTimeProvider clock,
        ILogger<StartSsoSignInCommandHandler> logger)
    {
        _facts = facts;
        _discovery = discovery;
        _stateStore = stateStore;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<SsoStartResult>> Handle(StartSsoSignInCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        // The same refusal for every reason the email cannot sign in through a company provider.
        var route = await _facts.FindRouteAsync(email, cancellationToken);
        var provider = route is null ? null : await _facts.GetProviderAsync(route.AccountId, cancellationToken);
        if (route is null || provider is null)
            return Unavailable();

        // The discovery document is read now, from the configured issuer: no address is taken from a stored or typed value.
        var discovery = await _discovery.DiscoverAsync(provider.Issuer, cancellationToken);
        if (discovery.Document is not { } document)
        {
            _logger.LogWarning("SSO start refused for account {AccountId}: discovery failed ({Reason})", route.AccountId, discovery.Failure);
            return Unavailable();
        }

        var state = RandomValue();
        var nonce = RandomValue();
        var codeVerifier = RandomValue();

        await _stateStore.StoreAsync(
            new SsoLoginState(
                state, route.AccountId, email, nonce, codeVerifier, request.RedirectUri,
                SafeReturnPath.OrNull(request.ReturnUrl), document.Issuer, _clock.UtcNow.Add(StateLifetime)),
            StateLifetime,
            cancellationToken);

        return Result<SsoStartResult>.Success(new SsoStartResult(
            BuildAuthorizationUrl(document.AuthorizationEndpoint, provider.ClientId, request.RedirectUri, state, nonce, codeVerifier, email)));
    }

    private static string BuildAuthorizationUrl(
        string endpoint, string clientId, string redirectUri, string state, string nonce, string codeVerifier, string loginHint)
    {
        var query = string.Join('&', new[]
        {
            ("response_type", "code"),
            ("client_id", clientId),
            ("redirect_uri", redirectUri),
            ("scope", "openid email profile"),
            ("state", state),
            ("nonce", nonce),
            ("code_challenge", Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)))),
            ("code_challenge_method", "S256"),
            ("login_hint", loginHint),
        }.Select(p => $"{p.Item1}={Uri.EscapeDataString(p.Item2)}"));
        return $"{endpoint}{(endpoint.Contains('?') ? '&' : '?')}{query}";
    }

    private static string RandomValue() => Base64Url(RandomNumberGenerator.GetBytes(32));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static Result<SsoStartResult> Unavailable() =>
        Result<SsoStartResult>.Failure(new ApplicationError(
            SsoErrorCodes.Unavailable, "Company sign-in is not available for this email.", ApplicationErrorType.NotFound));
}

public sealed class StartSsoSignInCommandValidator : AbstractValidator<StartSsoSignInCommand>
{
    public StartSsoSignInCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256).EmailAddress();

        RuleFor(x => x.ReturnUrl)
            .Must(SafeReturnPath.IsSafe)
            .When(x => !string.IsNullOrWhiteSpace(x.ReturnUrl))
            .WithMessage("ReturnUrl must be an internal path that starts with a single slash.");
    }
}
