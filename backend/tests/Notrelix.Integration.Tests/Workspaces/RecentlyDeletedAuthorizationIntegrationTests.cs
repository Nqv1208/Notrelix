using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetDeletedWorkspaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// UC-WSP-I02, the "Recently deleted" list through the real request pipeline (authorization included) on
/// PostgreSQL. The handler-level proof in <see cref="DeletedWorkspaceIntegrationTests"/> skips the pipeline, so
/// it could not see a request refused before the handler ran: the Owner lists the workspaces they own that were
/// deleted inside the window; a Member, an Account member who never joined and a person outside the Account
/// do not.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class RecentlyDeletedAuthorizationIntegrationTests : IAsyncLifetime
{
    private readonly OrgTreeTestHost _host;
    private readonly PostgresTestContainer _db;

    public RecentlyDeletedAuthorizationIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task DeleteAsync(OrgStack stack) =>
        await _host.WithSystemContextAsync(async context =>
        {
            var workspace = await context.Workspaces.IgnoreQueryFilters().SingleAsync(w => w.Id == stack.WorkspaceId);
            workspace.Delete(stack.OwnerId, OrgTreeTestHost.BaseTime);
        });

    private async Task<List<DeletedWorkspaceDto>> ListAsync(OrgStack stack, Guid userId)
    {
        using var provider = _host.CreateProvider(stack.AccountId, userId);
        return await OrgTreeTestHost.QueryAsync(provider, new GetDeletedWorkspacesQuery());
    }

    [Fact]
    public async Task TheOwnerListsTheirDeletedWorkspace_ThroughTheAuthorizationPipeline()
    {
        var stack = await _host.SeedWorkspaceAsync();
        await DeleteAsync(stack);

        var rows = await ListAsync(stack, stack.OwnerId);

        var row = rows.Should().ContainSingle().Which;
        row.Id.Should().Be(stack.WorkspaceId);
        row.PurgeAfter.Should().Be(row.DeletedAt.Add(Workspace.RestoreWindow));
    }

    [Fact]
    public async Task AMemberAdminAndAnAccountMemberWhoNeverJoined_ListNothing()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var outsider = await _host.AddOutsiderAsync(stack);
        await DeleteAsync(stack);

        foreach (var reader in new[] { admin, member, outsider })
        {
            (await ListAsync(stack, reader)).Should().BeEmpty("only an Owner of the workspace sees it in Recently deleted");
        }
    }

    [Fact]
    public async Task APersonOutsideTheAccount_IsRefusedBeforeTheHandlerRuns()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var elsewhere = (await _host.SeedWorkspaceAsync()).OwnerId;
        await DeleteAsync(stack);

        using var provider = _host.CreateProvider(stack.AccountId, elsewhere);
        await FluentActions.Awaiting(() => OrgTreeTestHost.SendAsync(provider, new GetDeletedWorkspacesQuery()))
            .Should().ThrowAsync<AppForbidden>();
    }
}
