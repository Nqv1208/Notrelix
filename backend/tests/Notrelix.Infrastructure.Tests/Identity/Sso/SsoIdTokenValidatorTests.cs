using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Notrelix.Infrastructure.Identity.OAuth;
using Notrelix.Infrastructure.Identity.Sso;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Infrastructure.Tests.Identity.Sso;

/// <summary>The company sign-in trusts an ID token only on a real RS256/ES256 signature by a key under its kid and the right claims. Real RSA keys throughout.</summary>
public class SsoIdTokenValidatorTests
{
    private const string Issuer = "https://login.acme.example";
    private const string ClientId = "client-1";
    private const string Nonce = "nonce-1";
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 9, 0, 0, TimeSpan.Zero);

    private static FakeOidcProvider Provider() =>
        new(Issuer, ClientId, "secret") { Email = "a@acme.com", Nonce = Nonce, Clock = () => Now };

    private static string Validate(FakeOidcProvider provider, string? token = null)
    {
        using var claims = SsoIdTokenValidator.Validate(
            token ?? provider.BuildIdToken(Now), new JsonWebKeySet(provider.JwksJson()), Issuer, ClientId, Nonce, Now);
        return OidcIdTokenValidator.StringClaim(claims.RootElement, "sub")!;
    }

    private static void Refused(FakeOidcProvider provider, string reason, string? token = null) =>
        FluentActions.Invoking(() => Validate(provider, token)).Should().Throw<OidcIdTokenException>().WithMessage(reason);

    [Fact]
    public void AValidToken_IsAccepted() => Validate(Provider()).Should().Be("subject-1");

    [Fact]
    public void ATokenSignedWithAnotherKey_IsRefused()
    {
        var provider = Provider();
        var forged = Provider();
        forged.Key = RSA.Create(2048);

        Refused(provider, "signature invalid", forged.BuildIdToken(Now));
    }

    [Fact]
    public void ATokenWhosePayloadWasAltered_IsRefused()
    {
        var provider = Provider();
        var parts = provider.BuildIdToken(Now).Split('.');
        var altered = $"{parts[0]}.{B64("{\"iss\":\"" + Issuer + "\",\"aud\":\"client-1\",\"sub\":\"admin\",\"nonce\":\"nonce-1\",\"iat\":" + Now.ToUnixTimeSeconds() + ",\"exp\":" + Now.AddHours(1).ToUnixTimeSeconds() + "}")}.{parts[2]}";

        Refused(provider, "signature invalid", altered);
    }

    [Fact]
    public void AnotherAudience_IsRefused()
    {
        var provider = Provider();
        provider.AudienceClaim = "someone-else";
        Refused(provider, "audience mismatch");
    }

    [Fact]
    public void AnotherIssuer_IsRefused()
    {
        var provider = Provider();
        provider.IssuerClaim = "https://evil.example";
        Refused(provider, "issuer mismatch");
    }

    [Fact]
    public void AnExpiredToken_IsRefused()
    {
        var provider = Provider();
        provider.ExpiresIn = TimeSpan.FromMinutes(-5);
        Refused(provider, "id token expired");
    }

    [Fact]
    public void ATokenAFewSecondsPastItsExpiry_IsStillWithinTheSixtySecondSkew_ButNotTwoMinutes()
    {
        var provider = Provider();
        provider.ExpiresIn = TimeSpan.FromSeconds(-30);
        Validate(provider).Should().Be("subject-1");

        provider.ExpiresIn = TimeSpan.FromSeconds(-90);
        Refused(provider, "id token expired");
    }

    [Fact]
    public void ANonceOtherThanTheSignInsOwn_IsRefused()
    {
        var provider = Provider();
        provider.NonceClaim = "replayed-nonce";
        Refused(provider, "nonce mismatch");
    }

    [Fact]
    public void SeveralAudiencesWithoutOurAuthorizedParty_IsRefused()
    {
        var provider = Provider();
        provider.AudienceClaim = new[] { ClientId, "other" };
        Refused(provider, "authorized party mismatch");

        provider.AuthorizedParty = ClientId;
        Validate(provider).Should().Be("subject-1");
    }

    [Theory]
    [InlineData("none")]
    [InlineData("HS256")]
    public void AnUnsignedOrSharedSecretToken_IsRefusedBeforeAnyKeyIsTried(string algorithm)
    {
        var provider = Provider();
        provider.Algorithm = algorithm;
        Refused(provider, "signature algorithm not allowed");
    }

    [Fact]
    public void ATokenWithoutAKeyId_IsRefused()
    {
        var provider = Provider();
        provider.IncludeKeyId = false;
        Refused(provider, "key id missing");
    }

    [Fact]
    public void ATokenNamingAKeyTheProviderDoesNotPublish_IsRefused()
    {
        var provider = Provider();
        var token = provider.BuildIdToken(Now);
        provider.KeyId = "rotated-away";

        Refused(provider, "signing key unknown", token);
    }

    [Fact]
    public void AMalformedToken_IsRefused() =>
        FluentActions.Invoking(() => SsoIdTokenValidator.RequireKeyId("not-a-jwt")).Should().Throw<OidcIdTokenException>();

    private static string B64(string text) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
