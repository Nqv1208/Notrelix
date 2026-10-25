using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Commands.CreateBlock;
using Notrelix.Application.Features.Documents.Blocks.Commands.UpdateBlock;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Testing.Application.Fakes;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>
/// Editing a document's blocks: a new block lands right after the block it is told to follow (or last),
/// editing a block rewrites its content, and a block of another page or a different type is refused.
/// </summary>
public sealed class BlockEditingHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public async Task CreatingAfterABlock_PlacesTheNewBlockBetweenItAndItsNextSibling()
    {
        var page = NewPage();
        var first = NewBlock(page, "a0");
        var second = NewBlock(page, "a1");
        var blocks = new List<Block> { first, second };
        var (sut, context) = CreateCreateSut(page, blocks);

        var result = await sut.Handle(
            new CreateBlockCommand(page.Id, BlockType.Text, "{\"text\":\"mid\"}", first.Id, null), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        var created = context.Added.Single();
        string.CompareOrdinal(created.Position.Value, first.Position.Value).Should().BePositive();
        string.CompareOrdinal(created.Position.Value, second.Position.Value).Should().BeNegative();
        created.Content.Data.Value.Should().Be("{\"text\":\"mid\"}");
    }

    [Fact]
    public async Task CreatingWithoutAnAnchor_PlacesTheNewBlockLast()
    {
        var page = NewPage();
        var last = NewBlock(page, "a5");
        var (sut, context) = CreateCreateSut(page, [NewBlock(page, "a0"), last]);

        await sut.Handle(new CreateBlockCommand(page.Id, BlockType.Heading1, "{}", null, null), CancellationToken.None);

        string.CompareOrdinal(context.Added.Single().Position.Value, last.Position.Value).Should().BePositive();
    }

    [Fact]
    public async Task CreatingAfterAnUnknownBlock_IsNotFound()
    {
        var page = NewPage();
        var (sut, context) = CreateCreateSut(page, [NewBlock(page, "a0")]);

        var act = () => sut.Handle(new CreateBlockCommand(page.Id, BlockType.Text, "{}", Guid.CreateVersion7(), null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        context.Added.Should().BeEmpty();
    }

    [Fact]
    public void CreatingABlock_NeedsOnlyTheEditLevelOnThePage_LikeUpdatingAndDeletingOne()
    {
        var pageId = Guid.CreateVersion7();
        var create = new CreateBlockCommand(pageId, BlockType.Text, "{}", null, null);
        var update = new Notrelix.Application.Features.Documents.Blocks.Commands.DeleteBlock.DeleteBlockCommand(pageId, Guid.CreateVersion7());

        create.Action.Should().Be(PermissionAction.UpdatePage);
        create.Action.Should().Be(update.Action);
        create.Resource.ResourceId.Should().Be(pageId);
    }

    [Fact]
    public async Task UpdatingABlock_RewritesItsContentAndAdvancesItsVersion()
    {
        var page = NewPage();
        var block = NewBlock(page, "a0");
        var sut = CreateUpdateSut([block]);

        var result = await sut.Handle(
            new UpdateBlockCommand(page.Id, block.Id, null, "{\"text\":\"edited\"}"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        block.Content.Data.Value.Should().Be("{\"text\":\"edited\"}");
        block.Version.Should().Be(2);
    }

    [Fact]
    public async Task UpdatingABlockToADifferentType_ChangesItInPlace()
    {
        var page = NewPage();
        var block = NewBlock(page, "a0");
        var id = block.Id;
        var sut = CreateUpdateSut([block]);

        var result = await sut.Handle(
            new UpdateBlockCommand(page.Id, block.Id, "heading_1", "{\"text\":\"x\"}"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        block.Id.Should().Be(id);
        block.Type.Should().Be(BlockType.Heading1);
        block.Content.Data.Value.Should().Be("{\"text\":\"x\"}");
    }

    [Fact]
    public async Task UpdatingABlockToAnUnknownType_IsRefusedWithoutChange()
    {
        var page = NewPage();
        var block = NewBlock(page, "a0");
        var version = block.Version;
        var sut = CreateUpdateSut([block]);

        var result = await sut.Handle(
            new UpdateBlockCommand(page.Id, block.Id, "Embed", "{\"text\":\"x\"}"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        block.Version.Should().Be(version);
    }

    [Fact]
    public async Task UpdatingABlockThroughAnotherPage_IsNotFound()
    {
        var page = NewPage();
        var block = NewBlock(page, "a0");
        var sut = CreateUpdateSut([block]);

        var act = () => sut.Handle(
            new UpdateBlockCommand(Guid.CreateVersion7(), block.Id, null, "{}"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static Page NewPage() => Page.Create(AccountId, WorkspaceId, "Doc", UserId, Now);

    private static Block NewBlock(Page page, string position) => Block.CreateRoot(
        AccountId, WorkspaceId, page.Id, BlockType.Text,
        BlockContent.Create(JsonValue.Create("{\"text\":\"t\"}")), FractionalIndex.Create(position), UserId, Now);

    private sealed class RecordingContext
    {
        public List<Block> Added { get; } = [];
    }

    private static (CreateBlockCommandHandler Handler, RecordingContext Recorded) CreateCreateSut(Page page, List<Block> blocks)
    {
        var recorded = new RecordingContext();
        var blockSet = TestDbSet.Create(blocks);
        blockSet.Setup(s => s.Add(It.IsAny<Block>())).Callback<Block>(block =>
        {
            recorded.Added.Add(block);
            blocks.Add(block);
        });
        var context = new Mock<IDocumentDbContext>();
        context.Setup(c => c.Pages).Returns(TestDbSet.Create([page]).Object);
        context.Setup(c => c.Blocks).Returns(blockSet.Object);
        var requestContext = new FakeCurrentRequestContext().AsUser(UserId);
        requestContext.Tenant.SetWorkspace(AccountId, WorkspaceId, UserId);
        return (new CreateBlockCommandHandler(context.Object, requestContext, new FixedClock()), recorded);
    }

    private static UpdateBlockCommandHandler CreateUpdateSut(List<Block> blocks)
    {
        var context = new Mock<IDocumentDbContext>();
        context.Setup(c => c.Blocks).Returns(TestDbSet.Create(blocks).Object);
        var user = new Mock<ICurrentUser>();
        user.Setup(u => u.UserId).Returns(UserId);
        return new UpdateBlockCommandHandler(context.Object, user.Object, new FixedClock());
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now.AddMinutes(5);
    }
}
