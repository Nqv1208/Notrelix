using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Calls;
using Notrelix.Application.Features.Chat.Calls.Services;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Calls;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.BackgroundJobs;
using Notrelix.Infrastructure.Chat;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Chat;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// The media server's events against PostgreSQL through the webhook runner (SPEC-CHAT-MEDIA-CALLS section 6): a replay of one event id
/// changes nothing, a person who left the room leaves the call, a finished room ends it with its one call message, and an event for a
/// room the platform does not know is ignored. The signature is proven in the adapter unit tests and the HTTP contract tests; this
/// fixture starts after verification. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatCallMediaWebhookIntegrationTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 17, 9, 0, 0, TimeSpan.Zero);

    private readonly OrgTreeTestHost _host;
    private readonly InMemoryChatCallMediaGateway _gateway = new(Options.Create(new ChatCallOptions()));

    public ChatCallMediaWebhookIntegrationTests(PostgresTestContainer db) => _host = new OrgTreeTestHost(db) { Now = T0 };

    private ServiceProvider ProviderFor(OrgStack stack) =>
        _host.CreateProvider(stack.AccountId, stack.OwnerId, services =>
        {
            services.AddScoped<IChatDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<ISpaceSequenceAllocator, ChatSpaceSequenceAllocator>();
            services.AddScoped<IWorkspaceMemberFacts, WorkspaceMemberFacts>();
            services.AddSingleton<IOptions<ChatCallOptions>>(Options.Create(new ChatCallOptions()));
            services.AddSingleton<IChatCallMediaGateway>(_gateway);
            services.AddScoped<ChatSystemMessages>();
            services.AddScoped<ChatCallEnding>();
            services.AddScoped<ChatCallMediaEventApplier>();
            services.AddScoped<ChatCallMediaWebhookRunner>();
            services.AddScoped<Notrelix.Application.Features.Chat.Audit.Services.ChatAuditFacts>();
        });

    private async Task<(Call Call, string Room)> SeedActiveCallAsync(OrgStack stack, int members)
    {
        var people = new List<Guid>();
        for (var i = 0; i < members; i++) people.Add(await _host.AddMemberAsync(stack, WorkspaceRole.Member, $"Talker{i}"));
        var channel = Space.CreateChannel(stack.AccountId, stack.WorkspaceId, SpaceKind.ChannelPublic, $"hook-{Guid.NewGuid():N}"[..14], null, null, 1, stack.OwnerId, T0);
        foreach (var person in people) channel.AddMember(person, MembershipRole.Member, T0);

        var room = $"room-{Guid.NewGuid():N}";
        var call = Call.Start(stack.AccountId, stack.WorkspaceId, channel.Id, SpaceKind.ChannelPublic, stack.OwnerId, CallKind.Voice, room, "ap-southeast-1", T0);
        foreach (var person in people) call.Join(person, T0);
        await _host.WithSystemContextAsync(seed =>
        {
            seed.ChatSpaces.Add(channel);
            seed.ChatCalls.Add(call);
            return Task.CompletedTask;
        });
        return (call, room);
    }

    private async Task<bool> RunAsync(OrgStack stack, CallMediaWebhookEvent mediaEvent, string? body = null)
    {
        using var provider = ProviderFor(stack);
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ChatCallMediaWebhookRunner>()
            .RunAsync(mediaEvent, body ?? $"{{\"id\":\"{mediaEvent.EventId}\"}}", CancellationToken.None);
    }

    private Task<Call> LoadAsync(Guid callId) =>
        _host.ReadAsync(c => c.ChatCalls.AsNoTracking().Include(x => x.Participants).SingleAsync(x => x.Id == callId));

    private Task<int> ClaimsAsync() =>
        _host.ReadAsync(c => c.IdempotencyRecords.CountAsync(r => r.Operation == ChatCallMediaWebhookRunner.Operation));

    private Task<int> CallMessagesAsync(Call call) =>
        _host.ReadAsync(c => c.ChatMessages.CountAsync(m => m.SpaceId == call.SpaceId && m.CallId == call.Id));

    [Fact]
    public async Task AParticipantLeftEventClosesTheParticipation_AndAReplayOfTheSameEventIdIsANoOp()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var (call, room) = await SeedActiveCallAsync(stack, 2);
        var leaving = call.Participants.First(p => p.PersonId != stack.OwnerId).PersonId;
        var before = (await LoadAsync(call.Id)).Version;
        var claimsBefore = await ClaimsAsync();
        var left = new CallMediaWebhookEvent(Guid.NewGuid().ToString("N"), "participant_left", room, leaving);

        (await RunAsync(stack, left)).Should().BeTrue("the first delivery is applied");
        var afterFirst = await LoadAsync(call.Id);
        afterFirst.Participants.Single(p => p.PersonId == leaving).IsJoined.Should().BeFalse();
        afterFirst.State.Should().Be(CallState.Active, "a group call lives on while others are in it");
        afterFirst.Version.Should().Be(before + 1);

        (await RunAsync(stack, left)).Should().BeFalse("the replay finds the event id already done");
        (await LoadAsync(call.Id)).Version.Should().Be(before + 1, "a replay changes nothing");
        (await ClaimsAsync()).Should().Be(claimsBefore + 1, "the event id is claimed once, however often it is delivered");
    }

    [Fact]
    public async Task ALateLeftEventOfAnEarlierSessionDoesNotDropAPersonWhoHasRejoined_ButTheCurrentSessionsLeftDoes()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var (call, room) = await SeedActiveCallAsync(stack, 2);
        var person = call.Participants.First(p => p.PersonId != stack.OwnerId).PersonId;
        // Well after the row's audit stamps, so the aggregate accepts the changes in order.
        var leftAt = T0.AddDays(1);
        var rejoinedAt = leftAt.AddDays(1);

        // The person leaves (the first session ends), then comes back: the same row, a later JoinedAt.
        await _host.WithSystemContextAsync(async c =>
        {
            var tracked = await c.ChatCalls.Include(x => x.Participants).SingleAsync(x => x.Id == call.Id);
            tracked.Leave(person, leftAt).Should().BeTrue();
            tracked.Join(person, rejoinedAt).Should().BeTrue();
        });
        _host.Now = rejoinedAt.AddMinutes(1); // the media server's events reach us after the rejoin
        var before = await LoadAsync(call.Id);
        before.Participants.Single(p => p.PersonId == person).IsJoined.Should().BeTrue();

        // The first session's participant_left reaches us late, carrying the time that earlier session was seated.
        var late = new CallMediaWebhookEvent(Guid.NewGuid().ToString("N"), "participant_left", room, person, T0);
        await RunAsync(stack, late);

        var after = await LoadAsync(call.Id);
        after.Participants.Single(p => p.PersonId == person).IsJoined.Should().BeTrue("the rejoined person stays in the call");
        after.Version.Should().Be(before.Version);

        // The left event of the current session (seated after the rejoin) still closes the participation.
        var current = new CallMediaWebhookEvent(Guid.NewGuid().ToString("N"), "participant_left", room, person, rejoinedAt.AddSeconds(1));
        await RunAsync(stack, current);
        (await LoadAsync(call.Id)).Participants.Single(p => p.PersonId == person).IsJoined.Should().BeFalse();
    }

    [Fact]
    public async Task ARoomFinishedEventEndsTheCallOnceWithItsOneMessage()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var (call, room) = await SeedActiveCallAsync(stack, 1);
        var finished = new CallMediaWebhookEvent(Guid.NewGuid().ToString("N"), "room_finished", room, null);

        (await RunAsync(stack, finished)).Should().BeTrue();
        var ended = await LoadAsync(call.Id);
        ended.State.Should().Be(CallState.Ended);
        ended.EndReason.Should().Be(CallEndReason.Completed);
        ended.Participants.Should().OnlyContain(p => !p.IsJoined);
        (await CallMessagesAsync(call)).Should().Be(1);

        await RunAsync(stack, finished);
        await RunAsync(stack, finished with { EventId = Guid.NewGuid().ToString("N") }); // the media server repeats it under a new id
        (await LoadAsync(call.Id)).Version.Should().Be(ended.Version, "an ended call stays as it ended");
        (await CallMessagesAsync(call)).Should().Be(1, "the call message is written once");
    }

    [Fact]
    public async Task AnEventForAnUnknownRoomOrAnUnrelatedKindChangesNothing()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var (call, room) = await SeedActiveCallAsync(stack, 1);
        var before = await LoadAsync(call.Id);

        await RunAsync(stack, new CallMediaWebhookEvent(Guid.NewGuid().ToString("N"), "room_finished", "room-nobody-knows", null));
        await RunAsync(stack, new CallMediaWebhookEvent(Guid.NewGuid().ToString("N"), "track_published", room, before.Participants.First().PersonId));

        (await LoadAsync(call.Id)).Version.Should().Be(before.Version);
    }

    [Fact]
    public async Task APersonWhoConnectsWithoutAnOpenParticipationIsDroppedFromTheRoom()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var (call, room) = await SeedActiveCallAsync(stack, 1);
        var intruder = Guid.NewGuid();

        await RunAsync(stack, new CallMediaWebhookEvent(Guid.NewGuid().ToString("N"), "participant_joined", room, intruder));

        _gateway.RemovedParticipants.Should().Contain((room, intruder));
        (await LoadAsync(call.Id)).State.Should().Be(CallState.Active);
    }
}
