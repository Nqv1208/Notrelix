using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Integrations.Webhooks;
using Xunit;

namespace Notrelix.Domain.Tests.Integrations;

public class WebhookSecretHashTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var hash = WebhookSecretHash.Create("sha256=abc123");
        hash.Hash.Should().Be("sha256=abc123");
    }

    [Fact]
    public void Create_WithEmpty_ShouldThrow()
    {
        var act = () => WebhookSecretHash.Create("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Equality_SameHash_ShouldBeEqual()
    {
        var h1 = WebhookSecretHash.Create("hash1");
        var h2 = WebhookSecretHash.Create("hash1");
        h1.Should().Be(h2);
    }

    [Fact]
    public void Equality_DifferentHash_ShouldNotBeEqual()
    {
        var h1 = WebhookSecretHash.Create("hash1");
        var h2 = WebhookSecretHash.Create("hash2");
        h1.Should().NotBe(h2);
    }
}
