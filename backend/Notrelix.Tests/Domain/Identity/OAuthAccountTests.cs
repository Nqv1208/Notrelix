using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Identity.OAuth.Events;
using Notrelix.Domain.Identity.Users;
using Xunit;

namespace Notrelix.Domain.Tests.Identity;

public class OAuthAccountTests
{
    private static readonly JsonValue EmptyProfile = JsonValue.EmptyObject();
    private static readonly SecretRef AccessRef = SecretRef.Create("access-token-ref-123");
    private static readonly SecretRef RefreshRef = SecretRef.Create("refresh-token-ref-456");

    [Fact]
    public void Create_WithToken_ShouldStoreOAuthToken()
    {
        var userId = Guid.NewGuid();
        var token = OAuthToken.Create(AccessRef, RefreshRef, DateTimeOffset.UtcNow.AddDays(30));

        var account = OAuthAccount.Create(userId, OAuthProvider.Google, "provider-id-123", EmptyProfile, token);

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

        var account = OAuthAccount.Create(userId, OAuthProvider.GitHub, "github-123", EmptyProfile);

        account.Token.Should().BeNull();
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
        user.ClearDomainEvents();

        var token = OAuthToken.Create(AccessRef, RefreshRef);
        user.LinkOAuthAccount(OAuthProvider.Google, "provider-id-123", EmptyProfile, token, now.AddMinutes(5));

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
    public void User_LinkDuplicateProviderDifferentId_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hash123", now);
        user.LinkOAuthAccount(OAuthProvider.Google, "id-1", EmptyProfile, null, now);

        var act = () => user.LinkOAuthAccount(OAuthProvider.Google, "id-2", EmptyProfile, null, now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*already linked with a different account*");
    }

    [Fact]
    public void User_UnlinkOAuthAccount_ShouldRemoveAccountAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hash123", now);
        user.LinkOAuthAccount(OAuthProvider.Google, "provider-id-123", EmptyProfile, null, now);
        user.ClearDomainEvents();

        user.UnlinkOAuthAccount(OAuthProvider.Google, now.AddMinutes(5));

        user.OAuthAccounts.Should().BeEmpty();
        user.DomainEvents.Should().ContainSingle(e => e is OAuthAccountUnlinkedDomainEvent);
        var evt = (OAuthAccountUnlinkedDomainEvent)user.DomainEvents.Single(e => e is OAuthAccountUnlinkedDomainEvent);
        evt.UserId.Should().Be(user.Id);
        evt.Provider.Should().Be(OAuthProvider.Google);
        evt.ProviderId.Should().Be("provider-id-123");
        evt.UnlinkedAt.Should().Be(now.AddMinutes(5));
    }

    [Fact]
    public void User_RotateOAuthToken_ShouldUpdateTokenAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hash123", now);
        var oldToken = OAuthToken.Create(AccessRef);
        user.LinkOAuthAccount(OAuthProvider.Google, "provider-id-123", EmptyProfile, oldToken, now);
        user.ClearDomainEvents();

        var newToken = OAuthToken.Create(SecretRef.Create("new-access-ref"));
        user.RotateOAuthToken(OAuthProvider.Google, newToken, now.AddMinutes(5));

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
