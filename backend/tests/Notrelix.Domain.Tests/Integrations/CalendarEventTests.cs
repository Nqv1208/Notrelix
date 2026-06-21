using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Integrations.Calendar;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.Integrations;

public class CalendarEventTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid());
        var syncHash = SyncHash.Create("Title", DateTime.Today);

        var evt = CalendarEvent.Create(Guid.NewGuid(), "ext_123", target, syncHash);

        evt.IntegrationId.Should().NotBeEmpty();
        evt.ExternalEventId.Should().Be("ext_123");
        evt.Target.Should().Be(target);
        evt.SyncHash.Should().Be(syncHash);
    }

    [Fact]
    public void Create_WithEmptyExternalId_ShouldThrow()
    {
        var act = () => CalendarEvent.Create(Guid.NewGuid(), "", ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid()), SyncHash.Create("T", DateTime.Today));
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithNullSyncHash_ShouldThrow()
    {
        var act = () => CalendarEvent.Create(Guid.NewGuid(), "ext_1", ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid()), null!);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void UpdateSyncHash_ShouldUpdate()
    {
        var evt = CalendarEvent.Create(Guid.NewGuid(), "ext_1", ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid()), SyncHash.Create("Old", DateTime.Today));
        var newHash = SyncHash.Create("New", DateTime.Today.AddDays(1));

        evt.UpdateSyncHash(newHash);

        evt.SyncHash.Should().Be(newHash);
    }

    [Fact]
    public void UpdateSyncHash_WithNull_ShouldThrow()
    {
        var evt = CalendarEvent.Create(Guid.NewGuid(), "ext_1", ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid()), SyncHash.Create("T", DateTime.Today));
        var act = () => evt.UpdateSyncHash(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
