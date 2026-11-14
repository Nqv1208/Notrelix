using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Context;
using Notrelix.Application.Features.Collaboration.Public.Watchers;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Public.ItemNotifications;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Notifications;
using Notrelix.Infrastructure.Data.Projections.Search;
using Notrelix.Infrastructure.Messaging.Consumers.Collaboration;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Infrastructure.Tests.Messaging.Consumers;

/// <summary>UC-COL-D05: followers hear about a change once each, except the actor, a muter and anyone who lost the board.</summary>
public sealed class ItemFollowerNotifierFanOutTests
{
    private static readonly Guid Account = Guid.NewGuid();
    private static readonly Guid Workspace = Guid.NewGuid();
    private static readonly Guid Board = Guid.NewGuid();
    private static readonly Guid Item = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly Guid Follower = Guid.NewGuid();
    private static readonly Guid Muter = Guid.NewGuid();
    private static readonly Guid Outsider = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task EachFollowerWhoCanSeeTheBoardGetsOneEntry_ButNotTheActorMuterOrOutsider()
    {
        var (context, notifier) = Build();
        var message = Guid.NewGuid();

        await Notify(notifier, message);

        var recipients = await context.NotificationRecipients.Select(r => r.RecipientUserId).ToListAsync();
        recipients.Should().BeEquivalentTo([Follower]);
    }

    [Fact]
    public async Task ARedeliveryOfTheSameMessageAddsNothing()
    {
        var (context, notifier) = Build();
        var message = Guid.NewGuid();

        await Notify(notifier, message);
        await Notify(notifier, message);

        context.NotificationItems.Should().ContainSingle();
        context.NotificationRecipients.Should().ContainSingle();
    }

    [Fact]
    public async Task ADifferentMessageNotifiesAgain()
    {
        var (context, notifier) = Build();

        await Notify(notifier, Guid.NewGuid());
        await Notify(notifier, Guid.NewGuid());

        context.NotificationItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task AnItemThatIsGoneNotifiesNobody()
    {
        var (context, notifier) = Build(itemExists: false);

        await Notify(notifier, Guid.NewGuid());

        context.NotificationItems.Should().BeEmpty("a missing item fails closed");
    }

    private static Task Notify(ItemFollowerNotifier notifier, Guid message) =>
        notifier.NotifyFollowersAsync(Account, Workspace, Item, Actor, message,
            NotificationTypes.ItemAssignedProducedType, "Changed", null, [], Now, CancellationToken.None);

    private static (ApplicationDbContext Context, ItemFollowerNotifier Notifier) Build(bool itemExists = true)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        var context = new TestContext(options, tenant);
        context.NotificationThreadMutes.Add(NotificationThreadMuteRecord.Create(
            Account, Workspace, Muter, ItemFollowerNotifier.BoardItemKind, Item, Now));
        context.SaveChanges();

        var work = new Mock<IWorkItemNotificationFacts>();
        work.Setup(w => w.FindAsync(Workspace, Item, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(itemExists ? new WorkItemNotificationFacts(Board, "Item", false, null) : null);
        var visibility = new Mock<IResourceVisibility>();
        visibility.Setup(v => v.VisibleBoardIdsAsync(Workspace, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid user, CancellationToken _) => user == Outsider ? [] : new[] { Board });
        var followers = new Mock<IResourceFollowers>();
        followers.Setup(f => f.FollowerIdsAsync(ItemFollowerNotifier.BoardItemKind, Item, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Actor, Follower, Muter, Outsider]);

        return (context, new ItemFollowerNotifier(context, context, followers.Object, new ItemAudience(work.Object, visibility.Object)));
    }

    private sealed class TestContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantContext tenant)
        : ApplicationDbContext(options, tenant)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<SearchDocumentRecord>();
        }
    }
}
