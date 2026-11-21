using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Channels.Commands.DeleteChannel;
using Notrelix.Application.Features.Chat.Channels.Commands.RestoreChannel;
using Notrelix.Application.Features.Chat.Channels.Queries.GetChannel;
using Notrelix.Application.Features.Chat.Channels.Queries.ListDeletedChannels;
using Notrelix.Application.Features.Chat.Links.Services;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// UC-CHT-A08 through the real request pipeline on PostgreSQL: a manager deletes a channel, sees it in the deleted list and restores it (members
/// and roles as they were); the version check loads the soft-deleted channel (stale is refused); a person who could not have deleted it sees and
/// restores nothing (404, a private name never leaks); a name taken meanwhile is name-conflict; past 30 days is restore-window-elapsed.
/// Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatChannelRestorePipelineTests
{
    private readonly OrgTreeTestHost _host;

    public ChatChannelRestorePipelineTests(PostgresTestContainer db) => _host = new OrgTreeTestHost(db);

    private ServiceProvider ProviderFor(OrgStack stack, Guid userId) =>
        _host.CreateProvider(stack.AccountId, userId, services =>
        {
            services.AddScoped<IChatDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<ChatAuditFacts>();
            services.AddScoped<ChatResourceViews>();
            services.AddScoped<Notrelix.Application.Features.Documents.Public.PageViewAccess.IPageViewAccess,
                Notrelix.Application.Features.Documents.Pages.Services.PageViewAccess>();
        });

    private async Task<Space> SeedChannelAsync(OrgStack stack, Guid manager, SpaceKind kind, string name, params Guid[] members)
    {
        var channel = Space.CreateChannel(stack.AccountId, stack.WorkspaceId, kind, name, null, null, 1, manager, OrgTreeTestHost.BaseTime);
        foreach (var member in members)
            channel.AddMember(member, MembershipRole.Member, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(seed =>
        {
            seed.ChatSpaces.Add(channel);
            return Task.CompletedTask;
        });
        return channel;
    }

    private static async Task<Result<T>> SendAsync<T>(ServiceProvider provider, IRequest<Result<T>> request)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    private static async Task<Result> SendAsync(ServiceProvider provider, IRequest<Result> request)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    private Task<long> VersionAsync(Guid channelId) =>
        _host.ReadAsync(c => c.ChatSpaces.IgnoreQueryFilters().Where(s => s.Id == channelId).Select(s => s.Version).SingleAsync());

    private Task<DateTimeOffset?> DeletedAtAsync(Guid channelId) =>
        _host.ReadAsync(c => c.ChatSpaces.IgnoreQueryFilters().Where(s => s.Id == channelId).Select(s => s.DeletedAt).SingleAsync());

    [Fact]
    public async Task AManagerDeletesListsAndRestores_TheChannelComesBackWithItsMembers_AndAStaleVersionIsRefused()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var manager = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Manager");
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var channel = await SeedChannelAsync(stack, manager, SpaceKind.ChannelPublic, $"ch-{Guid.NewGuid():N}"[..12], member);

        using var asManager = ProviderFor(stack, manager);
        (await SendAsync(asManager, new DeleteChannelCommand(stack.WorkspaceId, channel.Id, await VersionAsync(channel.Id)))).Succeeded.Should().BeTrue();

        var listed = await SendAsync(asManager, new ListDeletedChannelsQuery(stack.WorkspaceId));
        var row = listed.Data!.Should().ContainSingle(d => d.Id == channel.Id).Subject;
        row.DeletedBy.Should().Be(manager);
        row.RestorableUntil.Should().Be(row.DeletedAt + Space.RestoreWindow);

        var stale = await Record.ExceptionAsync(() => SendAsync(asManager, new RestoreChannelCommand(stack.WorkspaceId, channel.Id, row.Version - 1)));
        stale.Should().NotBeNull("a version the client did not read is refused (412 at the transport)");
        (await DeletedAtAsync(channel.Id)).Should().NotBeNull("the stale restore changed nothing");

        (await SendAsync(asManager, new RestoreChannelCommand(stack.WorkspaceId, channel.Id, row.Version))).Succeeded.Should().BeTrue();
        (await DeletedAtAsync(channel.Id)).Should().BeNull();

        var back = await SendAsync(asManager, new GetChannelQuery(stack.WorkspaceId, channel.Id));
        back.Data!.Members.Select(m => m.UserId).Should().BeEquivalentTo([manager, member]);
        (await SendAsync(asManager, new ListDeletedChannelsQuery(stack.WorkspaceId))).Data.Should().BeEmpty();
    }

    [Fact]
    public async Task APersonWhoCouldNotHaveDeletedTheChannel_SeesNothing_AndRestoreIsRefused_SoAPrivateNameNeverLeaks()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var manager = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Manager");
        var plain = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Plain");
        var publicChannel = await SeedChannelAsync(stack, manager, SpaceKind.ChannelPublic, $"pub-{Guid.NewGuid():N}"[..12], plain);
        var privateChannel = await SeedChannelAsync(stack, manager, SpaceKind.ChannelPrivate, $"sec-{Guid.NewGuid():N}"[..12]);

        using var asManager = ProviderFor(stack, manager);
        foreach (var channel in new[] { publicChannel, privateChannel })
            (await SendAsync(asManager, new DeleteChannelCommand(stack.WorkspaceId, channel.Id, await VersionAsync(channel.Id)))).Succeeded.Should().BeTrue();

        using var asPlain = ProviderFor(stack, plain);
        (await SendAsync(asPlain, new ListDeletedChannelsQuery(stack.WorkspaceId))).Data.Should().BeEmpty("a plain member could not have deleted either channel");
        foreach (var channel in new[] { publicChannel, privateChannel })
        {
            var version = await VersionAsync(channel.Id);
            await FluentActions.Awaiting(() => SendAsync(asPlain, new RestoreChannelCommand(stack.WorkspaceId, channel.Id, version)))
                .Should().ThrowAsync<Exception>();
            (await DeletedAtAsync(channel.Id)).Should().NotBeNull();
        }

        // The Workspace Owner may restore the public channel; the private one is member-only, so the Owner never sees its name.
        using var asOwner = ProviderFor(stack, stack.OwnerId);
        var ownerList = await SendAsync(asOwner, new ListDeletedChannelsQuery(stack.WorkspaceId));
        ownerList.Data!.Select(d => d.Id).Should().Contain(publicChannel.Id).And.NotContain(privateChannel.Id);
    }

    [Fact]
    public async Task ANameTakenMeanwhile_IsNameConflict_AndPastThirtyDaysIsRestoreWindowElapsed()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var name = $"dup-{Guid.NewGuid():N}"[..12];
        var channel = await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPublic, name);

        using var asOwner = ProviderFor(stack, stack.OwnerId);
        (await SendAsync(asOwner, new DeleteChannelCommand(stack.WorkspaceId, channel.Id, await VersionAsync(channel.Id)))).Succeeded.Should().BeTrue();
        await SeedChannelAsync(stack, stack.OwnerId, SpaceKind.ChannelPublic, name);

        var version = await VersionAsync(channel.Id);
        var conflict = await FluentActions.Awaiting(() => SendAsync(asOwner, new RestoreChannelCommand(stack.WorkspaceId, channel.Id, version)))
            .Should().ThrowAsync<ConflictException>();
        conflict.Which.ErrorCode.Should().Be(ChatRuleCodes.NameConflict);
        (await DeletedAtAsync(channel.Id)).Should().NotBeNull();

        _host.Now = OrgTreeTestHost.BaseTime.Add(Space.RestoreWindow).AddDays(1);
        try
        {
            var elapsed = await FluentActions.Awaiting(() => SendAsync(asOwner, new RestoreChannelCommand(stack.WorkspaceId, channel.Id, version)))
                .Should().ThrowAsync<ConflictException>();
            elapsed.Which.ErrorCode.Should().Be(ChatRuleCodes.RestoreWindowElapsed);
            (await SendAsync(asOwner, new ListDeletedChannelsQuery(stack.WorkspaceId))).Data.Should().BeEmpty("the window is closed, the row is not listed");
        }
        finally
        {
            _host.Now = OrgTreeTestHost.BaseTime;
        }
    }
}
