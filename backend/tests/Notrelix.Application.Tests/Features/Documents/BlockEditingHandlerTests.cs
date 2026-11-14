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
            new CreateBlockCommand(page.Id, null, "paragraph", Json("{\"v\":1,\"runs\":[{\"text\":\"mid\",\"marks\":[]}]}"), null, first.Id, null), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        var created = context.Added.Single();
        string.CompareOrdinal(created.Position.Value, first.Position.Value).Should().BePositive();
        string.CompareOrdinal(created.Position.Value, second.Position.Value).Should().BeNegative();
        created.Content.Data.Value.Should().Be("{\"v\":1,\"runs\":[{\"text\":\"mid\",\"marks\":[]}]}");
    }

    [Fact]
    public async Task CreatingWithoutAnAnchor_PlacesTheNewBlockLast()
    {
        var page = NewPage();
        var last = NewBlock(page, "a5");
        var (sut, context) = CreateCreateSut(page, [NewBlock(page, "a0"), last]);

        await sut.Handle(new CreateBlockCommand(page.Id, null, "heading_1", null, null, null, null), CancellationToken.None);

        string.CompareOrdinal(context.Added.Single().Position.Value, last.Position.Value).Should().BePositive();
    }

    [Fact]
    public async Task CreatingBetweenTwoBlocksOnTheSameKey_IsAConflict_NotAnUnmappedFailure()
    {
        var page = NewPage();
        var first = NewBlock(page, "a0");
        var (sut, context) = CreateCreateSut(page, [first, NewBlock(page, "a0")]);

        var act = () => sut.Handle(
            new CreateBlockCommand(page.Id, null, "paragraph", null, null, first.Id, null), CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>()).Which.ErrorCode.Should().Be("documents.block.position-conflict");
        context.Added.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatingAfterAnUnknownBlock_IsNotFound()
    {
        var page = NewPage();
        var (sut, context) = CreateCreateSut(page, [NewBlock(page, "a0")]);

        var act = () => sut.Handle(new CreateBlockCommand(page.Id, null, "Text", null, null, Guid.CreateVersion7(), null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        context.Added.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatingAfterABlockOfThisPageThatIsNotASibling_IsA400_NotANotFound()
    {
        var page = NewPage();
        var root = NewBlock(page, "a0");
        var child = Block.CreateChild(AccountId, WorkspaceId, page.Id, BlockType.Text,
            BlockContent.Create(JsonValue.Create("{\"v\":1,\"runs\":[{\"text\":\"child\",\"marks\":[]}]}")),
            FractionalIndex.Create("a0"), UserId, Now, BlockAncestorPath.Create(AccountId, WorkspaceId, page.Id, root.Id, []));
        var (sut, context) = CreateCreateSut(page, [root, child]);

        // The child exists on this page but sits under the root block, so it is not a sibling of a new root block.
        var act = () => sut.Handle(new CreateBlockCommand(page.Id, null, "paragraph", null, null, child.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ValidationException>();
        context.Added.Should().BeEmpty();
    }

    [Fact]
    public void CreatingABlock_NeedsOnlyTheEditLevelOnThePage_LikeUpdatingAndDeletingOne()
    {
        var pageId = Guid.CreateVersion7();
        var create = new CreateBlockCommand(pageId, null, "Text", null, null, null, null);
        var update = new Notrelix.Application.Features.Documents.Blocks.Commands.DeleteBlock.DeleteBlockCommand(pageId, Guid.CreateVersion7(), null);

        create.Action.Should().Be(PermissionAction.UpdatePage);
        create.Action.Should().Be(update.Action);
        create.Resource.ResourceId.Should().Be(pageId);
    }

    [Fact]
    public async Task UpdatingABlock_RewritesItsContentAndAdvancesItsVersion()
    {
        var page = NewPage();
        var block = NewBlock(page, "a0");
        var sut = CreateUpdateSut([block], page);

        var result = await sut.Handle(
            new UpdateBlockCommand(page.Id, block.Id, null, Json("{\"v\":1,\"runs\":[{\"text\":\"edited\",\"marks\":[]}]}"), null, 1), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        block.Content.Data.Value.Should().Be("{\"v\":1,\"runs\":[{\"text\":\"edited\",\"marks\":[]}]}");
        block.Version.Should().Be(2);
    }

    [Fact]
    public async Task UpdatingABlockToADifferentType_ChangesItInPlace()
    {
        var page = NewPage();
        var block = NewBlock(page, "a0");
        var id = block.Id;
        var sut = CreateUpdateSut([block], page);

        var result = await sut.Handle(
            new UpdateBlockCommand(page.Id, block.Id, "heading_1", Json("{\"v\":1,\"runs\":[{\"text\":\"x\",\"marks\":[]}]}"), null, 1), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        block.Id.Should().Be(id);
        block.Type.Should().Be(BlockType.Heading1);
        block.Content.Data.Value.Should().Be("{\"v\":1,\"runs\":[{\"text\":\"x\",\"marks\":[]}]}");
    }

    [Fact]
    public async Task UpdatingABlockToAnUnknownType_IsRefusedWithoutChange()
    {
        var page = NewPage();
        var block = NewBlock(page, "a0");
        var version = block.Version;
        var sut = CreateUpdateSut([block], page);

        var act = () => sut.Handle(
            new UpdateBlockCommand(page.Id, block.Id, "Embed", null, null, 1), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ValidationException>();
        block.Version.Should().Be(version);
    }

    [Fact]
    public async Task UpdatingABlockWithAStaleVersion_Answers412WithTheCurrentBlock_AndChangesNothing()
    {
        var page = NewPage();
        var block = NewBlock(page, "a0");
        block.UpdateProperties(BlockProperties.Create(JsonValue.Create("{\"align\":\"left\"}")), UserId, Now);
        var before = block.Content.Data.Value;
        var sut = CreateUpdateSut([block], page);

        var act = () => sut.Handle(
            new UpdateBlockCommand(page.Id, block.Id, null, Json("{\"v\":1,\"runs\":[{\"text\":\"x\",\"marks\":[]}]}"), null, 1), CancellationToken.None);

        var stale = (await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.PreconditionFailedException>()).Which;
        stale.ErrorCode.Should().Be("documents.block.version-conflict");
        stale.ExpectedVersion.Should().Be(1);
        stale.CurrentVersion.Should().Be(2);
        stale.Extensions["currentBlock"].Should().BeOfType<Notrelix.Application.Features.Documents.DTOs.BlockDto>()
            .Which.Version.Should().Be(2);
        block.Content.Data.Value.Should().Be(before);
    }

    [Fact]
    public async Task UpdatingABlockWithoutAVersion_IsRefused()
    {
        var page = NewPage();
        var block = NewBlock(page, "a0");
        var sut = CreateUpdateSut([block], page);

        var act = () => sut.Handle(new UpdateBlockCommand(page.Id, block.Id, null, null, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task UpdatingABlockWithInvalidContent_IsRefusedWithoutChange()
    {
        var page = NewPage();
        var block = NewBlock(page, "a0");
        var sut = CreateUpdateSut([block], page);

        var act = () => sut.Handle(new UpdateBlockCommand(page.Id, block.Id, null,
            Json("{\"v\":1,\"runs\":[{\"text\":\"x\",\"marks\":[{\"type\":\"link\",\"href\":\"javascript:alert(1)\"}]}]}"), null, 1), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        block.Version.Should().Be(1);
    }

    [Fact]
    public async Task UpdatingABlockThroughAnotherPage_IsNotFound()
    {
        var page = NewPage();
        var block = NewBlock(page, "a0");
        var sut = CreateUpdateSut([block], page);

        var act = () => sut.Handle(
            new UpdateBlockCommand(Guid.CreateVersion7(), block.Id, null, null, null, 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private const string Hello = "{\"v\":1,\"runs\":[{\"text\":\"hello\",\"marks\":[]}]}";

    [Fact]
    public async Task CreatingOnAPageAtTheBlockLimit_IsRefusedAndStoresNothing()
    {
        var page = NewPage();
        var keys = FractionalIndexGenerator.GenerateNKeysBetween(null, null, 5000);
        var blocks = keys.Select(key => NewBlock(page, key.Value)).ToList();
        var (sut, context) = CreateCreateSut(page, blocks);

        var act = () => sut.Handle(new CreateBlockCommand(page.Id, null, "paragraph", null, null, null, null), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(Notrelix.Domain.Documents.DocumentRuleCodes.Documents_Page_BlockLimitReached);
        context.Added.Should().BeEmpty();
    }

    [Fact]
    public async Task ReplayingACreateWithTheSameIdAndBody_AnswersTheStoredBlockAndAddsNone()
    {
        var page = NewPage();
        var (sut, context) = CreateCreateSut(page, []);
        var id = Guid.CreateVersion7();
        var command = new CreateBlockCommand(page.Id, id, "paragraph", Json(Hello), null, null, null);

        var first = await sut.Handle(command, CancellationToken.None);
        var replay = await sut.Handle(command, CancellationToken.None);

        replay.Succeeded.Should().BeTrue();
        replay.Data!.Id.Should().Be(first.Data!.Id);
        context.Added.Should().ContainSingle();
    }

    [Fact]
    public async Task ReplayingACreateWithTheSameIdAndAnotherBody_IsAConflict()
    {
        var page = NewPage();
        var (sut, context) = CreateCreateSut(page, []);
        var id = Guid.CreateVersion7();
        await sut.Handle(new CreateBlockCommand(page.Id, id, "paragraph", Json(Hello), null, null, null), CancellationToken.None);

        var act = () => sut.Handle(new CreateBlockCommand(page.Id, id, "heading_1", Json(Hello), null, null, null), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ConflictException>()).Which.ErrorCode.Should().Be("documents.block.id-taken");
        context.Added.Should().ContainSingle();
    }

    [Fact]
    public async Task CreatingWithAnIdOfABlockOnAnotherPage_IsAConflict_NotAReplay()
    {
        var page = NewPage();
        var other = NewPage();
        var foreign = NewBlock(other, "a0");
        var (sut, context) = CreateCreateSut(page, [foreign]);

        var act = () => sut.Handle(new CreateBlockCommand(page.Id, foreign.Id, "paragraph", null, null, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ConflictException>();
        context.Added.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatingOnAPageTheCallerCannotSee_IsNotFound()
    {
        var page = NewPage();
        var (sut, _) = CreateCreateSut(page, []);

        var act = () => sut.Handle(new CreateBlockCommand(Guid.CreateVersion7(), null, "paragraph", null, null, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ManyInsertsAtOneSpot_KeepEveryKeyShortAndInOrder_WithoutBumpingAVersion()
    {
        var page = NewPage();
        var first = NewBlock(page, "a0");
        var last = NewBlock(page, "a1");
        var blocks = new List<Block> { first, last };
        var (sut, _) = CreateCreateSut(page, blocks);

        // Each new block goes right after the first one, so every key falls between "a0" and the previous insert.
        var expected = new List<Guid>();
        for (var i = 0; i < 400; i++)
        {
            var id = Guid.CreateVersion7();
            await sut.Handle(new CreateBlockCommand(page.Id, id, "paragraph", Json(Hello), null, first.Id, null), CancellationToken.None);
            expected.Insert(0, id);
        }

        var ordered = blocks.OrderBy(b => b.Position.Value, StringComparer.Ordinal).Select(b => b.Id).ToList();
        ordered.Should().Equal([first.Id, .. expected, last.Id]);
        blocks.Should().OnlyContain(b => b.Position.Value.Length <= 32);
        blocks.Select(b => b.Position.Value).Should().OnlyHaveUniqueItems();
        blocks.Where(b => b.Id != first.Id && b.Id != last.Id).Should().OnlyContain(b => b.Version == 1);
        first.Version.Should().Be(1);
    }

    private static System.Text.Json.JsonElement Json(string json) => System.Text.Json.JsonDocument.Parse(json).RootElement.Clone();

    private static Page NewPage() => Page.Create(AccountId, WorkspaceId, "Doc", UserId, Now);

    private static Block NewBlock(Page page, string position) => Block.CreateRoot(
        AccountId, WorkspaceId, page.Id, BlockType.Text,
        BlockContent.Create(JsonValue.Create("{\"v\":1,\"runs\":[{\"text\":\"t\",\"marks\":[]}]}")), FractionalIndex.Create(position), UserId, Now);

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
        return (new CreateBlockCommandHandler(context.Object, new NoPageBlockWriteLock(), requestContext, new FixedClock()), recorded);
    }

    private static UpdateBlockCommandHandler CreateUpdateSut(List<Block> blocks, Page page)
    {
        var context = new Mock<IDocumentDbContext>();
        context.Setup(c => c.Blocks).Returns(TestDbSet.Create(blocks).Object);
        context.Setup(c => c.Pages).Returns(TestDbSet.Create([page]).Object);
        var user = new Mock<ICurrentUser>();
        user.Setup(u => u.UserId).Returns(UserId);
        return new UpdateBlockCommandHandler(context.Object, user.Object, new FixedClock());
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now.AddMinutes(5);
    }
}
