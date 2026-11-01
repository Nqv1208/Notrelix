using Notrelix.Application.Features.Documents.Blocks.Commands.DeleteBlock;
using Notrelix.Application.Features.Documents.Blocks.Queries.GetPageBlocks;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Versions.Commands.CreatePageVersion;
using Notrelix.Application.Features.Documents.Versions.Commands.RestorePageVersion;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;

namespace Notrelix.Integration.Tests.Documents;

/// <summary>
/// Restoring a page version on PostgreSQL through the real pipeline: the soft-delete filter hides deleted
/// blocks from a plain query, so a restore has to look past it to bring back a block deleted since the entry.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class PageVersionRestoreIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public PageVersionRestoreIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task RestoringAVersion_BringsBackABlockDeletedSinceThen()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var pageId = await DocumentsFlows.PageAsync(owner, stack, "Doc");
        var a = await DocumentsFlows.BlockAsync(owner, pageId);
        var b = await DocumentsFlows.BlockAsync(owner, pageId, afterBlockId: a.Id);
        var version = await OrgTreeTestHost.QueryAsync(owner, new CreatePageVersionCommand(pageId, "v1"));

        await OrgTreeTestHost.SendAsync(owner, new DeleteBlockCommand(pageId, b.Id, b.Version));
        var live = await OrgTreeTestHost.QueryAsync(owner, new GetPageBlocksQuery(pageId));
        live.Select(x => x.Id).Should().Equal(a.Id);

        var restored = await OrgTreeTestHost.QueryAsync(owner,
            new RestorePageVersionCommand(pageId, version.Id, live.Select(x => new SeenBlockVersion(x.Id, x.Version)).ToList()));

        restored.Blocks.Select(x => x.Id).Should().Equal(a.Id, b.Id);
        var after = await OrgTreeTestHost.QueryAsync(owner, new GetPageBlocksQuery(pageId));
        after.Select(x => x.Id).Should().Equal(new[] { a.Id, b.Id });
    }
}
