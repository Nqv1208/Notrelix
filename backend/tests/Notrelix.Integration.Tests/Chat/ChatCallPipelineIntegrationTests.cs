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
using Notrelix.Application.Features.Chat.Calls.Queries.GetActiveCall;
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
    private readonly IChatCallMediaGateway _gateway = new InMemoryChatCallMediaGateway(Options.Create(new ChatCallOptions()));

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
            services.AddScoped<ChatCallSweepRunner>();
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

    private async Task RunSweepAsync(OrgStack stack)
    {
        using var provider = ProviderFor(stack, stack.OwnerId);
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
        var started = await SendAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "video"));

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
    public async Task AStaleVersionIs412_AndTheCurrentVersionAnswers()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var direct = await SeedDirectAsync(stack, stack.OwnerId, member);
        using var asOwner = ProviderFor(stack, stack.OwnerId);
        using var asMember = ProviderFor(stack, member);
        var started = await SendAsync(asOwner, new StartCallCommand(stack.WorkspaceId, direct.Id, "voice"));

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

        var first = await SendAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"), key);
        var replay = await SendAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"), key);

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
        await SendAsync(asBob, new StartCallCommand(stack.WorkspaceId, bobCarol.Id, "voice"));

        var busy = await SendAsync(asOwner, new StartCallCommand(stack.WorkspaceId, ownerBob.Id, "voice"));
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
        var started = await SendAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"));

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
        var started = await SendAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "video"));

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
            var error = await Record.ExceptionAsync(() => SendAsync(provider, new StartCallCommand(stack.WorkspaceId, spaceId, "voice")));
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
        var started = await SendAsync(asOwner, new StartCallCommand(stack.WorkspaceId, direct.Id, "voice"));

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
        var started = await SendAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"));
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
        var started = await SendAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"));
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
        var started = await SendAsync(asOwner, new StartCallCommand(stack.WorkspaceId, direct.Id, "voice"));
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
        var started = await SendAsync(asOwner, new StartCallCommand(stack.WorkspaceId, channel.Id, "voice"));

        (await SendAsync(asMember, new GetActiveCallQuery(stack.WorkspaceId, channel.Id)))!.Id.Should().Be(started.Call.Id);
        (await RefusalAsync(asOutsider, new GetActiveCallQuery(stack.WorkspaceId, channel.Id))).Should().BeOfType<Notrelix.Application.Common.Exceptions.NotFoundException>();
        (await RefusalAsync(asOutsider, new ListCallsQuery(stack.WorkspaceId, channel.Id, null, 50))).Should().BeOfType<Notrelix.Application.Common.Exceptions.NotFoundException>();

        await SendAsync(asOwner, new Notrelix.Application.Features.Chat.Calls.Commands.CancelCall.CancelCallCommand(stack.WorkspaceId, started.Call.Id, 1));
        (await SendAsync(asMember, new GetActiveCallQuery(stack.WorkspaceId, channel.Id))).Should().BeNull();
        var history = await SendAsync(asMember, new ListCallsQuery(stack.WorkspaceId, channel.Id, null, 50));
        history.Items.Should().ContainSingle(c => c.Id == started.Call.Id && c.EndReason == "cancelled");
    }

    private sealed class UnreachableGateway : IChatCallMediaGateway
    {
        public Task<CallRoom> CreateRoomAsync(string roomKey, string region, int maxParticipants, CancellationToken cancellationToken) =>
            throw new CallMediaUnavailableException("The media plane is down.");

        public Task<CallJoinToken> IssueTokenAsync(string roomKey, Guid personId, bool canPublishVideo, TimeSpan ttl, CancellationToken cancellationToken) =>
            throw new CallMediaUnavailableException("The media plane is down.");

        public Task EndRoomAsync(string roomKey, CancellationToken cancellationToken) =>
            throw new CallMediaUnavailableException("The media plane is down.");

        public Task<IReadOnlyList<string>> ListRoomsAsync(CancellationToken cancellationToken) =>
            throw new CallMediaUnavailableException("The media plane is down.");

        public CallMediaWebhookEvent? VerifyWebhook(string rawBody, string? authorization) => null;
    }
}
