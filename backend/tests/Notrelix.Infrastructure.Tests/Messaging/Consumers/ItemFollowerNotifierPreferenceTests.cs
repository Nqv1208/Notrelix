using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Context;
using Notrelix.Application.Features.Collaboration.Public.Watchers;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Public.ItemNotifications;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Projections.Search;
using Notrelix.Infrastructure.Messaging.Consumers.Collaboration;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Infrastructure.Tests.Messaging.Consumers;

/// <summary>
/// UC-COL-G12: the in-app cell a person switched off for a kind of Item notification suppresses that notification
/// for them, and only that kind. Approval and assigned kinds read their own cells through the same notifier.
/// </summary>
public sealed class ItemFollowerNotifierPreferenceTests
{
    private static readonly Guid Account = Guid.NewGuid();
    private static readonly Guid Workspace = Guid.NewGuid();
    private static readonly Guid Board = Guid.NewGuid();
    private static readonly Guid Item = Guid.NewGuid();
    private static readonly Guid Person = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SwitchedOffApprovalCell_SuppressesAnApprovalRequest()
    {
        var (context, notifier) = await BuildAsync(new NotificationPreferenceChange(NotificationTypes.Approval, NotificationChannels.InApp, false));

        await notifier.NotifyAsync(Account, Workspace, Item, Guid.NewGuid(), Guid.NewGuid(),
            NotificationTypes.ApprovalRequestedProducedType, "Approval requested", null, [Person], Now, CancellationToken.None);

        context.NotificationItems.Should().BeEmpty("the person switched the approval kind off");
        context.NotificationRecipients.Should().BeEmpty();
    }

    [Fact]
    public async Task SwitchedOffAssignedCell_SuppressesAnAssignedNotification()
    {
        var (context, notifier) = await BuildAsync(new NotificationPreferenceChange(NotificationTypes.Assigned, NotificationChannels.InApp, false));

        await notifier.NotifyAsync(Account, Workspace, Item, Guid.NewGuid(), Guid.NewGuid(),
            NotificationTypes.ItemAssignedProducedType, "You were assigned an item", null, [Person], Now, CancellationToken.None);

        context.NotificationItems.Should().BeEmpty("the person switched the assigned kind off");
    }

    [Fact]
    public async Task SwitchedOffApprovalCell_DoesNotSuppressAnAssignedNotification()
    {
        var (context, notifier) = await BuildAsync(new NotificationPreferenceChange(NotificationTypes.Approval, NotificationChannels.InApp, false));

        await notifier.NotifyAsync(Account, Workspace, Item, Guid.NewGuid(), Guid.NewGuid(),
            NotificationTypes.ItemAssignedProducedType, "You were assigned an item", null, [Person], Now, CancellationToken.None);

        context.NotificationItems.Should().ContainSingle("only the approval cell is off; the assigned cell keeps its default");
    }

    private static async Task<(ApplicationDbContext Context, ItemFollowerNotifier Notifier)> BuildAsync(
        params NotificationPreferenceChange[] changes)
    {
        var context = CreateContext();
        context.NotificationPreferenceSets.Add(NotificationPreferenceSet.Create(Account, Workspace, Person, changes, Now)!);
        // Postgres fills quiet_hours_json from its '{}' column default; the in-memory store needs the value set.
        foreach (var tracked in context.ChangeTracker.Entries())
        {
            if (tracked.Metadata.FindProperty("QuietHoursJson") is not null)
                tracked.Property("QuietHoursJson").CurrentValue = JsonDocument.Parse("{}");
        }
        await context.SaveChangesAsync();

        var work = new Mock<IWorkItemNotificationFacts>();
        work.Setup(w => w.FindAsync(Workspace, Item, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkItemNotificationFacts(Board, "Item", false, null));
        var visibility = new Mock<IResourceVisibility>();
        visibility.Setup(v => v.VisibleBoardIdsAsync(Workspace, Person, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Board });
        var followers = new Mock<IResourceFollowers>();

        var notifier = new ItemFollowerNotifier(context, context, followers.Object, new ItemAudience(work.Object, visibility.Object));
        return (context, notifier);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return new PreferenceTestContext(options, tenant);
    }

    private sealed class PreferenceTestContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantContext tenant)
        : ApplicationDbContext(options, tenant)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<SearchDocumentRecord>();
        }
    }
}
