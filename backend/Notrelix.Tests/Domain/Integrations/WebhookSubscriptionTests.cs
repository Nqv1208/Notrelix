using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Integrations.Webhooks;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.Integrations;

public class WebhookSubscriptionTests
{
    [Fact]
    public void Create_ShouldRaiseEventAndSetProperties()
    {
        var workspaceId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var url = Url.Create("https://example.com/webhook");

        var sub = WebhookSubscription.Create(workspaceId, url, createdBy, now);

        sub.WorkspaceId.Should().Be(workspaceId);
        sub.TargetUrl.Should().Be(url);
        sub.IsActive.Should().BeTrue();
        sub.DomainEvents.Should().ContainSingle(e => e is WebhookSubscriptionCreatedEvent);
    }

    [Fact]
    public void EnableDisable_ShouldModifyIsActive()
    {
        var url = Url.Create("https://example.com/webhook");
        var sub = WebhookSubscription.Create(Guid.NewGuid(), url, Guid.NewGuid(), DateTimeOffset.UtcNow);

        sub.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        sub.IsActive.Should().BeFalse();

        sub.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        sub.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_ShouldDisableSubscription()
    {
        var url = Url.Create("https://example.com/webhook");
        var sub = WebhookSubscription.Create(Guid.NewGuid(), url, Guid.NewGuid(), DateTimeOffset.UtcNow);

        sub.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        sub.IsDeleted.Should().BeTrue();
        sub.IsActive.Should().BeFalse();
    }
}
