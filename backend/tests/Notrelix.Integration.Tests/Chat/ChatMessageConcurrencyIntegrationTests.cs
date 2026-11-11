using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Models;
using Notrelix.Application.EventMappers.Chat;
using Notrelix.Application.Features.Chat.Messages.Commands.SendMessage;
using Notrelix.Application.Features.Chat.Messages.DTOs;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Infrastructure.Data.Chat;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Events;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// UC-CHT-B01 and CHT-005/CHT-006 against PostgreSQL. Each send runs in its own context and transaction, the way the
/// pipeline runs a write request: the per-space row lock must serialize the sequence allocation so that concurrent sends
/// get a gap-free run of sequences, and a racing duplicate clientMessageId stores exactly one message. The outbox row for
/// chat.message.created is written inside the same transaction as the message. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatMessageConcurrencyIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
    private const int Racers = 20;

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private Guid _spaceId;

    public ChatMessageConcurrencyIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();

        var channel = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "race", null, null, 1, _member, Now);
        _spaceId = channel.Id;
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.ChatSpaces.Add(channel);
        await seed.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private sealed class FixedClock(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private static DomainEventInterceptor CreateOutboxInterceptor() =>
        new(
            new FixedClock(Now),
            new EventTypeRegistry(),
            ClassificationPolicy.CreateBuilder().Build(),
            DeliveryPolicy.CreateBuilder().Build(),
            new CompositeIntegrationEventMapper(
                new ServiceCollection()
                    .AddScoped<IIntegrationEventMapper, ChatMessageEventMapper>()
                    .BuildServiceProvider()),
            new IntegrationEventCollector());

    private Mock<ICurrentRequestContext> RequestAs(Guid userId)
    {
        var context = new Mock<ICurrentRequestContext>();
        context.Setup(c => c.UserId).Returns(userId);
        context.Setup(c => c.IsAuthenticated).Returns(true);
        context.Setup(c => c.RequireAccountId()).Returns(_accountId);
        context.Setup(c => c.RequireWorkspaceId()).Returns(_workspaceId);
        return context;
    }

    private Mock<IWorkspaceMemberFacts> ActiveMembers(Guid userId)
    {
        var members = new Mock<IWorkspaceMemberFacts>();
        members.Setup(m => m.FindAsync(_workspaceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(userId, "Member", true));
        return members;
    }

    /// <summary>One send, in its own transaction, the way the pipeline runs a write request.</summary>
    private async Task<Result<SendMessageResultDto>> SendInOwnTransactionAsync(string content, string clientMessageId, bool withOutbox = false)
    {
        var interceptors = withOutbox ? new[] { CreateOutboxInterceptor() } : Array.Empty<Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor>();
        await using var context = _db.CreateOwnerContext(SystemTenant(), interceptors);
        await using var transaction = await context.Database.BeginTransactionAsync();

        var handler = new SendMessageCommandHandler(
            context,
            RequestAs(_member).Object,
            ActiveMembers(_member).Object,
            new ChatSpaceSequenceAllocator(context),
            new FixedClock(Now));

        var result = await handler.Handle(
            new SendMessageCommand(_workspaceId, _spaceId, content, clientMessageId, null),
            CancellationToken.None);

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    }

    [Fact]
    public async Task TwentyConcurrentSends_GetAGapFreeRunOfSequences_AndEachIsStoredOnce()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, Racers)
            .Select(i => SendInOwnTransactionAsync($"racer {i}", $"client-{i}")));

        results.Should().OnlyContain(r => r.Succeeded && r.Data!.Created);
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        var sequences = await probe.ChatMessages.IgnoreQueryFilters()
            .Where(m => m.SpaceId == _spaceId)
            .OrderBy(m => m.Sequence)
            .Select(m => m.Sequence)
            .ToListAsync();
        sequences.Should().Equal(Enumerable.Range(1, Racers).Select(i => (long)i),
            "the row lock serializes the allocation, so no sequence is skipped or repeated");
    }

    [Fact]
    public async Task RacingDuplicateClientMessageId_StoresOneRow_AndNoSendFails()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => SendInOwnTransactionAsync("same text", "client-dup")));

        results.Should().OnlyContain(r => r.Succeeded);
        results.Count(r => r.Data!.Created).Should().Be(1, "exactly one racer creates the message; the others get it back");
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        var stored = await probe.ChatMessages.IgnoreQueryFilters()
            .CountAsync(m => m.SpaceId == _spaceId && m.ClientMessageId == "client-dup");
        stored.Should().Be(1);
    }

    [Fact]
    public async Task SendWithOutbox_StagesChatMessageCreatedInTheSameTransaction()
    {
        var result = await SendInOwnTransactionAsync("with outbox", "client-outbox", withOutbox: true);

        result.Succeeded.Should().BeTrue();
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        var outbox = await probe.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
            .SingleOrDefaultAsync(o => o.WorkspaceId == _workspaceId && o.MessageName == "chat.message.created");
        outbox.Should().NotBeNull("the created message stages exactly one outward event in the same commit");
        outbox!.PayloadJson.RootElement.GetProperty("sequence").GetInt64().Should().Be(1);
    }

    [Fact]
    public async Task RolledBackSend_LeavesNeitherMessageNorOutboxRow()
    {
        await using (var context = _db.CreateOwnerContext(SystemTenant(), CreateOutboxInterceptor()))
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            var handler = new SendMessageCommandHandler(
                context,
                RequestAs(_member).Object,
                ActiveMembers(_member).Object,
                new ChatSpaceSequenceAllocator(context),
                new FixedClock(Now));
            await handler.Handle(new SendMessageCommand(_workspaceId, _spaceId, "rolled back", "client-rollback", null), CancellationToken.None);
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.ChatMessages.IgnoreQueryFilters().CountAsync(m => m.SpaceId == _spaceId)).Should().Be(0);
        (await probe.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
            .CountAsync(o => o.WorkspaceId == _workspaceId && o.MessageName == "chat.message.created")).Should().Be(0);
    }
}
