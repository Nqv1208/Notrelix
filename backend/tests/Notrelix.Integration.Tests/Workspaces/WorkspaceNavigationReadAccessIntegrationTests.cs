using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Workspaces.Navigation.Commands.PinResource;
using Notrelix.Application.Features.Workspaces.Navigation.Commands.RecordRecentResource;
using Notrelix.Application.Features.Workspaces.Navigation.Queries.GetPinnedResources;
using Notrelix.Application.Features.Workspaces.Navigation.Queries.GetRecentResources;
using Notrelix.Application.Features.Workspaces.Navigation.Services;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Navigation;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Workspaces.Navigation;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using Notrelix.Domain.WorkManagement.Boards;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// Recents and pins only ever point at a Board or Doc the person can read, on PostgreSQL through the real pipeline: a
/// member who opens or pins a Private Board outside their reach is told it does not exist (the answer for a made-up
/// id), nothing is stored, and a Guest keeps recents and pins only for what was shared with them.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkspaceNavigationReadAccessIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public WorkspaceNavigationReadAccessIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private Microsoft.Extensions.DependencyInjection.ServiceProvider Provider(OrgStack stack, Guid userId) =>
        _host.CreateProvider(stack.AccountId, userId, services =>
            services.AddScoped<IWorkspaceNavigationStore>(sp => new WorkspaceNavigationStore(sp.GetRequiredService<ApplicationDbContext>())));

    private Task<int> RowsAsync() => _host.ReadAsync(async c => await c.RecentResources.CountAsync() + await c.PinnedResources.CountAsync());

    [Fact]
    public async Task APrivateBoardTheMemberCannotRead_CannotBeRecordedOrPinned_AndNothingIsStored()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var secret = await _host.SeedBoardAsync(stack, "Secret", BoardVisibility.Private);
        using var asMember = Provider(stack, member);

        var record = () => OrgTreeTestHost.SendIdempotentAsync(asMember, new RecordRecentResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, secret));
        var pin = () => OrgTreeTestHost.SendAsync(asMember, new PinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, secret));

        await record.Should().ThrowAsync<AppNotFound>("a Board the member cannot read answers like a made-up id");
        await pin.Should().ThrowAsync<AppNotFound>();
        (await RowsAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AGuestRecordsAndPinsOnlyABoardSharedWithThem()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var shared = await _host.SeedBoardAsync(stack, "Shared");
        var unshared = await _host.SeedBoardAsync(stack, "Unshared");
        await _host.SeedBoardMemberAsync(shared, guest, BoardRole.Guest);
        using var asGuest = Provider(stack, guest);

        var recordUnshared = () => OrgTreeTestHost.SendIdempotentAsync(asGuest, new RecordRecentResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, unshared));
        await recordUnshared.Should().ThrowAsync<AppNotFound>("a Guest cannot read a Board nobody shared with them");

        await OrgTreeTestHost.SendIdempotentAsync(asGuest, new RecordRecentResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, shared));
        await OrgTreeTestHost.SendAsync(asGuest, new PinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, shared));

        var recents = await OrgTreeTestHost.QueryAsync(asGuest, new GetRecentResourcesQuery(stack.WorkspaceId));
        var pins = await OrgTreeTestHost.QueryAsync(asGuest, new GetPinnedResourcesQuery(stack.WorkspaceId));
        recents.Select(r => r.ResourceId).Should().Equal(shared);
        pins.Select(p => p.ResourceId).Should().Equal(shared);
    }

    [Fact]
    public async Task APinOrRecentForABoardWhoseSharingIsWithdrawn_LeavesBothListsAtOnce()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var shared = await _host.SeedBoardAsync(stack, "Shared");
        await _host.SeedBoardMemberAsync(shared, guest, BoardRole.Guest);
        using var asGuest = Provider(stack, guest);
        await OrgTreeTestHost.SendIdempotentAsync(asGuest, new RecordRecentResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, shared));
        await OrgTreeTestHost.SendAsync(asGuest, new PinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, shared));

        await _host.WithSystemContextAsync(c => c.BoardMembers.Where(m => m.BoardId == shared && m.UserId == guest).ExecuteDeleteAsync());

        (await OrgTreeTestHost.QueryAsync(asGuest, new GetRecentResourcesQuery(stack.WorkspaceId))).Should().BeEmpty();
        (await OrgTreeTestHost.QueryAsync(asGuest, new GetPinnedResourcesQuery(stack.WorkspaceId))).Should().BeEmpty();
    }
}
