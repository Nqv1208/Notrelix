using FluentAssertions;

namespace Notrelix.Domain.Tests.Identity;

public class SsoProviderTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ShouldSetPropertiesAndRaiseEvent()
    {
        var provider = SsoProvider.Create(WorkspaceId, SsoProviderType.Oidc, "Azure SSO", UserId, Now);

        provider.WorkspaceId.Should().Be(WorkspaceId);
        provider.ProviderType.Should().Be(SsoProviderType.Oidc);
        provider.Name.Should().Be("Azure SSO");
        provider.Status.Should().Be(SsoProviderStatus.Draft);
        provider.DomainEvents.Should().ContainSingle(e => e is SsoProviderCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithEmptyWorkspaceId_ShouldThrow()
    {
        var act = () => SsoProvider.Create(Guid.Empty, SsoProviderType.Oidc, "SSO", UserId, Now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => SsoProvider.Create(WorkspaceId, SsoProviderType.Oidc, "", UserId, Now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Enable_ShouldTransitionToEnabled()
    {
        var provider = SsoProvider.Create(WorkspaceId, SsoProviderType.Oidc, "Azure SSO", UserId, Now);
        provider.ClearDomainEvents();

        provider.Enable(UserId, Now);

        provider.Status.Should().Be(SsoProviderStatus.Enabled);
        provider.DomainEvents.Should().ContainSingle(e => e is SsoProviderEnabledDomainEvent);
    }

    [Fact]
    public void Disable_ShouldTransitionToDisabled()
    {
        var provider = SsoProvider.Create(WorkspaceId, SsoProviderType.Oidc, "Azure SSO", UserId, Now);
        provider.Enable(UserId, Now);
        provider.ClearDomainEvents();

        provider.Disable(UserId, Now);

        provider.Status.Should().Be(SsoProviderStatus.Disabled);
        provider.DomainEvents.Should().ContainSingle(e => e is SsoProviderDisabledDomainEvent);
    }

    [Fact]
    public void Disable_AlreadyDisabled_ShouldBeIdempotent()
    {
        var provider = SsoProvider.Create(WorkspaceId, SsoProviderType.Oidc, "Azure SSO", UserId, Now);
        provider.Disable(UserId, Now);
        provider.ClearDomainEvents();

        provider.Disable(UserId, Now);

        provider.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void SoftDelete_ShouldMarkAsDeleted()
    {
        var provider = SsoProvider.Create(WorkspaceId, SsoProviderType.Oidc, "Azure SSO", UserId, Now);

        provider.SoftDelete(UserId, Now);

        provider.IsDeleted.Should().BeTrue();
        provider.DomainEvents.Should().Contain(e => e is SsoProviderSoftDeletedDomainEvent);
    }

    [Fact]
    public void Restore_AfterSoftDelete_ShouldSucceed()
    {
        var provider = SsoProvider.Create(WorkspaceId, SsoProviderType.Oidc, "Azure SSO", UserId, Now);
        provider.SoftDelete(UserId, Now);
        provider.ClearDomainEvents();

        provider.Restore(UserId, Now);

        provider.IsDeleted.Should().BeFalse();
        provider.DomainEvents.Should().Contain(e => e is SsoProviderRestoredDomainEvent);
    }
}
