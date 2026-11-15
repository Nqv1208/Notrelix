using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Queries.GetPageBlocks;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>The block read of a page is a cursor page of at most 1 000 in position order (LD14).</summary>
public sealed class PageBlockPagingTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public async Task APageOfTwoAndAHalfThousandBlocks_IsReadOnceInThreeWindows_InPositionOrder()
    {
        var (page, blocks) = Seed(2500);
        var sut = Handler(page, blocks);

        var seen = new List<Guid>();
        var sizes = new List<int>();
        string? cursor = null;
        do
        {
            var window = (await sut.Handle(new GetPageBlocksQuery(page.Id, cursor), CancellationToken.None)).Data!;
            sizes.Add(window.Items.Length);
            seen.AddRange(window.Items.Select(b => b.Id));
            cursor = window.NextCursor;
        } while (cursor is not null);

        sizes.Should().Equal(1000, 1000, 500);
        seen.Should().Equal(blocks.OrderBy(b => b.Position.Value, StringComparer.Ordinal).Select(b => b.Id));
    }

    [Fact]
    public async Task APageOfExactlyOneWindow_HasNoNextCursor_AndALimitNeverExceedsAThousand()
    {
        var (page, blocks) = Seed(1000);
        var sut = Handler(page, blocks);

        var all = (await sut.Handle(new GetPageBlocksQuery(page.Id), CancellationToken.None)).Data!;
        var few = (await sut.Handle(new GetPageBlocksQuery(page.Id, Limit: 7), CancellationToken.None)).Data!;

        all.Items.Should().HaveCount(1000);
        all.NextCursor.Should().BeNull();
        few.Items.Should().HaveCount(7);
        few.NextCursor.Should().NotBeNull();
        new GetPageBlocksQueryValidator().Validate(new GetPageBlocksQuery(page.Id, Limit: 1001)).IsValid.Should().BeFalse();
        new GetPageBlocksQueryValidator().Validate(new GetPageBlocksQuery(page.Id, Cursor: "not-a-cursor")).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ABlockAddedBeforeTheCursor_DoesNotShiftTheNextWindow()
    {
        var (page, blocks) = Seed(30);
        var sut = Handler(page, blocks);
        var first = (await sut.Handle(new GetPageBlocksQuery(page.Id, Limit: 10), CancellationToken.None)).Data!;
        blocks.Add(NewBlock(page, FractionalIndexGenerator.GenerateKeyBetween(null, blocks.Min(b => b.Position))));

        var second = (await sut.Handle(new GetPageBlocksQuery(page.Id, first.NextCursor, 10), CancellationToken.None)).Data!;

        second.Items.Select(b => b.Position).Should().NotIntersectWith(first.Items.Select(b => b.Position));
        second.Items.Should().HaveCount(10);
    }

    private static (Page, List<Block>) Seed(int count)
    {
        var page = Page.Create(AccountId, WorkspaceId, "Big", UserId, Now);
        var keys = FractionalIndexGenerator.GenerateNKeysBetween(null, null, count);
        return (page, keys.Select(key => NewBlock(page, key)).ToList());
    }

    private static Block NewBlock(Page page, FractionalIndex position) =>
        Block.CreateRoot(AccountId, WorkspaceId, page.Id, BlockType.Text,
            BlockContent.Create(JsonValue.Create("{\"v\":1,\"runs\":[{\"text\":\"x\",\"marks\":[]}]}")),
            position, UserId, Now);

    private static GetPageBlocksQueryHandler Handler(Page page, List<Block> blocks)
    {
        var context = new Mock<IDocumentDbContext>();
        context.Setup(c => c.Pages).Returns(TestDbSet.Create([page]).Object);
        context.Setup(c => c.Blocks).Returns(TestDbSet.Create(blocks).Object);
        return new GetPageBlocksQueryHandler(context.Object);
    }
}
