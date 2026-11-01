using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MediatR;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Events.WorkManagement;
using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;
using Notrelix.Domain.Collaboration.Watchers;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Data.Notifications;
using Notrelix.Infrastructure.Identity.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Messaging;

/// <summary>
/// Followers through the production delivery chain (worker role, RLS enforced, tenant restored from the
/// event envelope): a comment and an assignment on an Item notify the people who follow it except the actor,
/// the assignee follows automatically, and a redelivery adds neither a follower nor a notification.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ItemFollowerNotificationRuntimeChainIntegrationTests : IAsyncLifetime
{
    private const string CommentEndpoint = "notrelix-collab-comment-follow-v1";
    private const string AssignedEndpoint = "notrelix-collab-item-assigned-follow-v1";

    private readonly PostgresTestContainer _db;

    public ItemFollowerNotificationRuntimeChainIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record FollowGraph(Guid AccountId, Guid WorkspaceId, Guid AuthorId, Guid FollowerId, Guid AssigneeId, Guid ItemId);

    private async Task<FollowGraph> SeedAsync()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var follower = Domain.Identity.Users.User.Create($"follower-{Guid.NewGuid():N}@example.com", "Follower", "hashed", DateTimeOffset.UtcNow, true);
        var assignee = Domain.Identity.Users.User.Create($"assignee-{Guid.NewGuid():N}@example.com", "Assignee", "hashed", DateTimeOffset.UtcNow, true);
        var now = DateTimeOffset.UtcNow;
        var workspace = Workspace.Create(accountId, ownerId, "Follow WS", $"follow-{Guid.NewGuid():N}", now);
        var board = Board.Create(accountId, workspace.Id, ownerId, "Follow Board", null, now);
        var group = BoardGroup.Create(accountId, workspace.Id, board.Id, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), ownerId, now);
        var item = BoardItem.CreateRoot(accountId, workspace.Id, board.Id, group.Id, "Follow me", FractionalIndex.Create("a0"), ownerId, now);

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Users.Add(follower);
        seed.Users.Add(assignee);
        seed.Workspaces.Add(workspace);
        foreach (var (id, role) in new[]
                 {
                     (ownerId, Domain.Workspaces.Members.WorkspaceRole.Owner),
                     (follower.Id, Domain.Workspaces.Members.WorkspaceRole.Member),
                     (assignee.Id, Domain.Workspaces.Members.WorkspaceRole.Member),
                 })
        {
            seed.WorkspaceMembers.Add(Domain.Workspaces.Members.WorkspaceMember.Create(accountId, workspace.Id, id, role, ownerId, now));
        }

        seed.Boards.Add(board);
        seed.BoardGroups.Add(group);
        seed.BoardItems.Add(item);
        var kind = ResourceKind.Create("work-management.board-item");
        foreach (var userId in new[] { ownerId, follower.Id })
        {
            seed.ResourceWatchers.Add(ResourceWatcher.Create(
                accountId, workspace.Id, ResourceRef.Create(kind, item.Id, workspace.Id), userId, userId, now));
        }

        await seed.SaveChangesAsync();
        await AccessGrantSeed.WorkspaceMemberAsync(_db, accountId, workspace.Id, ownerId, Domain.Workspaces.Members.WorkspaceRole.Owner);
        await AccessGrantSeed.WorkspaceMemberAsync(_db, accountId, workspace.Id, follower.Id, Domain.Workspaces.Members.WorkspaceRole.Member);
        await AccessGrantSeed.WorkspaceMemberAsync(_db, accountId, workspace.Id, assignee.Id, Domain.Workspaces.Members.WorkspaceRole.Member);
        return new FollowGraph(accountId, workspace.Id, ownerId, follower.Id, assignee.Id, item.Id);
    }

    [Fact]
    public async Task ACommentNotifiesTheFollowersExceptItsAuthor()
    {
        var graph = await SeedAsync();
        await using var provider = BuildProvider(graph);
        var hosted = provider.GetServices<IHostedService>().ToArray();
        foreach (var service in hosted) await service.StartAsync(CancellationToken.None);

        try
        {
            await using (var scope = provider.CreateAsyncScope())
            {
                scope.ServiceProvider.GetRequiredService<Notrelix.Application.Common.Context.IExecutionContextAccessor>()
                    .SetUser(graph.AuthorId, "chain-author@example.com", "Chain Author");
                scope.ServiceProvider.GetRequiredService<Notrelix.Application.Common.Idempotency.IIdempotencyExecutionContextWriter>()
                    .Set($"chain-{Guid.NewGuid():N}", Notrelix.Application.Common.Idempotency.IdempotencyExecutionSource.Internal);
                var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                    CreateCommentCommand.ForBoardItem(graph.ItemId, "  <b>Looks</b>   good\nto me  ", null), CancellationToken.None);
                ((Result<Guid>)result).Succeeded.Should().BeTrue();
            }

            (await WaitForAsync(() => CountAsync(graph, NotificationTypes.ItemCommentedProducedType, graph.FollowerId), 1))
                .Should().BeTrue("the follower is told about the comment");
            (await WaitForDedupAsync(CommentEndpoint)).Should().BeTrue();
        }
        finally
        {
            foreach (var service in hosted.Reverse()) await service.StopAsync(CancellationToken.None);
        }

        await using var probe = _db.CreateOwnerContext(SystemTenant());
        var items = await probe.Set<NotificationItemRecord>().IgnoreQueryFilters()
            .Where(n => n.WorkspaceId == graph.WorkspaceId && n.NotificationType == NotificationTypes.ItemCommentedProducedType)
            .ToListAsync();
        items.Should().ContainSingle("only the follower who is not the author");
        items[0].Body.Should().Be("Looks good to me");
        items[0].ActorUserId.Should().Be(graph.AuthorId);
        (await probe.Set<NotificationRecipientRecord>().IgnoreQueryFilters()
            .AnyAsync(r => r.NotificationId == items[0].Id && r.RecipientUserId == graph.AuthorId))
            .Should().BeFalse("the actor is never notified of their own comment");
    }

    [Fact]
    public async Task AnAssignmentMakesTheAssigneeFollowAndNotifiesOnceEvenWhenRedelivered()
    {
        var graph = await SeedAsync();
        await using var provider = BuildProvider(graph);
        var hosted = provider.GetServices<IHostedService>().ToArray();
        foreach (var service in hosted) await service.StartAsync(CancellationToken.None);

        try
        {
            var assigned = new BoardItemMemberAssignedIntegrationEvent(
                Guid.NewGuid(), graph.AccountId, graph.WorkspaceId, graph.ItemId, graph.AssigneeId, graph.AuthorId,
                Guid.NewGuid(), OccurredAt: DateTimeOffset.UtcNow);
            var bus = provider.GetRequiredService<IIntegrationEventBus>();
            await bus.PublishAsync(assigned);
            (await WaitForDedupAsync(AssignedEndpoint)).Should().BeTrue();
            await bus.PublishAsync(assigned);
            await Task.Delay(1500);
        }
        finally
        {
            foreach (var service in hosted.Reverse()) await service.StopAsync(CancellationToken.None);
        }

        await using var probe = _db.CreateOwnerContext(SystemTenant());
        var kind = ResourceKind.Create("work-management.board-item");
        (await probe.ResourceWatchers.IgnoreQueryFilters()
            .CountAsync(w => w.Target.Kind == kind && w.Target.ResourceId == graph.ItemId && w.UserId == graph.AssigneeId))
            .Should().Be(1, "the assignee follows once");
        (await CountAsync(graph, NotificationTypes.ItemAssignedProducedType, graph.AssigneeId)).Should().Be(1);
        (await CountAsync(graph, NotificationTypes.ItemAssigneeAddedProducedType, graph.FollowerId)).Should().Be(1);
        (await CountAsync(graph, NotificationTypes.ItemAssigneeAddedProducedType, graph.AuthorId))
            .Should().Be(0, "the person who assigned is the actor");
    }

    private async Task<int> CountAsync(FollowGraph graph, string type, Guid recipient)
    {
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        return await (from r in probe.Set<NotificationRecipientRecord>().IgnoreQueryFilters()
                      join n in probe.Set<NotificationItemRecord>().IgnoreQueryFilters() on r.NotificationId equals n.Id
                      where n.WorkspaceId == graph.WorkspaceId && n.NotificationType == type && r.RecipientUserId == recipient
                      select r.Id).CountAsync();
    }

    private static async Task<bool> WaitForAsync(Func<Task<int>> count, int expected, int timeoutSeconds = 30)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            if (await count() >= expected) return true;
            await Task.Delay(200);
        }

        return false;
    }

    private async Task<bool> WaitForDedupAsync(string consumerName)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            await using var probe = _db.CreateOwnerContext(SystemTenant());
            if (await probe.Set<MessagingProcessedEvent>().IgnoreQueryFilters()
                    .AnyAsync(p => p.ConsumerName == consumerName && p.Status == "Succeeded"))
                return true;
            await Task.Delay(200);
        }

        return false;
    }

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private ServiceProvider BuildProvider(FollowGraph graph)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:NotrelixDb"] = _db.ConnectionString,
                ["ConnectionStrings:NotrelixWorkerDb"] = _db.WorkerConnectionString,
                ["ConnectionStrings:Redis"] = "localhost:6379,abortConnect=false",
                ["Messaging:Transport"] = "InMemory",
                ["Rls:Enabled"] = "true",
                ["Rls:SetSessionContext"] = "true",
                ["DOTNET_ENVIRONMENT"] = "Testing",
                ["JwtSettings:SecretKey"] = "test-secret-key-at-least-32-characters-long",
                ["JwtSettings:Issuer"] = "notrelix-test",
                ["JwtSettings:Audience"] = "notrelix-test",
                ["JwtSettings:ExpireMinutes"] = "30",
                ["JwtSettings:RefreshTokenExpireDays"] = "7",
            })
            .Build();

        var builder = new HostApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:NotrelixDb"] = _db.ConnectionString,
            ["ConnectionStrings:NotrelixWorkerDb"] = _db.WorkerConnectionString,
            ["ConnectionStrings:Redis"] = "localhost:6379,abortConnect=false",
            ["Messaging:Transport"] = "InMemory",
            ["Rls:Enabled"] = "true",
            ["Rls:SetSessionContext"] = "true",
            ["DOTNET_ENVIRONMENT"] = "Testing",
            ["JwtSettings:SecretKey"] = "test-secret-key-at-least-32-characters-long",
            ["JwtSettings:Issuer"] = "notrelix-test",
            ["JwtSettings:Audience"] = "notrelix-test",
            ["JwtSettings:ExpireMinutes"] = "30",
            ["JwtSettings:RefreshTokenExpireDays"] = "7",
        });

        builder.Services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        builder.Services.AddSingleton(TimeProvider.System);

        var environment = new Mock<IHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns("Testing");
        builder.Services.AddSingleton(environment.Object);

        builder.Services.AddInfrastructure(configuration, environment.Object);

        builder.Services.AddScoped<ICurrentTenantContext>(_ => new CurrentTenantContext());
        builder.Services.AddScoped<ICurrentUser>(_ => new FakeCurrentUser
        {
            UserId = graph.AuthorId,
            Email = "chain-author@example.com",
            Name = "Chain Author",
        });

        builder.Services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();
        builder.AddApplicationServices();

        return builder.Services.BuildServiceProvider();
    }
}
