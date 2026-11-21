using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Oidc;
using Notrelix.Application.Features.Identity.Sso;
using Notrelix.Application.Features.Identity.Sso.Commands.StartSsoSignIn;
using Notrelix.Application.Tests.Features.Accounts.Sso;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Identity.Sso;

public class StartSsoSignInTests
{
    private const string Issuer = "https://login.acme.example";
    private const string Callback = "https://api.example/api/v1/auth/sso/callback";

    private readonly AccountSsoTestKit _kit = new();
    private readonly InMemorySsoStateStore _states = new();

    private StartSsoSignInCommandHandler Handler(IOidcDiscoveryClient? discovery = null) =>
        new(_kit.Facts, discovery ?? _kit.Discovery, _states, _kit.Clock, NullLogger<StartSsoSignInCommandHandler>.Instance);

    private void Route(bool verified = true, bool active = true)
    {
        _kit.AddDomain(_kit.AccountId, "acme.com", verified);
        _kit.AddProvider(_kit.AccountId, active);
        _kit.Discovery.Serve(Issuer);
    }

    private Task<Result<SsoStartResult>> StartAsync(string email = "Ann@Acme.com", string? returnUrl = null, StartSsoSignInCommandHandler? handler = null) =>
        (handler ?? Handler()).Handle(new StartSsoSignInCommand { Email = email, ReturnUrl = returnUrl, RedirectUri = Callback }, CancellationToken.None);

    private static Dictionary<string, string> QueryOf(string url) =>
        new Uri(url).Query.TrimStart('?').Split('&').Select(p => p.Split('=', 2))
            .ToDictionary(p => p[0], p => Uri.UnescapeDataString(p[1]));

    [Fact]
    public async Task ARoutableEmail_GetsTheProvidersAuthorizationAddress_WithPkceStateAndNonce_AndAOneTimeStateIsStored()
    {
        Route();

        var result = await StartAsync(returnUrl: "/boards/1");

        result.Succeeded.Should().BeTrue();
        result.Data!.AuthorizationUrl.Should().StartWith(Issuer + "/authorize?");
        var query = QueryOf(result.Data.AuthorizationUrl);
        query["response_type"].Should().Be("code");
        query["client_id"].Should().Be("client-1");
        query["redirect_uri"].Should().Be(Callback);
        query["scope"].Should().Be("openid email profile");
        query["code_challenge_method"].Should().Be("S256");
        query["login_hint"].Should().Be("ann@acme.com");

        var stored = _states.Stored.Should().ContainSingle().Subject;
        stored.State.Should().Be(query["state"]);
        stored.Nonce.Should().Be(query["nonce"]);
        stored.AccountId.Should().Be(_kit.AccountId);
        stored.Email.Should().Be("ann@acme.com");
        stored.Issuer.Should().Be(Issuer);
        stored.RedirectUri.Should().Be(Callback);
        stored.ReturnUrl.Should().Be("/boards/1");
        stored.ExpiresAt.Should().Be(_kit.Clock.UtcNow.AddMinutes(10));
        query["code_challenge"].Should().Be(
            Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(stored.CodeVerifier)))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_'));
        result.Data.AuthorizationUrl.Should().NotContain("s3cret-plain", "the client secret never goes to the browser");
    }

    [Fact]
    public async Task AnUnsafeReturnPath_IsDroppedFromTheStoredState()
    {
        Route();
        await StartAsync(returnUrl: "//evil.example/steal");
        _states.Stored.Single().ReturnUrl.Should().BeNull();
    }

    [Fact]
    public async Task EveryEmailThatCannotSignInThroughAProvider_GetsTheSameRefusal_AndNothingIsStored()
    {
        var refusals = new List<Result<SsoStartResult>>();

        refusals.Add(await StartAsync("x@nobody.example"));                                    // no domain at all

        Route(verified: false);                                                                 // Pending domain
        refusals.Add(await StartAsync());

        var other = new AccountSsoTestKit();
        other.AddDomain(other.AccountId, "acme.com", verified: true);
        other.AddProvider(other.AccountId, active: false);                                      // Draft provider
        refusals.Add(await new StartSsoSignInCommandHandler(other.Facts, other.Discovery, _states, other.Clock, NullLogger<StartSsoSignInCommandHandler>.Instance)
            .Handle(new StartSsoSignInCommand { Email = "a@acme.com", RedirectUri = Callback }, CancellationToken.None));

        var unreachable = new AccountSsoTestKit();                                              // routable, provider unreachable
        unreachable.AddDomain(unreachable.AccountId, "acme.com", verified: true);
        unreachable.AddProvider(unreachable.AccountId);
        refusals.Add(await new StartSsoSignInCommandHandler(unreachable.Facts, unreachable.Discovery.Refuse(Issuer, OidcDiscoveryFailure.AddressRefused), _states, unreachable.Clock, NullLogger<StartSsoSignInCommandHandler>.Instance)
            .Handle(new StartSsoSignInCommand { Email = "a@acme.com", RedirectUri = Callback }, CancellationToken.None));

        refusals.Should().OnlyContain(r => !r.Succeeded);
        refusals.Select(r => r.TypedErrors.Single()).Distinct().Should().ContainSingle()
            .Which.Should().Be(new ApplicationError(SsoErrorCodes.Unavailable, "Company sign-in is not available for this email.", ApplicationErrorType.NotFound));
        _states.Stored.Should().BeEmpty();
    }

    [Fact]
    public void TheValidator_RejectsABadEmailAndAnUnsafeReturnPath()
    {
        var validator = new StartSsoSignInCommandValidator();

        validator.Validate(new StartSsoSignInCommand { Email = "not-an-email", RedirectUri = Callback }).IsValid.Should().BeFalse();
        validator.Validate(new StartSsoSignInCommand { Email = "a@acme.com", ReturnUrl = "https://evil.example", RedirectUri = Callback }).IsValid.Should().BeFalse();
        validator.Validate(new StartSsoSignInCommand { Email = "a@acme.com", ReturnUrl = "/ok", RedirectUri = Callback }).IsValid.Should().BeTrue();
    }
}
