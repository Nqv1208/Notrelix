using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Notrelix.Application.Common.Context;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Features.Documents.Realtime;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Data.Projections.Search;

namespace Notrelix.Infrastructure.Tests.Data;

/// <summary>UC-COL-E01 / UC-DCT-K01: a save that touches a page announces it once on that page's own stream.</summary>
public class PageRealtimeInterceptorTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid Author = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public async Task A_block_saved_on_a_page_announces_the_page_on_its_own_stream()
    {
        var (context, collector) = Create();
        await using var _ = context;
        var page = await SeedPageAsync(context, collector);

        context.Blocks.Add(NewBlock(page, "a0"));
        await context.SaveChangesAsync();

        var frame = Frames(collector).Should().ContainSingle().Subject;
        frame.TopicNamespace.Should().Be("page");
        frame.ResourceKind.Should().Be("Page");
        frame.ResourceId.Should().Be(page.Id);
        frame.StreamKey.Should().Be($"page:Page:{page.Id:N}");
        frame.ChangeKind.Should().Be(PageRealtime.ChangedEvent);
        frame.PayloadContract.Should().Be(PageRealtime.PayloadContract);
        frame.WorkspaceId.Should().Be(WorkspaceId);
        frame.AccountId.Should().Be(AccountId);
    }

    [Fact]
    public async Task Many_blocks_and_a_rename_of_one_page_announce_it_once()
    {
        var (context, collector) = Create();
        await using var _ = context;
        var page = await SeedPageAsync(context, collector);

        context.Blocks.Add(NewBlock(page, "a0"));
        context.Blocks.Add(NewBlock(page, "a1"));
        page.Rename("Kế hoạch Q4", Author, Now);
        await context.SaveChangesAsync();

        Frames(collector).Should().ContainSingle().Which.ResourceId.Should().Be(page.Id);
    }

    [Fact]
    public async Task A_comment_on_a_page_announces_the_page()
    {
        var (context, collector) = Create();
        await using var _ = context;
        var page = await SeedPageAsync(context, collector);

        context.Comments.Add(NewComment("documents.page", page.Id));
        await context.SaveChangesAsync();

        Frames(collector).Should().ContainSingle().Which.ResourceId.Should().Be(page.Id);
    }

    [Fact]
    public async Task A_comment_on_a_board_item_announces_no_page()
    {
        var (context, collector) = Create();
        await using var _ = context;

        context.Comments.Add(NewComment("work-management.board-item", Guid.NewGuid()));
        await context.SaveChangesAsync();

        Frames(collector).Should().BeEmpty();
    }

    [Fact]
    public async Task Two_pages_touched_in_one_save_announce_each()
    {
        var (context, collector) = Create();
        await using var _ = context;
        var first = await SeedPageAsync(context, collector);
        var second = await SeedPageAsync(context, collector);

        context.Blocks.Add(NewBlock(first, "a0"));
        context.Blocks.Add(NewBlock(second, "a0"));
        await context.SaveChangesAsync();

        Frames(collector).Select(f => f.ResourceId).Should().BeEquivalentTo([first.Id, second.Id]);
    }

    private static async Task<Page> SeedPageAsync(ApplicationDbContext context, IIntegrationEventCollector collector)
    {
        var page = Page.Create(AccountId, WorkspaceId, "Trang", Author, Now);
        context.Pages.Add(page);
        await context.SaveChangesAsync();
        collector.Acknowledge(collector.CapturePending());
        return page;
    }

    private static Block NewBlock(Page page, string position) => Block.CreateRoot(
        AccountId, WorkspaceId, page.Id, BlockType.Text,
        BlockContent.Create(JsonValue.Create("{\"v\":1,\"runs\":[{\"text\":\"hi\",\"marks\":[]}]}")),
        FractionalIndex.Create(position), Author, Now);

    private static Comment NewComment(string kind, Guid resourceId) => Comment.Create(
        AccountId, WorkspaceId, ResourceRef.Create(ResourceKind.Create(kind), resourceId, WorkspaceId), "Đã xem", Author, Now);

    private static List<RealtimeResourceChangedV1> Frames(IIntegrationEventCollector collector) =>
        collector.CapturePending().Events.OfType<RealtimeResourceChangedV1>().ToList();

    private static (ApplicationDbContext Context, IIntegrationEventCollector Collector) Create()
    {
        var time = new Mock<IDateTimeProvider>();
        time.Setup(x => x.UtcNow).Returns(Now);
        var execution = new Mock<IExecutionContextReader>();
        execution.Setup(x => x.UserId).Returns(Author);
        var collector = new IntegrationEventCollector();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-page-realtime-{Guid.NewGuid():N}")
            .AddInterceptors(new PageRealtimeInterceptor(collector, execution.Object, time.Object))
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        return (new TestApplicationDbContext(options), collector);
    }

    private sealed class TestApplicationDbContext : ApplicationDbContext
    {
        public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<CustomRole>().Ignore(x => x.Permissions);
            modelBuilder.Entity<SearchDocumentRecord>().Ignore(x => x.SearchVector);
        }
    }
}
