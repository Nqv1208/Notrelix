using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Commands.DeleteBlock;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.SharedKernel.Ordering;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>
/// A competing write against a block the caller has read is refused with 412 and the current block, and nothing
/// changes. Update, reorder and batch are covered in their own handler tests; this file covers the single delete.
/// </summary>
public sealed class BlockStaleWriteTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public async Task DeletingWithAStaleVersion_Answers412WithTheCurrentBlock_AndDeletesNothing()
    {
        var page = Page.Create(AccountId, WorkspaceId, "Doc", UserId, Now);
        var block = NewBlock(page, "a0");
        var child = Block.CreateChild(AccountId, WorkspaceId, page.Id, BlockType.Text, Content("child"), FractionalIndex.Create("a0"), UserId, Now,
            BlockAncestorPath.Create(AccountId, WorkspaceId, page.Id, block.Id, []));
        block.Edit(null, Content("edited elsewhere"), null, UserId, Now.AddMinutes(1));
        var sut = CreateSut(page, [block, child]);

        var act = () => sut.Handle(new DeleteBlockCommand(page.Id, block.Id, 1), CancellationToken.None);

        var stale = (await act.Should().ThrowAsync<PreconditionFailedException>()).Which;
        stale.CurrentVersion.Should().Be(2);
        stale.Extensions.Should().ContainKey("currentBlock");
        block.IsDeleted.Should().BeFalse();
        child.IsDeleted.Should().BeFalse("a refused delete removes nothing under the block either");
    }

    [Fact]
    public async Task DeletingWithoutAVersion_IsRefused()
    {
        var page = Page.Create(AccountId, WorkspaceId, "Doc", UserId, Now);
        var block = NewBlock(page, "a0");
        var sut = CreateSut(page, [block]);

        var act = () => sut.Handle(new DeleteBlockCommand(page.Id, block.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ValidationException>();
        block.IsDeleted.Should().BeFalse();
    }

    private static Block NewBlock(Page page, string position) =>
        Block.CreateRoot(AccountId, WorkspaceId, page.Id, BlockType.Text, Content("one"), FractionalIndex.Create(position), UserId, Now);

    private static BlockContent Content(string text) =>
        BlockContent.Create(JsonValue.Create($"{{\"v\":1,\"runs\":[{{\"text\":\"{text}\",\"marks\":[]}}]}}"));

    private static DeleteBlockCommandHandler CreateSut(Page page, List<Block> blocks)
    {
        var context = new Mock<IDocumentDbContext>();
        context.Setup(c => c.Pages).Returns(TestDbSet.Create([page]).Object);
        context.Setup(c => c.Blocks).Returns(TestDbSet.Create(blocks).Object);
        var user = new Mock<ICurrentUser>();
        user.Setup(u => u.UserId).Returns(UserId);
        return new DeleteBlockCommandHandler(context.Object, new NoPageBlockWriteLock(), user.Object, new FixedClock());
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now.AddMinutes(5);
    }
}
