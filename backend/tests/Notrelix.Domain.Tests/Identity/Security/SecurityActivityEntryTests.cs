using FluentAssertions;

namespace Notrelix.Domain.Tests.Identity;

public class SecurityActivityEntryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Record_StoresTheFactsOfTheEvent()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var entry = SecurityActivityEntry.Record(
            userId, SecurityActivityType.SignInSucceeded, Now, "203.0.113.7", "UA/1.0", sessionId);

        entry.Id.Should().NotBeEmpty();
        entry.UserId.Should().Be(userId);
        entry.Type.Should().Be(SecurityActivityType.SignInSucceeded);
        entry.OccurredAt.Should().Be(Now);
        entry.IpAddress.Should().Be("203.0.113.7");
        entry.UserAgent.Should().Be("UA/1.0");
        entry.SessionId.Should().Be(sessionId);
    }

    [Fact]
    public void Record_RejectsAnEmptyUserBecauseNoOwnerMeansNoRow()
    {
        var act = () => SecurityActivityEntry.Record(Guid.Empty, SecurityActivityType.SignInFailed, Now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Record_RejectsAnUnknownType()
    {
        var act = () => SecurityActivityEntry.Record(Guid.NewGuid(), (SecurityActivityType)999, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Record_BoundsClientMetadataToTheColumnWidthsAndDropsBlanks()
    {
        var entry = SecurityActivityEntry.Record(
            Guid.NewGuid(), SecurityActivityType.SignInFailed, Now,
            new string('1', 200), new string('a', 2000));
        var blank = SecurityActivityEntry.Record(
            Guid.NewGuid(), SecurityActivityType.SignInFailed, Now, "  ", "");

        entry.IpAddress!.Length.Should().Be(SecurityActivityEntry.IpAddressMaxLength);
        entry.UserAgent!.Length.Should().Be(SecurityActivityEntry.UserAgentMaxLength);
        blank.IpAddress.Should().BeNull();
        blank.UserAgent.Should().BeNull();
    }

    [Fact]
    public void Record_ProducesDistinctIds()
    {
        var userId = Guid.NewGuid();

        var first = SecurityActivityEntry.Record(userId, SecurityActivityType.PasswordChanged, Now);
        var second = SecurityActivityEntry.Record(userId, SecurityActivityType.PasswordChanged, Now);

        first.Id.Should().NotBe(second.Id);
    }
}
