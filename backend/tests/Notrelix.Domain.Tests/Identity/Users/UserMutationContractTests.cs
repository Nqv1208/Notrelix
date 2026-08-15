using FluentAssertions;

namespace Notrelix.Domain.Tests.Identity;

public class UserMutationContractTests
{
    private static User CreateUser()
    {
        var now = DateTimeOffset.UtcNow;
        return User.Create("test@example.com", "Test User", "hash123", now, hasPasswordCredential: true);
    }

    [Fact]
    public void UpdateProfile_NoOp_ShouldNotIncreaseVersion()
    {
        var user = CreateUser();
        var versionBefore = user.Version;
        var now = DateTimeOffset.UtcNow;

        user.UpdateProfile(user.Name, user.Avatar, user.Id, now);

        user.Version.Should().Be(versionBefore);
    }

    [Fact]
    public void UpdateProfile_NoOp_ShouldNotRaiseEvent()
    {
        var user = CreateUser();
        ((IHasDomainEvents)user).ClearDomainEvents();
        var now = DateTimeOffset.UtcNow;

        user.UpdateProfile(user.Name, user.Avatar, user.Id, now);

        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateProfile_NoOp_ShouldNotUpdateTimestamp()
    {
        var user = CreateUser();
        var updatedAtBefore = user.UpdatedAt;
        var now = DateTimeOffset.UtcNow;

        user.UpdateProfile(user.Name, user.Avatar, user.Id, now);

        user.UpdatedAt.Should().Be(updatedAtBefore);
    }

    [Fact]
    public void UpdateProfile_ChangedName_ShouldMutate()
    {
        var user = CreateUser();
        var versionBefore = user.Version;
        var now = DateTimeOffset.UtcNow;
        ((IHasDomainEvents)user).ClearDomainEvents();

        user.UpdateProfile("New Name", user.Avatar, user.Id, now);

        user.Name.Should().Be("New Name");
        user.Version.Should().Be(versionBefore + 1);
        user.DomainEvents.Should().ContainSingle(e => e is UserProfileUpdatedDomainEvent);
    }

    [Fact]
    public void UpdateProfile_ChangedAvatar_ShouldMutate()
    {
        var user = CreateUser();
        var versionBefore = user.Version;
        var now = DateTimeOffset.UtcNow;
        ((IHasDomainEvents)user).ClearDomainEvents();

        user.UpdateProfile(user.Name, "new-avatar.png", user.Id, now);

        user.Avatar.Should().Be("new-avatar.png");
        user.Version.Should().Be(versionBefore + 1);
        user.DomainEvents.Should().ContainSingle(e => e is UserProfileUpdatedDomainEvent);
    }

    [Fact]
    public void RecordLogin_FirstLogin_ShouldSucceed()
    {
        var user = CreateUser();
        ((IHasDomainEvents)user).ClearDomainEvents();
        var now = DateTimeOffset.UtcNow;

        user.RecordLogin(now);

        user.LastLoginAt.Should().Be(now);
        user.DomainEvents.Should().ContainSingle(e => e is UserLoggedInDomainEvent);
    }

    [Fact]
    public void RecordLogin_LaterTime_ShouldSucceed()
    {
        var user = CreateUser();
        var t1 = DateTimeOffset.UtcNow;
        user.RecordLogin(t1);
        ((IHasDomainEvents)user).ClearDomainEvents();

        var t2 = t1.AddHours(1);
        user.RecordLogin(t2);

        user.LastLoginAt.Should().Be(t2);
        user.DomainEvents.Should().ContainSingle(e => e is UserLoggedInDomainEvent);
    }

    [Fact]
    public void RecordLogin_SameTime_ShouldThrow()
    {
        var user = CreateUser();
        var t1 = DateTimeOffset.UtcNow;
        user.RecordLogin(t1);

        var act = () => user.RecordLogin(t1);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*cannot move backwards*");
    }

    [Fact]
    public void RecordLogin_EarlierTime_ShouldThrow()
    {
        var user = CreateUser();
        var t1 = DateTimeOffset.UtcNow;
        user.RecordLogin(t1);
        ((IHasDomainEvents)user).ClearDomainEvents();

        var t2 = t1.AddHours(-1);
        var act = () => user.RecordLogin(t2);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*cannot move backwards*");
        user.LastLoginAt.Should().Be(t1);
        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RecordLogin_EarlierTime_ShouldNotIncreaseVersion()
    {
        var user = CreateUser();
        var t1 = DateTimeOffset.UtcNow;
        user.RecordLogin(t1);
        var versionBefore = user.Version;

        var t2 = t1.AddHours(-1);
        var act = () => user.RecordLogin(t2);

        act.Should().Throw<BusinessRuleException>();
        user.Version.Should().Be(versionBefore);
    }
}
