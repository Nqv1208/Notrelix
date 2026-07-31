using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Integrations.Webhooks;

namespace Notrelix.Domain.Tests.Integrations;

[CoversAggregate(typeof(WebhookSubscription))]
public class WebhookSubscriptionTests
{
    [Fact]
    public void Create_ShouldRaiseEventAndSetProperties()
    {
        var workspaceId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var url = Url.Create("https://example.com/webhook");

        var sub = WebhookSubscription.Create(Guid.NewGuid(), workspaceId, url, createdBy, now);

        sub.WorkspaceId.Should().Be(workspaceId);
        sub.TargetUrl.Should().Be(url);
        sub.IsActive.Should().BeTrue();
        sub.DomainEvents.Should().ContainSingle(e => e is WebhookSubscriptionCreatedDomainEvent);
    }

    [CoversMutation(typeof(WebhookSubscription), nameof(WebhookSubscription.Enable), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void EnableDisable_ShouldModifyIsActive()
    {
        var url = Url.Create("https://example.com/webhook");
        var sub = WebhookSubscription.Create(Guid.NewGuid(), Guid.NewGuid(), url, Guid.NewGuid(), DateTimeOffset.UtcNow);

        sub.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        sub.IsActive.Should().BeFalse();

        sub.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        sub.IsActive.Should().BeTrue();
    }

    [CoversMutation(typeof(WebhookSubscription), nameof(WebhookSubscription.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_ShouldDisableSubscription()
    {
        var url = Url.Create("https://example.com/webhook");
        var sub = WebhookSubscription.Create(Guid.NewGuid(), Guid.NewGuid(), url, Guid.NewGuid(), DateTimeOffset.UtcNow);

        sub.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        sub.IsDeleted.Should().BeTrue();
        sub.IsActive.Should().BeFalse();
    }

    [CoversMutation(typeof(WebhookSubscription), nameof(WebhookSubscription.RotateSecret), MutationScenario.Valid, typeof(WebhookSecretHash), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void RotateSecret_ShouldUpdateHash()
    {
        var sub = WebhookSubscription.Create(Guid.NewGuid(), Guid.NewGuid(), Url.Create("https://example.com/webhook"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var newHash = WebhookSecretHash.Create("sha256=newhash");

        sub.RotateSecret(newHash, Guid.NewGuid(), DateTimeOffset.UtcNow);

        sub.SecretHash.Should().Be(newHash);
    }

    [CoversMutation(typeof(WebhookSubscription), nameof(WebhookSubscription.RotateSecret), MutationScenario.Invalid, typeof(WebhookSecretHash), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void RotateSecret_WhenDeleted_ShouldThrow()
    {
        var sub = WebhookSubscription.Create(Guid.NewGuid(), Guid.NewGuid(), Url.Create("https://example.com/webhook"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        sub.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => sub.RotateSecret(WebhookSecretHash.Create("sha256=x"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [CoversMutation(typeof(WebhookSubscription), nameof(WebhookSubscription.RotateSecret), MutationScenario.Invalid, typeof(WebhookSecretHash), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void RotateSecret_WithNullHash_ShouldThrow()
    {
        var sub = WebhookSubscription.Create(Guid.NewGuid(), Guid.NewGuid(), Url.Create("https://example.com/webhook"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var act = () => sub.RotateSecret(null!, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(WebhookSubscription), nameof(WebhookSubscription.Enable), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Enable_WhenDeleted_ShouldThrow()
    {
        var sub = WebhookSubscription.Create(Guid.NewGuid(), Guid.NewGuid(), Url.Create("https://example.com/webhook"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        sub.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => sub.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [CoversMutation(typeof(WebhookSubscription), nameof(WebhookSubscription.Disable), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Disable_WhenDeleted_ShouldThrow()
    {
        var sub = WebhookSubscription.Create(Guid.NewGuid(), Guid.NewGuid(), Url.Create("https://example.com/webhook"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        sub.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => sub.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }
}
