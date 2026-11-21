using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.ChannelGroups.Commands.CreateChannelGroup;
using Notrelix.Application.Features.Chat.ChannelGroups.Commands.DeleteChannelGroup;
using Notrelix.Application.Features.Chat.ChannelGroups.Commands.UpdateChannelGroup;
using Notrelix.Application.Features.Chat.ChannelGroups.Queries.ListChannelGroups;
using Notrelix.Application.Features.Chat.Channels.Commands.ReorderChannels;
using Notrelix.Application.Features.Chat.Channels.DTOs;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// UC-CHT-A02 and A04 through the real request pipeline on PostgreSQL: an Owner or Admin creates, renames, moves and deletes categories; a
/// Member and a Guest are refused (403) and nothing is stored; a person outside the Workspace and a person of another Workspace get the same
/// NotFound; a stale version is refused and changes nothing; deleting a category leaves its channels in place; a batch reorder is refused whole
/// for a channel the caller does not manage, a stale channel, or a channel or category of another Workspace. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatChannelGroupPipelineTests
{
    private readonly OrgTreeTestHost _host;

    public ChatChannelGroupPipelineTests(PostgresTestContainer db) => _host = new OrgTreeTestHost(db);

    private ServiceProvider ProviderFor(OrgStack stack, Guid userId) =>
        _host.CreateProvider(stack.AccountId, userId, services =>
            services.AddScoped<IChatDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>()));

    private static async Task<Result<T>> SendAsync<T>(ServiceProvider provider, IRequest<Result<T>> request)
    {
        using var scope = provider.CreateScope();
        // Create is idempotent: the transport binds a key before it dispatches.
        scope.ServiceProvider.GetRequiredService<IIdempotencyExecutionContextWriter>()
            .Set($"chat-group-{Guid.NewGuid():N}", IdempotencyExecutionSource.Internal);
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    private static async Task<Result> SendAsync(ServiceProvider provider, IRequest<Result> request)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    private async Task<Space> SeedChannelAsync(OrgStack stack, Guid manager, string name, Guid? groupId = null)
    {
        var channel = Space.CreateChannel(stack.AccountId, stack.WorkspaceId, SpaceKind.ChannelPublic, name, null, groupId, 1, manager, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(seed =>
        {
            seed.ChatSpaces.Add(channel);
            return Task.CompletedTask;
        });
        return channel;
    }

    private Task<ChannelGroup> GroupAsync(Guid id) =>
        _host.ReadAsync(c => c.ChatChannelGroups.Where(g => g.Id == id).SingleAsync());

    private Task<Space> ChannelAsync(Guid id) =>
        _host.ReadAsync(c => c.ChatSpaces.IgnoreQueryFilters().Where(s => s.Id == id).SingleAsync());

    private Task<int> GroupCountAsync(Guid workspaceId) =>
        _host.ReadAsync(c => c.ChatChannelGroups.CountAsync(g => g.WorkspaceId == workspaceId));

    private static string Name(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..14];

    [Fact]
    public async Task AnOwnerAndAnAdmin_CreateRenameMoveAndDeleteCategories_AndAStaleVersionIsRefused()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");

        using var asOwner = ProviderFor(stack, stack.OwnerId);
        var created = await SendAsync(asOwner, new CreateChannelGroupCommand(stack.WorkspaceId, Name("grp"), null));
        created.Succeeded.Should().BeTrue();
        var id = created.Data!.Id;

        using var asAdmin = ProviderFor(stack, admin);
        var renamed = await SendAsync(asAdmin, new UpdateChannelGroupCommand(stack.WorkspaceId, id, Name("renamed"), 7, created.Data.Version));
        renamed.Data!.Position.Should().Be(7);

        var stale = await Record.ExceptionAsync(() =>
            SendAsync(asAdmin, new UpdateChannelGroupCommand(stack.WorkspaceId, id, Name("stale"), 9, created.Data.Version)));
        stale.Should().NotBeNull("the version the client read is old");
        (await GroupAsync(id)).Position.Should().Be(7, "the stale write changed nothing");

        var staleDelete = await Record.ExceptionAsync(() => SendAsync(asAdmin, new DeleteChannelGroupCommand(stack.WorkspaceId, id, created.Data.Version)));
        staleDelete.Should().NotBeNull();
        (await GroupCountAsync(stack.WorkspaceId)).Should().Be(1);

        (await SendAsync(asAdmin, new DeleteChannelGroupCommand(stack.WorkspaceId, id, renamed.Data.Version))).Succeeded.Should().BeTrue();
        (await GroupCountAsync(stack.WorkspaceId)).Should().Be(0);
    }

    [Fact]
    public async Task AMemberAndAGuest_AreForbidden_AndNoCategoryIsStoredChangedOrDeleted()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");

        using var asOwner = ProviderFor(stack, stack.OwnerId);
        var group = (await SendAsync(asOwner, new CreateChannelGroupCommand(stack.WorkspaceId, Name("grp"), null))).Data!;

        foreach (var person in new[] { member, guest })
        {
            using var provider = ProviderFor(stack, person);
            await FluentActions.Awaiting(() => SendAsync(provider, new CreateChannelGroupCommand(stack.WorkspaceId, Name("nope"), null)))
                .Should().ThrowAsync<ForbiddenException>();
            await FluentActions.Awaiting(() => SendAsync(provider, new UpdateChannelGroupCommand(stack.WorkspaceId, group.Id, Name("nope"), 5, group.Version)))
                .Should().ThrowAsync<ForbiddenException>();
            await FluentActions.Awaiting(() => SendAsync(provider, new DeleteChannelGroupCommand(stack.WorkspaceId, group.Id, group.Version)))
                .Should().ThrowAsync<ForbiddenException>();
        }

        var stored = await GroupAsync(group.Id);
        (stored.Name, stored.Position, stored.Version).Should().Be((group.Name, group.Position, group.Version));
        (await GroupCountAsync(stack.WorkspaceId)).Should().Be(1);
    }

    [Fact]
    public async Task AnOutsiderAndAPersonOfAnotherWorkspace_GetNotFound_ForEveryCategoryOperation()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();
        var outsider = await _host.AddOutsiderAsync(stack);

        using var asOwner = ProviderFor(stack, stack.OwnerId);
        var group = (await SendAsync(asOwner, new CreateChannelGroupCommand(stack.WorkspaceId, Name("grp"), null))).Data!;

        // The other Workspace's Owner holds the action in their own Workspace, but not in this one.
        foreach (var (provider, label) in new[]
        {
            (ProviderFor(other, other.OwnerId), "another workspace's owner"),
            (ProviderFor(stack, outsider), "an account user outside the workspace"),
        })
        {
            using (provider)
            {
                await FluentActions.Awaiting(() => SendAsync(provider, new ListChannelGroupsQuery(stack.WorkspaceId))).Should().ThrowAsync<NotFoundException>(label);
                await FluentActions.Awaiting(() => SendAsync(provider, new CreateChannelGroupCommand(stack.WorkspaceId, Name("nope"), null))).Should().ThrowAsync<NotFoundException>(label);
                await FluentActions.Awaiting(() => SendAsync(provider, new UpdateChannelGroupCommand(stack.WorkspaceId, group.Id, Name("nope"), 5, group.Version))).Should().ThrowAsync<NotFoundException>(label);
                await FluentActions.Awaiting(() => SendAsync(provider, new DeleteChannelGroupCommand(stack.WorkspaceId, group.Id, group.Version))).Should().ThrowAsync<NotFoundException>(label);
            }
        }

        (await GroupCountAsync(stack.WorkspaceId)).Should().Be(1);
        (await GroupAsync(group.Id)).Name.Should().Be(group.Name);
    }

    [Fact]
    public async Task DeletingACategory_LeavesItsChannelsInPlace_AndTheIdOfAnotherWorkspacesCategoryIsNotFound()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();

        using var asOwner = ProviderFor(stack, stack.OwnerId);
        var group = (await SendAsync(asOwner, new CreateChannelGroupCommand(stack.WorkspaceId, Name("grp"), null))).Data!;
        var channel = await SeedChannelAsync(stack, stack.OwnerId, Name("ch"), group.Id);

        using var asOtherOwner = ProviderFor(other, other.OwnerId);
        await FluentActions.Awaiting(() => SendAsync(asOtherOwner, new DeleteChannelGroupCommand(other.WorkspaceId, group.Id, group.Version)))
            .Should().ThrowAsync<NotFoundException>("a category id of another Workspace names nothing here");
        (await ChannelAsync(channel.Id)).GroupId.Should().Be(group.Id);

        (await SendAsync(asOwner, new DeleteChannelGroupCommand(stack.WorkspaceId, group.Id, group.Version))).Succeeded.Should().BeTrue();
        var kept = await ChannelAsync(channel.Id);
        kept.GroupId.Should().BeNull();
        kept.DeletedAt.Should().BeNull("the channel is not deleted");
    }

    [Fact]
    public async Task Reorder_RefusesTheWholeBatch_ForAChannelTheCallerDoesNotManage_AStaleChannel_OrAnotherWorkspacesChannelOrCategory()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();
        var manager = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Manager");
        var plain = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Plain");

        using var asOwner = ProviderFor(stack, stack.OwnerId);
        var group = (await SendAsync(asOwner, new CreateChannelGroupCommand(stack.WorkspaceId, Name("grp"), null))).Data!;
        var mine = await SeedChannelAsync(stack, manager, Name("mine"));
        var notMine = await SeedChannelAsync(stack, stack.OwnerId, Name("owners"));
        var foreignChannel = await SeedChannelAsync(other, other.OwnerId, Name("foreign"));
        using var asOtherOwner = ProviderFor(other, other.OwnerId);
        var foreignGroup = (await SendAsync(asOtherOwner, new CreateChannelGroupCommand(other.WorkspaceId, Name("fgrp"), null))).Data!;

        using var asManager = ProviderFor(stack, manager);
        (await SendAsync(asManager, new ReorderChannelsCommand(stack.WorkspaceId, [new ChannelPlacement(mine.Id, group.Id, 3, mine.Version)])))
            .Succeeded.Should().BeTrue("a channel manager moves their own channel");
        var moved = await ChannelAsync(mine.Id);
        moved.GroupId.Should().Be(group.Id);

        // A channel the caller does not manage refuses the whole batch.
        await FluentActions.Awaiting(() => SendAsync(asManager, new ReorderChannelsCommand(stack.WorkspaceId,
            [new ChannelPlacement(mine.Id, null, 9, moved.Version), new ChannelPlacement(notMine.Id, null, 9, notMine.Version)])))
            .Should().ThrowAsync<ForbiddenException>();
        (await ChannelAsync(mine.Id)).Position.Should().Be(3);

        // A plain member moves nothing.
        using var asPlain = ProviderFor(stack, plain);
        await FluentActions.Awaiting(() => SendAsync(asPlain, new ReorderChannelsCommand(stack.WorkspaceId, [new ChannelPlacement(mine.Id, null, 8, moved.Version)])))
            .Should().ThrowAsync<ForbiddenException>();

        // A stale channel, and ids of another Workspace, change nothing.
        await FluentActions.Awaiting(() => SendAsync(asOwner, new ReorderChannelsCommand(stack.WorkspaceId, [new ChannelPlacement(notMine.Id, null, 5, notMine.Version + 4)])))
            .Should().ThrowAsync<PreconditionFailedException>();
        await FluentActions.Awaiting(() => SendAsync(asOwner, new ReorderChannelsCommand(stack.WorkspaceId, [new ChannelPlacement(foreignChannel.Id, null, 5, foreignChannel.Version)])))
            .Should().ThrowAsync<NotFoundException>();
        await FluentActions.Awaiting(() => SendAsync(asOwner, new ReorderChannelsCommand(stack.WorkspaceId, [new ChannelPlacement(notMine.Id, foreignGroup.Id, 5, notMine.Version)])))
            .Should().ThrowAsync<NotFoundException>();

        var after = await ChannelAsync(notMine.Id);
        (after.GroupId, after.Position).Should().Be(((Guid?)null, 1d));
        (await ChannelAsync(foreignChannel.Id)).Position.Should().Be(1);
    }
}
