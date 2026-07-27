using FluentAssertions;
using Notrelix.Domain.Integrations.Rules;

namespace Notrelix.Domain.Tests.Integrations.Rules;

public class CalendarSyncRulesTests
{
    [Fact]
    public void EnsureConnectionActive_WhenActive_ShouldNotThrow()
    {
        Action act = () => CalendarSyncRules.EnsureConnectionActive(true);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureConnectionActive_WhenInactive_ShouldThrow()
    {
        Action act = () => CalendarSyncRules.EnsureConnectionActive(false);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*must be active*");
    }

    [Fact]
    public void EnsureNoCircularSync_WhenDifferentIds_ShouldNotThrow()
    {
        var internalId = Guid.NewGuid();
        var externalId = Guid.NewGuid();

        Action act = () => CalendarSyncRules.EnsureNoCircularSync(internalId, externalId);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureNoCircularSync_WhenSameIds_ShouldThrow()
    {
        var eventId = Guid.NewGuid();

        Action act = () => CalendarSyncRules.EnsureNoCircularSync(eventId, eventId);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*itself*");
    }

    [Fact]
    public void EnsureNoCircularSync_WhenExternalIsNull_ShouldNotThrow()
    {
        var internalId = Guid.NewGuid();

        Action act = () => CalendarSyncRules.EnsureNoCircularSync(internalId, null);

        act.Should().NotThrow();
    }
}
