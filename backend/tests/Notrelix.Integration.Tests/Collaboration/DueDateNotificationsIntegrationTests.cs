using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Infrastructure.BackgroundJobs;
using Notrelix.Infrastructure.Data.Notifications;
using Notrelix.Infrastructure.Messaging.Consumers.Collaboration;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;

namespace Notrelix.Integration.Tests.Collaboration;

/// <summary>
/// The daily due-date job tells an assignee once that an open Item is due tomorrow and once that it became
/// overdue, by the calendar days of the Workspace's time zone, honours muted threads and the member's own
/// preference, and a second pass adds nothing. Runs on PostgreSQL under the worker scope.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class DueDateNotificationsIntegrationTests
{
    private readonly OrgTreeTestHost _host;

    public DueDateNotificationsIntegrationTests(PostgresTestContainer db) => _host = new OrgTreeTestHost(db);

    // 2026-10-11 08:00 UTC is 15:00 on 11 October in the default zone (Asia/Ho_Chi_Minh, UTC+7).
    private static DateTimeOffset LocalNoon(int day) => new DateTimeOffset(2026, 10, day, 12, 0, 0, TimeSpan.FromHours(7)).ToUniversalTime();

    private int _groups;

    private async Task<Guid> SeedItemAsync(
        OrgStack stack, Guid boardId, string name, DateTimeOffset? dueAt, params Guid[] assignees)
    {
        var group = BoardGroup.Create(
            stack.AccountId, stack.WorkspaceId, boardId, "Group", Color.Create("#0079BF"),
            FractionalIndex.Create($"a{_groups++}"), stack.OwnerId, OrgTreeTestHost.BaseTime);
        var item = BoardItem.CreateRoot(
            stack.AccountId, stack.WorkspaceId, boardId, group.Id, name, FractionalIndex.Create("a0"),
            stack.OwnerId, OrgTreeTestHost.BaseTime, dueAt: dueAt);
        await _host.WithSystemContextAsync(seed =>
        {
            seed.BoardGroups.Add(group);
            seed.BoardItems.Add(item);
            foreach (var user in assignees)
            {
                seed.BoardItemMembers.Add(BoardItemMember.Create(
                    stack.AccountId, stack.WorkspaceId, boardId, item.Id, user, stack.OwnerId, OrgTreeTestHost.BaseTime));
            }

            return Task.CompletedTask;
        });
        return item.Id;
    }

    private ServiceProvider ProviderFor(OrgStack stack) =>
        _host.CreateProvider(stack.AccountId, stack.OwnerId, services =>
        {
            services.AddSingleton<IOptions<DueDateNotificationOptions>>(
                Options.Create(new DueDateNotificationOptions { BatchSize = 2 }));
            services.AddScoped<Notrelix.Infrastructure.Data.Abstractions.INotificationDbContext>(
                sp => sp.GetRequiredService<Notrelix.Infrastructure.Data.ApplicationDbContext>());
            services.AddScoped<Notrelix.Application.Features.Collaboration.Abstractions.ICollaborationDbContext>(
                sp => sp.GetRequiredService<Notrelix.Infrastructure.Data.ApplicationDbContext>());
            services.AddScoped<Notrelix.Application.Features.Collaboration.Public.Watchers.IResourceFollowers,
                Notrelix.Infrastructure.CrossContext.Collaboration.Watchers.PostgresResourceFollowers>();
            services.AddScoped<ItemFollowerNotifier>();
            services.AddScoped<DueDateNotificationSweeper>();
        });

    private static async Task RunAsync(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DueDateNotificationSweeper>().RunOnceAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Pass_NotifiesAssigneesOfItemsDueTomorrowAndOverdue_OnceEach_AndASecondPassAddsNothing()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var boardId = await _host.SeedBoardAsync(stack, "Due board");
        var ana = await _host.AddMemberAsync(stack, Domain.Workspaces.Members.WorkspaceRole.Member, "Ana");
        var ben = await _host.AddMemberAsync(stack, Domain.Workspaces.Members.WorkspaceRole.Member, "Ben");

        var tomorrow = await SeedItemAsync(stack, boardId, "Due tomorrow", LocalNoon(12), ana, ben);
        var overdue = await SeedItemAsync(stack, boardId, "Overdue", LocalNoon(10), ana);
        await SeedItemAsync(stack, boardId, "Due today", LocalNoon(11), ana);
        await SeedItemAsync(stack, boardId, "Far overdue", LocalNoon(1), ana);
        await SeedItemAsync(stack, boardId, "No due date", null, ana);
        await SeedItemAsync(stack, boardId, "Unassigned", LocalNoon(12));

        using var provider = ProviderFor(stack);
        await RunAsync(provider);
        await RunAsync(provider);

        var rows = await _host.ReadAsync(c => (
                from r in c.NotificationRecipients
                join n in c.NotificationItems on r.NotificationId equals n.Id
                where n.WorkspaceId == stack.WorkspaceId
                select new { n.NotificationType, n.ResourceId, r.RecipientUserId })
            .ToListAsync());

        rows.Should().HaveCount(3, "tomorrow to both assignees and overdue to one, never twice");
        rows.Should().Contain(r => r.NotificationType == NotificationTypes.ItemDueTomorrowProducedType
                                   && r.ResourceId == tomorrow && r.RecipientUserId == ana);
        rows.Should().Contain(r => r.NotificationType == NotificationTypes.ItemDueTomorrowProducedType
                                   && r.ResourceId == tomorrow && r.RecipientUserId == ben);
        rows.Should().Contain(r => r.NotificationType == NotificationTypes.ItemOverdueProducedType
                                   && r.ResourceId == overdue && r.RecipientUserId == ana);
    }

    [Fact]
    public async Task Pass_SkipsAMemberWhoMutedTheItemOrTurnedTheOverduePreferenceOff()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var boardId = await _host.SeedBoardAsync(stack, "Due board");
        var ana = await _host.AddMemberAsync(stack, Domain.Workspaces.Members.WorkspaceRole.Member, "Ana");
        var ben = await _host.AddMemberAsync(stack, Domain.Workspaces.Members.WorkspaceRole.Member, "Ben");
        var item = await SeedItemAsync(stack, boardId, "Due tomorrow", LocalNoon(12), ana, ben);
        await _host.WithSystemContextAsync(seed =>
        {
            seed.NotificationThreadMutes.Add(NotificationThreadMuteRecord.Create(
                stack.AccountId, stack.WorkspaceId, ana, "work-management.board-item", item, OrgTreeTestHost.BaseTime));
            seed.NotificationPreferenceSets.Add(NotificationPreferenceSet.Create(
                stack.AccountId, stack.WorkspaceId, ben,
                [new NotificationPreferenceChange(NotificationTypes.Overdue, NotificationChannels.InApp, false)],
                OrgTreeTestHost.BaseTime)!);
            return Task.CompletedTask;
        });

        using var provider = ProviderFor(stack);
        await RunAsync(provider);

        (await _host.ReadAsync(c => c.NotificationRecipients
                .Where(r => r.WorkspaceId == stack.WorkspaceId).CountAsync()))
            .Should().Be(0);
    }

    [Fact]
    public void Options_DefaultToAnHourlyBatchedPass_AndAreValidated()
    {
        var defaults = new DueDateNotificationOptions();
        defaults.Enabled.Should().BeTrue();
        defaults.Interval.Should().Be(TimeSpan.FromHours(1));
        defaults.BatchSize.Should().Be(500);

        var validator = new DueDateNotificationOptionsValidator();
        validator.Validate(null, defaults).Succeeded.Should().BeTrue();
        validator.Validate(null, new DueDateNotificationOptions { Interval = TimeSpan.FromSeconds(1) }).Failed.Should().BeTrue();
        validator.Validate(null, new DueDateNotificationOptions { BatchSize = 0 }).Failed.Should().BeTrue();
        validator.Validate(null, new DueDateNotificationOptions { OverdueGraceDays = 0 }).Failed.Should().BeTrue();
    }
}
