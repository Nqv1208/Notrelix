using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Calls;
using Notrelix.Application.Features.Chat.Calls.Commands.UpdateCallMediaState;
using Notrelix.Application.Features.Chat.Calls.DTOs;
using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Calls;
using Notrelix.Domain.Chat.Permissions;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Chat;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Chat;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// Sharing a screen in a call through the real request pipeline: who may start a share (a Guest and a member the channel keeps from posting
/// are 403, an outsider is NotFound), a voice call carries no screen, one share at a time with an explicit replace, and the person who
/// stops, leaves or is removed shares nothing. The media plane is the in-memory gateway. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatCallScreenShareIntegrationTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 19, 9, 0, 0, TimeSpan.Zero);

    private readonly OrgTreeTestHost _host;
    private readonly InMemoryChatCallMediaGateway _gateway = new(Options.Create(new ChatCallOptions()));

    public ChatCallScreenShareIntegrationTests(PostgresTestContainer db) => _host = new OrgTreeTestHost(db) { Now = T0 };

    private ServiceProvider ProviderFor(OrgStack stack, Guid userId) =>
        _host.CreateProvider(stack.AccountId, userId, services =>
        {
            services.AddScoped<IChatDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<ISpaceSequenceAllocator, ChatSpaceSequenceAllocator>();
            services.AddScoped<IWorkspaceMemberFacts, WorkspaceMemberFacts>();
            services.AddSingleton<IOptions<ChatCallOptions>>(Options.Create(new ChatCallOptions()));
            services.AddSingleton<IChatCallMediaGateway>(_gateway);
        });

    private async Task<Space> SeedChannelAsync(OrgStack stack, IEnumerable<Guid> members, params ChannelPermissionEntry[] overrides)
    {
        var channel = Space.CreateChannel(stack.AccountId, stack.WorkspaceId, SpaceKind.ChannelPublic, $"share-{Guid.NewGuid():N}"[..14], null, null, 1, stack.OwnerId, T0);
        foreach (var member in members)
            channel.AddMember(member, MembershipRole.Member, T0);
        if (overrides.Length > 0)
            channel.SetPermissionOverrides(overrides, stack.OwnerId, T0);
        await _host.WithSystemContextAsync(seed =>
        {
            seed.ChatSpaces.Add(channel);
            return Task.CompletedTask;
        });
        return channel;
    }

    /// <summary>A live call seeded straight into the store, the people already seated.</summary>
    private async Task<Call> SeedCallAsync(OrgStack stack, Space channel, CallKind kind, params Guid[] joiners)
    {
        var call = Call.Start(stack.AccountId, stack.WorkspaceId, channel.Id, channel.Kind, stack.OwnerId, kind, $"room-{Guid.NewGuid():N}", "ap-southeast-1", T0);
        foreach (var person in joiners) call.Join(person, T0);
        await _host.WithSystemContextAsync(seed =>
        {
            seed.ChatCalls.Add(call);
            return Task.CompletedTask;
        });
        return call;
    }

    private async Task<Call> LoadAsync(Guid callId) =>
        await _host.ReadAsync(c => c.ChatCalls.AsNoTracking().Include(x => x.Participants).SingleAsync(x => x.Id == callId));

    private async Task<CallDto> ShareAsync(
        ServiceProvider provider, OrgStack stack, Call call, bool sharing, bool replace = false, bool videoOn = false)
    {
        var current = (await LoadAsync(call.Id)).Version;
        var result = (Result<CallDto>)(await OrgTreeTestHost.SendIdempotentAsync(
            provider, new UpdateCallMediaStateCommand(stack.WorkspaceId, call.Id, current, true, videoOn, sharing, replace), null))!;
        return result.Data!;
    }

    private static async Task<Exception> RefusedAsync(Func<Task> act)
    {
        var refused = await Record.ExceptionAsync(act);
        refused.Should().NotBeNull("the request must be refused");
        return refused!;
    }

    private static string? CodeOf(Exception exception) => exception switch
    {
        Notrelix.Domain.Common.Exceptions.BusinessRuleException rule => rule.RuleCode,
        Notrelix.Application.Common.Exceptions.ConflictException conflict => conflict.ErrorCode,
        _ => null,
    };

    [Fact]
    public async Task AMemberInAVideoCallShares_AndTheStoredCallSaysWho()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Sharer");
        var channel = await SeedChannelAsync(stack, [member]);
        var call = await SeedCallAsync(stack, channel, CallKind.Video, member);

        using var asMember = ProviderFor(stack, member);
        var shared = await ShareAsync(asMember, stack, call, sharing: true);

        shared.Participants.Single(p => p.PersonId == member).SharingScreen.Should().BeTrue();
        (await LoadAsync(call.Id)).Participants.Single(p => p.PersonId == member).SharingScreen.Should().BeTrue();
    }

    [Fact]
    public async Task AGuestAndAMemberTheChannelKeepsFromPostingCannotStartAShare_AndStoreNothing()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "SeatedGuest");
        var readOnly = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "SeatedReadOnly");
        var channel = await SeedChannelAsync(
            stack, [guest, readOnly],
            new ChannelPermissionEntry(ChannelPermissionSubject.Member, ChannelAction.Post, ChannelPermissionEffect.Deny));
        var call = await SeedCallAsync(stack, channel, CallKind.Video, guest, readOnly);

        foreach (var person in new[] { guest, readOnly })
        {
            using var provider = ProviderFor(stack, person);
            (await RefusedAsync(() => ShareAsync(provider, stack, call, sharing: true)))
                .Should().BeOfType<Notrelix.Application.Common.Exceptions.ForbiddenException>("a person who may not take part may not share either, seated or not");
        }

        (await LoadAsync(call.Id)).Participants.Should().OnlyContain(p => !p.SharingScreen);
    }

    [Fact]
    public async Task APersonOutsideTheSpaceIsNotFound_AndSharingNeverReachesTheirCall()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "InCall");
        var notInChannel = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "NotInChannel");
        var channel = await SeedChannelAsync(stack, [member]);
        var privateChannel = Space.CreateChannel(stack.AccountId, stack.WorkspaceId, SpaceKind.ChannelPrivate, $"priv-{Guid.NewGuid():N}"[..14], null, null, 1, stack.OwnerId, T0);
        privateChannel.AddMember(member, MembershipRole.Member, T0);
        await _host.WithSystemContextAsync(seed =>
        {
            seed.ChatSpaces.Add(privateChannel);
            return Task.CompletedTask;
        });
        var call = await SeedCallAsync(stack, privateChannel, CallKind.Video, member);

        using var asOutsider = ProviderFor(stack, notInChannel);
        (await RefusedAsync(() => ShareAsync(asOutsider, stack, call, sharing: true)))
            .Should().BeOfType<Notrelix.Application.Common.Exceptions.NotFoundException>("a workspace member outside a private channel does not learn its call exists");
        (await LoadAsync(call.Id)).Participants.Should().OnlyContain(p => !p.SharingScreen);
        channel.Id.Should().NotBe(privateChannel.Id);
    }

    [Fact]
    public async Task AVoiceCallRefusesAScreenShare()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "VoiceSharer");
        var channel = await SeedChannelAsync(stack, [member]);
        var call = await SeedCallAsync(stack, channel, CallKind.Voice, member);

        using var asMember = ProviderFor(stack, member);
        var refused = await RefusedAsync(() => ShareAsync(asMember, stack, call, sharing: true));

        CodeOf(refused).Should().Be(ChatRuleCodes.CallMediaNotAllowed);
        (await LoadAsync(call.Id)).Participants.Should().OnlyContain(p => !p.SharingScreen);
    }

    [Fact]
    public async Task OneShareAtATime_ASecondIs409UntilTheCallerConfirmsAReplace()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var first = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "First");
        var second = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Second");
        var channel = await SeedChannelAsync(stack, [first, second]);
        var call = await SeedCallAsync(stack, channel, CallKind.Video, first, second);

        using var asFirst = ProviderFor(stack, first);
        using var asSecond = ProviderFor(stack, second);
        await ShareAsync(asFirst, stack, call, sharing: true);

        var refused = await RefusedAsync(() => ShareAsync(asSecond, stack, call, sharing: true));
        CodeOf(refused).Should().Be(ChatRuleCodes.CallScreenShareTaken);
        var stored = await LoadAsync(call.Id);
        (stored.Participants.Single(p => p.PersonId == first).SharingScreen, stored.Participants.Single(p => p.PersonId == second).SharingScreen)
            .Should().Be((true, false));

        var replaced = await ShareAsync(asSecond, stack, call, sharing: true, replace: true);
        (replaced.Participants.Single(p => p.PersonId == first).SharingScreen, replaced.Participants.Single(p => p.PersonId == second).SharingScreen)
            .Should().Be((false, true));
    }

    [Fact]
    public async Task StoppingALeavingOrARevokedSharerFreesTheNextShare()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var first = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Leaver");
        var second = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Next");
        var channel = await SeedChannelAsync(stack, [first, second]);
        var call = await SeedCallAsync(stack, channel, CallKind.Video, first, second);

        using var asFirst = ProviderFor(stack, first);
        using var asSecond = ProviderFor(stack, second);
        await ShareAsync(asFirst, stack, call, sharing: true);
        await ShareAsync(asFirst, stack, call, sharing: false);
        (await ShareAsync(asSecond, stack, call, sharing: true)).Participants.Single(p => p.PersonId == second).SharingScreen.Should().BeTrue();

        await ShareAsync(asSecond, stack, call, sharing: false);
        await ShareAsync(asFirst, stack, call, sharing: true);
        await _host.WithSystemContextAsync(async seed =>
        {
            var tracked = await seed.ChatCalls.Include(c => c.Participants).SingleAsync(c => c.Id == call.Id);
            tracked.Leave(first, T0); // the clock of the host is fixed at T0
        });
        (await LoadAsync(call.Id)).Participants.Should().OnlyContain(p => !p.SharingScreen, "a sharer who left shares nothing");
        (await ShareAsync(asSecond, stack, call, sharing: true)).Participants.Single(p => p.PersonId == second).SharingScreen.Should().BeTrue();
    }
}
