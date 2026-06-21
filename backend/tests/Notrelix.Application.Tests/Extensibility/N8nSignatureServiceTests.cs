using Notrelix.Application.Common.Security;

namespace Notrelix.Application.Tests.Extensibility;

public class N8nSignatureServiceTests
{
    [Fact]
    public void Verify_ShouldAcceptValidSignature()
    {
        var service = new N8nSignatureService();
        var payload = """{"executionId":"018f0000-0000-7000-9000-000000000001","status":"delivered"}""";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var secret = "super-secret";
        var signature = service.CreateSignature(payload, timestamp, secret);

        var verified = service.VerifySignature(
            payload,
            timestamp,
            signature,
            secret,
            TimeSpan.FromMinutes(5));

        verified.Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldRejectTamperedPayload()
    {
        var service = new N8nSignatureService();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var secret = "super-secret";
        var signature = service.CreateSignature("""{"status":"delivered"}""", timestamp, secret);

        var verified = service.VerifySignature(
            """{"status":"failed"}""",
            timestamp,
            signature,
            secret,
            TimeSpan.FromMinutes(5));

        verified.Should().BeFalse();
    }
}
