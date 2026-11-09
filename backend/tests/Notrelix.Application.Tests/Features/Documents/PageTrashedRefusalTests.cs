using Notrelix.Domain.Documents;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Commands.UpdateBlock;
using Notrelix.Application.Features.Documents.Pages.Commands.UpdatePage;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.SharedKernel.Ordering;
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

    private static Page NewPage(string title) => Page.Create(AccountId, WorkspaceId, title, UserId, Now);

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
