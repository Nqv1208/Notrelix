using Notrelix.Domain.Documents;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Commands.BatchUpdateBlocks;
using Notrelix.Application.Features.Documents.Blocks.Commands.CreateBlock;
using Notrelix.Application.Features.Documents.Blocks.Commands.DeleteBlock;
using Notrelix.Application.Features.Documents.Blocks.Commands.UpdateBlock;
using Notrelix.Application.Features.Documents.Pages.Commands.MovePage;
using Notrelix.Application.Features.Documents.Pages.Commands.UpdatePage;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Application.Features.Documents.Versions.Commands.RestorePageVersion;
using Notrelix.Application.Features.Documents.Versions.Services;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Testing.Application.Fakes;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>
/// A trashed page (archived, UC-DCT-I06) is read-only to the handlers: a rename is refused with its rule code and
/// changes nothing, and a page removed for good is not found. The handler, not the client, refuses the write.
/// </summary>
public sealed class PageTrashedRefusalTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public async Task RenameOfATrashedPage_IsRefusedWithItsRuleCode_AndTheTitleStaysPut()
    {
        var page = NewPage("Plan");
        page.Archive(UserId, Now.AddMinutes(1));
        var before = page.Version;
        var sut = new UpdatePageCommandHandler(Context(page), User(), Clock());

        var act = () => sut.Handle(new UpdatePageCommand(page.Id, "Renamed", before), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_Page_CannotRenameArchived);
        page.Title.Should().Be("Plan");
        page.Version.Should().Be(before, "a refused rename does not bump the version");
    }

    [Fact]
    public async Task RenameOfAPageRemovedForGood_IsNotFound()
    {
        var page = NewPage("Gone");
        page.Archive(UserId, Now.AddMinutes(1));
        page.Delete(UserId, Now.AddMinutes(2));
        var sut = new UpdatePageCommandHandler(Context(page), User(), Clock());

        var act = () => sut.Handle(new UpdatePageCommand(page.Id, "Renamed", page.Version), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RenameOfAnActivePage_StillSucceeds()
    {
        var page = NewPage("Plan");
        var sut = new UpdatePageCommandHandler(Context(page), User(), Clock());

        var result = await sut.Handle(new UpdatePageCommand(page.Id, "Renamed", page.Version), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        page.Title.Should().Be("Renamed");
    }

    [Fact]
    public async Task AnEditOfABlockOnATrashedPage_IsRefusedWithItsRuleCode_AndTheBlockStaysPut()
    {
        var page = NewPage("Plan");
        var block = Block.CreateRoot(AccountId, WorkspaceId, page.Id, BlockType.Text,
            BlockContent.Create(Notrelix.Domain.SharedKernel.JsonValue.Create("{\"v\":1,\"runs\":[{\"text\":\"kept\",\"marks\":[]}]}")),
            FractionalIndex.Create("a0"), UserId, Now);
        page.Archive(UserId, Now.AddMinutes(1));
        var blocks = new List<Block> { block };
        var blockSet = TestDbSet.Create(blocks);
        var context = new Mock<IDocumentDbContext>();
        context.Setup(c => c.Blocks).Returns(blockSet.Object);
        context.Setup(c => c.Pages).Returns(TestDbSet.Create([page]).Object);
        var sut = new UpdateBlockCommandHandler(context.Object, User(), Clock());

        var act = () => sut.Handle(new UpdateBlockCommand(page.Id, block.Id, null, null, null, block.Version), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_Page_CannotEditArchived);
        block.Version.Should().Be(1, "a refused edit does not bump the block version");
    }

    [Fact]
    public async Task CreatingABlockOnATrashedPage_IsRefused_AndNoBlockIsAdded()
    {
        var page = NewPage("Plan");
        page.Archive(UserId, Now.AddMinutes(1));
        var blocks = new List<Block>();
        var sut = new CreateBlockCommandHandler(BlockContext(page, blocks), new NoPageBlockWriteLock(), RequestContext(), Clock());

        var act = () => sut.Handle(new CreateBlockCommand(page.Id, null, "paragraph", null, null, null, null), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()).Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_Page_CannotEditArchived);
        blocks.Should().BeEmpty("a refused create adds no block");
    }

    [Fact]
    public async Task DeletingABlockOnATrashedPage_IsRefused_AndTheBlockStaysLive()
    {
        var page = NewPage("Plan");
        var block = NewBlock(page, "a0");
        page.Archive(UserId, Now.AddMinutes(1));
        var sut = new DeleteBlockCommandHandler(BlockContext(page, [block]), new NoPageBlockWriteLock(), User(), Clock());

        var act = () => sut.Handle(new DeleteBlockCommand(page.Id, block.Id, block.Version), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()).Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_Page_CannotEditArchived);
        block.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task ABatchOnATrashedPage_IsRefused_AndNothingIsSaved()
    {
        var page = NewPage("Plan");
        var block = NewBlock(page, "a0");
        page.Archive(UserId, Now.AddMinutes(1));
        var blocks = new List<Block> { block };
        var sut = new BatchUpdateBlocksCommandHandler(BlockContext(page, blocks), new NoPageBlockWriteLock(), RequestContext(), Clock());

        var act = () => sut.Handle(new BatchUpdateBlocksCommand(page.Id,
            [new BatchUpdateBlockItem("upsert", Guid.CreateVersion7(), "paragraph", null, null, null, null, null, null)]), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()).Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_Page_CannotEditArchived);
        blocks.Should().ContainSingle().Which.Should().BeSameAs(block);
        block.Version.Should().Be(1);
    }

    [Fact]
    public async Task RestoringAVersionOfATrashedPage_IsRefused_BeforeTheVersionIsRead()
    {
        var page = NewPage("Plan");
        page.Archive(UserId, Now.AddMinutes(1));
        var sut = new RestorePageVersionCommandHandler(BlockContext(page, []), new NoPageBlockWriteLock(),
            new Mock<IPageVersionRecorder>().Object, RequestContext(), Clock());

        var act = () => sut.Handle(new RestorePageVersionCommand(page.Id, Guid.CreateVersion7(), []), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()).Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_Page_CannotEditArchived);
    }

    [Fact]
    public async Task MovingATrashedPage_IsRefused_AndItKeepsItsParent()
    {
        var root = NewPage("Root");
        var page = Page.Create(AccountId, WorkspaceId, "Plan", UserId, Now, root.Id);
        page.Archive(UserId, Now.AddMinutes(1));
        var pages = new InMemoryPageSubtreeStore(root, page);
        var sut = new MovePageCommandHandler(pages, new Mock<IPageSpacePlacement>().Object, new Mock<IPageSubtreeAuthorizer>().Object,
            RequestContext(), Clock());

        var act = () => sut.Handle(new MovePageCommand(page.Id, null, page.Version), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()).Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_Page_CannotMoveArchived);
        page.ParentId.Should().Be(root.Id);
    }

    private static Page NewPage(string title) => Page.Create(AccountId, WorkspaceId, title, UserId, Now);

    private static Block NewBlock(Page page, string position) => Block.CreateRoot(AccountId, WorkspaceId, page.Id, BlockType.Text,
        BlockContent.Create(Notrelix.Domain.SharedKernel.JsonValue.Create("{\"v\":1,\"runs\":[{\"text\":\"kept\",\"marks\":[]}]}")),
        FractionalIndex.Create(position), UserId, Now);

    private static IDocumentDbContext BlockContext(Page page, List<Block> blocks)
    {
        var contextMock = new Mock<IDocumentDbContext>();
        contextMock.Setup(c => c.Pages).Returns(TestDbSet.Create([page]).Object);
        contextMock.Setup(c => c.Blocks).Returns(TestDbSet.Create(blocks).Object);
        return contextMock.Object;
    }

    private static FakeCurrentRequestContext RequestContext()
    {
        var requestContext = new FakeCurrentRequestContext().AsUser(UserId);
        requestContext.Tenant.SetWorkspace(AccountId, WorkspaceId, UserId);
        return requestContext;
    }


    private static IDocumentDbContext Context(Page page)
    {
        var contextMock = new Mock<IDocumentDbContext>();
        contextMock.Setup(c => c.Pages).Returns(TestDbSet.Create([page]).Object);
        return contextMock.Object;
    }

    private static ICurrentUser User()
    {
        var user = new Mock<ICurrentUser>();
        user.Setup(u => u.UserId).Returns(UserId);
        return user.Object;
    }

    private static IDateTimeProvider Clock()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(Now.AddMinutes(5));
        return clock.Object;
    }
}
