using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Features.Collaboration.Notifications.Realtime;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Data.Notifications;
using Notrelix.Infrastructure.Data.Projections.Search;

namespace Notrelix.Infrastructure.Tests.Data;

/// <summary>UC-COL-E02: a written Inbox entry announces itself on its recipient's own stream, once per person and Workspace.</summary>
public class InboxRealtimeInterceptorTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public async Task A_new_entry_announces_an_inbox_change_on_each_recipients_own_stream()
    {
        var (context, collector) = Create();
        await using var _ = context;
        var anna = Guid.NewGuid();
        var binh = Guid.NewGuid();
        var item = AddItem(context);
        AddRecipient(context, item, anna);
        AddRecipient(context, item, binh);

        await context.SaveChangesAsync();

        var frames = Frames(collector);
        frames.Should().HaveCount(2);
        frames.Select(f => f.ResourceId).Should().BeEquivalentTo([anna, binh]);
        frames.Should().AllSatisfy(f =>
        {
            f.TopicNamespace.Should().Be("user");
            f.ResourceKind.Should().Be("notifications");
            f.StreamKey.Should().Be($"user:notifications:{f.ResourceId:N}");
            f.ChangeKind.Should().Be(InboxRealtime.ChangedEvent);
            f.PayloadContract.Should().Be(InboxRealtime.PayloadContract);
            f.WorkspaceId.Should().Be(WorkspaceId);
            f.Payload.GetProperty("workspaceId").GetGuid().Should().Be(WorkspaceId);
        });
    }

    [Fact]
    public async Task Several_entries_for_one_person_in_one_workspace_announce_once()
    {
        var (context, collector) = Create();
        await using var _ = context;
        var anna = Guid.NewGuid();
        AddRecipient(context, AddItem(context), anna);
        AddRecipient(context, AddItem(context), anna);

        await context.SaveChangesAsync();

        Frames(collector).Should().ContainSingle().Which.ResourceId.Should().Be(anna);
    }

    [Fact]
    public async Task A_tracked_change_to_an_entry_announces_it()
    {
        var (context, collector) = Create();
        await using var _ = context;
        var anna = Guid.NewGuid();
        var recipient = AddRecipient(context, AddItem(context), anna);
        await context.SaveChangesAsync();
        collector.Acknowledge(collector.CapturePending());

        recipient.MarkAsRead(Now);
        await context.SaveChangesAsync();

        Frames(collector).Should().ContainSingle().Which.ResourceId.Should().Be(anna);
    }

    [Fact]
    public async Task A_save_without_inbox_entries_announces_nothing()
    {
        var (context, collector) = Create();
        await using var _ = context;
        AddItem(context);

        await context.SaveChangesAsync();

        Frames(collector).Should().BeEmpty();
    }

    private static List<RealtimeResourceChangedV1> Frames(IIntegrationEventCollector collector) =>
        collector.CapturePending().Events.OfType<RealtimeResourceChangedV1>().ToList();

    private static NotificationItemRecord AddItem(ApplicationDbContext context)
    {
        var item = NotificationItemRecord.Create(
            AccountId, WorkspaceId, "collaboration", "mention.created", NotificationSeverity.Info, "You were mentioned", Now);
        context.NotificationItems.Add(item);
        return item;
    }

    private static NotificationRecipientRecord AddRecipient(ApplicationDbContext context, NotificationItemRecord item, Guid userId)
    {
        var recipient = NotificationRecipientRecord.Create(AccountId, item.Id, WorkspaceId, userId, Now);
        context.NotificationRecipients.Add(recipient);
        return recipient;
    }

    private static (ApplicationDbContext Context, IIntegrationEventCollector Collector) Create()
    {
        var time = new Mock<IDateTimeProvider>();
        time.Setup(x => x.UtcNow).Returns(Now);
        var collector = new IntegrationEventCollector();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-inbox-realtime-{Guid.NewGuid():N}")
            .AddInterceptors(new InboxRealtimeInterceptor(collector, time.Object))
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
