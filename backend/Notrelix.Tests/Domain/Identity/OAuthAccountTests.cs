using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.Identity;

public class OAuthAccountTests
{
    private static readonly JsonValue EmptyProfile = JsonValue.EmptyObject();

    [Fact]
    public void Create_WithToken_ShouldStoreOAuthToken()
    {
        var userId = Guid.NewGuid();
        var token = OAuthToken.Create("access123", "refresh456", DateTimeOffset.UtcNow.AddDays(30));

        var account = OAuthAccount.Create(userId, OAuthProvider.Google, "provider-id-123", EmptyProfile, token);

        account.Provider.Should().Be(OAuthProvider.Google);
        account.ProviderId.Should().Be("provider-id-123");
        account.Token.Should().NotBeNull();
        account.Token!.AccessTokenRef.Should().Be("access123");
        account.Token.RefreshTokenRef.Should().Be("refresh456");
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
    public void Link_ShouldRaiseLinkedEvent()
    {
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var account = OAuthAccount.Create(userId, OAuthProvider.Google, "provider-id", EmptyProfile);
        var token = OAuthToken.Create("access", "refresh");

        account.Link(token, now);

        account.Token.Should().Be(token);
        account.DomainEvents.Should().ContainSingle(e => e is OAuthAccountLinkedEvent);
        var evt = (OAuthAccountLinkedEvent)account.DomainEvents.First(e => e is OAuthAccountLinkedEvent);
        evt.UserId.Should().Be(userId);
        evt.Provider.Should().Be(OAuthProvider.Google);
    }

    [Fact]
    public void Unlink_ShouldClearTokenAndRaiseEvent()
    {
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var token = OAuthToken.Create("access", "refresh");
        var account = OAuthAccount.Create(userId, OAuthProvider.Apple, "apple-id", EmptyProfile, token);
        account.ClearDomainEvents();

        account.Unlink(now);

        account.Token.Should().BeNull();
        account.DomainEvents.Should().ContainSingle(e => e is OAuthAccountUnlinkedEvent);
    }

    [Fact]
    public void OAuthToken_ShouldBeImmutableValueObject()
    {
        var token1 = OAuthToken.Create("token-a", "refresh-a");
        var token2 = OAuthToken.Create("token-a", "refresh-a");
        var token3 = OAuthToken.Create("token-b", "refresh-b");

        token1.Should().Be(token2);
        token1.Should().NotBe(token3);
        token1.GetHashCode().Should().Be(token2.GetHashCode());
    }

    [Fact]
    public void OAuthToken_EqualTokens_ShouldBeEqual()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);
        var token1 = OAuthToken.Create("access123", "refresh456", expiresAt);
        var token2 = OAuthToken.Create("access123", "refresh456", expiresAt);

        token1.Should().Be(token2);
    }
}
