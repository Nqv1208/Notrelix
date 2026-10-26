using FluentAssertions;
using Notrelix.Domain.Documents.Pages;

namespace Notrelix.Domain.Tests.Documents;

public class PageTrashTests
{
    private static Page NewPage() =>
        Page.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", Guid.NewGuid(), DateTimeOffset.UtcNow);

    [Fact]
    public void Unarchive_PutsAnArchivedPageBack_AndRaisesRestored()
    {
        var page = NewPage();
        page.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)page).ClearDomainEvents();
        var version = page.Version;

        page.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        page.Status.Should().Be(PageStatus.Active);
        page.Version.Should().Be(version + 1);
        page.DomainEvents.Should().ContainSingle(e => e is PageRestoredDomainEvent);
    }

    [Fact]
    public void Unarchive_OnAnActivePage_IsANoOp()
    {
        var page = NewPage();
        ((IHasDomainEvents)page).ClearDomainEvents();
        var version = page.Version;

        page.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        page.Version.Should().Be(version);
        page.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Unarchive_OnADeletedPage_Throws()
    {
        var page = NewPage();
        page.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        page.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => page.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<Exception>();
    }
}
