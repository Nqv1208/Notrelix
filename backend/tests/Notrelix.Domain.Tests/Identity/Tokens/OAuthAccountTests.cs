using FluentAssertions;
using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.Domain.Tests.Identity;

public class OAuthAccountTests
{
    private static readonly OAuthProfileSnapshot TestSnapshot =
        OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, JsonValue.EmptyObject());
    private static readonly SecretRef AccessRef = SecretRef.Create("access-token-ref-123");
    private static readonly SecretRef RefreshRef = SecretRef.Create("refresh-token-ref-456");

    [Fact]
    public void Create_WithToken_ShouldStoreOAuthToken()
    {
        var userId = Guid.NewGuid();
        var token = OAuthToken.Create(AccessRef, RefreshRef, DateTimeOffset.UtcNow.AddDays(30));

        var account = OAuthAccount.Create(userId, OAuthProvider.Google, "provider-id-123", TestSnapshot, token);

        account.Provider.Should().Be(OAuthProvider.Google);
        account.ProviderId.Should().Be("provider-id-123");
        account.Token.Should().NotBeNull();
        account.Token!.AccessTokenRef.Value.Should().Be("access-token-ref-123");
        account.Token.RefreshTokenRef!.Value.Should().Be("refresh-token-ref-456");
    }

    [Fact]
    public void Create_WithoutToken_ShouldStoreNullToken()
    {
        var userId = Guid.NewGuid();

        var account = OAuthAccount.Create(userId, OAuthProvider.GitHub, "github-123", TestSnapshot);

        account.Token.Should().BeNull();
    }

    [Fact]
    public void Account_ShouldStoreProfileSnapshot()
    {
        var userId = Guid.NewGuid();

        var account = OAuthAccount.Create(userId, OAuthProvider.Google, "provider-id-123", TestSnapshot);

        account.ProfileSnapshot.Should().Be(TestSnapshot);
        account.ProfileSnapshot.Provider.Should().Be(OAuthProvider.Google);
        account.ProfileSnapshot.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public void Account_ShouldNotExposeRawTokenProperties()
    {
        var accountType = typeof(OAuthAccount);

        var accessTokenProp = accountType.GetProperty("AccessToken");
        accessTokenProp.Should().BeNull("AccessToken should be wrapped in OAuthToken VO");

        var refreshTokenProp = accountType.GetProperty("RefreshToken");
        refreshTokenProp.Should().BeNull("RefreshToken should be wrapped in OAuthToken VO");

        var tokenExpiresAtProp = accountType.GetProperty("TokenExpiresAt");
        tokenExpiresAtProp.Should().BeNull("TokenExpiresAt should be inside OAuthToken VO");
    }

    [Fact]
    public void User_LinkOAuthAccount_ShouldAddAccountAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hash123", now);
        ((IHasDomainEvents)user).ClearDomainEvents();

        var token = OAuthToken.Create(AccessRef, RefreshRef);
        user.LinkOAuthAccount(OAuthProvider.Google, "provider-id-123", TestSnapshot, token, user.Id, now.AddMinutes(5));

        user.OAuthAccounts.Should().ContainSingle();
        var account = user.OAuthAccounts.Single();
        account.Provider.Should().Be(OAuthProvider.Google);
        account.ProviderId.Should().Be("provider-id-123");
        account.Token.Should().Be(token);

        user.DomainEvents.Should().ContainSingle(e => e is OAuthAccountLinkedDomainEvent);
        var evt = (OAuthAccountLinkedDomainEvent)user.DomainEvents.Single(e => e is OAuthAccountLinkedDomainEvent);
        evt.UserId.Should().Be(user.Id);
        evt.Provider.Should().Be(OAuthProvider.Google);
        evt.ProviderId.Should().Be("provider-id-123");
        evt.LinkedAt.Should().Be(now.AddMinutes(5));
    }

    [Fact]
    public void User_LinkDuplicateProvider_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hash123", now);
        user.LinkOAuthAccount(OAuthProvider.Google, "id-1", TestSnapshot, null, user.Id, now);

        var act = () => user.LinkOAuthAccount(OAuthProvider.Google, "id-2", TestSnapshot, null, user.Id, now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*already linked*");
    }

    [Fact]
    public void User_UnlinkOAuthAccount_ShouldRemoveAccountAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hash123", now);
        user.LinkOAuthAccount(OAuthProvider.Google, "provider-id-123", TestSnapshot, null, user.Id, now);
        ((IHasDomainEvents)user).ClearDomainEvents();

        user.UnlinkOAuthAccount(OAuthProvider.Google, user.Id, now.AddMinutes(5));

        user.OAuthAccounts.Should().BeEmpty();
        user.DomainEvents.Should().ContainSingle(e => e is OAuthAccountUnlinkedDomainEvent);
        var evt = (OAuthAccountUnlinkedDomainEvent)user.DomainEvents.Single(e => e is OAuthAccountUnlinkedDomainEvent);
        evt.UserId.Should().Be(user.Id);
        evt.Provider.Should().Be(OAuthProvider.Google);
        evt.ProviderId.Should().Be("provider-id-123");
        evt.UnlinkedBy.Should().Be(user.Id);
        evt.UnlinkedAt.Should().Be(now.AddMinutes(5));
    }

    [Fact]
    public void User_RotateOAuthToken_ShouldUpdateTokenAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hash123", now);
        var oldToken = OAuthToken.Create(AccessRef);
        user.LinkOAuthAccount(OAuthProvider.Google, "provider-id-123", TestSnapshot, oldToken, user.Id, now);
        ((IHasDomainEvents)user).ClearDomainEvents();

        var newToken = OAuthToken.Create(SecretRef.Create("new-access-ref"));
        user.RotateOAuthToken(OAuthProvider.Google, newToken, user.Id, now.AddMinutes(5));

        user.OAuthAccounts.Single().Token.Should().Be(newToken);
        user.DomainEvents.Should().ContainSingle(e => e is OAuthTokenReferenceRotatedDomainEvent);
        var evt = (OAuthTokenReferenceRotatedDomainEvent)user.DomainEvents.Single(e => e is OAuthTokenReferenceRotatedDomainEvent);
        evt.UserId.Should().Be(user.Id);
        evt.Provider.Should().Be(OAuthProvider.Google);
        evt.RotatedAt.Should().Be(now.AddMinutes(5));
    }

    [Fact]
    public void OAuthToken_ShouldBeImmutableValueObject()
    {
        var token1 = OAuthToken.Create(AccessRef, RefreshRef);
        var token2 = OAuthToken.Create(AccessRef, RefreshRef);
        var token3 = OAuthToken.Create(AccessRef, null);

        token1.Should().Be(token2);
        token1.Should().NotBe(token3);
        token1.GetHashCode().Should().Be(token2.GetHashCode());
    }
}
