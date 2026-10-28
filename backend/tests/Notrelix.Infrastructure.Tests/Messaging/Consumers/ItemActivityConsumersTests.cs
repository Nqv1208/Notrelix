using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Context;
using Notrelix.Application.Events.WorkManagement;
using Notrelix.Application.Features.Collaboration.Activity;
using Notrelix.Application.Features.WorkManagement.Public.ItemActivity;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Collaboration;
using Notrelix.Infrastructure.Data.Projections.Search;
using Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

namespace Notrelix.Infrastructure.Tests.Messaging.Consumers;

/// <summary>
/// The Item history: each Work Management fact becomes one append-only row with actor, time, type, target and a
/// small detail; a redelivery adds nothing; a reorder inside a group is not history; the reader pages one item's
/// rows newest first inside its Workspace only.
/// </summary>
public sealed class ItemActivityConsumersTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid BoardId = Guid.CreateVersion7();
    private static readonly Guid ItemId = Guid.CreateVersion7();
    private static readonly Guid Actor = Guid.CreateVersion7();
    private static readonly Guid StatusFieldId = Guid.CreateVersion7();
    private static readonly Guid Todo = Guid.CreateVersion7();
    private static readonly Guid Doing = Guid.CreateVersion7();

    private static ItemActivityRecorder Recorder(ApplicationDbContext db) =>
        new(db, NullLogger<ItemActivityRecorder>.Instance);

    private static Mock<IWorkItemActivityFacts> Facts()
    {
        var facts = new Mock<IWorkItemActivityFacts>();
        facts.Setup(f => f.FindFieldAsync(WorkspaceId, StatusFieldId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkFieldActivityFacts("Status", true, WorkFieldValueMode.Option,
                new Dictionary<Guid, string> { [Todo] = "To do", [Doing] = "Doing" }));
        return facts;
    }

    [Fact]
    public async Task ItemCreated_RecordsOneRow_WithActorTimeTitleAndTarget()
    {
        await using var db = CreateDb();
        var message = new BoardItemCreatedIntegrationEventV2(
            Guid.CreateVersion7(), AccountId, ItemId, BoardId, WorkspaceId, "Write the plan", 1, Guid.CreateVersion7(), Actor, null, Now);

        await new ItemCreatedActivityConsumer(Recorder(db)).Consume(Context(message));

        var row = await db.WorkspaceActivityLogs.SingleAsync();
        row.ActivityType.Should().Be(ItemActivityTypes.Created);
        row.ActorUserId.Should().Be(Actor);
        row.OccurredAt.Should().Be(Now);
        row.SourceEventId.Should().Be(message.EventId);
        row.ResourceKind.Should().Be(ItemActivityTypes.ResourceKind);
        row.ResourceId.Should().Be(ItemId);
        row.ResourceDisplayName.Should().Be("Write the plan");
        row.TargetId.Should().Be(BoardId);
        row.WorkspaceId.Should().Be(WorkspaceId);
    }

    [Fact]
    public async Task ADuplicateDelivery_OfTheSameEvent_AddsNoSecondRow()
    {
        await using var db = CreateDb();
        var message = new BoardItemDeletedIntegrationEvent(
            Guid.CreateVersion7(), AccountId, WorkspaceId, ItemId, BoardId, Actor, Guid.CreateVersion7(), null, null, Now);
        var consumer = new ItemDeletedActivityConsumer(Recorder(db));

        await consumer.Consume(Context(message));
        await consumer.Consume(Context(message));

        (await db.WorkspaceActivityLogs.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task AStatusChange_RecordsTheStatusNames_NotTheOptionIds()
    {
        await using var db = CreateDb();
        var message = new BoardItemFieldValueChangedIntegrationEvent(
            Guid.CreateVersion7(), AccountId, ItemId, BoardId, StatusFieldId, WorkspaceId,
            $"\"{Todo}\"", $"\"{Doing}\"", Guid.CreateVersion7(), Actor, null, Now);

        await new ItemFieldChangedActivityConsumer(Recorder(db), Facts().Object).Consume(Context(message));

        var row = await db.WorkspaceActivityLogs.SingleAsync();
        row.ActivityType.Should().Be(ItemActivityTypes.StatusChanged);
        row.Title.Should().Be("changed the status from To do to Doing");
        var data = row.DataJson.RootElement;
        data.GetProperty("fieldName").GetString().Should().Be("Status");
        data.GetProperty("from").GetString().Should().Be("To do");
        data.GetProperty("to").GetString().Should().Be("Doing");
    }

    [Fact]
    public async Task ASensitiveFieldChange_RecordsThatItChanged_NeverTheValues()
    {
        await using var db = CreateDb();
        var secret = Guid.CreateVersion7();
        var facts = new Mock<IWorkItemActivityFacts>();
        facts.Setup(f => f.FindFieldAsync(WorkspaceId, secret, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkFieldActivityFacts("Contract value", false, WorkFieldValueMode.Hidden, new Dictionary<Guid, string>()));
        var message = new BoardItemFieldValueChangedIntegrationEvent(
            Guid.CreateVersion7(), AccountId, ItemId, BoardId, secret, WorkspaceId, "\"1000\"", "\"2000\"", Guid.CreateVersion7(), Actor, null, Now);

        await new ItemFieldChangedActivityConsumer(Recorder(db), facts.Object).Consume(Context(message));

        var row = await db.WorkspaceActivityLogs.SingleAsync();
        row.ActivityType.Should().Be(ItemActivityTypes.FieldChanged);
        row.Title.Should().Be("changed Contract value");
        row.DataJson.RootElement.TryGetProperty("from", out _).Should().BeFalse();
        row.DataJson.RootElement.TryGetProperty("to", out _).Should().BeFalse();
    }

    [Fact]
    public async Task ReorderingInsideAGroup_IsNotHistory_ButChangingGroupIs()
    {
        await using var db = CreateDb();
        var group = Guid.CreateVersion7();
        var other = Guid.CreateVersion7();
        var facts = new Mock<IWorkItemActivityFacts>();
        facts.Setup(f => f.FindGroupNameAsync(WorkspaceId, group, It.IsAny<CancellationToken>())).ReturnsAsync("Backlog");
        facts.Setup(f => f.FindGroupNameAsync(WorkspaceId, other, It.IsAny<CancellationToken>())).ReturnsAsync("Sprint 4");
        var consumer = new ItemMovedActivityConsumer(Recorder(db), facts.Object);

        await consumer.Consume(Context(Moved(group, group)));
        (await db.WorkspaceActivityLogs.CountAsync()).Should().Be(0);

        await consumer.Consume(Context(Moved(group, other)));
        var row = await db.WorkspaceActivityLogs.SingleAsync();
        row.ActivityType.Should().Be(ItemActivityTypes.MovedToGroup);
        row.Title.Should().Be("moved this item from Backlog to Sprint 4");
    }

    [Fact]
    public async Task MovingToAnotherBoard_RecordsBothBoardNamesAndTheNewKey()
    {
        await using var db = CreateDb();
        var target = Guid.CreateVersion7();
        var facts = new Mock<IWorkItemActivityFacts>();
        facts.Setup(f => f.FindBoardNameAsync(WorkspaceId, BoardId, It.IsAny<CancellationToken>())).ReturnsAsync("Website");
        facts.Setup(f => f.FindBoardNameAsync(WorkspaceId, target, It.IsAny<CancellationToken>())).ReturnsAsync("Mobile app");
        var message = new BoardItemMovedToBoardIntegrationEvent(
            Guid.CreateVersion7(), AccountId, WorkspaceId, ItemId, BoardId, target, Guid.CreateVersion7(), Guid.CreateVersion7(),
            "MOB-7", 3, Actor, Guid.CreateVersion7(), null, null, Now);

        await new ItemMovedToBoardActivityConsumer(Recorder(db), facts.Object).Consume(Context(message));

        var row = await db.WorkspaceActivityLogs.SingleAsync();
        row.ActivityType.Should().Be(ItemActivityTypes.MovedToBoard);
        row.TargetId.Should().Be(target);
        row.DataJson.RootElement.GetProperty("from").GetString().Should().Be("Website");
        row.DataJson.RootElement.GetProperty("to").GetString().Should().Be("Mobile app");
        row.DataJson.RootElement.GetProperty("itemKey").GetString().Should().Be("MOB-7");
    }

    [Fact]
    public async Task AssigningAndUnassigning_RecordThePerson_ByIdOnly()
    {
        await using var db = CreateDb();
        var person = Guid.CreateVersion7();
        var assigned = new BoardItemMemberAssignedIntegrationEvent(
            Guid.CreateVersion7(), AccountId, WorkspaceId, ItemId, person, Actor, Guid.CreateVersion7(), null, null, Now);
        var unassigned = new BoardItemMemberUnassignedIntegrationEvent(
            Guid.CreateVersion7(), AccountId, WorkspaceId, ItemId, person, Actor, Guid.CreateVersion7(), null, null, Now.AddMinutes(1));

        await new ItemAssignedActivityConsumer(Recorder(db)).Consume(Context(assigned));
        await new ItemUnassignedActivityConsumer(Recorder(db)).Consume(Context(unassigned));

        var rows = await db.WorkspaceActivityLogs.OrderBy(r => r.OccurredAt).ToListAsync();
        rows.Select(r => r.ActivityType).Should().Equal(ItemActivityTypes.Assigned, ItemActivityTypes.Unassigned);
        rows.Should().OnlyContain(r => r.ActorUserId == Actor && r.DataJson.RootElement.GetProperty("personId").GetString() == person.ToString());
    }

    [Fact]
    public async Task LabelAddedAndRemoved_RecordRowsWithTheLabelNameAndActor()
    {
        await using var db = CreateDb();
        var label = Guid.CreateVersion7();
        var added = new BoardItemLabelAddedIntegrationEvent(
            Guid.CreateVersion7(), AccountId, WorkspaceId, ItemId, label, "Bug", Actor, Guid.CreateVersion7(), null, null, Now);
        var removed = new BoardItemLabelRemovedIntegrationEvent(
            Guid.CreateVersion7(), AccountId, WorkspaceId, ItemId, label, "Bug", Actor, Guid.CreateVersion7(), null, null, Now.AddMinutes(1));

        await new ItemLabelAddedActivityConsumer(Recorder(db)).Consume(Context(added));
        await new ItemLabelAddedActivityConsumer(Recorder(db)).Consume(Context(added));
        await new ItemLabelRemovedActivityConsumer(Recorder(db)).Consume(Context(removed));

        var rows = await db.WorkspaceActivityLogs.OrderBy(r => r.OccurredAt).ToListAsync();
        rows.Select(r => r.ActivityType).Should().Equal(ItemActivityTypes.LabelAdded, ItemActivityTypes.LabelRemoved);
        rows.Should().OnlyContain(r => r.ActorUserId == Actor && r.DataJson.RootElement.GetProperty("to").GetString() == "Bug");
    }

    [Fact]
    public async Task ChecklistFacts_RecordOneRowEach_WithActorAndTitles_AndARedeliveryAddsNothing()
    {
        await using var db = CreateDb();
        var list = Guid.CreateVersion7();
        Guid Id() => Guid.CreateVersion7();
        var added = new BoardItemChecklistAddedIntegrationEvent(Id(), AccountId, WorkspaceId, ItemId, list, "Release", Actor, Id(), null, null, Now);
        var todo = new BoardItemChecklistItemAddedIntegrationEvent(Id(), AccountId, WorkspaceId, ItemId, list, "Release", "Tag build", Actor, Id(), null, null, Now.AddMinutes(1));
        var ticked = new BoardItemChecklistItemCompletionChangedIntegrationEvent(Id(), AccountId, WorkspaceId, ItemId, list, "Release", "Tag build", true, Actor, Id(), null, null, Now.AddMinutes(2));
        var unticked = new BoardItemChecklistItemCompletionChangedIntegrationEvent(Id(), AccountId, WorkspaceId, ItemId, list, "Release", "Tag build", false, Actor, Id(), null, null, Now.AddMinutes(3));
        var removed = new BoardItemChecklistItemRemovedIntegrationEvent(Id(), AccountId, WorkspaceId, ItemId, list, "Release", "Tag build", Actor, Id(), null, null, Now.AddMinutes(4));
        var renamed = new BoardItemChecklistRenamedIntegrationEvent(Id(), AccountId, WorkspaceId, ItemId, list, "Ship", Actor, Id(), null, null, Now.AddMinutes(5));
        var deleted = new BoardItemChecklistDeletedIntegrationEvent(Id(), AccountId, WorkspaceId, ItemId, list, "Ship", Actor, Id(), null, null, Now.AddMinutes(6));

        await new ItemChecklistAddedActivityConsumer(Recorder(db)).Consume(Context(added));
        await new ItemChecklistAddedActivityConsumer(Recorder(db)).Consume(Context(added));
        await new ItemChecklistItemAddedActivityConsumer(Recorder(db)).Consume(Context(todo));
        await new ItemChecklistItemCompletedActivityConsumer(Recorder(db)).Consume(Context(ticked));
        await new ItemChecklistItemCompletedActivityConsumer(Recorder(db)).Consume(Context(unticked));
        await new ItemChecklistItemRemovedActivityConsumer(Recorder(db)).Consume(Context(removed));
        await new ItemChecklistRenamedActivityConsumer(Recorder(db)).Consume(Context(renamed));
        await new ItemChecklistDeletedActivityConsumer(Recorder(db)).Consume(Context(deleted));

        var rows = await db.WorkspaceActivityLogs.OrderBy(r => r.OccurredAt).ToListAsync();
        rows.Select(r => r.ActivityType).Should().Equal(
            ItemActivityTypes.ChecklistAdded, ItemActivityTypes.ChecklistItemAdded, ItemActivityTypes.ChecklistItemCompleted,
            ItemActivityTypes.ChecklistItemReopened, ItemActivityTypes.ChecklistItemRemoved, ItemActivityTypes.ChecklistRenamed,
            ItemActivityTypes.ChecklistDeleted);
        rows.Should().OnlyContain(r => r.ActorUserId == Actor);
        rows[2].DataJson.RootElement.GetProperty("to").GetString().Should().Be("Tag build");
        rows[2].DataJson.RootElement.GetProperty("fieldName").GetString().Should().Be("Release");
    }

    [Fact]
    public async Task TheReader_PagesNewestFirst_ForOneItemInOneWorkspaceOnly()
    {
        await using var db = CreateDb();
        var otherItem = Guid.CreateVersion7();
        for (var minute = 0; minute < 5; minute++)
        {
            await Recorder(db).RecordAsync(WorkspaceId, ItemId, BoardId, ItemActivityTypes.Archived, Guid.CreateVersion7(), Actor,
                Now.AddMinutes(minute), $"row {minute}", null, null, CancellationToken.None);
        }

        await Recorder(db).RecordAsync(WorkspaceId, otherItem, BoardId, ItemActivityTypes.Archived, Guid.CreateVersion7(), Actor,
            Now.AddMinutes(9), "other item", null, null, CancellationToken.None);
        await Recorder(db).RecordAsync(Guid.CreateVersion7(), ItemId, BoardId, ItemActivityTypes.Archived, Guid.CreateVersion7(), Actor,
            Now.AddMinutes(9), "other workspace", null, null, CancellationToken.None);
        var reader = new ResourceActivityReader(db);

        var first = await reader.ListAsync(WorkspaceId, ItemActivityTypes.ResourceKind, ItemId, 1, 2, CancellationToken.None);
        var last = await reader.ListAsync(WorkspaceId, ItemActivityTypes.ResourceKind, ItemId, 3, 2, CancellationToken.None);

        first.Total.Should().Be(5);
        first.Rows.Select(r => r.Title).Should().Equal("row 4", "row 3");
        last.Rows.Select(r => r.Title).Should().Equal("row 0");
    }

    private static BoardItemMovedIntegrationEventV2 Moved(Guid from, Guid to) => new(
        Guid.CreateVersion7(), AccountId, ItemId, BoardId, WorkspaceId, from, to, 2, Guid.CreateVersion7(), Actor, null, Now);

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
            .UseInMemoryDatabase($"Notrelix-item-activity-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        // The consumer and the reader run under the tenant restored from the request or the event envelope.
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
