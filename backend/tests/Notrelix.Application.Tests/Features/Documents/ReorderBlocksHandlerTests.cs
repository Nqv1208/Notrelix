using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Commands.ReorderBlocks;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Testing.Application.Fakes;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>
/// Reordering blocks: the server makes the sort key from the siblings named, each item carries the version the
/// editor read, an unchanged place changes nothing, and a refused item leaves every block as it was.
/// </summary>
public sealed class ReorderBlocksHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public async Task MovingABlockAfterASibling_PutsItBetweenThatSiblingAndTheNextOne()
    {
        var (page, a, b, c) = ThreeBlocks();
        var sut = CreateSut(page, [a, b, c]);

        var result = await sut.Handle(Reorder(page, new ReorderBlockItem(a.Id, a.Version, null, b.Id, null, null)), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        string.CompareOrdinal(a.Position.Value, b.Position.Value).Should().BePositive();
        string.CompareOrdinal(a.Position.Value, c.Position.Value).Should().BeNegative();
        result.Data!.Select(block => block.Id).Should().Equal(a.Id);
    }

    [Fact]
    public async Task MovingABlockBeforeTheFirstSibling_PutsItAtTheTop()
    {
        var (page, a, b, c) = ThreeBlocks();
        var sut = CreateSut(page, [a, b, c]);

        await sut.Handle(Reorder(page, new ReorderBlockItem(c.Id, c.Version, null, null, a.Id, null)), CancellationToken.None);

        string.CompareOrdinal(c.Position.Value, a.Position.Value).Should().BeNegative();
    }

    [Fact]
    public async Task MovingABlockToWhereItAlreadySits_ChangesNothing()
    {
        var (page, a, b, c) = ThreeBlocks();
        var sut = CreateSut(page, [a, b, c]);
        var versions = new[] { a.Version, b.Version, c.Version };
        var pageVersion = page.Version;

        var result = await sut.Handle(Reorder(page, new ReorderBlockItem(b.Id, b.Version, null, a.Id, null, null)), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        new[] { a.Version, b.Version, c.Version }.Should().Equal(versions);
        page.Version.Should().Be(pageVersion, "a request that moved nothing announces no page save");
    }

    [Fact]
    public async Task MovingABlockWithAStaleVersion_Answers412_AndMovesNothing()
    {
        var (page, a, b, c) = ThreeBlocks();
        a.UpdateProperties(BlockProperties.Create(JsonValue.Create("{\"align\":\"left\"}")), UserId, Now);
        var key = a.Position.Value;
        var sut = CreateSut(page, [a, b, c]);

        var act = () => sut.Handle(Reorder(page, new ReorderBlockItem(a.Id, 1, null, c.Id, null, null)), CancellationToken.None);

        var stale = (await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.PreconditionFailedException>()).Which;
        stale.ErrorCode.Should().Be("documents.block.version-conflict");
        a.Position.Value.Should().Be(key);
        a.Version.Should().Be(2);
    }

    [Fact]
    public async Task APositionKeyWithoutANamedSibling_IsRefused()
    {
        var (page, a, b, c) = ThreeBlocks();
        var sut = CreateSut(page, [a, b, c]);

        var act = () => sut.Handle(Reorder(page, new ReorderBlockItem(a.Id, a.Version, null, null, null, "a5")), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task APositionKeyOutsideTheNamedSiblings_IsRefused()
    {
        var (page, a, b, c) = ThreeBlocks();
        var sut = CreateSut(page, [a, b, c]);

        var act = () => sut.Handle(Reorder(page, new ReorderBlockItem(c.Id, c.Version, null, a.Id, null, "a9")), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ValidationException>();
        c.Position.Value.Should().Be("a2");
    }

    [Fact]
    public async Task ReorderingATrashedPage_IsRefused()
    {
        var (page, a, b, c) = ThreeBlocks();
        page.Archive(UserId, Now);
        var sut = CreateSut(page, [a, b, c]);

        var act = () => sut.Handle(Reorder(page, new ReorderBlockItem(a.Id, a.Version, null, b.Id, null, null)), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
    }

    [Fact]
    public async Task ReorderingABlockOfAnotherPage_IsNotFound()
    {
        var (page, a, b, c) = ThreeBlocks();
        var sut = CreateSut(page, [a, b, c]);

        var act = () => sut.Handle(Reorder(page, new ReorderBlockItem(Guid.CreateVersion7(), 1, null, a.Id, null, null)), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static ReorderBlocksCommand Reorder(Page page, params ReorderBlockItem[] items) => new(page.Id, [.. items]);

    private static (Page Page, Block A, Block B, Block C) ThreeBlocks()
    {
        var page = Page.Create(AccountId, WorkspaceId, "Doc", UserId, Now);
        return (page, NewBlock(page, "a0"), NewBlock(page, "a1"), NewBlock(page, "a2"));
    }

    private static Block NewBlock(Page page, string position) => Block.CreateRoot(
        AccountId, WorkspaceId, page.Id, BlockType.Text,
        BlockContent.Create(JsonValue.Create("{\"v\":1,\"runs\":[{\"text\":\"t\",\"marks\":[]}]}")), FractionalIndex.Create(position), UserId, Now);

    private static ReorderBlocksCommandHandler CreateSut(Page page, List<Block> blocks)
    {
        var context = new Mock<IDocumentDbContext>();
        context.Setup(c => c.Pages).Returns(TestDbSet.Create([page]).Object);
        context.Setup(c => c.Blocks).Returns(TestDbSet.Create(blocks).Object);
        var requestContext = new FakeCurrentRequestContext().AsUser(UserId);
        requestContext.Tenant.SetWorkspace(AccountId, WorkspaceId, UserId);
        return new ReorderBlocksCommandHandler(context.Object, requestContext, new FixedClock());
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now.AddMinutes(5);
    }
}
