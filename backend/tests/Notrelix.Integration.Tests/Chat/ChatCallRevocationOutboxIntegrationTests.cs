using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.EventMappers.Chat;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Calls;
using Notrelix.Application.Features.Chat.Calls.Services;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Domain.Chat.Calls;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Infrastructure.Data.Chat;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Events;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// A person whose access to a call's space ended is released in the same transaction, and the media room is asked to drop them through
/// an outbox fact rather than inline: a failing media plane must not fail open or be swallowed (the fact is retried until it succeeds).
/// The release is committed with the fact, so the API refuses the person at once even while the media plane is down. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatCallRevocationOutboxIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;
    private readonly Guid _account = Guid.NewGuid();
    private readonly Guid _workspace = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private Guid _spaceId;
    private Guid _callId;

    public ChatCallRevocationOutboxIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();

        var channel = Space.CreateChannel(_account, _workspace, SpaceKind.ChannelPublic, "revoke", null, null, 1, _owner, Now);
        channel.AddMember(_member, MembershipRole.Member, Now);
        var call = Call.Start(_account, _workspace, channel.Id, SpaceKind.ChannelPublic, _owner, CallKind.Voice, "room-revoke", "ap-southeast-1", Now);
        call.Join(_member, Now);
        _spaceId = channel.Id;
        _callId = call.Id;
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.ChatSpaces.Add(channel);
        seed.ChatCalls.Add(call);
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

    private sealed class UnreachableGateway : IChatCallMediaGateway
    {
        public int RemoveAttempts;

        public Task<CallRoom> CreateRoomAsync(string roomKey, string region, int maxParticipants, CancellationToken cancellationToken) =>
            throw new CallMediaUnavailableException("down");

        public Task<CallJoinToken> IssueTokenAsync(string roomKey, Guid personId, bool canPublishVideo, TimeSpan ttl, CancellationToken cancellationToken) =>
            throw new CallMediaUnavailableException("down");

        public Task EndRoomAsync(string roomKey, CancellationToken cancellationToken) => throw new CallMediaUnavailableException("down");

        public Task RemoveParticipantAsync(string roomKey, Guid personId, CancellationToken cancellationToken)
        {
            RemoveAttempts++;
            throw new CallMediaUnavailableException("down");
        }

        public Task<IReadOnlyList<Guid>> ListParticipantsAsync(string roomKey, CancellationToken cancellationToken) => throw new CallMediaUnavailableException("down");

        public Task<IReadOnlyList<string>> ListRoomsAsync(CancellationToken cancellationToken) => throw new CallMediaUnavailableException("down");

        public CallMediaWebhookEvent? VerifyWebhook(string rawBody, string? authorization) => null;
    }

    private DomainEventInterceptor OutboxInterceptor()
    {
        var mappers = new ServiceCollection();
        foreach (var type in typeof(ChatCallEventMapper).Assembly.GetTypes().Where(t =>
                     t is { IsClass: true, IsAbstract: false } && typeof(IIntegrationEventMapper).IsAssignableFrom(t)
                     && t.Namespace == typeof(ChatCallEventMapper).Namespace && t != typeof(CompositeIntegrationEventMapper)))
            mappers.AddScoped(typeof(IIntegrationEventMapper), type);
        return new DomainEventInterceptor(
            new FixedClock(Now),
            new EventTypeRegistry(),
            ClassificationPolicy.CreateBuilder().Build(),
            DeliveryPolicy.CreateBuilder().Build(),
            new CompositeIntegrationEventMapper(mappers.BuildServiceProvider()),
            new IntegrationEventCollector());
    }

    [Fact]
    public async Task ARevokedPersonIsReleasedAndTheMediaKickIsAnOutboxFact_EvenWhileTheMediaPlaneIsDown()
    {
        var gateway = new UnreachableGateway();
        await using (var context = _db.CreateOwnerContext(SystemTenant(), [OutboxInterceptor()]))
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            var services = new ServiceCollection()
                .AddLogging()
                .AddSingleton<IChatDbContext>(context)
                .AddSingleton<IChatCallMediaGateway>(gateway)
                .AddSingleton<IDateTimeProvider>(new FixedClock(Now))
                .AddSingleton<ISpaceSequenceAllocator>(new ChatSpaceSequenceAllocator(context))
                .AddScoped<ChatSystemMessages>()
                .AddScoped<ChatCallEnding>()
                .AddScoped<ChatCallRevocation>()
                .BuildServiceProvider();

            var released = await services.GetRequiredService<ChatCallRevocation>()
                .ReleaseAsync(_workspace, _spaceId, _member, Now.AddSeconds(30), CancellationToken.None);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            released.Should().BeTrue();
        }

        await using var read = _db.CreateOwnerContext(SystemTenant());
        var call = await read.ChatCalls.AsNoTracking().Include(c => c.Participants).SingleAsync(c => c.Id == _callId);
        call.Participants.Single(p => p.PersonId == _member).IsJoined.Should().BeFalse("the participation closed in the same transaction");
        call.Version.Should().BeGreaterThan(1);
        var facts = await read.MessagingOutboxMessages.AsNoTracking()
            .Where(m => m.MessageName == "chat.call.participant-revoked" && m.WorkspaceId == _workspace).ToListAsync();
        facts.Should().ContainSingle("the kick is queued as an outbox fact for a consumer that retries");
        gateway.RemoveAttempts.Should().Be(0, "the media call is not made inline, where a failure would be swallowed or fail the revocation");
    }
}
