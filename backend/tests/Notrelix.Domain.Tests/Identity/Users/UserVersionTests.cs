using FluentAssertions;
using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.Domain.Tests.Identity;

public class UserVersionTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;
    private static readonly OAuthProfileSnapshot TestSnapshot =
        OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, JsonValue.EmptyObject());

    [Fact]
    public void UpdateProfile_ShouldIncrementVersion_AndEmitEvent()
    {
        var user = User.Create("test@test.com", "Test", "hash", _now);
        var version = user.Version;

        user.UpdateProfile("New Name", null, user.Id, _now);

        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().Contain(e => e is UserProfileUpdatedDomainEvent);
    }

    [Fact]
    public void UpdateEmail_ShouldIncrementVersion()
    {
        var user = User.Create("old@test.com", "Test", "hash", _now);
        var version = user.Version;

        user.UpdateEmail("new@test.com", user.Id, _now);

        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().Contain(e => e is UserEmailChangedDomainEvent);
    }

    [Fact]
    public void UpdatePassword_ShouldIncrementVersion()
    {
        var user = User.Create("test@test.com", "Test", "hash", _now);
        var version = user.Version;

        user.UpdatePassword("newhash", user.Id, _now);

        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().Contain(e => e is UserPasswordChangedDomainEvent);
    }

    [Fact]
    public void RecordLogin_ShouldIncrementVersion_AndSetAudit()
    {
        var user = User.Create("test@test.com", "Test", "hash", _now);
        var version = user.Version;

        user.RecordLogin(_now);

        user.Version.Should().Be(version + 1);
        user.LastLoginAt.Should().Be(_now);
        user.DomainEvents.Should().Contain(e => e is UserLoggedInDomainEvent);
    }

    [Fact]
    public void Activate_ShouldIncrementVersion()
    {
        var user = User.Create("test@test.com", "Test", "hash", _now);
        user.Deactivate(_actorId, _now);
        var version = user.Version;

        user.Activate(_actorId, _now);

        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().Contain(e => e is UserActivatedDomainEvent);
    }

    [Fact]
    public void Deactivate_ShouldIncrementVersion()
    {
        var user = User.Create("test@test.com", "Test", "hash", _now);
        var version = user.Version;

        user.Deactivate(_actorId, _now);

        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().Contain(e => e is UserDeactivatedDomainEvent);
    }

    [Fact]
    public void Suspend_ShouldIncrementVersion()
    {
        var user = User.Create("test@test.com", "Test", "hash", _now);
        var version = user.Version;

        user.Suspend(_actorId, _now);

        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().Contain(e => e is UserSuspendedDomainEvent);
    }

    [Fact]
    public void LinkOAuthAccount_ShouldIncrementVersion()
    {
        var user = User.Create("test@test.com", "Test", "hash", _now);
        var version = user.Version;

        user.LinkOAuthAccount(OAuthProvider.Google, "pid123", TestSnapshot, null, user.Id, _now);

        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().Contain(e => e is OAuthAccountLinkedDomainEvent);
    }

    [Fact]
    public void UnlinkOAuthAccount_ShouldIncrementVersion()
    {
        var user = User.Create("test@test.com", "Test", "hash", _now);
        user.LinkOAuthAccount(OAuthProvider.Google, "pid123", TestSnapshot, null, user.Id, _now);
        ((IHasDomainEvents)user).ClearDomainEvents();
        var version = user.Version;

        user.UnlinkOAuthAccount(OAuthProvider.Google, _actorId, _now);

        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().Contain(e => e is OAuthAccountUnlinkedDomainEvent);
    }

    [Fact]
    public void RotateOAuthToken_ShouldIncrementVersion()
    {
        var user = User.Create("test@test.com", "Test", "hash", _now);
        var token = OAuthToken.Create(SecretRef.Create("access"), SecretRef.Create("refresh"), _now.AddHours(1));
        user.LinkOAuthAccount(OAuthProvider.Google, "pid123", TestSnapshot, token, user.Id, _now);
        ((IHasDomainEvents)user).ClearDomainEvents();
        var version = user.Version;
        var newToken = OAuthToken.Create(SecretRef.Create("new-access"), SecretRef.Create("new-refresh"), _now.AddHours(2));

        user.RotateOAuthToken(OAuthProvider.Google, newToken, user.Id, _now);

        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().Contain(e => e is OAuthTokenReferenceRotatedDomainEvent);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldNotIncrementVersion()
    {
        var user = User.Create("test@test.com", "Test", "hash", _now);
        ((IHasDomainEvents)user).ClearDomainEvents();
        var version = user.Version;

        user.Activate(_actorId, _now);

        user.Version.Should().Be(version);
        user.DomainEvents.Should().BeEmpty();
    }
}
