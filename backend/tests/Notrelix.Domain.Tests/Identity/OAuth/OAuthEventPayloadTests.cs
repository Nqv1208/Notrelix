using FluentAssertions;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Identity.OAuth.Events;

namespace Notrelix.Domain.Tests.Identity.OAuth;

public class OAuthEventPayloadTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid ActorId = Guid.NewGuid();

    private static OAuthProfileSnapshot CreateSnapshot(OAuthProvider provider = OAuthProvider.Google)
        => OAuthProfileSnapshot.Create(provider, 1, JsonValue.EmptyObject());

    private static OAuthToken CreateToken()
        => OAuthToken.Create(SecretRef.Create("access-hash"), SecretRef.Create("refresh-hash"), Now.AddHours(1));

    [Fact]
    public void OAuthAccountLinked_ShouldContainProviderAndProviderId()
    {
        var user = User.Create("test@example.com", "Test User", "hash", Now);
        user.LinkOAuthAccount(OAuthProvider.Google, "provider-123", CreateSnapshot(), CreateToken(), ActorId, Now);
        var evt = user.DomainEvents.OfType<OAuthAccountLinkedDomainEvent>().Single();
        evt.Provider.Should().Be(OAuthProvider.Google);
        evt.ProviderId.Should().Be("provider-123");
    }

    [Fact]
    public void OAuthProfileUpdated_ShouldContainProvider()
    {
        var user = User.Create("test@example.com", "Test User", "hash", Now);
        user.LinkOAuthAccount(OAuthProvider.Google, "id", CreateSnapshot(), null, ActorId, Now);
        var newSnapshot = OAuthProfileSnapshot.Create(OAuthProvider.Google, 2, JsonValue.EmptyObject());
        user.UpdateOAuthProfile(OAuthProvider.Google, newSnapshot, ActorId, Now);
        var evt = user.DomainEvents.OfType<OAuthProfileUpdatedDomainEvent>().Single();
        evt.Provider.Should().Be(OAuthProvider.Google);
    }

    [Fact]
    public void OAuthTokenReferenceRotated_ShouldContainProvider()
    {
        var user = User.Create("test@example.com", "Test User", "hash", Now);
        user.LinkOAuthAccount(OAuthProvider.Google, "id", CreateSnapshot(), CreateToken(), ActorId, Now);
        var newToken = OAuthToken.Create(SecretRef.Create("new-access-hash"));
        user.RotateOAuthToken(OAuthProvider.Google, newToken, ActorId, Now);
        var evt = user.DomainEvents.OfType<OAuthTokenReferenceRotatedDomainEvent>().Single();
        evt.Provider.Should().Be(OAuthProvider.Google);
    }

    [Fact]
    public void OAuthAccountUnlinked_ShouldContainProviderAndProviderId()
    {
        var user = User.Create("test@example.com", "Test User", "hash", Now);
        user.LinkOAuthAccount(OAuthProvider.Google, "provider-123", CreateSnapshot(), null, ActorId, Now);
        user.UnlinkOAuthAccount(OAuthProvider.Google, ActorId, Now);
        var evt = user.DomainEvents.OfType<OAuthAccountUnlinkedDomainEvent>().Single();
        evt.Provider.Should().Be(OAuthProvider.Google);
        evt.ProviderId.Should().Be("provider-123");
    }
}
