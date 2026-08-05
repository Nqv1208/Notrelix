using FluentAssertions;
using Notrelix.Domain.Integrations.Calendar;

namespace Notrelix.Domain.Tests.Integrations;

public class CalendarEventTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid());
        var syncHash = CalendarSyncFingerprint.Create("Title", DateTime.Today);

        var evt = CalendarEvent.Create(Guid.NewGuid(), "ext_123", target, syncHash);

        evt.IntegrationId.Should().NotBeEmpty();
        evt.ExternalEventId.Should().Be("ext_123");
        evt.Target.Should().Be(target);
        evt.SyncHash.Should().Be(syncHash);
    }

    [Fact]
    public void Create_WithEmptyExternalId_ShouldThrow()
    {
        var act = () => CalendarEvent.Create(Guid.NewGuid(), "", ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid()), CalendarSyncFingerprint.Create("T", DateTime.Today));
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithNullCalendarSyncFingerprint_ShouldThrow()
    {
        var act = () => CalendarEvent.Create(Guid.NewGuid(), "ext_1", ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid()), null!);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void UpdateCalendarSyncFingerprint_ShouldUpdate()
    {
        var evt = CalendarEvent.Create(Guid.NewGuid(), "ext_1", ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid()), CalendarSyncFingerprint.Create("Old", DateTime.Today));
        var newHash = CalendarSyncFingerprint.Create("New", DateTime.Today.AddDays(1));

        evt.UpdateSyncHash(newHash);

        evt.SyncHash.Should().Be(newHash);
    }

    [Fact]
    public void UpdateCalendarSyncFingerprint_WithNull_ShouldThrow()
    {
        var evt = CalendarEvent.Create(Guid.NewGuid(), "ext_1", ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid()), CalendarSyncFingerprint.Create("T", DateTime.Today));
        var act = () => evt.UpdateSyncHash(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
