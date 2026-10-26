using FluentAssertions;
using Notrelix.Domain.Documents.Pages;

namespace Notrelix.Domain.Tests.Documents;

public class PageBlocksSavedTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 8, 0, 0, TimeSpan.Zero);

    private static Page NewPage()
    {
        var page = Page.Create(Guid.NewGuid(), Guid.NewGuid(), "Plan", Guid.NewGuid(), Now);
        ((IHasDomainEvents)page).ClearDomainEvents();
        return page;
    }

    [Fact]
    public void RecordBlocksSaved_RaisesTheSavedFact_WithoutChangingThePageVersion()
    {
        var page = NewPage();
        var version = page.Version;
        var author = Guid.NewGuid();
        var mention = new BlockMentionAdded(Guid.NewGuid(), Guid.NewGuid());

        page.RecordBlocksSaved(author, Now, [mention]);

        page.Version.Should().Be(version, "edits of the blocks are not changes of the page itself (DCT-016)");
        var raised = page.DomainEvents.OfType<PageBlocksSavedDomainEvent>().Should().ContainSingle().Which;
        raised.PageId.Should().Be(page.Id);
        raised.PageTitle.Should().Be("Plan");
        raised.SavedBy.Should().Be(author);
        raised.Mentions.Should().Equal(mention);
    }

    [Fact]
    public void RecordBlocksSaved_OnADeletedPage_IsRefused()
    {
        var page = NewPage();
        page.Delete(Guid.NewGuid(), Now);
        ((IHasDomainEvents)page).ClearDomainEvents();

        var act = () => page.RecordBlocksSaved(Guid.NewGuid(), Now, []);

        act.Should().Throw<Exception>();
        page.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RecordBlocksSaved_NeedsAnAuthor()
    {
        var act = () => NewPage().RecordBlocksSaved(Guid.Empty, Now, []);

        act.Should().Throw<Exception>();
    }
}
