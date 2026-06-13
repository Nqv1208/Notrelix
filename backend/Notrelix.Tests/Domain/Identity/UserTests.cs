using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Identity;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Common.Exceptions;
using Xunit;

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
        user.DomainEvents.Should().ContainSingle(e => e is UserRegisteredEvent);
        var evt = (UserRegisteredEvent)user.DomainEvents.First(e => e is UserRegisteredEvent);
        evt.UserId.Should().Be(user.Id);
        evt.Email.Value.Should().Be("test@example.com");
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
        user.ClearDomainEvents();

        var loginTime = new DateTimeOffset(2026, 6, 11, 10, 0, 0, TimeSpan.Zero);
        user.RecordLogin(loginTime);

        user.LastLoginAt.Should().Be(loginTime);
        user.DomainEvents.Should().ContainSingle(e => e is UserLoggedInEvent);
        var evt = (UserLoggedInEvent)user.DomainEvents.First(e => e is UserLoggedInEvent);
        evt.OccurredAt.Should().Be(loginTime);
    }

    [Fact]
    public void UpdateProfile_ShouldUseSuppliedTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hash123", now);
        user.ClearDomainEvents();

        var updateTime = new DateTimeOffset(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);
        user.UpdateProfile("New Name", "avatar.png", updateTime);

        user.Name.Should().Be("New Name");
        user.Avatar.Should().Be("avatar.png");
        user.UpdatedAt.Should().Be(updateTime);
    }

    [Fact]
    public void UpdateProfile_OnDeletedUser_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "hash123", now);
        user.SoftDelete(Guid.NewGuid(), now);

        var act = () => user.UpdateProfile("New Name", null, now);

        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void UpdateEmail_WithValidEmail_ShouldChangeEmail()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("old@example.com", "Test User", "hash123", now);

        user.UpdateEmail("new@example.com", now);

        user.Email.Value.Should().Be("new@example.com");
    }

    [Fact]
    public void UpdatePassword_ShouldChangePasswordHash()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("test@example.com", "Test User", "oldhash", now);

        user.UpdatePassword("newhash", now);

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
}
