using Notrelix.Application.Features.Documents.Pages.Commands.ArchivePage;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Documents.Pages.Events;
using Notrelix.Testing.Application.Fakes;

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

    [Fact]
    public async Task ArchivingAParent_TrashesItsActiveChildrenWithTheSameTime_AndChecksEachOne()
    {
        var parent = NewActivePage();
        var child = Page.Create(parent.AccountId, parent.WorkspaceId, "Child", Guid.CreateVersion7(), Now, parentId: parent.Id);
        var alreadyTrashed = Page.Create(parent.AccountId, parent.WorkspaceId, "Earlier", Guid.CreateVersion7(), Now.AddDays(-3), parentId: parent.Id);
        alreadyTrashed.Archive(Guid.CreateVersion7(), Now.AddDays(-1));
        var subtree = new RecordingSubtreeAuthorizer();
        var sut = new ArchivePageCommandHandler(
            new InMemoryPageSubtreeStore(parent, child, alreadyTrashed), subtree, PageRequestContext.For(parent), new FixedClock());

        var result = await sut.Handle(new ArchivePageCommand(parent.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        parent.Status.Should().Be(PageStatus.Archived);
        child.Status.Should().Be(PageStatus.Archived);
        child.UpdatedAt.Should().Be(parent.UpdatedAt, "one archive time says what went to the trash together");
        alreadyTrashed.UpdatedAt.Should().Be(Now.AddDays(-1), "a page trashed earlier is left alone");
        subtree.Checked.Should().Equal(child.Id);
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

    private static (ArchivePageCommandHandler Handler, RecordingSubtreeAuthorizer Subtree) CreateSut(Page? existingPage)
    {
        var store = existingPage is null ? new InMemoryPageSubtreeStore() : new InMemoryPageSubtreeStore(existingPage);
        var subtree = new RecordingSubtreeAuthorizer();
        var context = existingPage is null
            ? new FakeCurrentRequestContext().AsUser(Guid.CreateVersion7())
            : PageRequestContext.For(existingPage);
        if (existingPage is null) context.Tenant.SetWorkspace(Guid.CreateVersion7(), Guid.CreateVersion7(), context.UserId);
        return (new ArchivePageCommandHandler(store, subtree, context, new FixedClock()), subtree);
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}
