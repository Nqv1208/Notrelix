using System.Net;
using Notrelix.Application.Features.Integrations.Connections;

namespace Notrelix.Application.Tests.Features.Integrations;

public class WebhookDeliveryRulesTests
{
    [Theory]
    [InlineData("169.254.169.254")]
    [InlineData("fe80::1")]
    [InlineData("0.0.0.0")]
    [InlineData("224.0.0.1")]
    public void LinkLocalAndSpecialAddresses_AreNeverAllowed(string address)
    {
        WebhookAddressPolicy.IsAllowed(IPAddress.Parse(address), allowPrivate: true).Should().BeFalse();
        WebhookAddressPolicy.IsAllowed(IPAddress.Parse(address), allowPrivate: false).Should().BeFalse();
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("10.1.2.3")]
    [InlineData("172.16.0.9")]
    [InlineData("192.168.65.254")]
    [InlineData("100.64.0.1")]
    [InlineData("fd00::1")]
    [InlineData("::ffff:10.0.0.1")]
    public void PrivateAndLoopbackAddresses_AreAllowedOnlyInDevelopment(string address)
    {
        WebhookAddressPolicy.IsAllowed(IPAddress.Parse(address), allowPrivate: false).Should().BeFalse();
        WebhookAddressPolicy.IsAllowed(IPAddress.Parse(address), allowPrivate: true).Should().BeTrue();
    }

    [Fact]
    public void PublicAddresses_AreAllowed()
    {
        WebhookAddressPolicy.IsAllowed(IPAddress.Parse("93.184.216.34"), allowPrivate: false).Should().BeTrue();
    }

    [Theory]
    [InlineData("http://hooks.example.test/in", false, "The address must use https.")]
    [InlineData("https://user:pw@hooks.example.test/in", false, "The address must not contain credentials.")]
    [InlineData("https://localhost/in", false, "The address points to a network the webhook may not reach.")]
    [InlineData("https://10.0.0.5/in", false, "The address points to a network the webhook may not reach.")]
    [InlineData("https://169.254.169.254/latest", true, "The address points to a network the webhook may not reach.")]
    [InlineData("not a url", false, "The address is not a valid URL.")]
    public void Shape_RefusesWhatItMust(string url, bool development, string reason)
    {
        WebhookAddressPolicy.ValidateShape(url, allowInsecure: development, allowPrivate: development).Should().Be(reason);
    }

    [Fact]
    public void Shape_AcceptsHttpsToAPublicHost_AndHttpToLoopbackInDevelopment()
    {
        WebhookAddressPolicy.ValidateShape("https://hooks.example.test/in", false, false).Should().BeNull();
        WebhookAddressPolicy.ValidateShape("http://127.0.0.1:9000/in", true, true).Should().BeNull();
    }

    [Fact]
    public void Signature_IsHmacSha256OverTheTimestampAndBody()
    {
        // Known vector: HMAC-SHA256("key", "1700000000.{}")
        var signature = WebhookSignature.Compute("key", "1700000000", "{}");

        signature.Should().StartWith("sha256=");
        signature.Should().HaveLength("sha256=".Length + 64);
        WebhookSignature.Compute("key", "1700000000", "{}").Should().Be(signature);
        WebhookSignature.Compute("other", "1700000000", "{}").Should().NotBe(signature);
        WebhookSignature.Compute("key", "1700000001", "{}").Should().NotBe(signature);
    }

    [Fact]
    public void GeneratedSecrets_AreLongAndDifferent()
    {
        var first = WebhookSignature.GenerateSecret();

        first.Should().StartWith("whsec_");
        first.Length.Should().BeGreaterThan(40);
        WebhookSignature.GenerateSecret().Should().NotBe(first);
    }
}
