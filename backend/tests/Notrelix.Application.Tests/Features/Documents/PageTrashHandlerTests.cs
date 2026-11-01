using Notrelix.Application.Features.Documents.Pages.Commands.DeletePage;
using Notrelix.Application.Features.Documents.Pages.Commands.RestorePage;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Domain.Documents;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>
/// The trash lifecycle: a page moves to the trash (archive), comes back
/// (restore), and is deleted for good only from the trash.
/// </summary>
public sealed class PageTrashHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Restore_PutsATrashedPageBack()
    {
        var page = NewPage();
        page.Archive(Guid.CreateVersion7(), Now);

        var result = await Restore(page).Handle(new RestorePageCommand(page.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        page.Status.Should().Be(PageStatus.Active);
    }

    [Fact]
    public async Task Restore_OfAnActivePage_IsANoOp()
    {
        var page = NewPage();
        var version = page.Version;

        var result = await Restore(page).Handle(new RestorePageCommand(page.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        page.Version.Should().Be(version);
    }

    [Fact]
    public async Task Restore_OfADeletedPage_IsNotFound()
    {
        var page = NewPage();
        page.Archive(Guid.CreateVersion7(), Now);
        page.Delete(Guid.CreateVersion7(), Now);

        var act = () => Restore(page).Handle(new RestorePageCommand(page.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Delete_FromTheTrash_DeletesThePage()
    {
        var page = NewPage();
        page.Archive(Guid.CreateVersion7(), Now);

        var result = await Delete(page).Handle(new DeletePageCommand(page.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        page.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_OfAnActivePage_IsRefused_AndLeavesItUntouched()
    {
        var page = NewPage();

        var act = () => Delete(page).Handle(new DeletePageCommand(page.Id), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_Page_NotInTrash);
        page.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void DeleteForGood_NeedsTheManagerAction_WhileTrashAndRestoreStayEditorLevel()
    {
        new DeletePageCommand(Guid.CreateVersion7()).Action.Should().Be(PermissionAction.DeletePage);
        new RestorePageCommand(Guid.CreateVersion7()).Action.Should().Be(PermissionAction.ArchivePage);
    }

    private static Page NewPage() =>
        Page.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "Trash Candidate", Guid.CreateVersion7(), Now);

    private static RestorePageCommandHandler Restore(Page page) =>
        new(new InMemoryPageSubtreeStore(page), new Mock<IPageSpacePlacement>().Object, new RecordingSubtreeAuthorizer(), PageRequestContext.For(page), new FixedClock());

    private static DeletePageCommandHandler Delete(Page page) =>
        new(new InMemoryPageSubtreeStore(page), new RecordingSubtreeAuthorizer(), PageRequestContext.For(page), new FixedClock());

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}
