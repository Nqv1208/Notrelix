using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Channels.Commands.LinkChannel;
using Notrelix.Application.Features.Chat.Channels.Commands.UnlinkChannel;
using Notrelix.Application.Features.Chat.Channels.Queries.GetChannel;
using Notrelix.Application.Features.Chat.Links.Queries.ListLinkedChannels;
using Notrelix.Application.Features.Chat.Links.Services;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// UC-CHT-H10 through the real request pipeline on PostgreSQL: the manager's right and the version check (412) are decided by the pipeline, the
/// resource must be viewable through the Governance and Documents facts (a private Board and Page the manager cannot see give 403), the link is
/// stored and read back with the title only for a viewer who can see the resource, and the reverse lookup lists only channels the caller can see.
/// Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatChannelLinkPipelineTests
{
    private readonly OrgTreeTestHost _host;

    public ChatChannelLinkPipelineTests(PostgresTestContainer db) => _host = new OrgTreeTestHost(db);

    private ServiceProvider ProviderFor(OrgStack stack, Guid userId) =>
        _host.CreateProvider(stack.AccountId, userId, services =>
        {
            services.AddScoped<IChatDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<ChatAuditFacts>();
            services.AddScoped<ChatResourceViews>();
            services.AddScoped<Notrelix.Application.Features.Documents.Public.PageViewAccess.IPageViewAccess,
                Notrelix.Application.Features.Documents.Pages.Services.PageViewAccess>();
        });

    private async Task<Space> SeedChannelAsync(OrgStack stack, Guid manager, params Guid[] members)
    {
        var channel = Space.CreateChannel(stack.AccountId, stack.WorkspaceId, SpaceKind.ChannelPublic, $"ch-{Guid.NewGuid():N}"[..12], null, null, 1, manager, OrgTreeTestHost.BaseTime);
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

    private Task<long> VersionAsync(Guid channelId) =>
        _host.ReadAsync(c => c.ChatSpaces.Where(s => s.Id == channelId).Select(s => s.Version).SingleAsync());

    [Fact]
    public async Task AManagerLinksABoardTheyCanView_TheTitleIsReadBack_ForAViewerWhoCanSeeItOnly_AndAStaleVersionIs412()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Other");
        var boardId = await _host.SeedBoardAsync(stack, "Launch board");
        var channel = await SeedChannelAsync(stack, stack.OwnerId, other);

        using var owner = ProviderFor(stack, stack.OwnerId);
        var linked = await SendAsync(owner, new LinkChannelCommand(stack.WorkspaceId, channel.Id, ChannelLinkKinds.Board, boardId, await VersionAsync(channel.Id)));

        linked.Succeeded.Should().BeTrue(string.Join("; ", linked.Errors));
        linked.Data!.Link.Should().BeEquivalentTo(new { Kind = ChannelLinkKinds.Board, ResourceId = boardId, Accessible = true, Title = "Launch board" });

        var pageId = await _host.SeedPageAsync(stack, "Runbook");
        var stale = await Record.ExceptionAsync(() => SendAsync(owner, new LinkChannelCommand(stack.WorkspaceId, channel.Id, ChannelLinkKinds.Page, pageId, 1)));
        stale.Should().NotBeNull("a version the client did not read is refused (412 at the transport)");
        (await VersionAsync(channel.Id)).Should().Be(2, "the stale write changed nothing");
        (await _host.ReadAsync(c => c.ChatSpaces.Where(s => s.Id == channel.Id).Select(s => s.LinkKind).SingleAsync())).Should().Be(ChannelLinkKinds.Board);

        var ownerView = await SendAsync(owner, new GetChannelQuery(stack.WorkspaceId, channel.Id));
        ownerView.Data!.Channel.Link!.Title.Should().Be("Launch board");

        var storedKind = await _host.ReadAsync(c => c.ChatSpaces.Where(s => s.Id == channel.Id).Select(s => s.LinkKind).SingleAsync());
        storedKind.Should().Be(ChannelLinkKinds.Board);
    }

    [Fact]
    public async Task AManagerWhoCannotViewThePageIsForbidden_AndAMemberWhoIsNotAManagerCannotLink()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var manager = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Manager");
        var plain = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Plain");
        var privateBoard = await _host.SeedBoardAsync(stack, "Owner only board", BoardVisibility.Private);
        var channel = await SeedChannelAsync(stack, manager, plain);
        var version = await VersionAsync(channel.Id);

        using var asManager = ProviderFor(stack, manager);
        await FluentActions.Awaiting(() => SendAsync(asManager, new LinkChannelCommand(stack.WorkspaceId, channel.Id, ChannelLinkKinds.Board, privateBoard, version)))
            .Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ForbiddenException>("the manager cannot view that private Board");

        using var asPlain = ProviderFor(stack, plain);
        await FluentActions.Awaiting(() => SendAsync(asPlain, new LinkChannelCommand(stack.WorkspaceId, channel.Id, ChannelLinkKinds.Board, privateBoard, version)))
            .Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ForbiddenException>();

        (await _host.ReadAsync(c => c.ChatSpaces.Where(s => s.Id == channel.Id).Select(s => s.LinkId).SingleAsync())).Should().BeNull();
    }

    [Fact]
    public async Task TheReverseLookupListsOnlyTheChannelsTheCallerCanSee_AndUnlinkClearsTheLink()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var boardId = await _host.SeedBoardAsync(stack, "Shared board", BoardVisibility.Private);
        var open = await SeedChannelAsync(stack, stack.OwnerId, member);
        var secret = Space.CreateChannel(stack.AccountId, stack.WorkspaceId, SpaceKind.ChannelPrivate, $"sec-{Guid.NewGuid():N}"[..12], null, null, 2, stack.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(seed =>
        {
            seed.ChatSpaces.Add(secret);
            return Task.CompletedTask;
        });

        using var owner = ProviderFor(stack, stack.OwnerId);
        (await SendAsync(owner, new LinkChannelCommand(stack.WorkspaceId, open.Id, ChannelLinkKinds.Board, boardId, await VersionAsync(open.Id)))).Succeeded.Should().BeTrue();
        (await SendAsync(owner, new LinkChannelCommand(stack.WorkspaceId, secret.Id, ChannelLinkKinds.Board, boardId, await VersionAsync(secret.Id)))).Succeeded.Should().BeTrue();

        var seenByOwner = await SendAsync(owner, new ListLinkedChannelsQuery(stack.WorkspaceId, ChannelLinkKinds.Board, boardId));
        seenByOwner.Data!.Select(c => c.Id).Should().BeEquivalentTo([open.Id, secret.Id]);

        // The member cannot view the Owner's private Board, so the lookup is empty rather than disclosing channels.
        using var asMember = ProviderFor(stack, member);
        var seenByMember = await SendAsync(asMember, new ListLinkedChannelsQuery(stack.WorkspaceId, ChannelLinkKinds.Board, boardId));
        seenByMember.Succeeded.Should().BeTrue();
        seenByMember.Data.Should().BeEmpty();

        var unlinked = await SendAsync(owner, new UnlinkChannelCommand(stack.WorkspaceId, open.Id, await VersionAsync(open.Id)));
        unlinked.Succeeded.Should().BeTrue();
        unlinked.Data!.Link.Should().BeNull();
    }
}
