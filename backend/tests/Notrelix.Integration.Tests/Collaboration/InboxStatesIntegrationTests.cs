using Notrelix.Application.Features.Collaboration.Public.Notifications;
using Notrelix.Infrastructure.CrossContext.Collaboration.Notifications;
using Notrelix.Infrastructure.Data.Notifications;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Collaboration;

/// <summary>
/// The inbox keeps read, done, snooze and mute as separate states of one person's own entries: done leaves the
/// default list, a snooze hides an entry until its time and then returns it unread (evaluated at read time),
/// and a mute is the person's own per thread. Runs the production adapter against real PostgreSQL.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class InboxStatesIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);
    private const string ItemKind = "work-management.board-item";

    private readonly PostgresTestContainer _db;

    public InboxStatesIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private async Task<Guid> SeedAsync(Guid accountId, Guid workspaceId, Guid userId, Guid threadId, string type = "mention.created")
    {
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        var item = NotificationItemRecord.Create(
            accountId, workspaceId, "collaboration", type, NotificationSeverity.Info, "Title", T0,
            resourceType: ItemKind, resourceId: threadId, body: "You were mentioned");
        seed.NotificationItems.Add(item);
        seed.NotificationRecipients.Add(NotificationRecipientRecord.Create(accountId, item.Id, workspaceId, userId, T0));
        await seed.SaveChangesAsync();
        return item.Id;
    }

    private PostgresNotificationInbox InboxAt(DateTimeOffset now)
    {
        var context = _db.CreateOwnerContext(SystemTenant());
        return new PostgresNotificationInbox(context, FakeDateTimeProvider.WithFixedTime(now));
    }

    private static async Task<List<Guid>> IdsAsync(
        PostgresNotificationInbox inbox, Guid workspaceId, Guid userId, InboxView view) =>
        (await inbox.ListAsync(workspaceId, userId, view, null, null, 50, CancellationToken.None))
        .Select(n => n.NotificationId).ToList();

    [Fact]
    public async Task Done_LeavesTheDefaultListAndShowsInTheDoneView_AndReopenBringsItBack()
    {
        var (account, ws, user) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var keep = await SeedAsync(account, ws, user, Guid.NewGuid());
        var finished = await SeedAsync(account, ws, user, Guid.NewGuid());
        var inbox = InboxAt(T0.AddMinutes(1));

        (await inbox.MarkDoneAsync(ws, user, finished, T0.AddMinutes(1), CancellationToken.None)).Should().BeTrue();

        (await IdsAsync(inbox, ws, user, InboxView.Inbox)).Should().Equal(keep);
        (await IdsAsync(inbox, ws, user, InboxView.Done)).Should().Equal(finished);
        (await inbox.UnreadByWorkspaceAsync(user, CancellationToken.None))
            .Should().ContainSingle(c => c.WorkspaceId == ws && c.Unread == 1, "done is read and no longer counts");

        (await inbox.ReopenAsync(ws, user, finished, T0.AddMinutes(2), CancellationToken.None)).Should().BeTrue();
        (await IdsAsync(inbox, ws, user, InboxView.Done)).Should().BeEmpty();
        (await IdsAsync(inbox, ws, user, InboxView.Inbox)).Should().BeEquivalentTo([keep, finished]);
    }

    [Fact]
    public async Task Snooze_HidesTheEntryUntilItsTime_ThenItReturnsUnread()
    {
        var (account, ws, user) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var id = await SeedAsync(account, ws, user, Guid.NewGuid());
        var writer = InboxAt(T0);
        await writer.MarkReadAsync(ws, user, id, T0, CancellationToken.None);
        (await writer.SnoozeAsync(ws, user, id, T0.AddHours(2), T0, CancellationToken.None)).Should().BeTrue();

        var during = InboxAt(T0.AddHours(1));
        (await IdsAsync(during, ws, user, InboxView.Inbox)).Should().BeEmpty();
        (await IdsAsync(during, ws, user, InboxView.Snoozed)).Should().Equal(id);
        (await during.UnreadByWorkspaceAsync(user, CancellationToken.None)).Should().BeEmpty();
        (await during.MarkAllReadAsync(ws, user, T0.AddHours(1), CancellationToken.None)).Should().Be(0,
            "a snoozed entry is not in the inbox, so read-all leaves it alone");

        var after = InboxAt(T0.AddHours(3));
        var back = (await after.ListAsync(ws, user, InboxView.Inbox, null, null, 50, CancellationToken.None)).Should().ContainSingle().Subject;
        back.NotificationId.Should().Be(id);
        back.Unread.Should().BeTrue("a snooze that ran out returns the entry unread");
        back.SnoozedUntil.Should().BeNull();
        (await IdsAsync(after, ws, user, InboxView.Snoozed)).Should().BeEmpty();
    }

    [Fact]
    public async Task EntriesAreTheRecipientsOwn_AnotherMemberCannotDoneSnoozeOrMuteThem()
    {
        var (account, ws, user) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var stranger = Guid.NewGuid();
        var id = await SeedAsync(account, ws, user, Guid.NewGuid());
        var inbox = InboxAt(T0);

        (await inbox.MarkDoneAsync(ws, stranger, id, T0, CancellationToken.None)).Should().BeFalse();
        (await inbox.SnoozeAsync(ws, stranger, id, T0.AddHours(1), T0, CancellationToken.None)).Should().BeFalse();
        (await inbox.MuteThreadAsync(ws, stranger, id, T0, CancellationToken.None)).Should().BeFalse();

        (await IdsAsync(inbox, ws, user, InboxView.Inbox)).Should().Equal(id);
    }

    [Fact]
    public async Task NeedsReply_HoldsForAMentionUntilItIsRepliedToOrDone()
    {
        var (account, ws, user) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var mention = await SeedAsync(account, ws, user, Guid.NewGuid());
        var assigned = await SeedAsync(account, ws, user, Guid.NewGuid(), "item.assigned");
        var other = await SeedAsync(account, ws, user, Guid.NewGuid());
        var inbox = InboxAt(T0);

        var before = await inbox.ListAsync(ws, user, InboxView.Inbox, null, null, 50, CancellationToken.None);
        before.Single(n => n.NotificationId == mention).NeedsReply.Should().BeTrue();
        before.Single(n => n.NotificationId == assigned).NeedsReply.Should().BeFalse("only a mention waits for a reply");

        await inbox.MarkRepliedAsync(ws, user, mention, T0, CancellationToken.None);
        await inbox.MarkDoneAsync(ws, user, other, T0, CancellationToken.None);

        var after = await inbox.ListAsync(ws, user, InboxView.Inbox, null, null, 50, CancellationToken.None);
        after.Single(n => n.NotificationId == mention).NeedsReply.Should().BeFalse();
        var done = await inbox.ListAsync(ws, user, InboxView.Done, null, null, 50, CancellationToken.None);
        done.Single().NeedsReply.Should().BeFalse("done ends the wait too");
    }

    [Fact]
    public async Task Mute_IsThePersonsOwnPerThread_AndUnmuteLiftsIt()
    {
        var (account, ws, user) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var thread = Guid.NewGuid();
        var id = await SeedAsync(account, ws, user, thread);

        await using var context = _db.CreateOwnerContext(SystemTenant());
        var inbox = new PostgresNotificationInbox(context, FakeDateTimeProvider.WithFixedTime(T0));
        (await inbox.MuteThreadAsync(ws, user, id, T0, CancellationToken.None)).Should().BeTrue();
        await context.SaveChangesAsync();
        (await inbox.MuteThreadAsync(ws, user, id, T0, CancellationToken.None)).Should().BeTrue("a repeat is a no-op");
        await context.SaveChangesAsync();

        (await context.NotificationThreadMutes.CountAsync(m => m.UserId == user && m.ResourceId == thread)).Should().Be(1);
        (await inbox.ListAsync(ws, user, InboxView.Inbox, null, null, 50, CancellationToken.None))
            .Single().Muted.Should().BeTrue();

        (await inbox.UnmuteThreadAsync(ws, user, id, CancellationToken.None)).Should().BeTrue();
        (await inbox.ListAsync(ws, user, InboxView.Inbox, null, null, 50, CancellationToken.None))
            .Single().Muted.Should().BeFalse();
    }

    [Fact]
    public async Task ACommentOnTheThreadByTheMentionedPerson_EndsNeedsReplyOnlyForThem()
    {
        var (account, ws, user) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var other = Guid.NewGuid();
        var thread = Guid.NewGuid();
        var mine = await SeedAsync(account, ws, user, thread);
        var theirs = await SeedAsync(account, ws, other, thread);
        var elsewhere = await SeedAsync(account, ws, user, Guid.NewGuid());

        await using var context = _db.CreateOwnerContext(SystemTenant());
        var notifier = new Notrelix.Infrastructure.Messaging.Consumers.Collaboration.ItemFollowerNotifier(context, context, null!);
        await notifier.MarkMentionsRepliedAsync(ws, ItemKind, thread, user, T0.AddMinutes(5), CancellationToken.None);

        var inbox = new PostgresNotificationInbox(context, FakeDateTimeProvider.WithFixedTime(T0.AddMinutes(6)));
        (await inbox.ListAsync(ws, user, InboxView.Inbox, null, null, 50, CancellationToken.None))
            .ToDictionary(n => n.NotificationId, n => n.NeedsReply)
            .Should().Contain(new KeyValuePair<Guid, bool>(mine, false))
            .And.Contain(new KeyValuePair<Guid, bool>(elsewhere, true));
        (await inbox.ListAsync(ws, other, InboxView.Inbox, null, null, 50, CancellationToken.None))
            .Single(n => n.NotificationId == theirs).NeedsReply.Should().BeTrue();
    }
}
