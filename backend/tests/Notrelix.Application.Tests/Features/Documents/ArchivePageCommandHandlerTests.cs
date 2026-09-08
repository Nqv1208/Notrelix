using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Pages.Commands.ArchivePage;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Documents.Pages.Events;
using Notrelix.Testing.Application.Fakes;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>
/// TAC-DC-FLOW-02 handler matrix — the archive use case loads the page from
/// the Documents-owned context, delegates the lifecycle mutation to the
/// Page.Archive aggregate authority, and treats an already-archived page as
/// the documented Domain no-op. PublishPage is never substitute evidence.
/// </summary>
public sealed class ArchivePageCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ActivePage_Archives_RaisingTheOwnedFact()
    {
        var page = NewActivePage();
        var (sut, _) = CreateSut(page);

        var result = await sut.Handle(new ArchivePageCommand(page.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        page.Status.Should().Be(PageStatus.Archived);
        page.DomainEvents.Should().ContainSingle(e => e is PageArchivedDomainEvent);
    }

    [Fact]
    public async Task UnknownPage_IsNotFound_WithoutMutation()
    {
        var (sut, _) = CreateSut(existingPage: null);

        var act = () => sut.Handle(new ArchivePageCommand(Guid.CreateVersion7()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AlreadyArchivedPage_IsADocumentedNoOp_WithoutSecondEvent()
    {
        var page = NewActivePage();
        page.Archive(Guid.CreateVersion7(), Now);
        var firstEventCount = page.DomainEvents.Count;

        var (sut, _) = CreateSut(page);

        var result = await sut.Handle(new ArchivePageCommand(page.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        page.Status.Should().Be(PageStatus.Archived);
        page.DomainEvents.Should().HaveCount(firstEventCount);
    }

    [Fact]
    public async Task DeletedPage_IsNotFound()
    {
        var page = NewActivePage();
        page.Delete(Guid.CreateVersion7(), Now);

        var (sut, _) = CreateSut(page);

        var act = () => sut.Handle(new ArchivePageCommand(page.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>("a deleted page is no longer an archive candidate");
    }

    private static Page NewActivePage()
    {
        var page = Page.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Archive Candidate",
            Guid.CreateVersion7(),
            Now);
        return page;
    }

    private static (ArchivePageCommandHandler Handler, Mock<IDocumentDbContext> ContextMock) CreateSut(Page? existingPage)
    {
        var data = existingPage is null ? [] : new List<Page> { existingPage };
        var pages = TestDbSet.Create(data).Object;
        var contextMock = new Mock<IDocumentDbContext>();
        contextMock.Setup(c => c.Pages).Returns(pages);
        return (new ArchivePageCommandHandler(
            contextMock.Object,
            new FakeCurrentRequestContext().AsUser(Guid.CreateVersion7()),
            new FixedClock()), contextMock);
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}
