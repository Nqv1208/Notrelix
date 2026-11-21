using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Context;
using Notrelix.Application.Events.Documents;
using Notrelix.Application.Features.Collaboration.Activity;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Collaboration;
using Notrelix.Infrastructure.Data.Projections.Search;
using Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

namespace Notrelix.Infrastructure.Tests.Messaging.Consumers;

/// <summary>
/// Page activity: each Documents fact becomes one append-only row (actor, time, type); a redelivery adds
/// nothing; the reader pages one page's rows newest first inside its Workspace only.
/// </summary>
public sealed class PageActivityConsumersTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid PageId = Guid.CreateVersion7();
    private static readonly Guid Actor = Guid.CreateVersion7();

    private static ItemActivityRecorder Recorder(ApplicationDbContext db) =>
        new(db, NullLogger<ItemActivityRecorder>.Instance,
            new Mock<Notrelix.Application.Features.Automation.Public.ActivityAttribution.IAutomationActivityFacts>().Object);

    [Fact]
    public async Task ARename_RecordsOneRow_WithBothTitles_UnderThePageResource()
    {
        await using var db = CreateDb();
        var message = new PageRenamedIntegrationEvent(
            Guid.CreateVersion7(), AccountId, PageId, WorkspaceId, "Old plan", "New plan", Guid.CreateVersion7(), Actor, null, Now);

        await new PageRenamedActivityConsumer(Recorder(db)).Consume(Context(message));

        var row = await db.WorkspaceActivityLogs.SingleAsync();
        row.ActivityType.Should().Be(PageActivityTypes.Renamed);
        row.ResourceKind.Should().Be(PageActivityTypes.ResourceKind);
        row.ResourceId.Should().Be(PageId);
        row.ActorUserId.Should().Be(Actor);
        row.OccurredAt.Should().Be(Now);
        row.WorkspaceId.Should().Be(WorkspaceId);
        row.DataJson.RootElement.GetProperty("from").GetString().Should().Be("Old plan");
        row.DataJson.RootElement.GetProperty("to").GetString().Should().Be("New plan");
    }

    [Fact]
    public async Task MoveTrashAndRestore_EachRecordTheirType_AndARedeliveryAddsNothing()
    {
        await using var db = CreateDb();
        var moved = new PageMovedIntegrationEvent(Guid.CreateVersion7(), AccountId, PageId, WorkspaceId, null, Guid.CreateVersion7(), Guid.CreateVersion7(), Actor, null, Now);
        var trashed = new PageArchivedIntegrationEvent(Guid.CreateVersion7(), AccountId, PageId, WorkspaceId, Guid.CreateVersion7(), Actor, null, Now.AddMinutes(1));
        var restored = new PageRestoredIntegrationEvent(Guid.CreateVersion7(), AccountId, PageId, WorkspaceId, Guid.CreateVersion7(), Actor, null, Now.AddMinutes(2));

        await new PageMovedActivityConsumer(Recorder(db)).Consume(Context(moved));
        await new PageTrashedActivityConsumer(Recorder(db)).Consume(Context(trashed));
        var restoredConsumer = new PageRestoredActivityConsumer(Recorder(db));
        await restoredConsumer.Consume(Context(restored));
        await restoredConsumer.Consume(Context(restored));

        var types = await db.WorkspaceActivityLogs.OrderBy(r => r.OccurredAt).Select(r => r.ActivityType).ToListAsync();
        types.Should().Equal(PageActivityTypes.Moved, PageActivityTypes.Trashed, PageActivityTypes.Restored);
    }

    [Fact]
    public async Task AFactWithoutAWorkspace_RecordsNothing()
    {
        await using var db = CreateDb();
        var message = new PageRestoredIntegrationEvent(Guid.CreateVersion7(), AccountId, PageId, null, Guid.CreateVersion7(), Actor, null, Now);

        await new PageRestoredActivityConsumer(Recorder(db)).Consume(Context(message));

        (await db.WorkspaceActivityLogs.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task TheReader_PagesNewestFirst_ForOnePageInOneWorkspaceOnly()
    {
        await using var db = CreateDb();
        var consumer = new PageMovedActivityConsumer(Recorder(db));
        for (var i = 0; i < 3; i++)
            await consumer.Consume(Context(new PageMovedIntegrationEvent(
                Guid.CreateVersion7(), AccountId, PageId, WorkspaceId, null, Guid.CreateVersion7(), Guid.CreateVersion7(), Actor, null, Now.AddMinutes(i))));
        await consumer.Consume(Context(new PageMovedIntegrationEvent(
            Guid.CreateVersion7(), AccountId, Guid.CreateVersion7(), WorkspaceId, null, Guid.CreateVersion7(), Guid.CreateVersion7(), Actor, null, Now)));

        var reader = new ResourceActivityReader(db);
        var first = await reader.ListAsync(WorkspaceId, PageActivityTypes.ResourceKind, PageId, 1, 2, CancellationToken.None);
        var other = await reader.ListAsync(Guid.CreateVersion7(), PageActivityTypes.ResourceKind, PageId, 1, 2, CancellationToken.None);

        first.Total.Should().Be(3);
        first.Rows.Select(r => r.OccurredAt).Should().BeInDescendingOrder().And.HaveCount(2);
        other.Total.Should().Be(0);
    }

    private static ConsumeContext<T> Context<T>(T message) where T : class
    {
        var context = new Mock<ConsumeContext<T>>();
        context.SetupGet(c => c.Message).Returns(message);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return context.Object;
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-page-activity-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        var tenant = new Mock<ICurrentTenantContext>();
        tenant.SetupGet(t => t.IsSystemContext).Returns(true);
        return new TestApplicationDbContext(options, tenant.Object);
    }

    private sealed class TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantContext tenant)
        : ApplicationDbContext(options, tenant)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<CustomRole>().Ignore(x => x.Permissions);
            modelBuilder.Entity<SearchDocumentRecord>().Ignore(x => x.SearchVector);
        }
    }
}
