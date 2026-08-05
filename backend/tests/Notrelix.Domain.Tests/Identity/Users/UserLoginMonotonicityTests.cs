using FluentAssertions;

namespace Notrelix.Domain.Tests.Identity.Users;

public class UserLoginMonotonicityTests
{
    private static readonly DateTimeOffset BaseTime = DateTimeOffset.UtcNow;

    private static User CreateUser() => User.Create("test@example.com", "Test User", "hash", BaseTime);

    [Fact]
    public void FirstLogin_ShouldSucceed()
    {
        var user = CreateUser();
        user.RecordLogin(BaseTime.AddMinutes(1));
        user.LastLoginAt.Should().Be(BaseTime.AddMinutes(1));
    }

    [Fact]
    public void SecondLogin_LaterTime_ShouldSucceed()
    {
        var user = CreateUser();
        user.RecordLogin(BaseTime.AddMinutes(1));
        user.RecordLogin(BaseTime.AddMinutes(2));
        user.LastLoginAt.Should().Be(BaseTime.AddMinutes(2));
    }

    [Fact]
    public void Login_SameTime_ShouldThrow()
    {
        var user = CreateUser();
        user.RecordLogin(BaseTime.AddMinutes(1));
        var act = () => user.RecordLogin(BaseTime.AddMinutes(1));
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Login_EarlierTime_ShouldThrow()
    {
        var user = CreateUser();
        user.RecordLogin(BaseTime.AddMinutes(2));
        var act = () => user.RecordLogin(BaseTime.AddMinutes(1));
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Login_ShouldRaiseEvent()
    {
        var user = CreateUser();
        user.RecordLogin(BaseTime.AddMinutes(1));
        user.DomainEvents.Should().ContainSingle(e => e is UserLoggedInDomainEvent);
    }

    [Fact]
    public void Login_ShouldIncrementVersion()
    {
        var user = CreateUser();
        var before = user.Version;
        user.RecordLogin(BaseTime.AddMinutes(1));
        user.Version.Should().Be(before + 1);
    }
}
