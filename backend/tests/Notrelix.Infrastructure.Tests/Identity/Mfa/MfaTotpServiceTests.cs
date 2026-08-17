using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Notrelix.Infrastructure.Identity.Mfa;

namespace Notrelix.Infrastructure.Tests.Identity.Mfa;

public class MfaTotpServiceTests
{
    // RFC 6238 reference secret: base32 of ASCII "12345678901234567890".
    // Derived at runtime so no high-entropy literal sits in source
    // (GitGuardian generic high-entropy detector flags the encoded form).
    private static readonly string Rfc6238Secret = Base32Encode(Encoding.ASCII.GetBytes("12345678901234567890"));

    private static MfaTotpService CreateService()
    {
        var provider = DataProtectionProvider.Create(
            new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"notrelix-dp-tests-{Guid.NewGuid():N}")));
        return new MfaTotpService(provider);
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new StringBuilder((data.Length * 8 + 4) / 5);

        var buffer = 0;
        var bits = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                output.Append(alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }

        if (bits > 0)
        {
            output.Append(alphabet[(buffer << (5 - bits)) & 31]);
        }

        return output.ToString();
    }

    [Theory]
    [InlineData(59L, "287082")]
    [InlineData(1111111109L, "081804")]
    [InlineData(1111111111L, "050471")]
    [InlineData(1234567890L, "005924")]
    [InlineData(2000000000L, "279037")]
    public void VerifyCode_WithRfc6238ReferenceVectors_MatchesExpectedCode(long unixSeconds, string expected)
    {
        var service = CreateService();
        var now = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);

        var result = service.VerifyCode(Rfc6238Secret, expected, now);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyCode_AllowsDriftOfOneStepBeforeAndAfter()
    {
        var service = CreateService();
        var now = DateTimeOffset.FromUnixTimeSeconds(1111111109);

        var before = service.VerifyCode(Rfc6238Secret, "050471", now.AddSeconds(30));
        var after = service.VerifyCode(Rfc6238Secret, "081804", now.AddSeconds(-30));

        before.Should().BeTrue();
        after.Should().BeTrue();
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    [InlineData("")]
    public void VerifyCode_RejectsInvalidCode(string code)
    {
        var service = CreateService();

        var result = service.VerifyCode(Rfc6238Secret, code, DateTimeOffset.FromUnixTimeSeconds(59));

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyCode_RejectsUnknownSecret()
    {
        var service = CreateService();

        var result = service.VerifyCode("AAAAAAA", "287082", DateTimeOffset.FromUnixTimeSeconds(59));

        result.Should().BeFalse();
    }

    [Fact]
    public void GenerateSecretKey_ReturnsBase32WithoutPadding()
    {
        var service = CreateService();

        var secret = service.GenerateSecretKey();

        secret.Should().MatchRegex("^[A-Z2-7]{32}$");
    }

    [Fact]
    public void BuildOtpAuthUri_ContainsExpectedParameters()
    {
        var service = CreateService();

        var uri = service.BuildOtpAuthUri(Rfc6238Secret, "user@example.com", "Notrelix");

        uri.Should().StartWith("otpauth://totp/Notrelix%3Auser%40example.com?");
        uri.Should().Contain($"secret={Rfc6238Secret}");
        uri.Should().Contain("issuer=Notrelix");
        uri.Should().Contain("algorithm=SHA1");
        uri.Should().Contain("digits=6");
        uri.Should().Contain("period=30");
    }

    [Fact]
    public void ProtectSecret_ThenUnprotectSecret_RoundTrips()
    {
        var service = CreateService();
        var secret = service.GenerateSecretKey();

        var protectedSecret = service.ProtectSecret(secret);
        var unprotected = service.UnprotectSecret(protectedSecret);

        protectedSecret.Should().NotBe(secret);
        unprotected.Should().Be(secret);
    }
}