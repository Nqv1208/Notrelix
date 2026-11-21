using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Application.Features.Workspaces.Navigation.Commands.PinResource;
using Notrelix.Application.Features.Workspaces.Navigation.Commands.RecordRecentResource;
using Notrelix.Application.Features.Workspaces.Navigation.Commands.UnpinResource;
using Notrelix.Application.Features.Workspaces.Navigation.Queries.GetPinnedResources;
using Notrelix.Application.Features.Workspaces.Navigation.Queries.GetRecentResources;
using Notrelix.Application.Features.Workspaces.Navigation.Services;
using Notrelix.Application.Features.Workspaces.Search.Queries.CommandSearch;
using Notrelix.Application.Features.Workspaces.Settings.Commands.UpdateWorkspaceSettings;
using Notrelix.Application.Features.Workspaces.Settings.Queries.GetWorkspaceSecurityExt;
using Notrelix.Application.Features.Workspaces.Settings.Queries.GetWorkspaceSettings;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.ArchiveWorkspace;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Navigation;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Workspaces.Members;
using Notrelix.Infrastructure.Workspaces.Navigation;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// An archived workspace on PostgreSQL through the real pipeline: its settings are still read by every member and the
/// sharing extras by the roles that read them, a settings write is refused with the archived code and changes nothing,
/// and the per-person state (recents, pins) and the command search keep working, because they are not changes to the
/// workspace.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ArchivedWorkspaceSettingsAndNavigationIntegrationTests : IAsyncLifetime
{
    private const string ArchivedCode = "workspace.archived";

    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public ArchivedWorkspaceSettingsAndNavigationIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static void Register(IServiceCollection services)
    {
        services.AddSingleton(new Moq.Mock<Notrelix.Application.Common.Realtime.IAccessChangeSignal>().Object);
        services.AddScoped<AccessGrantProjectionService>();
        services.AddScoped<IWorkspaceGrantProjectionService, WorkspaceGrantProjectionServiceAdapter>();
        services.AddScoped<IWorkspaceOwnerUpdateLocker, WorkspaceOwnerUpdateLocker>();
        services.AddScoped<IWorkspaceNavigationStore>(sp => new WorkspaceNavigationStore(sp.GetRequiredService<ApplicationDbContext>()));
    }

    private ServiceProvider As(OrgStack stack, Guid userId) => _host.CreateProvider(stack.AccountId, userId, Register);

    private Task<long> VersionAsync(OrgStack stack) =>
        _host.ReadAsync(c => c.Workspaces.Where(w => w.Id == stack.WorkspaceId).Select(w => w.Version).SingleAsync());

    private async Task ArchiveAsync(OrgStack stack)
    {
        using var asOwner = As(stack, stack.OwnerId);
        await OrgTreeTestHost.SendAsync(asOwner, new ArchiveWorkspaceCommand(stack.WorkspaceId, await VersionAsync(stack)));
    }

    [Fact]
    public async Task ASettingsWrite_IsRefusedWithTheArchivedCode_AndChangesNothing_WhileEveryMemberStillReadsTheSettings()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        await ArchiveAsync(stack);
        var version = await VersionAsync(stack);
        using var asOwner = As(stack, stack.OwnerId);

        var write = () => OrgTreeTestHost.SendAsync(asOwner, new UpdateWorkspaceSettingsCommand(stack.WorkspaceId, true, true, "Member", 14, version));

        (await write.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().Be(ArchivedCode);
        (await VersionAsync(stack)).Should().Be(version, "a refused write does not move the version");
        foreach (var userId in new[] { stack.OwnerId, member, guest })
        {
            using var provider = As(stack, userId);
            (await OrgTreeTestHost.QueryAsync(provider, new GetWorkspaceSettingsQuery(stack.WorkspaceId)))
                .AllowPublicSharing.Should().BeFalse("the archived workspace's settings read as they were");
        }
        (await OrgTreeTestHost.QueryAsync(asOwner, new GetWorkspaceSecurityExtQuery(stack.WorkspaceId))).GuestCount.Should().Be(1);
    }

    [Fact]
    public async Task RecentsPinsAndCommandSearch_KeepWorking_InAnArchivedWorkspace()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var board = await _host.SeedBoardAsync(stack, "Archived Roadmap");
        await ArchiveAsync(stack);
        using var asMember = As(stack, member);

        await OrgTreeTestHost.SendIdempotentAsync(asMember, new RecordRecentResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, board));
        await OrgTreeTestHost.SendAsync(asMember, new PinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, board));

        (await OrgTreeTestHost.QueryAsync(asMember, new GetRecentResourcesQuery(stack.WorkspaceId))).Select(r => r.ResourceId).Should().Equal(board);
        (await OrgTreeTestHost.QueryAsync(asMember, new GetPinnedResourcesQuery(stack.WorkspaceId))).Select(p => p.ResourceId).Should().Equal(board);
        (await OrgTreeTestHost.QueryAsync(asMember, new CommandSearchQuery(stack.WorkspaceId, "archived")))
            .Boards.Select(b => b.Id).Should().Equal(board);

        await OrgTreeTestHost.SendAsync(asMember, new UnpinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, board));
        (await OrgTreeTestHost.QueryAsync(asMember, new GetPinnedResourcesQuery(stack.WorkspaceId))).Should().BeEmpty();
    }
}
