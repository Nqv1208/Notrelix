using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;
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
/// TAC-DC-006 / M2F — the mention fact chain: a CreateComment command that
/// carries explicit MentionedUserIds commits the comment and the Mention
/// aggregates in one transaction, the producer mapper maps the trusted
/// mentioner actor from the Domain fact (never the mentioned user), and the
/// real production delivery chain (outbox -> dispatcher -> tenant restore ->
/// dedup -> real notification consumer) persists a notification whose
/// AccountId is the authoritative event envelope — no Guid.Empty tenant.
/// A duplicate delivery does not duplicate the logical notification.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class MentionCreatedNotificationRuntimeChainIntegrationTests : IAsyncLifetime
{
    private const string NotificationConsumerEndpoint = "notrelix-collab-mention-notification-v1";

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public MentionCreatedNotificationRuntimeChainIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record MentionGraph(
        Guid AccountId,
        Guid WorkspaceId,
        Guid AuthorId,
        Guid MentionedUserId,
        Guid ItemId,
        string Content);

    private async Task<MentionGraph> SeedMentionStackAsync()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var mentionedUserId = Guid.NewGuid();
        var content = $"mention chain {Guid.NewGuid():N}";
        var now = DateTimeOffset.UtcNow;
        var workspace = Workspace.Create(accountId, ownerId, "DC Mention WS", $"mention-{Guid.NewGuid():N}", now);
        var board = Board.Create(accountId, workspace.Id, ownerId, "DC Mention Board", null, now);
        var group = BoardGroup.Create(
            accountId, workspace.Id, board.Id, "Todo",
            Color.Create("#808080"), FractionalIndex.Create("a0"), ownerId, now);
        var item = BoardItem.CreateRoot(
            accountId, workspace.Id, board.Id, group.Id, "Task",
            FractionalIndex.Create("a0"), ownerId, now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(Domain.Workspaces.Members.WorkspaceMember.Create(
            accountId, workspace.Id, ownerId, Domain.Workspaces.Members.WorkspaceRole.Owner, ownerId, now));
        seed.Boards.Add(board);
        seed.BoardGroups.Add(group);
        seed.BoardItems.Add(item);
        await seed.SaveChangesAsync();

        return new MentionGraph(accountId, workspace.Id, ownerId, mentionedUserId, item.Id, content);
    }

    private static async Task CreateCommentWithMentionAsync(ServiceProvider provider, MentionGraph graph)
    {
        await using var scope = provider.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<Notrelix.Application.Common.Context.IExecutionContextAccessor>()
            .SetUser(graph.AuthorId, "chain-author@example.com", "Chain Author");

        var result = await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(
                CreateCommentCommand.ForBoardItem(
                    graph.ItemId,
                    graph.Content,
                    null,
                    [graph.MentionedUserId, graph.MentionedUserId, Guid.Empty]),
                CancellationToken.None);

        ((Result<Guid>)result).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task MentionCreated_RunsThroughProductionChainWithAuthoritativeActorAndAccountEnvelope()
    {
        var graph = await SeedMentionStackAsync();
        await using var provider = BuildProvider(graph);

        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        foreach (var hosted in hostedServices)
        {
            await hosted.StartAsync(CancellationToken.None);
        }

        Guid outboxEventId;
        Guid notificationId;
        try
        {
            await CreateCommentWithMentionAsync(provider, graph);

            var mentionOutbox = await WaitForOutboxAsync(graph, "mention.created");
            var commentOutbox = await WaitForOutboxAsync(graph, "comment.created");
            mentionOutbox.Should().NotBeNull("the mention fact must stage its own producer-owned outward event");
            commentOutbox.Should().NotBeNull();

            var outbox = mentionOutbox!;
            outboxEventId = outbox.EventId;
            outbox.AccountId.Should().Be(graph.AccountId);
            outbox.WorkspaceId.Should().Be(graph.WorkspaceId);
            outbox.PayloadJson.RootElement.GetProperty("mentionedUserId").GetGuid().Should().Be(graph.MentionedUserId);
            outbox.PayloadJson.RootElement.GetProperty("mentionedByUserId").GetGuid().Should().Be(
                graph.AuthorId,
                "M2F: the mapper maps the trusted mentioner actor from the Domain fact, never the mentioned user");
            outbox.PayloadJson.RootElement.GetProperty("mentionedByUserId").GetGuid().Should().NotBe(
                outbox.PayloadJson.RootElement.GetProperty("mentionedUserId").GetGuid());
            outbox.PayloadJson.RootElement.GetProperty("actorUserId").GetGuid().Should().Be(graph.AuthorId);

            var notification = await WaitForNotificationAsync(outboxEventId, graph);
            notification.Should().NotBeNull();
            notificationId = notification!.Id;
            notification.AccountId.Should().Be(graph.AccountId,
                "the notification consumer must persist the authoritative account envelope, not Guid.Empty");
            notification.ActorUserId.Should().Be(graph.AuthorId);

            var dedupCompleted = await WaitForDedupSucceededAsync(outboxEventId, NotificationConsumerEndpoint);
            dedupCompleted.Should().BeTrue();

            var dispatcherCompleted = await WaitForOutboxProcessedAsync(outbox.Id);
            dispatcherCompleted.Should().BeTrue();
        }
        finally
        {
            foreach (var hosted in hostedServices.Reverse())
            {
                await hosted.StopAsync(CancellationToken.None);
            }
        }

        await using var probe = _db.CreateContext(SystemTenant());
        (await probe.Set<NotificationItemRecord>()
            .IgnoreQueryFilters()
            .CountAsync(n => n.SourceEventId == outboxEventId
                && n.WorkspaceId == graph.WorkspaceId)).Should().Be(1,
            "exactly one logical notification survives the chain");

        var recipient = await probe.Set<NotificationRecipientRecord>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(r => r.NotificationId == notificationId);
        recipient.Should().NotBeNull();
        recipient!.AccountId.Should().Be(graph.AccountId, "the recipient carries the authoritative account, not Guid.Empty");
        recipient.RecipientUserId.Should().Be(graph.MentionedUserId);
    }

    private ServiceProvider BuildProvider(MentionGraph graph)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:NotrelixDb"] = _db.ConnectionString,
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

    private async Task<MessagingOutboxMessage?> WaitForOutboxAsync(MentionGraph graph, string messageName)
    {
        MessagingOutboxMessage? found = null;
        var completed = await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            found = await probe.Set<MessagingOutboxMessage>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.MessageName == messageName
                    && m.AccountId == graph.AccountId
                    && m.WorkspaceId == graph.WorkspaceId);
            return found is not null;
        });

        return completed ? found : null;
    }

    private async Task<NotificationItemRecord?> WaitForNotificationAsync(Guid sourceEventId, MentionGraph graph)
    {
        NotificationItemRecord? found = null;
        var completed = await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            found = await probe.Set<NotificationItemRecord>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(n => n.SourceEventId == sourceEventId
                    && n.WorkspaceId == graph.WorkspaceId);
            return found is not null;
        });

        return completed ? found : null;
    }

    private async Task<bool> WaitForOutboxProcessedAsync(Guid outboxId)
    {
        return await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            return await probe.Set<MessagingOutboxMessage>()
                .IgnoreQueryFilters()
                .AnyAsync(m => m.Id == outboxId && m.Status == "Processed");
        });
    }

    private async Task<bool> WaitForDedupSucceededAsync(Guid eventId, string consumerName)
    {
        return await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            return await probe.Set<MessagingProcessedEvent>()
                .IgnoreQueryFilters()
                .AnyAsync(p => p.EventId == eventId
                    && p.ConsumerName == consumerName
                    && p.Status == "Succeeded");
        });
    }

    private static async Task<bool> WaitForAsync(Func<Task<bool>> predicate, int timeoutSeconds = 30)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            if (await predicate())
            {
                return true;
            }

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
}
