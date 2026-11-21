using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Documents.Pages.Commands.CreatePage;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace;
using Notrelix.Application.Features.Workspaces.Members.Commands.LeaveWorkspace;
using Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMyJobTitle;
using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.ArchiveWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.UnarchiveWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetWorkspace;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// An archived workspace is read-only for every member (product workspaces.md, archive a workspace), through the
/// real pipeline and the real policy engine on PostgreSQL: a write inside it is refused with
/// <c>workspace.archived</c> and changes nothing, whatever the person's role; a role that is already refused keeps the
/// plain refusal; reads stay open to members and a stranger gets the identical NotFound; the lifecycle (archive again,
/// unarchive, leave) still works, and unarchiving makes the same write succeed again.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ArchivedWorkspaceWriteGateIntegrationTests : IAsyncLifetime
{
    private const string ArchivedCode = "workspace.archived";

    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public ArchivedWorkspaceWriteGateIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton(new Moq.Mock<Notrelix.Application.Common.Realtime.IAccessChangeSignal>().Object);
        services.AddScoped<AccessGrantProjectionService>();
        services.AddScoped<IWorkspaceGrantProjectionService, WorkspaceGrantProjectionServiceAdapter>();
        services.AddScoped<IWorkspaceOwnerUpdateLocker, WorkspaceOwnerUpdateLocker>();
        services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();
    }

    private ServiceProvider As(OrgStack stack, Guid userId) => _host.CreateProvider(stack.AccountId, userId, RegisterServices);

    private async Task<(OrgStack Stack, Dictionary<string, Guid> People)> SeedAsync()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var people = new Dictionary<string, Guid>
        {
            ["Owner"] = stack.OwnerId,
            ["Admin"] = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin"),
            ["Member"] = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member"),
            ["Guest"] = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest"),
        };
        return (stack, people);
    }

    private Task<long> VersionAsync(OrgStack stack) =>
        _host.ReadAsync(c => c.Workspaces.Where(w => w.Id == stack.WorkspaceId).Select(w => w.Version).SingleAsync());

    private Task<WorkspaceStatus> StatusAsync(OrgStack stack) =>
        _host.ReadAsync(c => c.Workspaces.Where(w => w.Id == stack.WorkspaceId).Select(w => w.Status).SingleAsync());

    private Task<(string? JobTitle, long Version)> MemberRowAsync(OrgStack stack, Guid userId) =>
        _host.ReadAsync(async c =>
        {
            var row = await c.WorkspaceMembers.Where(m => m.WorkspaceId == stack.WorkspaceId && m.UserId == userId)
                .Select(m => new { m.JobTitle, m.Version }).SingleAsync();
            return (row.JobTitle, row.Version);
        });

    private Task<int> BoardCountAsync(OrgStack stack) =>
        _host.ReadAsync(c => c.Boards.CountAsync(b => b.WorkspaceId == stack.WorkspaceId));

    private Task<int> PageCountAsync(OrgStack stack) =>
        _host.ReadAsync(c => c.Pages.CountAsync(p => p.WorkspaceId == stack.WorkspaceId));

    private async Task ArchiveAsync(OrgStack stack, Guid by)
    {
        using var provider = As(stack, by);
        await OrgTreeTestHost.SendAsync(provider, new ArchiveWorkspaceCommand(stack.WorkspaceId, await VersionAsync(stack)));
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("Admin")]
    [InlineData("Member")]
    [InlineData("Guest")]
    public async Task AWriteInAnArchivedWorkspace_IsRefusedWithTheArchivedCode_ForEveryRole_AndChangesNothing(string role)
    {
        var (stack, people) = await SeedAsync();
        await ArchiveAsync(stack, stack.OwnerId);
        using var asPerson = As(stack, people[role]);
        var before = await MemberRowAsync(stack, people[role]);
        var workspaceVersion = await VersionAsync(stack);

        // Every role may set its own job title in an active workspace; once archived the same write is refused.
        var title = () => OrgTreeTestHost.SendAsync(asPerson, new UpdateMyJobTitleCommand(stack.WorkspaceId, "Archived edit", before.Version));

        (await title.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().Be(ArchivedCode);
        (await MemberRowAsync(stack, people[role])).Should().Be(before, "a refused write changes nothing");
        (await VersionAsync(stack)).Should().Be(workspaceVersion);
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("Admin")]
    [InlineData("Member")]
    public async Task ABoardAndAPageCannotBeCreated_InAnArchivedWorkspace(string role)
    {
        var (stack, people) = await SeedAsync();
        await ArchiveAsync(stack, stack.OwnerId);
        using var asPerson = As(stack, people[role]);

        var board = () => OrgTreeTestHost.SendIdempotentAsync(
            asPerson, new CreateBoardInWorkspaceCommand(stack.WorkspaceId, $"{role} board", null, null, null));
        var page = () => OrgTreeTestHost.SendAsync(asPerson, new CreatePageCommand(stack.WorkspaceId, $"{role} page", null, null));

        (await board.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().Be(ArchivedCode);
        (await page.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().Be(ArchivedCode);
        (await BoardCountAsync(stack)).Should().Be(0);
        (await PageCountAsync(stack)).Should().Be(0);
    }

    [Fact]
    public async Task AGuest_WhoCannotCreateABoardAnyway_KeepsThePlainRefusal_InAnArchivedWorkspace()
    {
        var (stack, people) = await SeedAsync();
        await ArchiveAsync(stack, stack.OwnerId);
        using var asGuest = As(stack, people["Guest"]);

        var board = () => OrgTreeTestHost.SendIdempotentAsync(
            asGuest, new CreateBoardInWorkspaceCommand(stack.WorkspaceId, "Guest board", null, null, null));

        (await board.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().BeNull("the role refusal comes first");
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("Admin")]
    [InlineData("Member")]
    [InlineData("Guest")]
    public async Task AnArchivedWorkspace_StaysReadable_ForEveryMember(string role)
    {
        var (stack, people) = await SeedAsync();
        await ArchiveAsync(stack, stack.OwnerId);
        using var asPerson = As(stack, people[role]);

        var workspace = await OrgTreeTestHost.QueryAsync(asPerson, new GetWorkspaceQuery(stack.WorkspaceId));

        workspace.IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task AStranger_GetsTheIdenticalNotFound_ForAReadAndForAWrite_AndNeverLearnsItIsArchived()
    {
        var (stack, _) = await SeedAsync();
        await ArchiveAsync(stack, stack.OwnerId);
        var outsider = await _host.AddOutsiderAsync(stack);
        using var asOutsider = As(stack, outsider);

        var read = () => OrgTreeTestHost.SendAsync(asOutsider, new GetWorkspaceQuery(stack.WorkspaceId));
        var write = () => OrgTreeTestHost.SendAsync(asOutsider, new UpdateMyJobTitleCommand(stack.WorkspaceId, "Hi", 1));

        await read.Should().ThrowAsync<AppNotFound>();
        await write.Should().ThrowAsync<AppNotFound>();
    }

    [Fact]
    public async Task AnAdmin_CannotUnarchive_SoTheWorkspaceStaysArchived()
    {
        var (stack, people) = await SeedAsync();
        await ArchiveAsync(stack, stack.OwnerId);
        using var asAdmin = As(stack, people["Admin"]);
        var refused = async () => await OrgTreeTestHost.SendAsync(asAdmin, new UnarchiveWorkspaceCommand(stack.WorkspaceId, await VersionAsync(stack)));
        await refused.Should().ThrowAsync<AppForbidden>();
        (await StatusAsync(stack)).Should().Be(WorkspaceStatus.Archived);
    }

    [Theory]
    [InlineData("Member")]
    [InlineData("Guest")]
    public async Task AMemberOrGuest_CannotUnarchive_SoTheWorkspaceStaysArchivedAndUnchanged(string role)
    {
        var (stack, people) = await SeedAsync();
        await ArchiveAsync(stack, stack.OwnerId);
        var version = await VersionAsync(stack);
        using var asPerson = As(stack, people[role]);

        var refused = async () => await OrgTreeTestHost.SendAsync(asPerson, new UnarchiveWorkspaceCommand(stack.WorkspaceId, version));

        await refused.Should().ThrowAsync<AppForbidden>();
        (await StatusAsync(stack)).Should().Be(WorkspaceStatus.Archived);
        (await VersionAsync(stack)).Should().Be(version);
    }

    [Fact]
    public async Task AStaleVersion_RefusesTheUnarchive_AndTheWorkspaceStaysArchived()
    {
        var (stack, _) = await SeedAsync();
        await ArchiveAsync(stack, stack.OwnerId);
        var version = await VersionAsync(stack);
        using var asOwner = As(stack, stack.OwnerId);

        var stale = async () => await OrgTreeTestHost.SendAsync(asOwner, new UnarchiveWorkspaceCommand(stack.WorkspaceId, version - 1));

        await stale.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.PreconditionFailedException>();
        (await StatusAsync(stack)).Should().Be(WorkspaceStatus.Archived);
        (await VersionAsync(stack)).Should().Be(version);
    }

    [Fact]
    public async Task UnarchivingAnActiveWorkspace_IsASuccessfulNoOp_AndTheVersionIsUnchanged()
    {
        var (stack, _) = await SeedAsync();
        var version = await VersionAsync(stack);
        using var asOwner = As(stack, stack.OwnerId);

        await OrgTreeTestHost.SendAsync(asOwner, new UnarchiveWorkspaceCommand(stack.WorkspaceId, version));

        (await StatusAsync(stack)).Should().Be(WorkspaceStatus.Active);
        (await VersionAsync(stack)).Should().Be(version);
    }

    [Fact]
    public async Task Unarchiving_LetsTheSameWriteSucceedAgain()
    {
        const string unarchiver = "Owner";
        var (stack, people) = await SeedAsync();
        await ArchiveAsync(stack, stack.OwnerId);
        using var asMember = As(stack, people["Member"]);
        var before = await MemberRowAsync(stack, people["Member"]);
        var refused = () => OrgTreeTestHost.SendAsync(asMember, new UpdateMyJobTitleCommand(stack.WorkspaceId, "Engineer", before.Version));
        (await refused.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().Be(ArchivedCode);

        using (var asUnarchiver = As(stack, people[unarchiver]))
        {
            await OrgTreeTestHost.SendAsync(asUnarchiver, new UnarchiveWorkspaceCommand(stack.WorkspaceId, await VersionAsync(stack)));
        }

        (await StatusAsync(stack)).Should().Be(WorkspaceStatus.Active);
        await OrgTreeTestHost.SendAsync(asMember, new UpdateMyJobTitleCommand(stack.WorkspaceId, "Engineer", before.Version));
        (await MemberRowAsync(stack, people["Member"])).JobTitle.Should().Be("Engineer");
    }

    [Fact]
    public async Task ASecondArchive_IsASuccessfulNoOp_AndTheVersionIsUnchanged()
    {
        var (stack, _) = await SeedAsync();
        await ArchiveAsync(stack, stack.OwnerId);
        var version = await VersionAsync(stack);

        await ArchiveAsync(stack, stack.OwnerId);

        (await StatusAsync(stack)).Should().Be(WorkspaceStatus.Archived);
        (await VersionAsync(stack)).Should().Be(version);
    }

    [Fact]
    public async Task AMember_CanStillLeave_AnArchivedWorkspace()
    {
        var (stack, people) = await SeedAsync();
        await ArchiveAsync(stack, stack.OwnerId);
        using var asMember = As(stack, people["Member"]);

        await OrgTreeTestHost.SendAsync(asMember, new LeaveWorkspaceCommand(stack.WorkspaceId));

        var row = await _host.ReadAsync(c => c.WorkspaceMembers
            .Where(m => m.WorkspaceId == stack.WorkspaceId && m.UserId == people["Member"]).Select(m => m.Status).SingleAsync());
        row.Should().Be(WorkspaceMemberStatus.Removed);
    }
}
