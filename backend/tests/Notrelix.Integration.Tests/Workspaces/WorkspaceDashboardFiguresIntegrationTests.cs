using Notrelix.Application.Features.Workspaces.Workspaces.Commands.ArchiveWorkspace;
using Notrelix.Application.Features.Workspaces.WorkspaceHome.Queries.GetWorkspaceDashboard;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// The workspace figures on PostgreSQL through the real pipeline: Owner and Admin read them (through the policy engine,
/// not a handler role check), a Member and a Guest are forbidden, a person outside the workspace or in another tenant is
/// told it does not exist, only active members are counted (suspended and removed people are not), and an archived
/// workspace still answers because the read changes nothing.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkspaceDashboardFiguresIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public WorkspaceDashboardFiguresIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<WorkspaceDashboardDto> FiguresAsync(OrgStack stack, Guid userId)
    {
        using var provider = _host.CreateProvider(stack.AccountId, userId);
        return await OrgTreeTestHost.QueryAsync(provider, new GetWorkspaceDashboardQuery(stack.WorkspaceId));
    }

    [Fact]
    public async Task OwnerAndAdmin_ReadTheFigures()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");

        foreach (var reader in new[] { stack.OwnerId, admin })
        {
            var figures = await FiguresAsync(stack, reader);
            figures.WorkspaceId.Should().Be(stack.WorkspaceId);
            figures.MemberCount.Should().Be(3);
            figures.IsArchived.Should().BeFalse();
        }
    }

    [Fact]
    public async Task MemberAndGuest_AreForbidden_AnOutsiderAndAnotherTenantAreNotFound()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var outsider = await _host.AddOutsiderAsync(stack);
        var other = await _host.SeedWorkspaceAsync();

        foreach (var reader in new[] { member, guest })
        {
            var read = () => FiguresAsync(stack, reader);
            await read.Should().ThrowAsync<AppForbidden>("the figures are for Owner and Admin");
        }

        var asOutsider = () => FiguresAsync(stack, outsider);
        await asOutsider.Should().ThrowAsync<AppNotFound>();

        using var asOtherOwner = _host.CreateProvider(other.AccountId, other.OwnerId);
        var foreign = () => OrgTreeTestHost.QueryAsync(asOtherOwner, new GetWorkspaceDashboardQuery(stack.WorkspaceId));
        await foreign.Should().ThrowAsync<AppNotFound>("another tenant's workspace answers like a made-up id");
    }

    [Fact]
    public async Task SuspendedAndRemovedMembers_AreNotCounted()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var suspended = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Suspended");
        var removed = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Removed");
        await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Active");
        await _host.WithSystemContextAsync(async seed =>
        {
            var members = await seed.WorkspaceMembers.Where(m => m.WorkspaceId == stack.WorkspaceId).ToListAsync();
            members.Single(m => m.UserId == suspended).Suspend(stack.OwnerId, OrgTreeTestHost.BaseTime, 1);
            members.Single(m => m.UserId == removed).Remove(1, stack.OwnerId, OrgTreeTestHost.BaseTime);
        });

        var figures = await FiguresAsync(stack, stack.OwnerId);

        figures.MemberCount.Should().Be(2, "the Owner and the one active member; suspended and removed people are not counted");
    }

    [Fact]
    public async Task AnArchivedWorkspace_StillAnswers_AndSaysItIsArchived()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var version = await _host.ReadAsync(c => c.Workspaces.Where(w => w.Id == stack.WorkspaceId).Select(w => w.Version).SingleAsync());
        await OrgTreeTestHost.SendAsync(asOwner, new ArchiveWorkspaceCommand(stack.WorkspaceId, version));

        var figures = await OrgTreeTestHost.QueryAsync(asOwner, new GetWorkspaceDashboardQuery(stack.WorkspaceId));

        figures.IsArchived.Should().BeTrue();
        figures.MemberCount.Should().Be(1);
    }
}
