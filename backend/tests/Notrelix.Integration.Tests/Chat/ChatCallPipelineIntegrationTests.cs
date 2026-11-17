using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Calls;
using Notrelix.Application.Features.Chat.Calls.Commands.AnswerCall;
using Notrelix.Application.Features.Chat.Calls.Commands.EndCall;
using Notrelix.Application.Features.Chat.Calls.Commands.JoinCall;
using Notrelix.Application.Features.Chat.Calls.Commands.LeaveCall;
using Notrelix.Application.Features.Chat.Calls.Commands.StartCall;
using Notrelix.Application.Features.Chat.Calls.DTOs;
using Notrelix.Application.Features.Chat.Calls.Queries.GetActiveCall;
using Notrelix.Application.Features.Chat.Calls.Queries.GetCallJoinToken;
using Notrelix.Application.Features.Chat.Calls.Queries.ListCalls;
using Notrelix.Application.Features.Chat.Calls.Services;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Calls;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Permissions;
using Notrelix.Domain.Chat.Rules;
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
/// Voice and video calls against PostgreSQL through the real request pipeline (SPEC-CHAT-MEDIA-CALLS): who may start and who sees a call
/// (an outsider is NotFound, a Guest and a read-only member are refused), a stale version is 412, a retried start returns the same
/// call, a person in another call answers Busy, the 25 voice and 12 video caps, the race of simultaneous starts in one channel (the unique
/// index and the space lock leave exactly one call), the ring timeout and the empty-call timeout of the worker, and the one call message
/// per call. The media plane is the in-memory gateway. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatCallPipelineIntegrationTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);

    private readonly OrgTreeTestHost _host;
    private readonly InMemoryChatCallMediaGateway _gateway = new(Options.Create(new ChatCallOptions()));

    public ChatCallPipelineIntegrationTests(PostgresTestContainer db) => _host = new OrgTreeTestHost(db) { Now = T0 };

    private ServiceProvider ProviderFor(OrgStack stack, Guid userId, IChatCallMediaGateway? gateway = null, ChatCallOptions? options = null) =>
        _host.CreateProvider(stack.AccountId, userId, services =>
        {
            services.AddScoped<IChatDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<ISpaceSequenceAllocator, ChatSpaceSequenceAllocator>();
            services.AddScoped<IWorkspaceMemberFacts, WorkspaceMemberFacts>();
            services.AddSingleton<IOptions<ChatCallOptions>>(Options.Create(options ?? new ChatCallOptions()));
            services.AddSingleton(gateway ?? _gateway);
            services.AddScoped<ChatSystemMessages>();
            services.AddScoped<ChatCallEnding>();
            services.AddScoped<ChatCallSweeper>();
            services.AddScoped<ChatCallRevocation>();
            services.AddScoped<ChatCallSweepRunner>();
            services.AddScoped<Notrelix.Application.Features.Chat.Audit.Services.ChatAuditFacts>();
            services.AddScoped<Notrelix.Application.Features.Chat.Members.Services.ChatWorkspaceExit>();
        });

    private async Task<Space> SeedChannelAsync(
        OrgStack stack, Guid manager, SpaceKind kind, IEnumerable<Guid> members, params ChannelPermissionEntry[] overrides)
    {
        var channel = Space.CreateChannel(stack.AccountId, stack.WorkspaceId, kind, $"calls-{Guid.NewGuid():N}"[..14], null, null, 1, manager, T0);
        foreach (var member in members)
            channel.AddMember(member, MembershipRole.Member, T0);
        if (overrides.Length > 0)
            channel.SetPermissionOverrides(overrides, manager, T0);
        await _host.WithSystemContextAsync(seed =>
        {
            seed.ChatSpaces.Add(channel);
            return Task.CompletedTask;
        });
        return channel;
    }

    private async Task<Space> SeedDirectAsync(OrgStack stack, Guid first, Guid second)
    {
        var direct = Space.CreateDirect(stack.AccountId, stack.WorkspaceId, first, second, T0);
        await _host.WithSystemContextAsync(seed =>
        {
            seed.ChatSpaces.Add(direct);
            return Task.CompletedTask;
        });
        return direct;
    }

    private static async Task<T> SendAsync<T>(ServiceProvider provider, IRequest<Result<T>> request, string? key = null)
    {
        var result = (Result<T>)(await OrgTreeTestHost.SendIdempotentAsync(provider, request, key))!;
        return result.Data!;
    }

    /// <summary>Starts a call the way the transport does and returns the call with the starter's token.</summary>
    private static async Task<CallJoinDto> StartAsync(ServiceProvider provider, StartCallCommand command, string? key = null)
    {
        var started = await SendAsync(provider, command, key);
        return await SendAsync(provider, new GetCallJoinTokenQuery(command.WorkspaceId, started.Id));
    }

    private static async Task<Exception> RefusalAsync<T>(ServiceProvider provider, IRequest<Result<T>> request)
    {
        var refused = await Record.ExceptionAsync(() => SendAsync(provider, request));
        refused.Should().NotBeNull("the request must be refused");
        return refused!;
    }

    private static string? CodeOf(Exception exception) => exception switch
    {
        Notrelix.Domain.Common.Exceptions.BusinessRuleException rule => rule.RuleCode,
        Notrelix.Application.Common.Exceptions.ConflictException conflict => conflict.ErrorCode,
        _ => null,
    };

    private Task<long> VersionAsync(Guid callId) =>
        _host.ReadAsync(c => c.ChatCalls.Where(x => x.Id == callId).Select(x => x.Version).SingleAsync());

    private Task<Call> LoadAsync(Guid callId) =>
        _host.ReadAsync(c => c.ChatCalls.AsNoTracking().Include(x => x.Participants).SingleAsync(x => x.Id == callId));

    private Task<int> CallMessagesAsync(Guid spaceId, Guid callId) =>
        _host.ReadAsync(c => c.ChatMessages.CountAsync(m => m.SpaceId == spaceId && m.CallId == callId));

    private Task<int> LiveCallsAsync(Guid spaceId) =>
        _host.ReadAsync(c => c.ChatCalls.CountAsync(x => x.SpaceId == spaceId && x.State != CallState.Ended));

    private async Task<List<Guid>> AddMembersAsync(OrgStack stack, int count)
    {
        var ids = new List<Guid>();
        for (var i = 0; i < count; i++)
            ids.Add(await _host.AddMemberAsync(stack, WorkspaceRole.Member, $"Caller{i}"));
        return ids;
    }

    private async Task RunSweepAsync(OrgStack stack, IChatCallMediaGateway? gateway = null)
    {
        using var provider = ProviderFor(stack, stack.OwnerId, gateway);
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ChatCallSweepRunner>().RunOnceAsync(CancellationToken.None);
    }

    [Fact]
    public async Task AMemberStartsACall_ItRingsWithTheServerRegionAndTheStarterHoldsAJoinToken()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var channel = await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPublic, [member]);

        using var asOwner = ProviderFor(stack, stack.OwnerId, options: new ChatCallOptions { Region = "ap-test-1" });
        var started = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "video"));

        started.Call.State.Should().Be("ringing");
        started.Call.Kind.Should().Be("video");
        started.Call.Version.Should().Be(1);
        started.Call.Region.Should().Be("ap-test-1", "the region comes from the server configuration");
        started.Call.Participants.Should().ContainSingle(p => p.PersonId == stack.OwnerId);
        started.JoinToken.Should().NotBeNull();
        started.JoinToken!.Token.Should().Contain(stack.OwnerId.ToString("N"), "the token is for the starter only");
        (await LoadAsync(started.Call.Id)).Region.Should().Be("ap-test-1");
    }

    [Fact]
    public async Task AnOutsiderIsNotFound_AGuestAndAReadOnlyMemberAreRefused_AndNoRoomOrCallIsCreated()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var notInChannel = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "NotInChannel");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var stranger = await _host.AddOutsiderAsync(stack);
        var privateChannel = await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPrivate, [member, guest]);
        var readOnly = await SeedChannelAsync(
            stack, stack.OwnerId, SpaceKind.ChannelPublic, [member],
            new ChannelPermissionEntry(ChannelPermissionSubject.Member, ChannelAction.Post, ChannelPermissionEffect.Deny));

        using var asNotInChannel = ProviderFor(stack, notInChannel);
        (await RefusalAsync(asNotInChannel, new StartCallCommand(stack.WorkspaceId, privateChannel.Id, "voice")))
            .Should().BeOfType<Notrelix.Application.Common.Exceptions.NotFoundException>("a workspace member outside a private channel does not learn it exists");

        using var asStranger = ProviderFor(stack, stranger);
        await RefusalAsync(asStranger, new StartCallCommand(stack.WorkspaceId, privateChannel.Id, "voice"));

        using var asGuest = ProviderFor(stack, guest);
        (await RefusalAsync(asGuest, new StartCallCommand(stack.WorkspaceId, privateChannel.Id, "voice")))
            .Should().BeOfType<Notrelix.Application.Common.Exceptions.ForbiddenException>("a Guest never starts a call");

        using var asMember = ProviderFor(stack, member);
        (await RefusalAsync(asMember, new StartCallCommand(stack.WorkspaceId, readOnly.Id, "voice")))
            .Should().BeOfType<Notrelix.Application.Common.Exceptions.ForbiddenException>("the channel keeps this member from posting");

        (await _host.ReadAsync(c => c.ChatCalls.CountAsync(x => x.SpaceId == privateChannel.Id || x.SpaceId == readOnly.Id))).Should().Be(0);
        (await _gateway.ListRoomsAsync(CancellationToken.None)).Should().BeEmpty("no room is created for a refused start");
    }

    [Fact]
    public void TheTokenQueryAsksForTheSameActionAsStart()
    {
        var id = Guid.NewGuid();
        new GetCallJoinTokenQuery(id, id).Action.Should().Be(new StartCallCommand(id, id, "voice").Action,
            "the token is the starter's way into the room, so it must ask what starting asked");
    }

    [Fact]
    public async Task AGuestOrAReadOnlyMemberSeatedInALiveCallGetsNoToken()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "SeatedGuest");
        var readOnlyMember = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "SeatedReadOnly");
        var channel = await SeedChannelAsync(
            stack, stack.OwnerId, SpaceKind.ChannelPublic, [guest, readOnlyMember],
            new ChannelPermissionEntry(ChannelPermissionSubject.Member, ChannelAction.Post, ChannelPermissionEffect.Deny));
        var call = Call.Start(stack.AccountId, stack.WorkspaceId, channel.Id, SpaceKind.ChannelPublic, stack.OwnerId, CallKind.Voice, $"room-{Guid.NewGuid():N}", "ap-southeast-1", T0);
        call.Join(guest, T0);
        call.Join(readOnlyMember, T0);
        await _host.WithSystemContextAsync(seed =>
        {
            seed.ChatCalls.Add(call);
            return Task.CompletedTask;
        });

        foreach (var person in new[] { guest, readOnlyMember })
        {
            using var provider = ProviderFor(stack, person);
            (await RefusalAsync(provider, new GetCallJoinTokenQuery(stack.WorkspaceId, call.Id)))
                .Should().BeOfType<Notrelix.Application.Common.Exceptions.ForbiddenException>("a person who may not take part in a call is given no token, seated or not");
        }
    }

    [Fact]
    public async Task AStaleVersionIs412_AndTheCurrentVersionAnswers()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var direct = await SeedDirectAsync(stack, stack.OwnerId, member);
        using var asOwner = ProviderFor(stack, stack.OwnerId);
        using var asMember = ProviderFor(stack, member);
        var started = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, direct.Id, "voice"));

        var stale = await RefusalAsync(asMember, new AnswerCallCommand(stack.WorkspaceId, started.Call.Id, ExpectedVersion: 99));
        stale.Should().BeOfType<PreconditionFailedException>("a version the client did not read is 412");
        (await LoadAsync(started.Call.Id)).State.Should().Be(CallState.Ringing);
        (await VersionAsync(started.Call.Id)).Should().Be(1);

        var answered = await SendAsync(asMember, new AnswerCallCommand(stack.WorkspaceId, started.Call.Id, 1));
        answered.Call.State.Should().Be("active");
        answered.Call.Version.Should().Be(2);
        answered.JoinToken.Should().NotBeNull();

        var staleEnd = await RefusalAsync(asOwner, new EndCallCommand(stack.WorkspaceId, started.Call.Id, ExpectedVersion: 1));
        staleEnd.Should().BeOfType<PreconditionFailedException>();
    }

    [Fact]
    public async Task ARetriedStartWithTheSameKeyReturnsTheSameCall_AndStoresOne()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var channel = await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPublic, [member]);
        using var asOwner = ProviderFor(stack, stack.OwnerId);
        var key = $"call-start-{Guid.NewGuid():N}";

        var first = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"), key);
        var replay = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"), key);

        replay.Call.Id.Should().Be(first.Call.Id);
        (await _host.ReadAsync(c => c.ChatCalls.CountAsync(x => x.SpaceId == channel.Id))).Should().Be(1);
    }

    [Fact]
    public async Task APersonInAnotherCallAnswersBusy_AndACallToThemIsStoredEndedBusyWithNoRing()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var bob = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Bob");
        var carol = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Carol");
        var bobCarol = await SeedDirectAsync(stack, bob, carol);
        var ownerBob = await SeedDirectAsync(stack, stack.OwnerId, bob);
        var other = await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPublic, [bob, carol]);

        using var asBob = ProviderFor(stack, bob);
        using var asOwner = ProviderFor(stack, stack.OwnerId);
        await StartAsync(asBob, new StartCallCommand(stack.WorkspaceId, bobCarol.Id, "voice"));

        var busy = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, ownerBob.Id, "voice"));
        busy.Call.State.Should().Be("ended");
        busy.Call.EndReason.Should().Be("busy");
        busy.JoinToken.Should().BeNull("a busy call never rings and holds no token");
        (await CallMessagesAsync(ownerBob.Id, busy.Call.Id)).Should().Be(1);
        (await LoadAsync(busy.Call.Id)).Participants.Should().BeEmpty();

        var bobAgain = await RefusalAsync(asBob, new StartCallCommand(stack.WorkspaceId, other.Id, "voice"));
        CodeOf(bobAgain).Should().Be(ChatRuleCodes.CallBusy, "a person in a call cannot start another");
    }

    [Fact]
    public async Task AVoiceCallHolds25People_AndTheNext_IsRefusedAsFull()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var members = await AddMembersAsync(stack, ChatLimits.CallMaxParticipantsVoice);
        var channel = await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPublic, members);
        using var asOwner = ProviderFor(stack, stack.OwnerId);
        var started = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"));

        for (var i = 0; i < ChatLimits.CallMaxParticipantsVoice - 1; i++)
        {
            using var asMember = ProviderFor(stack, members[i]);
            await SendAsync(asMember, new JoinCallCommand(stack.WorkspaceId, started.Call.Id, await VersionAsync(started.Call.Id)));
        }

        (await LoadAsync(started.Call.Id)).Participants.Count(p => p.IsJoined).Should().Be(25);
        using var last = ProviderFor(stack, members[^1]);
        var refused = await RefusalAsync(last, new JoinCallCommand(stack.WorkspaceId, started.Call.Id, await VersionAsync(started.Call.Id)));
        CodeOf(refused).Should().Be(ChatRuleCodes.CallFull);
        (await LoadAsync(started.Call.Id)).Participants.Count(p => p.IsJoined).Should().Be(25);
    }

    [Fact]
    public async Task AVideoCallHolds12People_AndTheNext_IsRefusedAsFull()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var members = await AddMembersAsync(stack, ChatLimits.CallMaxParticipantsVideo);
        var channel = await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPublic, members);
        using var asOwner = ProviderFor(stack, stack.OwnerId);
        var started = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "video"));

        for (var i = 0; i < ChatLimits.CallMaxParticipantsVideo - 1; i++)
        {
            using var asMember = ProviderFor(stack, members[i]);
            await SendAsync(asMember, new JoinCallCommand(stack.WorkspaceId, started.Call.Id, await VersionAsync(started.Call.Id)));
        }

        (await LoadAsync(started.Call.Id)).Participants.Count(p => p.IsJoined).Should().Be(12);
        using var last = ProviderFor(stack, members[^1]);
        var refused = await RefusalAsync(last, new JoinCallCommand(stack.WorkspaceId, started.Call.Id, await VersionAsync(started.Call.Id)));
        CodeOf(refused).Should().Be(ChatRuleCodes.CallFull);
    }

    [Fact]
    public async Task TwoSimultaneousStartsInOneChannelGiveExactlyOneSuccessAndOneRefusal()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var channel = await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPublic, [member]);

        var outcomes = await RaceStartsAsync(stack, channel.Id, [stack.OwnerId, member]);

        outcomes.Count(o => o.Error is null).Should().Be(1, "exactly one start wins");
        outcomes.Count(o => o.Error is not null).Should().Be(1, "the other is refused");
        CodeOf(outcomes.Single(o => o.Error is not null).Error!).Should().Be(ChatRuleCodes.CallAlreadyActive);
        (await LiveCallsAsync(channel.Id)).Should().Be(1);
    }

    [Fact]
    public async Task FiveSimultaneousStartsInOneChannelLeaveOneLiveCall()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var members = await AddMembersAsync(stack, 4);
        var channel = await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPublic, members);

        var outcomes = await RaceStartsAsync(stack, channel.Id, [stack.OwnerId, .. members]);

        outcomes.Count(o => o.Error is null).Should().Be(1);
        outcomes.Where(o => o.Error is not null).Should().OnlyContain(o => CodeOf(o.Error!) == ChatRuleCodes.CallAlreadyActive);
        (await LiveCallsAsync(channel.Id)).Should().Be(1);
    }

    private async Task<List<(Guid User, Exception? Error)>> RaceStartsAsync(OrgStack stack, Guid spaceId, IReadOnlyList<Guid> users)
    {
        var gate = new TaskCompletionSource();
        var racers = users.Select(user => Task.Run(async () =>
        {
            using var provider = ProviderFor(stack, user);
            await gate.Task;
            var error = await Record.ExceptionAsync(() => StartAsync(provider, new StartCallCommand(stack.WorkspaceId, spaceId, "voice")));
            return (user, error);
        })).ToList();

        await Task.Delay(200);
        gate.SetResult();
        return [.. await Task.WhenAll(racers)];
    }

    [Fact]
    public async Task ACallThatRingsForMoreThan45SecondsEndsAsMissed_AndOneCallMessageIsWritten()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var direct = await SeedDirectAsync(stack, stack.OwnerId, member);
        using var asOwner = ProviderFor(stack, stack.OwnerId);
        var started = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, direct.Id, "voice"));

        _host.Now = T0.AddSeconds(30);
        await RunSweepAsync(stack);
        (await LoadAsync(started.Call.Id)).State.Should().Be(CallState.Ringing, "30 seconds is inside the ring time");

        _host.Now = T0.AddSeconds(46);
        await RunSweepAsync(stack);
        await RunSweepAsync(stack);

        var missed = await LoadAsync(started.Call.Id);
        missed.State.Should().Be(CallState.Ended);
        missed.EndReason.Should().Be(CallEndReason.Missed);
        missed.Participants.Should().OnlyContain(p => !p.IsJoined);
        missed.Version.Should().BeGreaterThan(1);
        (await CallMessagesAsync(direct.Id, started.Call.Id)).Should().Be(1, "a repeated pass writes no second message");
    }

    [Fact]
    public async Task AnActiveCallWithNobodyInItEndsAfter60Seconds_ButNotBefore_AndNotWhenSomeoneRejoins()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var channel = await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPublic, [member]);
        using var asOwner = ProviderFor(stack, stack.OwnerId);
        using var asMember = ProviderFor(stack, member);
        var started = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"));
        await SendAsync(asMember, new AnswerCallCommand(stack.WorkspaceId, started.Call.Id, await VersionAsync(started.Call.Id)));

        await SendAsync(asMember, new LeaveCallCommand(stack.WorkspaceId, started.Call.Id, await VersionAsync(started.Call.Id)));
        await SendAsync(asOwner, new LeaveCallCommand(stack.WorkspaceId, started.Call.Id, await VersionAsync(started.Call.Id)));
        (await LoadAsync(started.Call.Id)).EmptySince.Should().Be(T0);

        _host.Now = T0.AddSeconds(59);
        await RunSweepAsync(stack);
        (await LoadAsync(started.Call.Id)).State.Should().Be(CallState.Active, "59 seconds is inside the grace period");

        _host.Now = T0.AddSeconds(61);
        await RunSweepAsync(stack);
        var ended = await LoadAsync(started.Call.Id);
        ended.State.Should().Be(CallState.Ended);
        ended.EndReason.Should().Be(CallEndReason.Completed);
        (await CallMessagesAsync(channel.Id, started.Call.Id)).Should().Be(1);
    }

    [Fact]
    public async Task AnActiveCallThatSomeoneRejoinedIsNotEndedByTheSweep()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var channel = await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPublic, [member]);
        using var asOwner = ProviderFor(stack, stack.OwnerId);
        using var asMember = ProviderFor(stack, member);
        var started = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"));
        await SendAsync(asMember, new AnswerCallCommand(stack.WorkspaceId, started.Call.Id, await VersionAsync(started.Call.Id)));
        await SendAsync(asMember, new LeaveCallCommand(stack.WorkspaceId, started.Call.Id, await VersionAsync(started.Call.Id)));
        await SendAsync(asOwner, new LeaveCallCommand(stack.WorkspaceId, started.Call.Id, await VersionAsync(started.Call.Id)));
        await SendAsync(asMember, new JoinCallCommand(stack.WorkspaceId, started.Call.Id, await VersionAsync(started.Call.Id)));

        _host.Now = T0.AddMinutes(10);
        await RunSweepAsync(stack);

        (await LoadAsync(started.Call.Id)).State.Should().Be(CallState.Active);
    }

    [Fact]
    public async Task EndingADirectCallWritesExactlyOneCallMessage_AndASecondEndOrASecondMessageIsRefused()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var direct = await SeedDirectAsync(stack, stack.OwnerId, member);
        using var asOwner = ProviderFor(stack, stack.OwnerId);
        using var asMember = ProviderFor(stack, member);
        var started = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, direct.Id, "voice"));
        await SendAsync(asMember, new AnswerCallCommand(stack.WorkspaceId, started.Call.Id, 1));

        _host.Now = T0.AddSeconds(125);
        var ended = await SendAsync(asOwner, new EndCallCommand(stack.WorkspaceId, started.Call.Id, 2));
        ended.State.Should().Be("ended");
        ended.EndReason.Should().Be("completed");
        (await CallMessagesAsync(direct.Id, started.Call.Id)).Should().Be(1);
        var text = await _host.ReadAsync(c => c.ChatMessages.Where(m => m.SpaceId == direct.Id && m.CallId == started.Call.Id).Select(m => m.Content).SingleAsync());
        text.Should().Contain("voice call").And.Contain("2m 5s");

        var again = await RefusalAsync(asMember, new EndCallCommand(stack.WorkspaceId, started.Call.Id, 3));
        CodeOf(again).Should().Be(ChatRuleCodes.CallEnded);
        (await CallMessagesAsync(direct.Id, started.Call.Id)).Should().Be(1);

        // The unique index is the backstop of the rule: a second call message for the same call is refused by the database.
        var duplicate = await Record.ExceptionAsync(() => _host.WithSystemContextAsync(async seed =>
        {
            var next = await new ChatSpaceSequenceAllocator(seed).NextAsync(direct.Id, CancellationToken.None);
            seed.ChatMessages.Add(Message.CreateCallMessage(stack.AccountId, stack.WorkspaceId, direct.Id, next, stack.OwnerId, started.Call.Id, "again", T0));
        }));
        duplicate.Should().NotBeNull("one call message per space and call");
    }

    [Fact]
    public async Task AnUnreachableMediaPlaneAnswers503AndLeavesNothingRinging()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var channel = await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPublic, [member]);
        using var asOwner = ProviderFor(stack, stack.OwnerId, gateway: new UnreachableGateway());

        var refused = await RefusalAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"));

        CodeOf(refused).Should().Be(ChatRuleCodes.CallsUnavailable);
        (await _host.ReadAsync(c => c.ChatCalls.CountAsync(x => x.SpaceId == channel.Id))).Should().Be(0);
    }

    [Fact]
    public async Task SwitchedOffCallsAreUnavailable()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var channel = await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPublic, []);
        using var asOwner = ProviderFor(stack, stack.OwnerId, options: new ChatCallOptions { Enabled = false });

        var refused = await RefusalAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"));

        CodeOf(refused).Should().Be(ChatRuleCodes.CallsUnavailable);
    }

    [Fact]
    public async Task TheActiveCallAndTheHistoryAreReadByMembersOnly()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var outsider = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Outsider");
        var channel = await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPrivate, [member]);
        using var asOwner = ProviderFor(stack, stack.OwnerId);
        using var asMember = ProviderFor(stack, member);
        using var asOutsider = ProviderFor(stack, outsider);
        var started = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"));

        (await SendAsync(asMember, new GetActiveCallQuery(stack.WorkspaceId, channel.Id)))!.Id.Should().Be(started.Call.Id);
        (await RefusalAsync(asOutsider, new GetActiveCallQuery(stack.WorkspaceId, channel.Id))).Should().BeOfType<Notrelix.Application.Common.Exceptions.NotFoundException>();
        (await RefusalAsync(asOutsider, new ListCallsQuery(stack.WorkspaceId, channel.Id, null, 50))).Should().BeOfType<Notrelix.Application.Common.Exceptions.NotFoundException>();

        await SendAsync(asOwner, new Notrelix.Application.Features.Chat.Calls.Commands.CancelCall.CancelCallCommand(stack.WorkspaceId, started.Call.Id, 1));
        (await SendAsync(asMember, new GetActiveCallQuery(stack.WorkspaceId, channel.Id))).Should().BeNull();
        var history = await SendAsync(asMember, new ListCallsQuery(stack.WorkspaceId, channel.Id, null, 50));
        history.Items.Should().ContainSingle(c => c.Id == started.Call.Id && c.EndReason == "cancelled");
    }

    [Fact]
    public async Task AGuestAndAReadOnlyMemberAreRefusedOnAnswerAndJoin_AndGetNoTokenNorSeat()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var readOnly = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "ReadOnly");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var channel = await SeedChannelAsync(
            stack, stack.OwnerId, SpaceKind.ChannelPrivate, [member, readOnly, guest],
            new ChannelPermissionEntry(ChannelPermissionSubject.Member, ChannelAction.Post, ChannelPermissionEffect.Deny));
        // The owner is a workspace admin, so the override leaves them free to post; the plain member joins as the live caller.
        using var asOwner = ProviderFor(stack, stack.OwnerId);
        var started = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"));

        using var asGuest = ProviderFor(stack, guest);
        using var asReadOnly = ProviderFor(stack, readOnly);
        (await RefusalAsync(asGuest, new AnswerCallCommand(stack.WorkspaceId, started.Call.Id, 1)))
            .Should().BeOfType<Notrelix.Application.Common.Exceptions.ForbiddenException>("a Guest never takes part in a call");
        (await RefusalAsync(asReadOnly, new AnswerCallCommand(stack.WorkspaceId, started.Call.Id, 1)))
            .Should().BeOfType<Notrelix.Application.Common.Exceptions.ForbiddenException>("the channel keeps this member from posting");

        // Make the call Active through the owner's own seat (a group call), then try to join.
        await _host.WithSystemContextAsync(async seed =>
        {
            var call = await seed.ChatCalls.Include(c => c.Participants).SingleAsync(c => c.Id == started.Call.Id);
            call.Join(stack.OwnerId, T0);
            call.Answer(Guid.NewGuid(), T0);
        });
        var version = await VersionAsync(started.Call.Id);
        (await RefusalAsync(asGuest, new JoinCallCommand(stack.WorkspaceId, started.Call.Id, version)))
            .Should().BeOfType<Notrelix.Application.Common.Exceptions.ForbiddenException>();
        (await RefusalAsync(asReadOnly, new JoinCallCommand(stack.WorkspaceId, started.Call.Id, version)))
            .Should().BeOfType<Notrelix.Application.Common.Exceptions.ForbiddenException>();

        var stored = await LoadAsync(started.Call.Id);
        stored.Participants.Should().NotContain(p => p.PersonId == guest || p.PersonId == readOnly);
    }

    [Fact]
    public async Task AnArchivedSpaceRefusesAnswerAndJoin()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Other");
        var channel = await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPublic, [other]);
        using var asOwner = ProviderFor(stack, stack.OwnerId);
        using var asOther = ProviderFor(stack, other);
        var viaChannel = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"));
        await _host.WithSystemContextAsync(async seed =>
        {
            var space = await seed.ChatSpaces.SingleAsync(s => s.Id == channel.Id);
            space.SetArchived(true, stack.OwnerId, T0);
        });

        var joinRefused = await RefusalAsync(asOther, new JoinCallCommand(stack.WorkspaceId, viaChannel.Call.Id, 1));
        CodeOf(joinRefused).Should().Be(ChatRuleCodes.ChannelArchived);
        var answerRefused = await RefusalAsync(asOther, new AnswerCallCommand(stack.WorkspaceId, viaChannel.Call.Id, 1));
        CodeOf(answerRefused).Should().Be(ChatRuleCodes.ChannelArchived);
        (await LoadAsync(viaChannel.Call.Id)).Participants.Should().NotContain(p => p.PersonId == other);
    }

    [Fact]
    public async Task ABlockBetweenTwoPeopleRefusesAnswerOfTheirDirectCall()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var direct = await SeedDirectAsync(stack, stack.OwnerId, member);
        using var asOwner = ProviderFor(stack, stack.OwnerId);
        using var asMember = ProviderFor(stack, member);
        var started = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, direct.Id, "voice"));
        await _host.WithSystemContextAsync(seed =>
        {
            seed.ChatBlocks.Add(PersonBlock.Create(stack.AccountId, stack.WorkspaceId, stack.OwnerId, member, T0));
            return Task.CompletedTask;
        });

        (await RefusalAsync(asMember, new AnswerCallCommand(stack.WorkspaceId, started.Call.Id, 1)))
            .Should().BeOfType<Notrelix.Application.Common.Exceptions.NotFoundException>("a blocked pair does not call each other");
        (await LoadAsync(started.Call.Id)).State.Should().Be(CallState.Ringing);
    }

    [Fact]
    public async Task ARemovedChannelMemberIsReleasedFromTheirLiveCallAtOnce()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var other = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Other");
        var channel = await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPublic, [member, other]);
        using var asOwner = ProviderFor(stack, stack.OwnerId);
        using var asMember = ProviderFor(stack, member);
        var started = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"));
        await SendAsync(asMember, new AnswerCallCommand(stack.WorkspaceId, started.Call.Id, 1));
        var before = await VersionAsync(started.Call.Id);

        await OrgTreeTestHost.SendAsync(asOwner, new Notrelix.Application.Features.Chat.Members.Commands.RemoveChannelMember.RemoveChannelMemberCommand(stack.WorkspaceId, channel.Id, member));

        var after = await LoadAsync(started.Call.Id);
        after.Participants.Single(p => p.PersonId == member).IsJoined.Should().BeFalse("revoking access ends the participation");
        after.Version.Should().BeGreaterThan(before);
        after.State.Should().Be(CallState.Active, "the owner is still in the call");
    }

    [Fact]
    public async Task ARemovedMemberOfADirectCallEndsTheCallAndWritesOneMessage()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var direct = await SeedDirectAsync(stack, stack.OwnerId, member);
        using var asOwner = ProviderFor(stack, stack.OwnerId);
        using var asMember = ProviderFor(stack, member);
        var started = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, direct.Id, "voice"));
        await SendAsync(asMember, new AnswerCallCommand(stack.WorkspaceId, started.Call.Id, 1));

        using (var scope = asOwner.CreateScope())
        {
            var exit = scope.ServiceProvider.GetRequiredService<Notrelix.Application.Features.Chat.Members.Services.ChatWorkspaceExit>();
            await exit.EndMembershipsAsync(stack.WorkspaceId, member, T0.AddSeconds(5), CancellationToken.None);
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().SaveChangesAsync();
        }

        var after = await LoadAsync(started.Call.Id);
        after.State.Should().Be(CallState.Ended);
        after.Participants.Should().OnlyContain(p => !p.IsJoined);
        (await CallMessagesAsync(direct.Id, started.Call.Id)).Should().Be(1);
    }

    [Fact]
    public async Task TheSweepReleasesAParticipantWhoseAccessEndedAndEndsTheEmptyCall()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var channel = await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPublic, [member]);
        using var asOwner = ProviderFor(stack, stack.OwnerId);
        using var asMember = ProviderFor(stack, member);
        var started = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"));
        await SendAsync(asMember, new AnswerCallCommand(stack.WorkspaceId, started.Call.Id, 1));
        await SendAsync(asOwner, new LeaveCallCommand(stack.WorkspaceId, started.Call.Id, await VersionAsync(started.Call.Id)));
        // The member's access ended without a hook running (a missed event): the membership row is closed directly.
        await _host.WithSystemContextAsync(async seed =>
        {
            var space = await Notrelix.Application.Features.Chat.Channels.Services.ChannelAccess.LoadLiveAsync(seed, stack.WorkspaceId, channel.Id, CancellationToken.None);
            space.RemoveMember(member, T0);
        });

        _host.Now = T0.AddSeconds(5);
        await RunSweepAsync(stack);

        var after = await LoadAsync(started.Call.Id);
        after.Participants.Single(p => p.PersonId == member).IsJoined.Should().BeFalse();
        (await VersionAsync(started.Call.Id)).Should().BeGreaterThan(3);
        after.State.Should().Be(CallState.Ended, "nobody with access is left in the call");
    }

    [Fact]
    public async Task ARetriedStartStoresNoTokenAndMintsAFreshOneEachTime()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var channel = await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPublic, [member]);
        using var asOwner = ProviderFor(stack, stack.OwnerId);
        var key = $"call-token-{Guid.NewGuid():N}";

        var first = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"), key);
        var replay = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"), key);

        first.JoinToken.Should().NotBeNull();
        replay.JoinToken.Should().NotBeNull();
        replay.JoinToken!.Token.Should().NotBe(first.JoinToken!.Token, "a replay mints a fresh token");
        var stored = await _host.ReadAsync(c => c.IdempotencyRecords.Where(r => r.Operation == "chat.calls.start-call.v1").Select(r => r.ResultJson).ToListAsync());
        stored.Should().NotBeEmpty();
        stored.Should().OnlyContain(json => json != null && !json.Contains(first.JoinToken.Token) && !json.Contains("joinToken", StringComparison.OrdinalIgnoreCase),
            "the idempotency store never keeps a media token");
    }

    [Fact]
    public async Task AReplayByACallerWhoLeftTheCallGetsNoToken()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var channel = await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPublic, [member]);
        using var asOwner = ProviderFor(stack, stack.OwnerId);
        using var asMember = ProviderFor(stack, member);
        var key = $"call-left-{Guid.NewGuid():N}";
        var first = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"), key);
        await SendAsync(asMember, new AnswerCallCommand(stack.WorkspaceId, first.Call.Id, 1));
        await SendAsync(asOwner, new LeaveCallCommand(stack.WorkspaceId, first.Call.Id, await VersionAsync(first.Call.Id)));

        var replay = await Record.ExceptionAsync(() => StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"), key));
        replay.Should().NotBeNull();

        CodeOf(replay!).Should().Be(ChatRuleCodes.CallNotParticipant, "a caller who is no longer in the call gets no token");
    }

    [Fact]
    public async Task TheSweepKicksAPersonTheMediaRoomStillHoldsAfterTheirAccessEnded_AndRetriesAFailedKick()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var other = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Other");
        var channel = await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPublic, [member, other]);
        var room = new LeakyGateway { FailedKicksLeft = 1 };
        using var asOwner = ProviderFor(stack, stack.OwnerId, gateway: room);
        using var asMember = ProviderFor(stack, member, gateway: room);
        var started = await StartAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"));
        await SendAsync(asMember, new AnswerCallCommand(stack.WorkspaceId, started.Call.Id, 1));
        // The kick fact is queued for a consumer that is not running here, so the room keeps the revoked person: the leak the sweep closes.
        await OrgTreeTestHost.SendAsync(asOwner, new Notrelix.Application.Features.Chat.Members.Commands.RemoveChannelMember.RemoveChannelMemberCommand(stack.WorkspaceId, channel.Id, member));
        room.Connected.Should().Contain(member, "every kick failed, so the media room still holds the revoked person");

        await RunSweepAsync(stack, room);
        room.Connected.Should().Contain(member, "the first kick of the sweep fails and is only logged");

        await RunSweepAsync(stack, room);
        room.Connected.Should().NotContain(member, "the next pass retries and the room is clean");
        room.Connected.Should().Contain(stack.OwnerId, "a person with an open participation is never kicked");
    }

    /// <summary>A media room that keeps whoever was given a token until a kick succeeds; the first kicks can be made to fail.</summary>
    private sealed class LeakyGateway : IChatCallMediaGateway
    {
        private readonly object _gate = new();
        private readonly HashSet<(string Room, Guid Person)> _connected = [];
        public int FailedKicksLeft;

        public IReadOnlyCollection<Guid> Connected
        {
            get { lock (_gate) return _connected.Select(c => c.Person).ToList(); }
        }

        public Task<CallRoom> CreateRoomAsync(string roomKey, string region, int maxParticipants, CancellationToken cancellationToken) =>
            Task.FromResult(new CallRoom(roomKey, region));

        public Task<CallJoinToken> IssueTokenAsync(string roomKey, Guid personId, bool canPublishVideo, TimeSpan ttl, CancellationToken cancellationToken)
        {
            lock (_gate) _connected.Add((roomKey, personId));
            return Task.FromResult(new CallJoinToken("wss://leaky.invalid", $"leaky.{Guid.NewGuid():N}", DateTimeOffset.UtcNow + ttl));
        }

        public Task EndRoomAsync(string roomKey, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateParticipantPermissionAsync(string roomKey, Guid personId, bool canPublish, bool canPublishVideo, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RemoveParticipantAsync(string roomKey, Guid personId, CancellationToken cancellationToken)
        {
            if (Interlocked.Decrement(ref FailedKicksLeft) >= 0)
                throw new CallMediaUnavailableException("The media plane refuses the kick.");
            lock (_gate) _connected.Remove((roomKey, personId));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Guid>> ListParticipantsAsync(string roomKey, CancellationToken cancellationToken)
        {
            lock (_gate) return Task.FromResult<IReadOnlyList<Guid>>(_connected.Where(c => c.Room == roomKey).Select(c => c.Person).ToList());
        }

        public Task<IReadOnlyList<string>> ListRoomsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<string>>([]);

        public CallMediaWebhookEvent? VerifyWebhook(string rawBody, string? authorization) => null;
    }

    private sealed class UnreachableGateway : IChatCallMediaGateway
    {
        public Task<CallRoom> CreateRoomAsync(string roomKey, string region, int maxParticipants, CancellationToken cancellationToken) =>
            throw new CallMediaUnavailableException("The media plane is down.");

        public Task<CallJoinToken> IssueTokenAsync(string roomKey, Guid personId, bool canPublishVideo, TimeSpan ttl, CancellationToken cancellationToken) =>
            throw new CallMediaUnavailableException("The media plane is down.");

        public Task EndRoomAsync(string roomKey, CancellationToken cancellationToken) =>
            throw new CallMediaUnavailableException("The media plane is down.");

        public Task UpdateParticipantPermissionAsync(string roomKey, Guid personId, bool canPublish, bool canPublishVideo, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RemoveParticipantAsync(string roomKey, Guid personId, CancellationToken cancellationToken) =>
            throw new CallMediaUnavailableException("The media plane is down.");

        public Task<IReadOnlyList<Guid>> ListParticipantsAsync(string roomKey, CancellationToken cancellationToken) =>
            throw new CallMediaUnavailableException("The media plane is down.");

        public Task<IReadOnlyList<string>> ListRoomsAsync(CancellationToken cancellationToken) =>
            throw new CallMediaUnavailableException("The media plane is down.");

        public CallMediaWebhookEvent? VerifyWebhook(string rawBody, string? authorization) => null;
    }
}
