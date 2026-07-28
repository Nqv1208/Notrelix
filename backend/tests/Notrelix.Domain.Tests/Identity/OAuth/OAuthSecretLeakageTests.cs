using FluentAssertions;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Identity.OAuth.Events;

namespace Notrelix.Domain.Tests.Identity.OAuth;

public class OAuthSecretLeakageTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid ActorId = Guid.NewGuid();

    [Fact]
    public void OAuthAccountLinked_ShouldNotContainTokenInEvent()
    {
        var user = User.Create("test@example.com", "Test User", "hash", Now);
        var snapshot = OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, JsonValue.EmptyObject());
        var token = OAuthToken.Create(
            SecretRef.Create("secret-access-token"),
            SecretRef.Create("secret-refresh-token"),
            Now.AddHours(1));
        user.LinkOAuthAccount(OAuthProvider.Google, "id", snapshot, token, ActorId, Now);
        var evt = user.DomainEvents.OfType<OAuthAccountLinkedDomainEvent>().Single();
        typeof(OAuthAccountLinkedDomainEvent).GetProperties()
            .Should().NotContain(p => p.Name.Contains("Token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void OAuthTokenReferenceRotated_ShouldNotContainTokenInEvent()
    {
        var user = User.Create("test@example.com", "Test User", "hash", Now);
        var snapshot = OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, JsonValue.EmptyObject());
        var token = OAuthToken.Create(SecretRef.Create("old-access"));
        user.LinkOAuthAccount(OAuthProvider.Google, "id", snapshot, token, ActorId, Now);
        var newToken = OAuthToken.Create(SecretRef.Create("new-access"));
        user.RotateOAuthToken(OAuthProvider.Google, newToken, ActorId, Now);
        var evt = user.DomainEvents.OfType<OAuthTokenReferenceRotatedDomainEvent>().Single();
        typeof(OAuthTokenReferenceRotatedDomainEvent).GetProperties()
            .Should().NotContain(p => p.Name.Contains("Token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void OAuthProfileUpdated_ShouldNotContainTokenInEvent()
    {
        var user = User.Create("test@example.com", "Test User", "hash", Now);
        var snapshot = OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, JsonValue.EmptyObject());
        user.LinkOAuthAccount(OAuthProvider.Google, "id", snapshot, null, ActorId, Now);
        var newSnapshot = OAuthProfileSnapshot.Create(OAuthProvider.Google, 2, JsonValue.EmptyObject());
        user.UpdateOAuthProfile(OAuthProvider.Google, newSnapshot, ActorId, Now);
        var evt = user.DomainEvents.OfType<OAuthProfileUpdatedDomainEvent>().Single();
        typeof(OAuthProfileUpdatedDomainEvent).GetProperties()
            .Should().NotContain(p => p.Name.Contains("Token", StringComparison.OrdinalIgnoreCase));
    }
}
