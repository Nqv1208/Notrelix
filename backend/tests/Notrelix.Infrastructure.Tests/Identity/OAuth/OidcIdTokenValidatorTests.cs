using System.Text;
using System.Text.Json;
using Notrelix.Infrastructure.Identity.OAuth;

namespace Notrelix.Infrastructure.Tests.Identity.OAuth;

public class OidcIdTokenValidatorTests
{
    private const string ClientId = "client-1";
    private const string Issuer = "https://accounts.google.com";
    private const string Nonce = "nonce-1";
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly string[] Issuers = [Issuer];

    [Fact]
    public void AValidToken_IsAccepted()
    {
        using var claims = Validate(Token());

        OidcIdTokenValidator.StringClaim(claims.RootElement, "sub").Should().Be("subject-1");
    }

    [Fact]
    public void AnotherIssuer_IsRefused() =>
        Refused(Token(iss: "https://evil.example"), "issuer mismatch");

    [Fact]
    public void AnotherAudience_IsRefused() =>
        Refused(Token(aud: "someone-else"), "audience mismatch");

    [Fact]
    public void ANonceOtherThanTheSignInsOwn_IsRefused() =>
        Refused(Token(nonce: "replayed-nonce"), "nonce mismatch");

    [Fact]
    public void AMissingNonce_IsRefused() =>
        Refused(Token(nonce: null), "nonce missing");

    [Fact]
    public void SeveralAudiencesWithAnotherAuthorizedParty_IsRefused() =>
        Refused(Token(aud: new[] { ClientId, "other" }, azp: "other"), "authorized party mismatch");

    [Fact]
    public void SeveralAudiencesWithoutAnAuthorizedParty_IsRefused() =>
        Refused(Token(aud: new[] { ClientId, "other" }), "authorized party mismatch");

    [Fact]
    public void SeveralAudiencesWithOurOwnAuthorizedParty_IsAccepted()
    {
        using var claims = Validate(Token(aud: new[] { ClientId, "other" }, azp: ClientId));

        claims.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void AnExpiredToken_IsRefused() =>
        Refused(Token(exp: Now.AddMinutes(-5)), "id token expired");

    private static void Refused(string token, string reason)
    {
        var act = () => Validate(token);

        act.Should().Throw<OidcIdTokenException>().WithMessage(reason);
    }

    private static JsonDocument Validate(string token) =>
        OidcIdTokenValidator.Validate(token, Issuers, ClientId, Nonce, Now);

    private static string Token(
        string iss = Issuer,
        object? aud = null,
        string? azp = null,
        string? nonce = Nonce,
        DateTimeOffset? exp = null)
    {
        var claims = new Dictionary<string, object?>
        {
            ["iss"] = iss,
            ["aud"] = aud ?? ClientId,
            ["sub"] = "subject-1",
            ["iat"] = Now.AddMinutes(-1).ToUnixTimeSeconds(),
            ["exp"] = (exp ?? Now.AddMinutes(5)).ToUnixTimeSeconds(),
        };
        if (azp is not null) claims["azp"] = azp;
        if (nonce is not null) claims["nonce"] = nonce;

        return $"{B64("{\"alg\":\"none\"}")}.{B64(JsonSerializer.Serialize(claims))}.sig";
    }

    private static string B64(string json) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
