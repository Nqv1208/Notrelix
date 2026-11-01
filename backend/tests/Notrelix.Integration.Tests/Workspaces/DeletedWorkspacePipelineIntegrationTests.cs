using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoards;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.DeleteWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.RestoreWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetWorkspace;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// The real pipeline (execution context, data session, access facts SQL, policy engine) on PostgreSQL: once a
/// workspace is deleted every request on it, in its Workspace scope or on a resource inside it, answers the
/// identical NotFound to its Owner, Admin and Member alike; only its Owner can restore it, and then everything
/// answers again. Owner decision 2026-11-01.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class DeletedWorkspacePipelineIntegrationTests : IAsyncLifetime
{
    private readonly OrgTreeTestHost _host;
    private readonly PostgresTestContainer _db;

    public DeletedWorkspacePipelineIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DeleteThenRestore_EveryRequestIsTheIdenticalNotFound_UntilTheOwnerRestores()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var board = await _host.SeedBoardAsync(stack, "Roadmap");
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        using var asAdmin = _host.CreateProvider(stack.AccountId, admin);
        using var asMember = _host.CreateProvider(stack.AccountId, member);

        (await OrgTreeTestHost.QueryAsync(asMember, new GetBoardsQuery(stack.WorkspaceId))).Should().ContainSingle();
        (await OrgTreeTestHost.QueryAsync(asMember, new GetBoardQuery(board))).Id.Should().Be(board);

        await OrgTreeTestHost.SendAsync(asOwner, new DeleteWorkspaceCommand(stack.WorkspaceId, await VersionAsync(stack)));

        foreach (var (name, provider) in new[] { ("owner", asOwner), ("admin", asAdmin), ("member", asMember) })
        {
            await AssertNotFoundAsync(provider, new GetWorkspaceQuery(stack.WorkspaceId), $"{name} reads the workspace");
            await AssertNotFoundAsync(provider, new GetBoardsQuery(stack.WorkspaceId), $"{name} lists boards");
            await AssertNotFoundAsync(provider, new GetBoardQuery(board), $"{name} reads a board inside it");
            await AssertNotFoundAsync(provider, new DeleteWorkspaceCommand(stack.WorkspaceId, await VersionAsync(stack)), $"{name} deletes it again");
        }

        var refused = async () => await OrgTreeTestHost.SendAsync(asMember, new RestoreWorkspaceCommand(stack.WorkspaceId, await VersionAsync(stack)));
        await refused.Should().ThrowAsync<NotFoundException>("a member cannot even learn the workspace exists");
        var refusedAdmin = async () => await OrgTreeTestHost.SendAsync(asAdmin, new RestoreWorkspaceCommand(stack.WorkspaceId, await VersionAsync(stack)));
        await refusedAdmin.Should().ThrowAsync<NotFoundException>();

        await OrgTreeTestHost.SendAsync(asOwner, new RestoreWorkspaceCommand(stack.WorkspaceId, await VersionAsync(stack)));

        (await OrgTreeTestHost.QueryAsync(asMember, new GetWorkspaceQuery(stack.WorkspaceId))).Id.Should().Be(stack.WorkspaceId);
        (await OrgTreeTestHost.QueryAsync(asMember, new GetBoardQuery(board))).Id.Should().Be(board);
    }

    [Fact]
    public async Task ADeletedWorkspace_OfAnotherTenant_NeverChangesWhatAnotherWorkspaceAnswers()
    {
        var one = await _host.SeedWorkspaceAsync();
        var two = await _host.SeedWorkspaceAsync();
        var boardTwo = await _host.SeedBoardAsync(two, "Other board");
        using var asOne = _host.CreateProvider(one.AccountId, one.OwnerId);
        using var asTwo = _host.CreateProvider(two.AccountId, two.OwnerId);

        await OrgTreeTestHost.SendAsync(asOne, new DeleteWorkspaceCommand(one.WorkspaceId, await VersionAsync(one)));

        (await OrgTreeTestHost.QueryAsync(asTwo, new GetBoardQuery(boardTwo))).Id.Should().Be(boardTwo);
        var crossTenant = async () => await OrgTreeTestHost.SendAsync(asTwo, new RestoreWorkspaceCommand(one.WorkspaceId, await VersionAsync(one)));
        await crossTenant.Should().ThrowAsync<NotFoundException>("another tenant's Owner cannot reach it");
    }

    private static async Task AssertNotFoundAsync(Microsoft.Extensions.DependencyInjection.ServiceProvider provider, object request, string because)
    {
        var act = () => OrgTreeTestHost.SendAsync(provider, request);
        await act.Should().ThrowAsync<NotFoundException>(because);
    }

    private Task<long> VersionAsync(OrgStack stack) =>
        _host.ReadAsync(c => c.Workspaces.IgnoreQueryFilters().Where(w => w.Id == stack.WorkspaceId).Select(w => w.Version).SingleAsync());
}
