using FluentAssertions;
using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.Domain.Tests.Identity;

public class UserTests
{
    [Fact]
    public void Create_ShouldRaiseUserRegisteredEvent()
    {
        var now = DateTimeOffset.UtcNow;

        var user = User.Create("test@example.com", "Test User", "hash123", now);

        user.Email.Value.Should().Be("test@example.com");
        user.Name.Should().Be("Test User");
        user.Status.Should().Be(UserStatus.Active);
        user.DomainEvents.Should().ContainSingle(e => e is UserRegisteredDomainEvent);
        var evt = (UserRegisteredDomainEvent)user.DomainEvents.First(e => e is UserRegisteredDomainEvent);
        evt.UserId.Should().Be(user.Id);
        evt.Email.Should().Be("test@example.com");
        user.CreatedBy.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var now = DateTimeOffset.UtcNow;

        var user = User.Create("test@example.com", "Test User", "hash123", now);

        user.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void RecordLogin_ShouldUseSuppliedTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hash123", now);
        ((IHasDomainEvents)user).ClearDomainEvents();

        var loginTime = now.AddHours(1);
        user.RecordLogin(loginTime);

        user.LastLoginAt.Should().Be(loginTime);
        user.DomainEvents.Should().ContainSingle(e => e is UserLoggedInDomainEvent);
        var evt = (UserLoggedInDomainEvent)user.DomainEvents.First(e => e is UserLoggedInDomainEvent);
        evt.OccurredAt.Should().Be(loginTime);
    }

    [Fact]
    public void UpdateProfile_ShouldUseSuppliedTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hash123", now);
        ((IHasDomainEvents)user).ClearDomainEvents();

        var updateTime = now.AddHours(1);
        user.UpdateProfile("New Name", "avatar.png", user.Id, updateTime);

        user.Name.Should().Be("New Name");
        user.Avatar.Should().Be("avatar.png");
        user.UpdatedAt.Should().Be(updateTime);
    }

    [Fact]
    public void UpdateProfile_OnDeletedUser_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hash123", now);
        user.Delete(Guid.NewGuid(), now);

        var act = () => user.UpdateProfile("New Name", null, user.Id, now);

        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void UpdateEmail_WithValidEmail_ShouldChangeEmail()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("old@example.com", "Test User", "hash123", now);

        user.UpdateEmail("new@example.com", user.Id, now);

        user.Email.Value.Should().Be("new@example.com");
    }

    [Fact]
    public void UpdatePassword_ShouldChangePasswordHash()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "oldhash", now);

        user.UpdatePassword("newhash", user.Id, now);

        user.PasswordHash.Should().Be("newhash");
    }

    [Fact]
    public void Suspend_ShouldSetStatusToSuspended()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hash123", now);

        user.Suspend(user.Id, now);

        user.Status.Should().Be(UserStatus.Suspended);
    }

    [Fact]
    public void Activate_AfterSuspend_ShouldSetStatusToActive()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hash123", now);
        user.Suspend(user.Id, now);

        user.Activate(user.Id, now);

        user.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public void User_ShouldExtendAggregateRoot()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hash123", now);

        user.Should().BeAssignableTo<AggregateRoot>();
    }

    [Fact]
    public void User_ShouldNotHaveSessionManagement()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hash123", now);

        var hasSessionProperty = user.GetType().GetProperty("Sessions");
        hasSessionProperty.Should().BeNull("sessions are managed by a separate aggregate");

        var hasCreateSessionMethod = user.GetType().GetMethod("CreateSession");
        hasCreateSessionMethod.Should().BeNull("session creation belongs to UserSession aggregate");

        var hasRevokeSessionMethod = user.GetType().GetMethod("RevokeSession");
        hasRevokeSessionMethod.Should().BeNull("session revocation belongs to UserSession aggregate");
    }

    [Fact]
    public void UpdatePassword_SameHash_ShouldBeNoOp()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hash123", now);
        ((IHasDomainEvents)user).ClearDomainEvents();
        var version = user.Version;

        user.UpdatePassword("hash123", user.Id, now);

        user.PasswordHash.Should().Be("hash123");
        user.Version.Should().Be(version);
        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdatePassword_DifferentHash_ShouldChangePassword()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "oldhash", now);
        ((IHasDomainEvents)user).ClearDomainEvents();
        var version = user.Version;

        user.UpdatePassword("newhash", user.Id, now);

        user.PasswordHash.Should().Be("newhash");
        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().ContainSingle(e => e is UserPasswordChangedDomainEvent);
    }

    [Fact]
    public void RotateOAuthToken_SameToken_ShouldBeNoOp()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hash123", now);
        var token = OAuthToken.Create(SecretRef.Create("access"), SecretRef.Create("refresh"), now.AddHours(1));
        user.LinkOAuthAccount(OAuthProvider.Google, "pid123",
            OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, JsonValue.EmptyObject()),
            token, user.Id, now);
        ((IHasDomainEvents)user).ClearDomainEvents();
        var version = user.Version;

        user.RotateOAuthToken(OAuthProvider.Google, token, user.Id, now.AddMinutes(5));

        user.OAuthAccounts.Single().Token.Should().Be(token);
        user.Version.Should().Be(version);
        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RotateOAuthToken_DifferentToken_ShouldRotate()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hash123", now);
        var oldToken = OAuthToken.Create(SecretRef.Create("old-access"));
        user.LinkOAuthAccount(OAuthProvider.Google, "pid123",
            OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, JsonValue.EmptyObject()),
            oldToken, user.Id, now);
        ((IHasDomainEvents)user).ClearDomainEvents();
        var version = user.Version;

        var newToken = OAuthToken.Create(SecretRef.Create("new-access"));
        user.RotateOAuthToken(OAuthProvider.Google, newToken, user.Id, now.AddMinutes(5));

        user.OAuthAccounts.Single().Token.Should().Be(newToken);
        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().ContainSingle(e => e is OAuthTokenReferenceRotatedDomainEvent);
    }

    [Fact]
    public void UnlinkOAuthAccount_EmptyActor_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hash123", now);
        user.LinkOAuthAccount(OAuthProvider.Google, "pid123",
            OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, JsonValue.EmptyObject()),
            null, user.Id, now);

        var act = () => user.UnlinkOAuthAccount(OAuthProvider.Google, Guid.Empty, now);

        act.Should().Throw<BusinessRuleException>();
    }
}
