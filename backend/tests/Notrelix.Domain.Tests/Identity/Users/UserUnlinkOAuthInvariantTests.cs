using FluentAssertions;
using Notrelix.Domain.Identity;
using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.Domain.Tests.Identity.Users;

public class UserUnlinkOAuthInvariantTests
{
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static User CreateUser(bool hasPasswordCredential = true)
    {
        return User.Create("test@example.com", "Test User", "hash", Now, hasPasswordCredential);
    }

    private static OAuthProfileSnapshot CreateSnapshot(OAuthProvider provider = OAuthProvider.Google)
    {
        return OAuthProfileSnapshot.Create(provider, 1, JsonValue.EmptyObject());
    }

    private static void Link(User user, OAuthProvider provider, string providerId)
    {
        user.LinkOAuthAccount(provider, providerId, CreateSnapshot(provider), null, ActorId, Now);
    }

    [Fact]
    public void Create_WithHasPasswordCredentialTrue_ShouldSetFlag()
    {
        var user = CreateUser(hasPasswordCredential: true);

        user.HasPasswordCredential.Should().BeTrue();
    }

    [Fact]
    public void Create_WithHasPasswordCredentialFalse_ShouldSetFlag()
    {
        var user = CreateUser(hasPasswordCredential: false);

        user.HasPasswordCredential.Should().BeFalse();
    }

    [Fact]
    public void UpdatePassword_OnOAuthOnlyUser_ShouldSetHasPasswordCredentialTrue()
    {
        var user = CreateUser(hasPasswordCredential: false);
        ((IHasDomainEvents)user).ClearDomainEvents();

        user.UpdatePassword("newhash", ActorId, Now);

        user.HasPasswordCredential.Should().BeTrue();
    }

    [Fact]
    public void UnlinkOAuthAccount_WhenPasswordAndOneOAuth_ShouldUnlink()
    {
        var user = CreateUser(hasPasswordCredential: true);
        Link(user, OAuthProvider.Google, "pid-google");
        ((IHasDomainEvents)user).ClearDomainEvents();
        var versionBefore = user.Version;

        user.UnlinkOAuthAccount(OAuthProvider.Google, ActorId, Now);

        user.OAuthAccounts.Should().BeEmpty();
        user.HasPasswordCredential.Should().BeTrue();
        user.Version.Should().Be(versionBefore + 1);
        user.DomainEvents.Should().ContainSingle(e => e is OAuthAccountUnlinkedDomainEvent);
    }

    [Fact]
    public void UnlinkOAuthAccount_WhenOAuthOnlyAndSingleOAuth_ShouldRejectLastPrimaryMethod()
    {
        var user = CreateUser(hasPasswordCredential: false);
        Link(user, OAuthProvider.Google, "pid-google");
        ((IHasDomainEvents)user).ClearDomainEvents();
        var versionBefore = user.Version;

        var act = () => user.UnlinkOAuthAccount(OAuthProvider.Google, ActorId, Now);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(IdentityRuleCodes.Identity_User_LastPrimaryAuthMethod);
        user.OAuthAccounts.Should().ContainSingle(a => a.Provider == OAuthProvider.Google);
        user.Version.Should().Be(versionBefore);
        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UnlinkOAuthAccount_WhenOAuthOnlyAndTwoProviders_ShouldUnlinkOne()
    {
        var user = CreateUser(hasPasswordCredential: false);
        Link(user, OAuthProvider.Google, "pid-google");
        Link(user, OAuthProvider.Microsoft, "pid-microsoft");
        ((IHasDomainEvents)user).ClearDomainEvents();

        user.UnlinkOAuthAccount(OAuthProvider.Google, ActorId, Now);

        user.OAuthAccounts.Should().ContainSingle(a => a.Provider == OAuthProvider.Microsoft);
        user.DomainEvents.Should().ContainSingle(e => e is OAuthAccountUnlinkedDomainEvent);
    }

    [Fact]
    public void UnlinkOAuthAccount_WhenOAuthOnlyThenPasswordSet_ShouldUnlink()
    {
        var user = CreateUser(hasPasswordCredential: false);
        Link(user, OAuthProvider.Google, "pid-google");
        user.UpdatePassword("newhash", ActorId, Now);

        user.UnlinkOAuthAccount(OAuthProvider.Google, ActorId, Now);

        user.OAuthAccounts.Should().BeEmpty();
        user.HasPasswordCredential.Should().BeTrue();
    }
}
