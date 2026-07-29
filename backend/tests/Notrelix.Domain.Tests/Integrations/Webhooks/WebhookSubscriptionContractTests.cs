using FluentAssertions;
using Notrelix.Domain.Integrations.Webhooks;
using Notrelix.Domain.SharedKernel;
using Xunit;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Integrations.Webhooks;

public class WebhookSubscriptionContractTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static WebhookSubscription CreateActive()
    {
        var url = Url.Create("https://example.com/hook");
        return WebhookSubscription.Create(AccountId, WorkspaceId, url, Actor, Now);
    }

    [Fact]
    public void Create_ShouldSetActive()
    {
        var url = Url.Create("https://example.com/hook");
        var sub = WebhookSubscription.Create(AccountId, WorkspaceId, url, Actor, Now);
        sub.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldRaiseEvent()
    {
        var url = Url.Create("https://example.com/hook");
        var sub = WebhookSubscription.Create(AccountId, WorkspaceId, url, Actor, Now);
        sub.DomainEvents.Should().ContainSingle(e => e is WebhookSubscriptionCreatedDomainEvent);
    }

    [Fact]
    public void Enable_ShouldActivate()
    {
        var sub = CreateActive();
        sub.Disable(Actor, Now);
        sub.Enable(Actor, Now);
        sub.IsActive.Should().BeTrue();
    }

[CoversMutation(typeof(WebhookSubscription), "Enable(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Enable_NoOp_ShouldNotRaiseEvent()
    {
        var sub = CreateActive();
        sub.Enable(Actor, Now);
        sub.DomainEvents.Should().ContainSingle(e => e is WebhookSubscriptionCreatedDomainEvent);
    }

    [Fact]
    public void Disable_ShouldDeactivate()
    {
        var sub = CreateActive();
        sub.Disable(Actor, Now);
        sub.IsActive.Should().BeFalse();
    }

[CoversMutation(typeof(WebhookSubscription), "Disable(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Disable_NoOp_ShouldNotRaiseEvent()
    {
        var sub = CreateActive();
        sub.Disable(Actor, Now);
        ((IHasDomainEvents)sub).ClearDomainEvents();
        sub.Disable(Actor, Now);
        sub.DomainEvents.Should().BeEmpty();
    }

[CoversMutation(typeof(WebhookSubscription), "RotateSecret(Notrelix.Domain.Integrations.Webhooks.WebhookSecretHash,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void RotateSecret_ShouldUpdateHash()
    {
        var sub = CreateActive();
        var hash = WebhookSecretHash.Create("newhash");
        sub.RotateSecret(hash, Actor, Now);
        sub.SecretHash.Should().Be(hash);
    }

[CoversMutation(typeof(WebhookSubscription), "RotateSecret(Notrelix.Domain.Integrations.Webhooks.WebhookSecretHash,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void RotateSecret_WithNullHash_ShouldThrow()
    {
        var sub = CreateActive();
        var act = () => sub.RotateSecret(null!, Actor, Now);
        act.Should().Throw<BusinessRuleException>();
    }

[CoversMutation(typeof(WebhookSubscription), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SoftDelete_ShouldDeactivate()
    {
        var sub = CreateActive();
        sub.SoftDelete(Actor, Now);
        sub.IsDeleted.Should().BeTrue();
        sub.IsActive.Should().BeFalse();
    }

[CoversMutation(typeof(WebhookSubscription), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.NoOp)]
    [Fact]
    public void SoftDelete_NoOp_ShouldNotChangeState()
    {
        var sub = CreateActive();
        sub.SoftDelete(Actor, Now);
        var before = sub.Version;
        sub.SoftDelete(Actor, Now);
        sub.Version.Should().Be(before);
    }
}
